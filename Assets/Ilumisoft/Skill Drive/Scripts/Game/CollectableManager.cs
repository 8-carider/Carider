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
        [SerializeField] private int maxSpawnAttempts = 100;

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
            Debug.Log($"[CollectableManager] Registered {collectable2SpawnPoints.Count} Collectable2 items");

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
            string key = collectable.gameObject.GetInstanceID().ToString();
            collectable2SpawnPoints[key] = new CollectableSpawnPoint
            {
                position = collectable.transform.position,
                rotation = collectable.transform.rotation,
                prefab = Resources.Load<GameObject>("Collectable2")
            };
            Debug.Log($"[CollectableManager] Registered Collectable2: {key} at {collectable.transform.position}");
        }

        /// <summary>
        /// Called when a Collectable2 is picked up - triggers respawn
        /// </summary>
        public void OnCollectable2PickedUp(Collectable2 collectable)
        {
            string key = collectable.gameObject.GetInstanceID().ToString();
            Debug.Log($"[CollectableManager] Collectable2 picked up: {key}");

            if (PhotonNetwork.IsMasterClient)
            {
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
            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(RespawnCollectable2Coroutine(key));
            }
        }

        private IEnumerator RespawnCollectable2Coroutine(string key)
        {
            if (!collectable2SpawnPoints.ContainsKey(key))
            {
                Debug.LogWarning($"[CollectableManager] Spawn point not found for: {key}");
                yield break;
            }

            var spawnPoint = collectable2SpawnPoints[key];
            Debug.Log($"[CollectableManager] Waiting {respawnDelay}s before respawning Collectable2 at {spawnPoint.position}");
            
            yield return new WaitForSeconds(respawnDelay);

            Debug.Log($"[CollectableManager] Respawning Collectable2 at {spawnPoint.position}");

#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
            {
                try
                {
                    PhotonNetwork.Instantiate("Collectable2", spawnPoint.position, spawnPoint.rotation);
                    Debug.Log($"[CollectableManager] Successfully instantiated Collectable2");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[CollectableManager] PhotonNetwork.Instantiate failed: {ex.Message}. Falling back to local instantiate");
                    if (spawnPoint.prefab != null)
                    {
                        Instantiate(spawnPoint.prefab, spawnPoint.position, spawnPoint.rotation);
                    }
                }
            }
#else
            if (spawnPoint.prefab != null)
            {
                Instantiate(spawnPoint.prefab, spawnPoint.position, spawnPoint.rotation);
            }
#endif
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
    }
}
