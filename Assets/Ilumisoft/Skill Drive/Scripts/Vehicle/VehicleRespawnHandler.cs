using Photon.Pun;
using UnityEngine;

namespace Ilumisoft.SkillDrive
{
    /// <summary>
    /// Handles vehicle respawn/reset on F key press
    /// Teleports vehicle back to spawn point instead of creating new instance
    /// </summary>
    public class VehicleRespawnHandler : MonoBehaviourPun
    {
        [SerializeField] private KeyCode respawnKey = KeyCode.F;
        [SerializeField] private Transform spawnPoint; // Set by MapVehicleSpawner
        
        private Rigidbody rb;
        private Vehicle vehicle;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            vehicle = GetComponent<Vehicle>();
        }

        private void Update()
        {
            // Only process input from players assigned to this vehicle
            if (!CanRespawn()) return;

            if (UnityEngine.Input.GetKeyDown(respawnKey))
            {
                RequestRespawn();
            }
        }

        private bool CanRespawn()
        {
            // In multiplayer, only assigned players can respawn their vehicle
            if (PhotonNetwork.IsConnected)
            {
                var seatManager = GetComponent<Ilumisoft.SkillDrive.Network.VehicleSeatManager>();
                if (seatManager != null)
                {
                    // Check if local player is assigned to this vehicle (throttle or steering)
                    int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
                    // We'll add a public method to check assignment
                    return seatManager.IsPlayerAssigned(localActor);
                }
                return false;
            }
            
            // In single player, always allow
            return true;
        }

        private void RequestRespawn()
        {
            if (PhotonNetwork.IsConnected && photonView != null)
            {
                // Send RPC to all clients to sync respawn
                photonView.RPC("RPC_Respawn", RpcTarget.All);
            }
            else
            {
                // Single player
                PerformRespawn();
            }
        }

        [PunRPC]
        private void RPC_Respawn()
        {
            PerformRespawn();
        }

        private void PerformRespawn()
        {
            if (spawnPoint == null)
            {
                Debug.LogWarning("[VehicleRespawnHandler] No spawn point assigned!");
                return;
            }

            // Reset physics
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Teleport to spawn point
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;

            // 리스폰 후 카메라가 다시 따라오도록 재활성화
            if (PhotonNetwork.IsConnected)
            {
                var seatManager = GetComponent<Ilumisoft.SkillDrive.Network.VehicleSeatManager>();
                var cameraManager = GetComponent<Ilumisoft.SkillDrive.Network.VehicleCameraManager>();
                
                if (seatManager != null && cameraManager != null)
                {
                    int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
                    // 로컬 플레이어가 이 차량에 배정되어 있으면 카메라 재활성화
                    if (seatManager.IsPlayerAssigned(localActor))
                    {
                        cameraManager.SetCameraActive(true, localActor);
                        Debug.Log($"[VehicleRespawnHandler] Camera reactivated after respawn for vehicle {gameObject.name}");
                    }
                }
            }

            Debug.Log($"[VehicleRespawnHandler] Vehicle respawned at {spawnPoint.position}");
        }

        /// <summary>
        /// Called by MapVehicleSpawner to set the spawn point
        /// </summary>
        public void SetSpawnPoint(Transform point)
        {
            spawnPoint = point;
        }
    }
}
