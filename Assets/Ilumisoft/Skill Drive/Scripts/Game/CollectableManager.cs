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

        [SerializeField, Tooltip("한 번에 맵에 존재할 콜렉터블 개수")]
        private int collectableCount = 10;

        private int collected = 0;
        private int spawned = 0;

        private void Awake()
        {
            // Ground 레이어 자동 설정
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
            // 기존에 남아있는 Collectable 모두 제거
            var remainings = FindObjectsByType<Collectable>(FindObjectsSortMode.None);
            if (remainings != null)
            {
                foreach (var c in remainings)
                {
                    Destroy(c.gameObject);
                }
            }

            spawned = 0;
            int attempts = 0;
            string prefabName = collectablePrefab != null ? collectablePrefab.name : "Collectable";

            while (spawned < collectableCount && attempts < collectableCount * maxSpawnAttempts)
            {
                float randomX = Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
                float randomZ = Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
                Vector3 probePos = transform.position + new Vector3(randomX, 10f, randomZ);
                if (Physics.Raycast(probePos, Vector3.down, out RaycastHit hit, 30f, groundLayer))
                {
                    Vector3 spawnPosition = hit.point + Vector3.up * 0.5f;
                    Collider[] overlaps = Physics.OverlapSphere(spawnPosition, spawnCheckRadius);
                    bool hasCollectableNearby = false;
                    foreach (var col in overlaps)
                    {
                        if (col.GetComponent<Collectable>() != null)
                        {
                            hasCollectableNearby = true;
                            break;
                        }
                    }
                    if (hasCollectableNearby)
                    {
                        attempts++;
                        continue;
                    }
#if PHOTON_UNITY_NETWORKING
                    GameObject newCollectable = Photon.Pun.PhotonNetwork.Instantiate(prefabName, spawnPosition, Quaternion.identity);
#else
                    GameObject newCollectable = Instantiate(collectablePrefab, spawnPosition, Quaternion.identity);
#endif
                    Collectable collectableComponent = newCollectable.GetComponent<Collectable>();
                    if (collectableComponent != null)
                    {
                        collectableComponent.OnCollect += OnCollect;
                        spawned++;
                    }
                }
                attempts++;
            }

            // 부족하면 fallback으로 채우기
            while (spawned < collectableCount)
            {
                Vector3 fallbackPos = transform.position + new Vector3(Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f), 0.5f, Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f));
#if PHOTON_UNITY_NETWORKING
                GameObject fallbackCollectable = Photon.Pun.PhotonNetwork.Instantiate(prefabName, fallbackPos, Quaternion.identity);
#else
                GameObject fallbackCollectable = Instantiate(collectablePrefab, fallbackPos, Quaternion.identity);
#endif
                var fallbackComp = fallbackCollectable.GetComponent<Collectable>();
                if (fallbackComp != null)
                {
                    fallbackComp.OnCollect += OnCollect;
                    spawned++;
                }
            }

            OnCollectablesSpawned?.Invoke();
        }
    }
}