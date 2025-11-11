using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Ilumisoft.SkillDrive.Input;

namespace Ilumisoft.SkillDrive.Network
{
    // Attach to each spawned vehicle at runtime by MapVehicleSpawner
    // Receives input from players and applies combined steering/throttle
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
        }

        private void FixedUpdate()
        {
            // Apply combined input to vehicle
            if (vehiclePlayerInput != null)
            {
                vehiclePlayerInput.SetMovementInput(combinedInput);
                
                // Debug logging (remove after testing)
                if (combinedInput.magnitude > 0.01f)
                {
                    Debug.Log($"[VehicleSeatManager] Applying input: steering={combinedInput.x:F2}, throttle={combinedInput.y:F2}");
                }
            }
        }

        [PunRPC]
        public void RPC_SetSeats(int throttleActor, int steeringActor)
        {
            throttleSeatActorNumber = throttleActor;
            steeringSeatActorNumber = steeringActor;
            Debug.Log($"[VehicleSeatManager] Seats assigned on {gameObject.name} -> Throttle:{throttleActor} Steering:{steeringActor}");
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
    }
}
