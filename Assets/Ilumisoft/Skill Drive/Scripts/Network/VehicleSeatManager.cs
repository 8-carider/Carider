using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Ilumisoft.RadarSystem;

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
        
        // 각 플레이어의 개별 입력 저장
        private Vector2 throttlePlayerInput = Vector2.zero;  // Throttle 담당 플레이어의 입력
        private Vector2 steeringPlayerInput = Vector2.zero;  // Steering 담당 플레이어의 입력

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
        /// 로컬 플레이어의 입력과 파트너의 입력을 분리해서 반환
        /// localActor가 throttle이면: 자신의 throttle input + 파트너의 steering input
        /// localActor가 steering이면: 자신의 steering input + 파트너의 throttle input
        /// 같은 플레이어가 둘 다 담당하면: 자신의 전체 입력 반환
        /// </summary>
        public Vector2 GetMixedInputForUI(int localActor)
        {
            Vector2 result = Vector2.zero;

            // 같은 플레이어가 throttle과 steering을 모두 담당하는 경우
            if (throttleSeatActorNumber == steeringSeatActorNumber && throttleSeatActorNumber == localActor)
            {
                // 자신의 전체 입력 사용
                result = throttlePlayerInput;  // throttle과 steering이 같으므로 둘 다 포함
            }
            else if (localActor == throttleSeatActorNumber)
            {
                // 나는 throttle (앞뒤), 파트너는 steering (좌우)
                result.y = throttlePlayerInput.y;  // 내 Y축 (W/S)
                result.x = steeringPlayerInput.x;  // 파트너의 X축 (A/D)
            }
            else if (localActor == steeringSeatActorNumber)
            {
                // 나는 steering (좌우), 파트너는 throttle (앞뒤)
                result.x = steeringPlayerInput.x;  // 내 X축 (A/D)
                result.y = throttlePlayerInput.y;  // 파트너의 Y축 (W/S)
            }

            return result;
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
                throttlePlayerInput = new Vector2(x, y);  // 전체 입력 저장 (WASD UI용)
                Debug.Log($"[VehicleSeatManager] Throttle input from actor {actor}: {throttleInput:F2}");
            }
            
            // Steering 담당 플레이어의 X축 입력
            if (actor == steeringSeatActorNumber)
            {
                steeringInput = Mathf.Clamp(x, -1f, 1f);
                steeringPlayerInput = new Vector2(x, y);  // 전체 입력 저장 (WASD UI용)
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

                // Radar 활성/비활성: 내 차량에만 Radar가 보이게
                var radar = GetComponentInChildren<Radar>(true);
                if (radar != null)
                {
                    radar.gameObject.SetActive(active);
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
