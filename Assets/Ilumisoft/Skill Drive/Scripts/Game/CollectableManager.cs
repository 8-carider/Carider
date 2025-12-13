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
                Debug.Log($"[CollectableManager] Collectable2 등록 성공: ID={key}, 이름={collectable.gameObject.name}, 위치={collectable.transform.position}");
            }
            else
            {
                Debug.LogWarning($"[CollectableManager] SpawnPointID 중복! ID={key}, 기존 등록된 위치={collectable2SpawnPoints[key].position}, 새 오브젝트={collectable.gameObject.name} (무시됨)");
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

            Debug.Log($"[CollectableManager] OnCollectable2PickedUp 호출: ID={key}, 마스터={PhotonNetwork.IsMasterClient}");
            respawningKeys.Add(key);
            StartCoroutine(RespawnCollectable2Coroutine(key));
        }

        private IEnumerator RespawnCollectable2Coroutine(string key)
        {
            try
            {
                if (!collectable2SpawnPoints.ContainsKey(key))
                {
                    Debug.LogWarning($"[CollectableManager] Spawn point not found for: {key}");
                    yield break;
                }

                var spawnPoint = collectable2SpawnPoints[key];
                Debug.Log($"[CollectableManager] {respawnDelay}초 후 재생성 예정: ID={key}, 위치={spawnPoint.position}");
                yield return new WaitForSeconds(respawnDelay);

                Debug.Log($"[CollectableManager] Collectable2 재생성 시작: ID={key}, 마스터={PhotonNetwork.IsMasterClient}");

#if PHOTON_UNITY_NETWORKING
                if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
                {
                    try
                    {
                        GameObject obj = PhotonNetwork.Instantiate("Collectable2", spawnPoint.position, spawnPoint.rotation);
                        if (obj != null)
                        {
                            // 생성된 오브젝트에 SpawnPointID 설정
                            var collectable = obj.GetComponent<Collectable2>();
                            if (collectable != null)
                            {
                                var pv = obj.GetPhotonView();
                                if (pv != null)
                                {
                                    photonView.RPC("RPC_SetSpawnPointID", RpcTarget.AllBuffered, pv.ViewID, int.Parse(key));
                                    Debug.Log($"[CollectableManager] Collectable2 재생성 완료: ID={key}, ViewID={pv.ViewID}");
                                }
                            }
                        }
                        else
                        {
                            Debug.LogError($"[CollectableManager] PhotonNetwork.Instantiate returned null for ID={key}");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[CollectableManager] PhotonNetwork.Instantiate failed for ID={key}: {ex.Message}");
                        // Fallback to local instantiate
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
                else
                {
                    Debug.LogWarning($"[CollectableManager] 마스터가 아니거나 방에 없어서 리스폰 중단: ID={key}");
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
            }
            finally
            {
                // 리스폰 완료 또는 실패 시 항상 플래그 제거
                respawningKeys.Remove(key);
                Debug.Log($"[CollectableManager] 리스폰 작업 종료: ID={key}");
            }
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
            int totalAttempts = 0;
            int maxTotalAttempts = maxSpawnAttempts * collectableCount; // 전체 시도 횟수 제한

            while (spawned < collectableCount && totalAttempts < maxTotalAttempts)
            {
                totalAttempts++;
                if (TrySpawnOne())
                {
                    Debug.Log($"[CollectableManager] Collectable 생성 성공: {spawned}/{collectableCount}");
                }
                else if (totalAttempts % 100 == 0)
                {
                    Debug.LogWarning($"[CollectableManager] Collectable 생성 시도 중... {spawned}/{collectableCount} (시도: {totalAttempts})");
                }
            }

            if (spawned < collectableCount)
            {
                Debug.LogError($"[CollectableManager] Collectable 생성 실패! 생성된 수: {spawned}/{collectableCount}");
            }
            else
            {
                Debug.Log($"[CollectableManager] Collectable 전체 생성 완료: {spawned}/{collectableCount}");
            }

            AttachCollectEventsToAll();
            OnCollectablesSpawned?.Invoke();
        }

        private bool TrySpawnOne()
        {
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
                bool obstacleOverlap = Physics.CheckSphere(finalPos, obstacleCheckRadius, obstacleLayer);
                if (obstacleOverlap)
                    return false;

                // 기존 Collectable과 너무 가까운지 체크
                var existingCollectables = FindObjectsByType<Collectable>(FindObjectsSortMode.None);
                foreach (var existing in existingCollectables)
                {
                    if (Vector3.Distance(existing.transform.position, finalPos) < spawnCheckRadius * 2)
                    {
                        return false; // 너무 가까움
                    }
                }

                try
                {
#if PHOTON_UNITY_NETWORKING
                    var obj = PhotonNetwork.Instantiate("Collectable", finalPos + Vector3.up * 0.1f, Quaternion.identity);
                    if (obj != null)
                    {
                        spawned++;
                        return true;
                    }
                    else
                    {
                        Debug.LogError("[CollectableManager] PhotonNetwork.Instantiate returned null");
                        return false;
                    }
#else
                    if (collectablePrefab != null)
                    {
                        Instantiate(collectablePrefab, finalPos + Vector3.up * 0.1f, Quaternion.identity);
                        spawned++;
                        return true;
                    }
                    else
                    {
                        Debug.LogError("[CollectableManager] collectablePrefab is null!");
                        return false;
                    }
#endif
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[CollectableManager] Collectable 생성 실패: {ex.Message}");
                    return false;
                }
            }

            return false; // Raycast 실패
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
