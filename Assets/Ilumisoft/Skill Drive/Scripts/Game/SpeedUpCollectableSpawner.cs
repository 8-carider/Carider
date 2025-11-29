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
        // 일정 시간 후 아이템 재생성
        public void RespawnAfterDelay(float delay)
        {
            StartCoroutine(RespawnCoroutine(delay));
        }

        private IEnumerator RespawnCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            SpawnRandomItem();
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

        [Header("하늘 높이")]
        [SerializeField]
        private float skyHeight = 20f;

        private void Start()
        {
            // 마스터 클라이언트만 아이템 생성
#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.IsMasterClient)
            {
                SpawnRandomItem();
            }
#else
            SpawnRandomItem();
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
                Vector3 skyPos = new Vector3(x, skyHeight, z);

                // Raycast로 아래에 Ground가 있는지 확인
                if (Physics.Raycast(skyPos, Vector3.down, out RaycastHit hit, skyHeight * 2, groundLayer))
                {
                    Vector3 groundPos = hit.point + Vector3.up * 0.5f; // 살짝 띄우기

                    // 장애물과 겹치는지 확인
                    if (!Physics.CheckSphere(groundPos, checkRadius, obstacleLayer))
                    {
                        // 하늘에서 떨어지게 Y값만 skyHeight로 변경
                        return new Vector3(groundPos.x, skyHeight, groundPos.z);
                    }
                }
            }
            // 실패 시 기본값 반환
            return new Vector3(0, skyHeight, 0);
        }

        private void SpawnRandomItem()
        {
            Vector3 spawnPos = GetValidSpawnPosition();
#if PHOTON_UNITY_NETWORKING
            PhotonNetwork.Instantiate("PowerIconSpeedv3", spawnPos, Quaternion.identity);
#else
            Instantiate(speedUpCollectablePrefab, spawnPos, Quaternion.identity);
#endif
        }
    }
}
