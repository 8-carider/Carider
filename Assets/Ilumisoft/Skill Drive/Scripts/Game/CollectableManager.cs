using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

namespace Ilumisoft.SkillDrive.Game
{
    public class CollectableManager : MonoBehaviour
    {
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

        [SerializeField] private LayerMask extraGroundLayers;

        [SerializeField] private LayerMask obstacleLayer;

        [Header("Spawn Checks")]
        [SerializeField] private float spawnCheckRadius = 0.6f;
        [SerializeField] private float obstacleCheckRadius = 1.0f;
        [SerializeField] private int maxSpawnAttempts = 100;

        [Header("Count")]
        [SerializeField] private int collectableCount = 10;

        private int collected = 0;
        private int spawned = 0;

        private void Awake()
        {
            if (groundLayer == 0)
            {
                int layer = LayerMask.NameToLayer("Ground");
                if (layer >= 0)
                    groundLayer = 1 << layer;
            }
        }

        private void Start()
        {
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
        //허공 생성 불가능하게
        private bool TrySpawnOne()
        {
            int attempts = 0;

            int combinedGroundMask = groundLayer;

            while (attempts < maxSpawnAttempts)
            {
                attempts++;

                Vector3 posXZ = new Vector3(
                    Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                    50f, // 레이캐스트로 ground 찾음
                    Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2)
                );

                if (Physics.Raycast(posXZ, Vector3.down, out RaycastHit hit, 100f, combinedGroundMask))
                {
                    Vector3 finalPos = hit.point;

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
