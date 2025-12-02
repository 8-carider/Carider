using UnityEngine;
using System.Collections;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using Photon.Realtime;
#endif

namespace Ilumisoft.SkillDrive.Game
{
#if PHOTON_UNITY_NETWORKING
    public class ItemSpawner : MonoBehaviourPunCallbacks
#else
    public class ItemSpawner : MonoBehaviour
#endif
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

        // 개별 아이템 대체용: 하나만 스폰 (전체 spawnCount 증가 방지)
        public void RespawnOneAfterDelay(float delay)
        {
#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(RespawnOneCoroutine(delay));
            }
#else
            StartCoroutine(RespawnOneCoroutine(delay));
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

        private IEnumerator RespawnOneCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.IsMasterClient)
            {
                if (TryGetValidSpawnPosition(out Vector3 pos))
                {
                    PhotonNetwork.Instantiate("PowerIconSpeedv3", pos, Quaternion.identity);
                }
            }
#else
            if (TryGetValidSpawnPosition(out Vector3 pos))
            {
                Instantiate(speedUpCollectablePrefab, pos, Quaternion.identity);
            }
#endif
        }
            [Header("레이어 설정")]
            [SerializeField, Tooltip("Ground 레이어")]
            private LayerMask groundLayer;
            [SerializeField, Tooltip("Obstacle 레이어")]
            private LayerMask obstacleLayer;

            [Header("랜덤 위치 샘플링")]
            [SerializeField, Tooltip("장애물 피하기 체크 반경 (겹침이 보이면 1.5~2.0 권장)")]
            private float checkRadius = 1.5f;
            [SerializeField, Tooltip("랜덤 위치 최대 시도 횟수")]
            private int maxTries = 30;

        [Header("고급 스폰 검사")]
        [SerializeField, Tooltip("스폰 위치를 지면 위로 얼마나 올릴지 (아이템 떠있게)")] private float spawnHeightOffset = 0.5f;
        [SerializeField, Tooltip("위쪽 여유 공간 검사 높이")] private float verticalClearance = 1.0f;
        [SerializeField, Tooltip("유효한 위치를 찾지 못했을 때 중앙 강제 스폰 사용 여부")] private bool useCenterFallback = false;

        [Header("Photon 스폰 옵션")]
        [SerializeField, Tooltip("Start() 시 스폰 (룸 가입 이전에는 Photon 조건 미충족 가능)")] private bool spawnOnStart = true;
        [SerializeField, Tooltip("룸에 참가(OnJoinedRoom)했을 때 스폰")] private bool spawnOnJoinRoom = true;
        [SerializeField, Tooltip("마스터 클라이언트가 바뀌었을 때 부족한 아이템 보충")] private bool spawnOnMasterSwitch = true;
        [SerializeField, Tooltip("디버그: 스폰 위치 실패 사유 로깅")] private bool logSpawnAttempts = false;

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
            if (!spawnOnStart) return;
#if PHOTON_UNITY_NETWORKING
            // 아직 룸에 없는 경우 IsMasterClient=false → 실제 스폰은 OnJoinedRoom에서 처리
            if (PhotonNetwork.InRoom)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    SpawnRandomItems();
                }
            }
#else
            SpawnRandomItems();
#endif
        }

#if PHOTON_UNITY_NETWORKING
        public override void OnJoinedRoom()
        {
            if (!spawnOnJoinRoom) return;
            if (PhotonNetwork.IsMasterClient)
            {
                SpawnRandomItems();
            }
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            if (!spawnOnMasterSwitch) return;
            if (PhotonNetwork.IsMasterClient)
            {
                // 현재 활성 SpeedUpCollectable 수가 spawnCount보다 적으면 보충
                int current = FindObjectsOfType<SpeedUpCollectable>().Length;
                int deficit = Mathf.Max(0, spawnCount - current);
                for (int i = 0; i < deficit; i++)
                {
                    if (TryGetValidSpawnPosition(out Vector3 pos))
                    {
                        PhotonNetwork.Instantiate("PowerIconSpeedv3", pos, Quaternion.identity);
                    }
                }
            }
        }
#endif

        // 맵의 랜덤 위치에 아이템을 생성 (장애물 내부 회피 강화)
        private bool TryGetValidSpawnPosition(out Vector3 position)
        {
            for (int i = 0; i < maxTries; i++)
            {
                float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
                float z = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
                Vector3 rayStart = new Vector3(x, 100f, z); // 높은 곳에서 아래로

                // Ground 탐지
                if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 200f, groundLayer, QueryTriggerInteraction.Collide))
                {
                    if (logSpawnAttempts) Debug.Log($"[ItemSpawner] 지면 없음 pass (x:{x:F1} z:{z:F1})");
                    continue; // 지면 없음
                }

                Vector3 candidate = hit.point + Vector3.up * spawnHeightOffset;

                // 장애물/트리거 겹침 검사 (트리거 포함 강제)
                var overlaps = Physics.OverlapSphere(candidate, checkRadius, obstacleLayer, QueryTriggerInteraction.Collide);
                if (overlaps != null && overlaps.Length > 0)
                {
                    if (logSpawnAttempts) Debug.Log($"[ItemSpawner] 수평 겹침 {overlaps.Length}개 pass");
                    continue; // 수평 겹침
                }

                // 위쪽 공간 확보 (천장/오버행 내부 방지)
                if (Physics.Raycast(candidate, Vector3.up, verticalClearance, obstacleLayer, QueryTriggerInteraction.Collide))
                {
                    if (logSpawnAttempts) Debug.Log("[ItemSpawner] 위쪽 막힘 pass");
                    continue; // 위로 막힘
                }

                // 아래쪽이 실제 지면인지 추가 짧은 확인 (미세한 공중 스폰 방지)
                if (!Physics.Raycast(candidate + Vector3.up * 0.1f, Vector3.down, out RaycastHit groundCheck, 0.5f, groundLayer, QueryTriggerInteraction.Collide))
                {
                    if (logSpawnAttempts) Debug.Log("[ItemSpawner] 미세 지면 재확인 실패 pass");
                    continue;
                }

                position = candidate;
                return true;
            }

            // 실패 처리
            if (useCenterFallback)
            {
                position = new Vector3(0f, spawnHeightOffset, 0f);
                // 최후 수단 위치라도 장애물 내부이면 무시
                var overlapsCenter = Physics.OverlapSphere(position, checkRadius, obstacleLayer, QueryTriggerInteraction.Collide);
                if (overlapsCenter != null && overlapsCenter.Length > 0)
                {
                    Debug.LogWarning("ItemSpawner: 중앙 fallback 위치도 장애물과 겹침. 스폰 취소.");
                    position = Vector3.zero;
                    return false;
                }
                return true;
            }

            Debug.LogWarning("ItemSpawner: 유효한 스폰 위치를 찾지 못했습니다. 이번 재생성 건너뜀.");
            position = Vector3.zero;
            return false;
        }

        // 지정된 개수만큼 아이템을 생성
        private void SpawnRandomItems()
        {
            for (int i = 0; i < spawnCount; i++)
            {
                if (TryGetValidSpawnPosition(out Vector3 spawnPos))
                {
#if PHOTON_UNITY_NETWORKING
                    PhotonNetwork.Instantiate("PowerIconSpeedv3", spawnPos, Quaternion.identity);
#else
                    Instantiate(speedUpCollectablePrefab, spawnPos, Quaternion.identity);
#endif
                }
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
