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
                    SpawnRandomItem(pos);
                }
            }
#else
            if (TryGetValidSpawnPosition(out Vector3 pos))
            {
                SpawnRandomItem(pos);
            }
#endif
        }
        [Header("레이어 설정")]
        [SerializeField, Tooltip("Ground 레이어")]
        private LayerMask groundLayer;
        [SerializeField, Tooltip("Obstacle 레이어")]
        private LayerMask obstacleLayer;

        [Header("랜덤 위치 샘플링 (CollectableManager와 동일 방식)")]
        [SerializeField, Tooltip("Collectable 겹침 체크 반경")]
        private float spawnCheckRadius = 0.6f;
        [SerializeField, Tooltip("장애물 겹침 체크 반경 (트리거 포함)")]
        private float obstacleCheckRadius = 1.2f;
        [SerializeField, Tooltip("랜덤 위치 최대 시도 횟수")]
        private int maxTries = 30;

        [Header("고급 스폰 검사")]
        [SerializeField, Tooltip("스폰 위치를 지면 위로 얼마나 올릴지 (아이템 떠있게)")] private float spawnHeightOffset = 1f;
        [SerializeField, Tooltip("위쪽 여유 공간 검사 높이")] private float verticalClearance = 1.0f;
        [SerializeField, Tooltip("유효한 위치를 찾지 못했을 때 중앙 강제 스폰 사용 여부")] private bool useCenterFallback = false;

        [Header("Photon 스폰 옵션")]
        [SerializeField, Tooltip("Start() 시 스폰 (룸 가입 이전에는 Photon 조건 미충족 가능)")] private bool spawnOnStart = true;
        [SerializeField, Tooltip("룸에 참가(OnJoinedRoom)했을 때 스폰")] private bool spawnOnJoinRoom = true;
        [SerializeField, Tooltip("마스터 클라이언트가 바뀌었을 때 부족한 아이템 보충")] private bool spawnOnMasterSwitch = true;
        [SerializeField, Tooltip("디버그: 스폰 위치 실패 사유 로깅")] private bool logSpawnAttempts = false;

        [Header("아이템 프리팹")]
        [SerializeField, Tooltip("랜덤 생성할 아이템 프리팹 배열 (하나만 넣으면 그것만 생성)")]
        private GameObject[] collectablePrefabs;

        [Header("Photon Resources 이름")]
        [SerializeField, Tooltip("Photon으로 생성할 프리팹 이름들 (프리팹 배열과 순서 일치 필요)")]
        private string[] photonPrefabNames;

        [Header("랜덤 생성 영역 (상대 좌표)")]
        [SerializeField, Tooltip("이 오브젝트 위치를 중심으로 한 스폰 영역 크기 (XZ)")]
        private Vector2 spawnAreaSize = new Vector2(60, 190);

        // [Header("하늘 높이")]
        // [SerializeField]
        // private float skyHeight = 20f;

        private void Start()
        {
            Debug.Log($"[ItemSpawner] Start 호출됨. spawnOnStart={spawnOnStart}");
            Debug.Log($"[ItemSpawner] 이 오브젝트 위치: {transform.position}");

#if PHOTON_UNITY_NETWORKING
            // CollectableManager 방식: 마스터 클라이언트면 바로 스폰
            if (Photon.Pun.PhotonNetwork.IsMasterClient)
            {
                Debug.Log("[ItemSpawner] 마스터 클라이언트 - Start에서 SpawnRandomItems 호출");
                SpawnRandomItems();
            }
            else
            {
                Debug.Log("[ItemSpawner] 마스터가 아님 - Start에서 스폰 안 함");
            }
#else
            Debug.Log("[ItemSpawner] 싱글플레이 모드 - 즉시 스폰");
            SpawnRandomItems();
#endif
        }

#if PHOTON_UNITY_NETWORKING
        public override void OnJoinedRoom()
        {
            Debug.Log($"[ItemSpawner] OnJoinedRoom 호출됨. spawnOnJoinRoom={spawnOnJoinRoom}, IsMaster={PhotonNetwork.IsMasterClient}");
            if (!spawnOnJoinRoom) return;
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("[ItemSpawner] OnJoinedRoom에서 SpawnRandomItems 호출");
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
                        SpawnRandomItem(pos);
                    }
                }
            }
        }
