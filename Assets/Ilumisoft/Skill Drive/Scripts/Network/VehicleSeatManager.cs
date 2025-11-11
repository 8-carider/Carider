using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Ilumisoft.SkillDrive.Input;

namespace Ilumisoft.SkillDrive.Network
{
    // Attach to each spawned vehicle at runtime by MapVehicleSpawner
    // Receives input from players and applies combined steering/throttle
    // Uses MonoBehaviourPun to access photonView and receive RPC calls
    public class VehicleSeatManager : MonoBehaviourPun
    {
        [SerializeField] private int throttleSeatActorNumber = -1; // up/down player
        [SerializeField] private int steeringSeatActorNumber = -1; // left/right player

        private Vector2 combinedInput; // x: steering, y: throttle
        private VehiclePlayerInput vehiclePlayerInput;

        private void Awake()
        {
            vehiclePlayerInput = GetComponent<VehiclePlayerInput>();
            if (vehiclePlayerInput == null)
            {
                Debug.LogError("[VehicleSeatManager] VehiclePlayerInput not found!");
            }
            // VehiclePhotonViewID 관련 코드 제거 (MonoBehaviourPun이 자동으로 photonView 제공)
        }

        private void FixedUpdate()
        {
            // Apply combined input to vehicle
            if (vehiclePlayerInput != null)
            {
                vehiclePlayerInput.SetMovementInput(combinedInput);
            }
        }

        [PunRPC]
        public void RPC_SetSeats(int throttleActor, int steeringActor)
        {
            throttleSeatActorNumber = throttleActor;
            steeringSeatActorNumber = steeringActor;
            // Debug.Log($"[VehicleSeatManager] Seats assigned on {gameObject.name} -> Throttle:{throttleActor} Steering:{steeringActor}");

            // 로컬 카메라 활성/비활성은 좌석 배정 정보만으로 결정 (RPC 인자 불일치 문제 방지)
            var cam = GetComponent<VehicleCameraManager>();
            if (cam != null && PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null)
            {
                int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
                bool active = IsPlayerAssigned(localActor);
                
                // 로컬 플레이어의 배정 정보 로그 출력
                if (active)
                {
                    string vehicleName = gameObject.name;
                    string seatRole = "";
                    if (throttleSeatActorNumber == localActor && steeringSeatActorNumber == localActor)
                    {
                        seatRole = "조향+가속";
                    }
                    else if (throttleSeatActorNumber == localActor)
                    {
                        seatRole = "가속";
                    }
                    else if (steeringSeatActorNumber == localActor)
                    {
                        seatRole = "조향";
                    }
                    Debug.Log($"[플레이어 배정] {vehicleName} ({seatRole})");
                }
                
                // Enable or disable the virtual camera component explicitly
                cam.SetCameraActive(active, localActor);
                if (cam.transform.TryGetComponent<Unity.Cinemachine.CinemachineCamera>(out var vcam))
                {
                    vcam.enabled = active; // prevent inactive seat cameras from grabbing priority
                }
            }
        }

        [PunRPC]
        public void RPC_SetThrottle(float y, int actor)
        {
            // Accept input only from assigned throttle seat
            if (throttleSeatActorNumber == -1 || actor != throttleSeatActorNumber) return;
            combinedInput.y = Mathf.Clamp(y, -1f, 1f);
        }

        [PunRPC]
        public void RPC_SetSteering(float x, int actor)
        {
            // Accept input only from assigned steering seat
            if (steeringSeatActorNumber == -1 || actor != steeringSeatActorNumber) return;
            combinedInput.x = Mathf.Clamp(x, -1f, 1f);
        }

        /// <summary>
        /// Check if a player is assigned to this vehicle (throttle or steering)
        /// </summary>
        public bool IsPlayerAssigned(int actorNumber)
        {
            return actorNumber == throttleSeatActorNumber || actorNumber == steeringSeatActorNumber;
        }

        /// <summary>
        /// Get assigned seat actors for external systems
        /// </summary>
        public (int throttle, int steering) GetAssignedActors()
        {
            return (throttleSeatActorNumber, steeringSeatActorNumber);
        }

        // Set name consistently across clients (from spawner)
        [PunRPC]
        public void RPC_SetRuntimeName(string runtimeName)
        {
            if (!string.IsNullOrEmpty(runtimeName))
            {
                gameObject.name = runtimeName;
            }
        }

    }
}
