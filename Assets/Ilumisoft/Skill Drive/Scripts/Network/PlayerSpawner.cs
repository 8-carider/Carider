using Photon.Pun;
using UnityEngine;

namespace Ilumisoft.SkillDrive.Network
{
    // Place this on a GameObject in the Level Selection (or Game) scene.
    // Provide spawn points by:
    // 1) Assigning 'spawnPoints' in the inspector, or
    // 2) Creating a child GameObject named 'SpawnPoints' with children as points
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] private string vehiclePrefabName = "Vehicle"; // Prefab name in Resources/
        [SerializeField] private Transform[] spawnPoints;               // Optional explicit points
        [SerializeField] private bool findSpawnPointsByName = true;     // Search for child 'SpawnPoints'

        private void Awake()
        {
            // Try to find child container named 'SpawnPoints'
            if ((spawnPoints == null || spawnPoints.Length == 0) && findSpawnPointsByName)
            {
                var container = transform.Find("SpawnPoints");
                if (container != null)
                {
                    int count = container.childCount;
                    spawnPoints = new Transform[count];
                    for (int i = 0; i < count; i++)
                    {
                        spawnPoints[i] = container.GetChild(i);
                    }
                }
            }
        }

        private void Start()
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            {
                Debug.LogWarning("[PlayerSpawner] Not in Photon room; skipping spawn.");
                return;
            }

            // 맵 전용 2대 스폰 시스템이 있으면, 이 스포너는 비활성화 동작
            if (FindObjectOfType<MapVehicleSpawner>() != null)
            {
                Debug.Log("[PlayerSpawner] MapVehicleSpawner detected. Skipping per-player spawn.");
                // 완전히 비활성화 (씬에서 제거)
                Destroy(this);
                return;
            }

            // 이미 차량이 있으면 중복 스폰 방지

            var existing = FindObjectsOfType<Ilumisoft.SkillDrive.Vehicle>();
            if (existing != null && existing.Length > 0)
            {
                Debug.Log("[PlayerSpawner] Vehicles already present. Skipping per-player spawn.");
                return;
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("[PlayerSpawner] No spawn points assigned/found. Spawning at origin.");
                PhotonNetwork.Instantiate(vehiclePrefabName, Vector3.zero, Quaternion.identity);
                return;
            }

            // Use ActorNumber to get a stable index per player
            int index = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
            Transform sp = spawnPoints[index];
            PhotonNetwork.Instantiate(vehiclePrefabName, sp.position, sp.rotation);
        }
    }
}
