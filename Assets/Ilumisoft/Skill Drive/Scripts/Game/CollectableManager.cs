using UnityEngine;
using UnityEngine.Events;

namespace Ilumisoft.SkillDrive.Game
{
    public class CollectableManager : MonoBehaviour
    {
        public UnityAction OnAllCollected;
        public UnityAction OnCollectableCollected;
        public UnityAction OnCollectablesSpawned;

        public int CollectedCount => collected;
        public int TotalCount => collectableCount;

        [SerializeField]
        private GameObject collectablePrefab;

        [SerializeField]
        private Vector2 spawnAreaSize = new Vector2(10f, 10f);

        [SerializeField, Tooltip("Layer mask used to detect ground when spawning collectables")]
        private LayerMask groundLayer = ~0;

        [SerializeField, Tooltip("Maximum number of attempts to find a non-overlapping spawn position")]
        private int maxSpawnAttempts = 10;

        [SerializeField, Tooltip("Radius used to check for overlapping collectables when spawning")]
        private float spawnCheckRadius = 0.6f;

        [SerializeField, Tooltip("장애물 겹침 체크 반경 (트리거 포함)")]
        private float obstacleCheckRadius = 1.2f;

        [SerializeField, Tooltip("한 번에 맵에 존재할 콜렉터블 개수")]
        private int collectableCount = 10;

        [SerializeField, Tooltip("피할 레이어(예: 장애물)")]
        private LayerMask obstacleLayer = 0;

        private int collected = 0;
        private int spawned = 0;

        private void Awake()
        {
            // Ground 레이어 자동 설정만 남김
            if (groundLayer.value == -1)
            {
                int layer = LayerMask.NameToLayer("Ground");
                if (layer != -1)
                {
                    groundLayer = 1 << layer;
                    Debug.Log($"[CollectableManager] groundLayer auto-set to 'Ground' (layer {layer})");
                }
                else
                {
                    Debug.LogWarning("[CollectableManager] Layer named 'Ground' not found. Please set Ground LayerMask in the inspector.");
                }
            }
        }

        private void Start()
        {
#if PHOTON_UNITY_NETWORKING
            if (Photon.Pun.PhotonNetwork.IsMasterClient)
            {
                // 기존 맵에 존재하는 Collectable 제거 (초기화)
                var preExistings = FindObjectsByType<Collectable>(FindObjectsSortMode.None);
                if (preExistings != null)
                {
                    foreach (var c in preExistings)
                    {
                        Destroy(c.gameObject);
                    }
                }
                SpawnCollectables();
            }
#endif
            AttachCollectEventsToAll();

            // 콜렉터블 개수 및 상태 로그
            var allCollects = FindObjectsByType<Collectable>(FindObjectsSortMode.None);
            Debug.Log($"[CollectableManager] 현재 씬에 존재하는 Collectable 개수: {allCollects.Length}");
            foreach (var c in allCollects)
            {
                Debug.Log($"[CollectableManager] 오브젝트: {c.gameObject.name}, 활성화: {c.gameObject.activeInHierarchy}");
            }
        }

        private void AttachCollectEventsToAll()
        {
            var allCollects = FindObjectsByType<Collectable>(FindObjectsSortMode.None);
            if (allCollects != null)
            {
                foreach (var c in allCollects)
                {
                    c.OnCollect -= OnCollect; // 중복 방지
                    c.OnCollect += OnCollect;
                }
            }
        }

        private void OnCollect()
        {
            collected++;
            OnCollectableCollected?.Invoke();

            if (collected >= collectableCount)
            {
                OnAllCollected?.Invoke();
                // 모두 먹었으면 다시 랜덤 위치에 일괄 생성
                collected = 0;
                SpawnCollectables();
            }
        }

        private void SpawnCollectables()
        {
            spawned = 0;
            int attempts = 0;
            string prefabName = "Collectable"; // Resources 폴더 내 프리팹 이름

            while (spawned < collectableCount && attempts < maxSpawnAttempts * collectableCount)
            {
                Vector3 spawnPosition = transform.position + new Vector3(
                    Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f),
                    0.5f,
                    Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f)
                );

                // 겹침 체크: groundLayer와 obstacleLayer 모두 피함
                bool overlapGround = Physics.CheckSphere(spawnPosition, spawnCheckRadius, groundLayer);
                bool overlapObstacle = obstacleLayer.value != 0 && Physics.OverlapSphere(spawnPosition, obstacleCheckRadius, obstacleLayer, QueryTriggerInteraction.Collide).Length > 0;
                if (!overlapGround && !overlapObstacle)
                {
#if PHOTON_UNITY_NETWORKING
                    Photon.Pun.PhotonNetwork.Instantiate(prefabName, spawnPosition, Quaternion.identity);
#else
                    Instantiate(collectablePrefab, spawnPosition, Quaternion.identity);
#endif
                    spawned++;
                }
                attempts++;
            }

            // 부족하면 fallback으로 채우기 (장애물 피하기 적용)
            while (spawned < collectableCount)
            {
                Vector3 fallbackPos = transform.position + new Vector3(
                    Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f),
                    0.5f,
                    Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f)
                );
                bool overlapGround = Physics.CheckSphere(fallbackPos, spawnCheckRadius, groundLayer);
                bool overlapObstacle = obstacleLayer.value != 0 && Physics.OverlapSphere(fallbackPos, obstacleCheckRadius, obstacleLayer, QueryTriggerInteraction.Collide).Length > 0;
                if (!overlapGround && !overlapObstacle)
                {
#if PHOTON_UNITY_NETWORKING
                    Photon.Pun.PhotonNetwork.Instantiate(prefabName, fallbackPos, Quaternion.identity);
#else
                    Instantiate(collectablePrefab, fallbackPos, Quaternion.identity);
#endif
                    spawned++;
                }
            }

            AttachCollectEventsToAll();
            OnCollectablesSpawned?.Invoke();
        }
    }
}