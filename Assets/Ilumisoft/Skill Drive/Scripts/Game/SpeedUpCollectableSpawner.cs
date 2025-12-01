#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif
using UnityEngine;
using System.Collections;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

namespace Ilumisoft.SkillDrive.Game
{
    public class ItemSpawner : MonoBehaviour
    {
        [Header("생성 개수")]
        [SerializeField, Tooltip("한 번에 생성할 아이템 개수")]
        private int spawnCount = 1;
        // 일정 시간 후 아이템 재생성
        public void RespawnAfterDelay(float delay)
        {
#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(RespawnCoroutine(delay));
            }
#else
            StartCoroutine(RespawnCoroutine(delay));
#endif
        }

        private IEnumerator RespawnCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            // 네트워크 환경에서는 마스터 클라이언트만 생성
#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.IsMasterClient)
            {
                SpawnRandomItems();
            }
#else
            SpawnRandomItems();
#endif
        }
            [Header("레이어 설정")]
            [SerializeField, Tooltip("Ground 레이어")]
            private LayerMask groundLayer;
            [SerializeField, Tooltip("Obstacle 레이어")]
            private LayerMask obstacleLayer;

            [Header("랜덤 위치 샘플링")]
            [SerializeField, Tooltip("장애물 피하기 체크 반경")]
            private float checkRadius = 1.0f;
            [SerializeField, Tooltip("랜덤 위치 최대 시도 횟수")]
            private int maxTries = 30;

        [Header("아이템 프리팹")]
        [SerializeField]
        private GameObject speedUpCollectablePrefab;

        [Header("랜덤 생성 영역 (XZ)")]
        [SerializeField, Tooltip("맵의 최소 XZ 좌표")]
        private Vector2 spawnAreaMin = new Vector2(-20, -20);
        [SerializeField, Tooltip("맵의 최대 XZ 좌표")]
        private Vector2 spawnAreaMax = new Vector2(20, 20);

        // [Header("하늘 높이")]
        // [SerializeField]
        // private float skyHeight = 20f;

        private void Start()
        {
            // 마스터 클라이언트만 아이템 생성
            // 네트워크 환경에서는 마스터 클라이언트만 생성
            // 네트워크 환경에서는 마스터 클라이언트만 생성
#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.IsMasterClient)
            {
                SpawnRandomItems();
            }
#else
            SpawnRandomItems();
#endif
        }

        // 맵의 랜덤 위치에 아이템을 하늘에서 떨어지게 생성
        // 장애물 피해서 Ground 위에만 랜덤 위치 찾기
        private Vector3 GetValidSpawnPosition()
        {
            for (int i = 0; i < maxTries; i++)
            {
                float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
                float z = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
                Vector3 checkPos = new Vector3(x, 100f, z); // 충분히 높은 곳에서 아래로 Raycast

                // Raycast로 아래에 Ground가 있는지 확인
                if (Physics.Raycast(checkPos, Vector3.down, out RaycastHit hit, 200f, groundLayer))
                {
                    Vector3 groundPos = hit.point + Vector3.up * 0.5f; // 살짝 띄우기

                    // 장애물과 겹치는지 확인
                    if (!Physics.CheckSphere(groundPos, checkRadius, obstacleLayer))
                    {
                        // Ground 위에 바로 생성
                        return groundPos;
                    }
                }
            }
            // 실패 시 기본값 반환 (맵 중앙)
            return new Vector3(0, 1f, 0);
        }

        // 지정된 개수만큼 아이템을 생성
        private void SpawnRandomItems()
        {
            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 spawnPos = GetValidSpawnPosition();
#if PHOTON_UNITY_NETWORKING
                PhotonNetwork.Instantiate("PowerIconSpeedv3", spawnPos, Quaternion.identity);
#else
                Instantiate(speedUpCollectablePrefab, spawnPos, Quaternion.identity);
#endif
            }
        }
#if PHOTON_UNITY_NETWORKING
[PunRPC]
private void RPC_RequestRespawn(float delay)
{
    FindObjectOfType<ItemSpawner>()?.RespawnAfterDelay(delay);
}
#endif
    }
}
