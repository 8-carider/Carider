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
        
        private float throttleInput = 0f;  // Y축 (앞/뒤)
        private float steeringInput = 0f;  // X축 (좌/우)
        
        // 각 플레이어의 개별 입력 저장
        private Vector2 throttlePlayerInput = Vector2.zero;  // Throttle 담당 플레이어의 입력
        private Vector2 steeringPlayerInput = Vector2.zero;  // Steering 담당 플레이어의 입력
        
        // 입력 스무딩 (더 부드러운 입력)
        private Vector2 smoothedThrottleInput = Vector2.zero;
        private Vector2 smoothedSteeringInput = Vector2.zero;
        private float inputSmoothSpeed = 15f; // 스무딩 속도

        // 역할 교체 관련
        private float roleSwapTimer = 0f;
        private int originalThrottleActor = -1;
        private int originalSteeringActor = -1;
        private bool isRoleSwapped = false;

        public bool IsRoleSwapped => isRoleSwapped;

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
                smoothedThrottleInput = throttlePlayerInput; // 즉시 적용
            }
            
            // Steering 담당 플레이어의 X쵩1 입력
            if (actor == steeringSeatActorNumber)
            {
                steeringInput = Mathf.Clamp(x, -1f, 1f);
                steeringPlayerInput = new Vector2(x, y);  // 전체 입력 저장 (WASD UI용)
                smoothedSteeringInput = steeringPlayerInput; // 즉시 적용
            }
        }

        [PunRPC]
        private void RPC_ReceivePlayerInput(float x, float y, int actor)
        {
            ReceivePlayerInput(x, y, actor);
        }

        [PunRPC]
        public void RPC_SetSeats(int throttleActor, int steeringActor)
        {
            throttleSeatActorNumber = throttleActor;
            steeringSeatActorNumber = steeringActor;

            // 로컬 카메라 활성/비활성
            var cam = GetComponent<VehicleCameraManager>();
            if (cam != null && PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null)
            {
                int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
                bool active = IsPlayerAssigned(localActor);
                
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

        private void Update()
        {
            // 역할 교체 타이머 처리
            if (roleSwapTimer > 0f)
            {
                roleSwapTimer -= Time.deltaTime;
                if (roleSwapTimer <= 0f)
                {
                    // 원래 역할로 복구
                    RestoreOriginalRoles();
                }
            }
        }

        /// <summary>
        /// 특정 시간 동안 역할 교체 (가속 <-> 조향)
        /// </summary>
        public void SwapRoles(float duration)
        {
            if (PhotonNetwork.InRoom && photonView.IsMine)
            {
                photonView.RPC("RPC_SwapRoles", RpcTarget.All, duration);
            }
            else
            {
                ApplyRoleSwap(duration);
            }
        }

        [PunRPC]
        private void RPC_SwapRoles(float duration)
        {
            ApplyRoleSwap(duration);
        }

        private void ApplyRoleSwap(float duration)
        {
            // 같은 플레이어가 둘 다 담당하면 교체 불가
            if (throttleSeatActorNumber == steeringSeatActorNumber)
                return;

            // 원래 역할 저장 (처음 교체 시에만)
            if (!isRoleSwapped)
            {
                originalThrottleActor = throttleSeatActorNumber;
                originalSteeringActor = steeringSeatActorNumber;
            }

            // 역할 교체
            int temp = throttleSeatActorNumber;
            throttleSeatActorNumber = steeringSeatActorNumber;
            steeringSeatActorNumber = temp;

            // 타이머 설정
            roleSwapTimer = duration;
            isRoleSwapped = true;

            // 입력 초기화
            throttleInput = 0f;
            steeringInput = 0f;

            Debug.Log($"[VehicleSeatManager] 역할 교체! Throttle: {throttleSeatActorNumber}, Steering: {steeringSeatActorNumber}");
        }

        private void RestoreOriginalRoles()
        {
            if (!isRoleSwapped)
                return;

            // 원래 역할로 복구
            throttleSeatActorNumber = originalThrottleActor;
            steeringSeatActorNumber = originalSteeringActor;

            isRoleSwapped = false;
            roleSwapTimer = 0f;

            // 입력 초기화
            throttleInput = 0f;
            steeringInput = 0f;

            Debug.Log($"[VehicleSeatManager] 역할 복구! Throttle: {throttleSeatActorNumber}, Steering: {steeringSeatActorNumber}");
        }

    }
}