#endif

        // CollectableManager와 동일한 방식//
        private bool TryGetValidSpawnPosition(out Vector3 position)
        {
            for (int i = 0; i < maxTries; i++)
            {
                // 1) 랜덤 XZ 선택
                Vector3 posXZ = transform.position + new Vector3(
                    Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f),
                    50f,
                    Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f)
                );

                // 2)Ground 위인지 검사
                if (Physics.Raycast(posXZ, Vector3.down, out RaycastHit hit, 200f, groundLayer))
                {
                    Vector3 finalPos = hit.point + Vector3.up * spawnHeightOffset;

                    // 3) 장애물과 겹치는지 확인
                    bool obstacleOverlap = Physics.CheckSphere(finalPos, 0.5f, obstacleLayer);

                    if (obstacleOverlap)
                        continue;

                    position = finalPos;
                    return true;
                }
            }

            // 유효 위치 실패
            position = Vector3.zero;
            return false;
        }

        // 지정된 개수만큼 아이템을 생성
        private void SpawnRandomItems()
        {
            Debug.Log($"[ItemSpawner] SpawnRandomItems 시작. spawnCount={spawnCount}");
            int successCount = 0;
            for (int i = 0; i < spawnCount; i++)
            {
                if (TryGetValidSpawnPosition(out Vector3 spawnPos))
                {
                    Debug.Log($"[ItemSpawner] 유효한 위치 찾음: {spawnPos}");
                    // 최종적으로 지면 윗면에 스냅
                    var snappedPos = SnapToGround(spawnPos);
                    SpawnRandomItem(snappedPos);
                    successCount++;
                }
                else
                {
                    Debug.LogWarning($"[ItemSpawner] {i + 1}번째 아이템 스폰 실패 - 유효한 위치를 찾지 못함");
                }
            }
            Debug.Log($"[ItemSpawner] SpawnRandomItems 완료. 성공: {successCount}/{spawnCount}");
        }

        // 랜덤 아이템 하나를 생성
        private void SpawnRandomItem(Vector3 position)
        {
            if (collectablePrefabs == null || collectablePrefabs.Length == 0)
            {
                Debug.LogError("[ItemSpawner] collectablePrefabs 배열이 비어있습니다!");
                return;
            }

            // 배열에서 랜덤 선택
            int randomIndex = Random.Range(0, collectablePrefabs.Length);

#if PHOTON_UNITY_NETWORKING
            // Photon 이름 배열이 있고 유효한 인덱스면 사용
            if (photonPrefabNames != null && randomIndex < photonPrefabNames.Length && !string.IsNullOrEmpty(photonPrefabNames[randomIndex]))
            {
                var obj = PhotonNetwork.Instantiate(photonPrefabNames[randomIndex], position, Quaternion.identity);
                Debug.Log($"[ItemSpawner] Photon Instantiate 성공: {obj.name} (인덱스 {randomIndex})");
            }
            else
            {
                Debug.LogWarning($"[ItemSpawner] Photon 프리팹 이름이 없습니다. 인덱스: {randomIndex}");
            }
#else
            if (collectablePrefabs[randomIndex] != null)
            {
                Instantiate(collectablePrefabs[randomIndex], position, Quaternion.identity);
                Debug.Log($"[ItemSpawner] Instantiate 성공: {collectablePrefabs[randomIndex].name} (인덱스 {randomIndex})");
            }
#endif
        }

        // 주어진 위치를 기준으로 바로 위에서 아래로 레이캐스트하여 지면 윗면으로 Y를 스냅
        private Vector3 SnapToGround(Vector3 pos)
        {
            Vector3 origin = pos + Vector3.up * 5f;

            // Raycast로 Ground 찾기
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 50f, groundLayer))
            {
                pos.y = hit.point.y + spawnHeightOffset;
            }

            return pos;
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