using Photon.Pun;
using UnityEngine;

namespace Ilumisoft.SkillDrive.Network
{
    // Put this in each GAME LEVEL scene (not in Level Selection).
    // Provide two spawn points for two shared vehicles.
    // Master client spawns both vehicles; all players receive them via Photon.
    public class MapVehicleSpawner : MonoBehaviourPun
    {
        [SerializeField] private string vehiclePrefabName = "Vehicle";
        [SerializeField] private Transform[] vehicleSpawnPoints; // Expect length >= 2
        [SerializeField] private bool autoSpawnOnStart = true;

        private GameObject[] spawnedVehicles;

        private void Awake()
        {
            // 자동으로 'Point1', 'Point2'를 찾아서 세팅 (인스펙터 미지정 시)
            if ((vehicleSpawnPoints == null || vehicleSpawnPoints.Length < 2))
            {
                var p1 = GameObject.Find("Point1");
                var p2 = GameObject.Find("Point2");
                if (p1 != null && p2 != null)
                {
                    vehicleSpawnPoints = new Transform[2] { p1.transform, p2.transform };
                    Debug.Log("[MapVehicleSpawner] Auto-assigned spawn points from Point1/Point2.");
                }
            }
        }

        private void Start()
        {
            if (!autoSpawnOnStart) return;
            TrySpawnVehicles();
        }

        public void TrySpawnVehicles()
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            {
                Debug.LogWarning("[MapVehicleSpawner] Not in room.");
                return;
            }

            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.Log("[MapVehicleSpawner] Only master spawns vehicles.");
                return;
            }

            // 이미 차량이 존재하면 중복 스폰 방지
            var existing = FindObjectsOfType<Ilumisoft.SkillDrive.Vehicle>();
            if (existing != null && existing.Length >= 2)
            {
                Debug.Log("[MapVehicleSpawner] Vehicles already present. Skipping spawn.");
                return;
            }

            if (vehicleSpawnPoints == null || vehicleSpawnPoints.Length < 2)
            {
                Debug.LogError("[MapVehicleSpawner] Need at least 2 spawn points for two vehicles.");
                return;
            }

            spawnedVehicles = new GameObject[2];
            for (int i = 0; i < 2; i++)
            {
                var sp = vehicleSpawnPoints[i];
                if (sp == null)
                {
                    Debug.LogError($"[MapVehicleSpawner] Spawn point {i} is null.");
                    continue;
                }
                spawnedVehicles[i] = PhotonNetwork.Instantiate(vehiclePrefabName, sp.position, sp.rotation);
            }

            photonView.RPC("RPC_LogSpawn", RpcTarget.AllBuffered);
        }

        [PunRPC]
        void RPC_LogSpawn()
        {
            Debug.Log("[MapVehicleSpawner] Vehicles spawned (RPC sync). Ready for seat assignment.");
        }
    }
}
