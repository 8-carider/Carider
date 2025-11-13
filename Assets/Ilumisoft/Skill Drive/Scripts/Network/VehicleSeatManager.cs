using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Ilumisoft.SkillDrive.Network
{
    /// <summary>
    /// 단순화된 좌석 관리자
    /// - 각 플레이어의 입력을 actor number로 구분하여 조합
    /// - Vehicle은 GetCombinedInput()으로 최종 입력 사용
    /// </summary>
    public class VehicleSeatManager : MonoBehaviourPun
    {
        [SerializeField] private int throttleSeatActorNumber = -1;
        [SerializeField] private int steeringSeatActorNumber = -1;
        
        public enum SeatType { Throttle = 0, Steering = 1, Both = 2 }

        private float throttleInput = 0f;  // Y축 (앞/뒤)
        private float steeringInput = 0f;  // X축 (좌/우)

        /// <summary>
        /// 플레이어가 이 차량에 배정되었는지 확인
        /// </summary>
        public bool IsPlayerAssigned(int actorNumber)
        {
            return actorNumber == throttleSeatActorNumber || actorNumber == steeringSeatActorNumber;
        }

        /// <summary>
        /// 배정된 좌석 정보 반환
        /// </summary>
        public (int throttle, int steering) GetAssignedActors()
        {
            return (throttleSeatActorNumber, steeringSeatActorNumber);
        }

        /// <summary>
        /// Vehicle이 호출 - 조합된 최종 입력 반환
        /// </summary>
        public Vector2 GetCombinedInput()
        {
            return new Vector2(steeringInput, throttleInput);
        }

        /// <summary>
        /// VehiclePlayerInput이 RPC로 받은 입력을 전달
        /// </summary>
        public void ReceivePlayerInput(float x, float y, int actor)
        {
            // Throttle 담당 플레이어의 Y축 입력
            if (actor == throttleSeatActorNumber)
            {
                throttleInput = Mathf.Clamp(y, -1f, 1f);
                Debug.Log($"[VehicleSeatManager] Throttle input from actor {actor}: {throttleInput:F2}");
            }
            
            // Steering 담당 플레이어의 X축 입력
            if (actor == steeringSeatActorNumber)
            {
                steeringInput = Mathf.Clamp(x, -1f, 1f);
                Debug.Log($"[VehicleSeatManager] Steering input from actor {actor}: {steeringInput:F2}");
            }
        }

        [PunRPC]
        private void RPC_SendInput(float x, float y, int actor)
        {
            // VehiclePlayerInput에서 호출 - 입력을 처리
            ReceivePlayerInput(x, y, actor);
        }

        [PunRPC]
        public void RPC_SetSeats(int throttleActor, int steeringActor)
        {
            throttleSeatActorNumber = throttleActor;
            steeringSeatActorNumber = steeringActor;
            
            Debug.Log($"[VehicleSeatManager] Seats assigned on {gameObject.name} -> Throttle:{throttleActor} Steering:{steeringActor}");

            // 로컬 카메라 활성/비활성
            var cam = GetComponent<VehicleCameraManager>();
            if (cam != null && PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null)
            {
                int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
                bool active = IsPlayerAssigned(localActor);
                
                if (active)
                {
                    string seatRole = "";
                    if (throttleSeatActorNumber == localActor && steeringSeatActorNumber == localActor)
                        seatRole = "조향+가속";
                    else if (throttleSeatActorNumber == localActor)
                        seatRole = "가속";
                    else if (steeringSeatActorNumber == localActor)
                        seatRole = "조향";
                    Debug.Log($"[플레이어 배정] {gameObject.name} ({seatRole})");
                }
                
                cam.SetCameraActive(active, localActor);
                if (cam.transform.TryGetComponent<Unity.Cinemachine.CinemachineCamera>(out var vcam))
                {
                    vcam.enabled = active;
                }
            }
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

        /// <summary>
        /// Client calls this on the desired vehicle's PhotonView to request a seat.
        /// Master processes the request, unassigns the requester from other vehicles, and broadcasts new seats.
        /// </summary>
        [PunRPC]
        public void RPC_RequestSeat(int seatType, int requestingActor)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                // Only master updates assignments; non-master ignores
                return;
            }

            // Remove requester from any other vehicle seats
            var allManagers = FindObjectsOfType<VehicleSeatManager>();
            foreach (var mgr in allManagers)
            {
                if (mgr == null) continue;
                var (t, s) = mgr.GetAssignedActors();
                bool changed = false;
                if (t == requestingActor) { t = -1; changed = true; }
                if (s == requestingActor) { s = -1; changed = true; }
                if (changed)
                {
                    mgr.photonView.RPC("RPC_SetSeats", RpcTarget.AllBuffered, t, s);
                }
            }

            // Apply request on THIS vehicle
            int newThrottle = throttleSeatActorNumber;
            int newSteering = steeringSeatActorNumber;

            switch ((SeatType)seatType)
            {
                case SeatType.Throttle:
                    newThrottle = requestingActor;
                    break;
                case SeatType.Steering:
                    newSteering = requestingActor;
                    break;
                case SeatType.Both:
                    newThrottle = requestingActor;
                    newSteering = requestingActor;
                    break;
            }

            photonView.RPC("RPC_SetSeats", RpcTarget.AllBuffered, newThrottle, newSteering);
        }

    }
}
