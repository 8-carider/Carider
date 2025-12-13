using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

namespace Ilumisoft.SkillDrive.Game
{
    /// <summary>
    /// Central manager for all Collectable2 items on the map.
    /// Handles respawning logic for all collectables.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class CollectableManager : MonoBehaviourPunCallbacks
    {
        public static CollectableManager Instance { get; private set; }

        public UnityAction OnAllCollected;
        public UnityAction OnCollectableCollected;
        public UnityAction OnCollectablesSpawned;

        public int CollectedCount => collected;
        public int TotalCount => collectableCount;

        [SerializeField] private GameObject collectablePrefab;

        [Header("Spawn Range (X,Z)")]
        [SerializeField] private Vector2 spawnAreaSize = new Vector2(10f, 10f);

        [Header("Layers")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask obstacleLayer;

        [Header("Spawn Checks")]
        [SerializeField] private float spawnCheckRadius = 0.6f;
        [SerializeField] private float obstacleCheckRadius = 1.0f;
        [SerializeField] private int maxSpawnAttempts = 1000;

        [Header("Count")]
        [SerializeField] private int collectableCount = 10;
        
        [Header("Respawn Settings")]
        [SerializeField] private float respawnDelay = 2f;

        private int collected = 0;
        private int spawned = 0;
        
        // Collectable2 tracking
        private struct CollectableSpawnPoint
        {
            public Vector3 position;
            public Quaternion rotation;
            public GameObject prefab;
        }
        private Dictionary<string, CollectableSpawnPoint> collectable2SpawnPoints = new Dictionary<string, CollectableSpawnPoint>();
        private HashSet<string> respawningKeys = new HashSet<string>(); // 현재 리스폰 중인 키들
        private PhotonView photonView;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            photonView = GetComponent<PhotonView>();

            if (groundLayer == 0)
            {
                int layer = LayerMask.NameToLayer("Ground");
                if (layer >= 0)
                    groundLayer = 1 << layer;
            }
        }

        private void Start()
        {
            // Find and register all Collectable2 items
            var collectables = FindObjectsOfType<Collectable2>();
            foreach (var collectable in collectables)
            {
                RegisterCollectable2(collectable);
            }

#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.IsMasterClient)
            {
                ClearExistingCollectables();
                SpawnCollectables();
            }
#else
            ClearExistingCollectables();
            SpawnCollectables();
#endif
            AttachCollectEventsToAll();
        }

        /// <summary>
        /// Register a Collectable2's spawn point for respawning
        /// </summary>
        public void RegisterCollectable2(Collectable2 collectable)
        {
            // SpawnPointID를 키로 사용 (InstanceID 대신)
            string key = collectable.SpawnPointID.ToString();
            
            // 이미 등록되어 있으면 업데이트하지 않음 (처음 등록된 위치 유지)
            if (!collectable2SpawnPoints.ContainsKey(key))
            {
                collectable2SpawnPoints[key] = new CollectableSpawnPoint
                {
                    position = collectable.transform.position,
                    rotation = collectable.transform.rotation,
                    prefab = Resources.Load<GameObject>("Collectable2")
                };
                Debug.Log($"[CollectableManager] Collectable2 등록: ID={key}, 위치={collectable.transform.position}");
            }
        }

        /// <summary>
        /// Called when a Collectable2 is picked up - triggers respawn
        /// </summary>
        public void OnCollectable2PickedUp(Collectable2 collectable)
        {
            string key = collectable.SpawnPointID.ToString();

            // 이미 리스폰 중이면 무시
            if (respawningKeys.Contains(key))
            {
                Debug.Log($"[CollectableManager] 이미 리스폰 중: ID={key}");
                return;
            }

            if (PhotonNetwork.IsMasterClient)
            {
                respawningKeys.Add(key);
                StartCoroutine(RespawnCollectable2Coroutine(key));
            }
            else
            {
                photonView.RPC("RPC_OnCollectable2PickedUp", RpcTarget.MasterClient, key);
            }
        }

        [PunRPC]
        private void RPC_OnCollectable2PickedUp(string key)
        {
            // 이미 리스폰 중이면 무시
            if (respawningKeys.Contains(key))
            {
                Debug.Log($"[CollectableManager] RPC: 이미 리스폰 중: ID={key}");
                return;
            }

            if (PhotonNetwork.IsMasterClient)
            {
                respawningKeys.Add(key);
                StartCoroutine(RespawnCollectable2Coroutine(key));
            }
        }

        private IEnumerator RespawnCollectable2Coroutine(string key)
        {
            if (!collectable2SpawnPoints.ContainsKey(key))
            {
                Debug.LogWarning($"[CollectableManager] Spawn point not found for: {key}");
                respawningKeys.Remove(key); // 실패 시 플래그 제거
                yield break;
            }

            var spawnPoint = collectable2SpawnPoints[key];
            yield return new WaitForSeconds(respawnDelay);

            Debug.Log($"[CollectableManager] Collectable2 재생성: ID={key}, 위치={spawnPoint.position}");

#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
            {
                try
                {
                    GameObject obj = PhotonNetwork.Instantiate("Collectable2", spawnPoint.position, spawnPoint.rotation);
                    // 생성된 오브젝트에 SpawnPointID 설정
                    var collectable = obj.GetComponent<Collectable2>();
                    if (collectable != null)
                    {
                        photonView.RPC("RPC_SetSpawnPointID", RpcTarget.AllBuffered, obj.GetPhotonView().ViewID, int.Parse(key));
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[CollectableManager] PhotonNetwork.Instantiate failed: {ex.Message}. Falling back to local instantiate");
                    if (spawnPoint.prefab != null)
                    {
                        GameObject obj = Instantiate(spawnPoint.prefab, spawnPoint.position, spawnPoint.rotation);
                        var collectable = obj.GetComponent<Collectable2>();
                        if (collectable != null)
                        {
                            collectable.SetSpawnPointID(int.Parse(key));
                        }
                    }
                }
            }
#else
            if (spawnPoint.prefab != null)
            {
                GameObject obj = Instantiate(spawnPoint.prefab, spawnPoint.position, spawnPoint.rotation);
                var collectable = obj.GetComponent<Collectable2>();
                if (collectable != null)
                {
                    collectable.SetSpawnPointID(int.Parse(key));
                }
            }
#endif
            
            // 리스폰 완료 후 플래그 제거
            respawningKeys.Remove(key);
        }

        private void ClearExistingCollectables()
        {
            var exists = FindObjectsByType<Collectable>(FindObjectsSortMode.None);
            foreach (var c in exists)
                Destroy(c.gameObject);
        }

        private void AttachCollectEventsToAll()
        {
            var all = FindObjectsByType<Collectable>(FindObjectsSortMode.None);
            foreach (var c in all)
            {
                c.OnCollect -= HandleCollectableCollected;
                c.OnCollect += HandleCollectableCollected;
            }
        }

        private void HandleCollectableCollected()
        {
            collected++;
            OnCollectableCollected?.Invoke();

            if (collected >= collectableCount)
            {
                OnAllCollected?.Invoke();
                collected = 0;

                SpawnCollectables();
            }
        }

        private void SpawnCollectables()
        {
            spawned = 0;

            while (spawned < collectableCount)
            {
                if (!TrySpawnOne())
                    break;
            }

            AttachCollectEventsToAll();
            OnCollectablesSpawned?.Invoke();
        }

        private bool TrySpawnOne()
        {
            int attempts = 0;

            while (attempts < maxSpawnAttempts)
            {
                attempts++;

                Vector3 posXZ = new Vector3(
                    Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                    50f,
                    Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2)
                );

                // Raycast로 땅 찾기(허공 생성 방지)
                if (Physics.Raycast(posXZ, Vector3.down, out RaycastHit hit, 100f, groundLayer))
                {
                    Vector3 finalPos = hit.point;

                    // 장애물 여부
                    bool obstacleOverlap =
                        Physics.CheckSphere(finalPos, obstacleCheckRadius, obstacleLayer);

                    if (obstacleOverlap)
                        continue;

#if PHOTON_UNITY_NETWORKING
                    PhotonNetwork.Instantiate("Collectable", finalPos + Vector3.up * 0.1f, Quaternion.identity);
#else
                    Instantiate(collectablePrefab, finalPos + Vector3.up * 0.1f, Quaternion.identity);
#endif
                    spawned++;
                    return true;
                }
            }

            return false; // spawn 실패
        }

#if PHOTON_UNITY_NETWORKING
        [PunRPC]
        private void RPC_SetSpawnPointID(int viewID, int spawnPointID)
        {
            PhotonView pv = PhotonView.Find(viewID);
            if (pv != null)
            {
                var collectable = pv.GetComponent<Collectable2>();
                if (collectable != null)
                {
                    collectable.SetSpawnPointID(spawnPointID);
                }
            }
        }
#endif
    }
}
