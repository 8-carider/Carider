using UnityEngine;
using UnityEngine.Events;

namespace Ilumisoft.SkillDrive.Game
{
    public class CollectableManager : MonoBehaviour
    {
        public UnityAction OnAllCollected;
        public UnityAction OnCollectableCollected;
        public UnityAction OnCollectableSpawned;

        public int CollectedCount => collected;
        public int TotalCount => amount;

        [SerializeField]
        private GameObject collectablePrefab; // 수집품 프리팹 참조

    [SerializeField]
    private Vector2 spawnAreaSize = new Vector2(10f, 10f); // 생성 영역 크기

    [SerializeField, Tooltip("Layer mask used to detect ground when spawning collectables")]
    private LayerMask groundLayer = ~0;

    [SerializeField, Tooltip("Maximum number of attempts to find a non-overlapping spawn position")]
    private int maxSpawnAttempts = 10;

    [SerializeField, Tooltip("Radius used to check for overlapping collectables when spawning")]
    private float spawnCheckRadius = 0.6f;

        Collectable[] collectables;

        int amount = 0;
        int collected = 0;

        private void Awake()
        {
            // 자동으로 Ground 레이어가 지정되어 있지 않다면 프로젝트에서 "Ground"라는 이름의 레이어를 찾아 설정합니다.
            // Inspector에서 명시적으로 설정한 경우에는 덮어쓰지 않습니다.
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

            collectables = FindObjectsByType<Collectable>(FindObjectsSortMode.None);

            if (collectables != null)
            {
                amount = collectables.Length;

                foreach (var collectable in collectables)
                {
                    collectable.OnCollect += OnCollect;
                }
            }
        }

        private void OnCollect()
        {
            collected++;

            // 수집 이벤트 발생
            OnCollectableCollected?.Invoke();

            // 새로운 수집품 생성
            SpawnNewCollectable();

            if (collected == amount)
            {
                OnAllCollected?.Invoke();
            }
        }

        private void SpawnNewCollectable()
        {
            // Photon 네트워크 동기화로 Collectable 생성 (마스터 클라이언트만 생성)
            #if PHOTON_UNITY_NETWORKING
            if (!Photon.Pun.PhotonNetwork.IsMasterClient) return;
            #endif

            string prefabName = collectablePrefab != null ? collectablePrefab.name : "Collectable";

            for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
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
                        continue;
#if PHOTON_UNITY_NETWORKING
                    GameObject newCollectable = Photon.Pun.PhotonNetwork.Instantiate(prefabName, spawnPosition, Quaternion.identity);
#else
                    GameObject newCollectable = Instantiate(collectablePrefab, spawnPosition, Quaternion.identity);
#endif
                    Collectable collectableComponent = newCollectable.GetComponent<Collectable>();
                    if (collectableComponent != null)
                    {
                        collectableComponent.OnCollect += OnCollect;
                        amount++;
                        OnCollectableSpawned?.Invoke();
                    }
                    return;
                }
            }
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
                amount++;
            }
        }
    }
}