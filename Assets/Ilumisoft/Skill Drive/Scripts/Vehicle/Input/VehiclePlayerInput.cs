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
        
        // WASD Canvas 참조
        private Canvas wasdCanvas;
        
        // 다른 플레이어의 입력 상태 저장
        private Vector2 otherPlayerInput = Vector2.zero;

        private void Awake()
        {
            seatManager = GetComponent<Network.VehicleSeatManager>();
            
            // WASD Canvas 찾기 (자신의 차량 아래)
            wasdCanvas = GetComponentInChildren<Canvas>(true);
            if (wasdCanvas != null)
            {
                // 기본적으로 비활성화 (배정될 때만 활성화)
                wasdCanvas.gameObject.SetActive(false);
                Debug.Log($"[VehiclePlayerInput] Canvas found on {gameObject.name}");
            }
            
            // 디버그: seatManager 찾음 확인
            Debug.Log($"[VehiclePlayerInput] Vehicle {gameObject.name}: seatManager found = {seatManager != null}");
            
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
            // 멀티플레이어: 로컬 플레이어만 입력 읽기
            if (!PhotonNetwork.IsConnected)
            {
                // 싱글 플레이어: 직접 입력 사용
                if (moveAction != null)
                {
                    currentInput = moveAction.ReadValue<Vector2>();
                }
                
                // UI 업데이트 (싱글 플레이어)
                if (WKey != null) WKey.color = UnityEngine.Input.GetKey(KeyCode.W) ? pressedColor : defaultColor;
                if (AKey != null) AKey.color = UnityEngine.Input.GetKey(KeyCode.A) ? pressedColor : defaultColor;
                if (SKey != null) SKey.color = UnityEngine.Input.GetKey(KeyCode.S) ? pressedColor : defaultColor;
                if (DKey != null) DKey.color = UnityEngine.Input.GetKey(KeyCode.D) ? pressedColor : defaultColor;
                return;
            }

            // 멀티플레이어: 내가 이 차량에 배정되었는지 확인
            if (seatManager == null)
            {
                Debug.LogWarning($"[VehiclePlayerInput] {gameObject.name}: seatManager is null!");
                if (WKey != null) WKey.color = defaultColor;
                if (AKey != null) AKey.color = defaultColor;
                if (SKey != null) SKey.color = defaultColor;
                if (DKey != null) DKey.color = defaultColor;
                return;
            }

            int myActor = PhotonNetwork.LocalPlayer?.ActorNumber ?? -1;
            bool isAssigned = seatManager.IsPlayerAssigned(myActor);

            // Canvas 활성화/비활성화 - 자신의 차량일 때만 보여주기
            if (wasdCanvas != null)
            {
                wasdCanvas.gameObject.SetActive(isAssigned);
            }

            if (!isAssigned)
            {
                // 내 차량이 아니면 입력 처리 안 함
                return; 
            }

            // 내 차량이면 내 입력만 표시
            if (moveAction != null)
            {
                Vector2 localInput = moveAction.ReadValue<Vector2>();

                // 입력이 있을 때 더 자주 RPC 전송 (반응성 향상)
                if (localInput.sqrMagnitude > 0.001f || currentInput.sqrMagnitude > 0.001f)
                {
                    if (Vector2.Distance(localInput, currentInput) > 0.005f)
                    {
                        currentInput = localInput;

                        // VehicleSeatManager의 PhotonView를 통해 RPC 전송
                        if (seatManager.photonView != null)
                        {
                            // 로꺾 즉시 반영 (클라이언트 사이드 예측)
                            seatManager.ReceivePlayerInput(localInput.x, localInput.y, myActor);
                            
                            // All(비신뢰성)로 빠르게 전송 - 입력은 지연보다 속도 중요
                            seatManager.photonView.RPC("RPC_ReceivePlayerInput", RpcTarget.Others, localInput.x, localInput.y, myActor);
                        }
                    }
                }
                
                // UI 업데이트: 자신의 입력 + 파트너의 입력 혼합
                Vector2 displayInput = seatManager.GetMixedInputForUI(myActor);
                
                if (WKey != null) WKey.color = displayInput.y > 0.1f ? pressedColor : defaultColor;
                if (AKey != null) AKey.color = displayInput.x < -0.1f ? pressedColor : defaultColor;
                if (SKey != null) SKey.color = displayInput.y < -0.1f ? pressedColor : defaultColor;
                if (DKey != null) DKey.color = displayInput.x > 0.1f ? pressedColor : defaultColor;
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
