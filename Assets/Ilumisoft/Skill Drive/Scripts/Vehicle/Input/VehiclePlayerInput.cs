using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

namespace Ilumisoft.SkillDrive.Input
{
    /// <summary>
    /// 단순화된 차량 입력 시스템
    /// - 로컬 플레이어: 키보드 입력 읽어서 RPC 전송
    /// - Vehicle은 VehicleSeatManager가 제공하는 조합된 입력 사용
    /// </summary>
    public class VehiclePlayerInput : VehicleInputSource
    {
        // WASD UI 오브젝트 연결용 변수
        public UnityEngine.UI.Image WKey, AKey, SKey, DKey;
        public Color pressedColor = Color.gray;
        public Color defaultColor = Color.white;

        private InputAction moveAction;
        private Network.VehicleSeatManager seatManager;
        private Vector2 currentInput = Vector2.zero;

        private void Awake()
        {
            seatManager = GetComponent<Network.VehicleSeatManager>();
            
            // Input Action 설정 - PlayerInput 컴포넌트에서 가져오기
            var playerInput = GetComponent<PlayerInput>();
            if (playerInput != null && playerInput.actions != null)
            {
                var actionMap = playerInput.actions.FindActionMap("Player");
                if (actionMap != null)
                {
                    moveAction = actionMap.FindAction("Move");
                    if (moveAction != null)
                    {
                        moveAction.Enable();
                        Debug.Log($"[VehiclePlayerInput] Move action enabled from PlayerInput on {gameObject.name}");
                    }
                }
            }
            
            // PlayerInput에서 못 찾으면 Resources에서 로드
            if (moveAction == null)
            {
                var actions = Resources.Load<InputActionAsset>("Ilumisoft/Skill Drive/Input");
                if (actions != null)
                {
                    var actionMap = actions.FindActionMap("Player");
                    if (actionMap != null)
                    {
                        moveAction = actionMap.FindAction("Move");
                        if (moveAction != null)
                        {
                            moveAction.Enable();
                            Debug.Log($"[VehiclePlayerInput] Move action enabled from Resources on {gameObject.name}");
                        }
                    }
                }
            }
            
            if (moveAction == null)
            {
                Debug.LogError($"[VehiclePlayerInput] Failed to find Move action on {gameObject.name}!");
            }
        }

        private void OnEnable()
        {
            moveAction?.Enable();
        }

        private void OnDisable()
        {
            moveAction?.Disable();
        }

        private void Update()
        {
            // WASD UI 색상 변경
            if (WKey != null) WKey.color = UnityEngine.Input.GetKey(KeyCode.W) ? pressedColor : defaultColor;
            if (AKey != null) AKey.color = UnityEngine.Input.GetKey(KeyCode.A) ? pressedColor : defaultColor;
            if (SKey != null) SKey.color = UnityEngine.Input.GetKey(KeyCode.S) ? pressedColor : defaultColor;
            if (DKey != null) DKey.color = UnityEngine.Input.GetKey(KeyCode.D) ? pressedColor : defaultColor;

            // 멀티플레이어: 로컬 플레이어만 입력 읽기
            if (!PhotonNetwork.IsConnected)
            {
                // 싱글 플레이어: 직접 입력 사용
                if (moveAction != null)
                {
                    currentInput = moveAction.ReadValue<Vector2>();
                }
                return;
            }

            // 멀티플레이어: 내가 이 차량에 배정되었는지 확인
            if (seatManager == null || !seatManager.IsPlayerAssigned(PhotonNetwork.LocalPlayer.ActorNumber))
            {
                return; // 내 차량이 아니면 입력 무시
            }

            // 키보드에서 입력 읽기
            if (moveAction != null)
            {
                Vector2 localInput = moveAction.ReadValue<Vector2>();

                // 입력이 있을 때만 RPC 전송 (대역폭 절약)
                if (localInput.sqrMagnitude > 0.01f || currentInput.sqrMagnitude > 0.01f)
                {
                    if (Vector2.Distance(localInput, currentInput) > 0.01f)
                    {
                        currentInput = localInput;
                        int myActor = PhotonNetwork.LocalPlayer.ActorNumber;

                        // VehicleSeatManager의 PhotonView를 통해 RPC 전송
                        if (seatManager.photonView != null)
                        {
                            seatManager.photonView.RPC("RPC_SendInput", RpcTarget.AllBuffered, localInput.x, localInput.y, myActor);
                            Debug.Log($"[VehiclePlayerInput] Sending input ({localInput.x:F2}, {localInput.y:F2}) from actor {myActor}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Vehicle이 호출 - VehicleSeatManager가 조합한 입력 반환
        /// </summary>
        public override Vector2 GetInput()
        {
            if (seatManager != null)
            {
                return seatManager.GetCombinedInput();
            }
            return currentInput; // 싱글 플레이어 폴백
        }
        
        private void Reset()
        {
            // Automatically add and setup the PlayerInput component if none exists
            if(GetComponent<PlayerInput>() == null)
            {
                var playerInput = gameObject.AddComponent<PlayerInput>();

                // Use our default input scheme
                playerInput.actions = Resources.Load<InputActionAsset>("Ilumisoft/Skill Drive/Input");
            }
        }
    }
}
