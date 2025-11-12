using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

namespace Ilumisoft.SkillDrive.Network
{
    /// <summary>
    /// UI for players to select which vehicle and seat (throttle/steering) they want to control.
    /// Shows in lobby or level selection before entering the game level.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class SeatSelectionUI : MonoBehaviourPunCallbacks
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private Button vehicle1ThrottleButton;
        [SerializeField] private Button vehicle1SteeringButton;
        [SerializeField] private Button vehicle2ThrottleButton;
        [SerializeField] private Button vehicle2SteeringButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button startGameButton; // 마스터 전용 시작 버튼

        [Header("Settings")]
        [SerializeField] private bool showInLevelSelection = true;

    private int selectedVehicle = -1; // 0 or 1 (for temporary highlight)
    private bool selectedIsThrottle = true; // true = throttle, false = steering (for temporary highlight)
        private bool isPanelLockedForSession = false; // 한 번 확정하면 레벨 클리어까지 재오픈 불가

        private float refreshTimer = 0f;
        private readonly Color unoccupiedColor = new Color(0.3f,0.3f,0.3f);
        private readonly Color localHighlightColor = new Color(0.2f,0.8f,0.2f);
        private readonly Color[] playerColors = new Color[]
        {
            new Color(0.9f,0.3f,0.3f), // Player 1
            new Color(0.3f,0.5f,0.9f), // Player 2
            new Color(0.9f,0.8f,0.3f), // Player 3
            new Color(0.7f,0.4f,0.9f)  // Player 4
        };

        private void Start()
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            {
                if (selectionPanel != null) selectionPanel.SetActive(false);
                return;
            }

            SetupButtons();
            UpdateStatusText("Select your seat");

            // Start 버튼 자동 바인딩(필드 미지정 시 이름으로 찾기)
            if (startGameButton == null)
            {
                if (selectionPanel != null)
                {
                    var buttons = selectionPanel.GetComponentsInChildren<Button>(true);
                    foreach (var b in buttons)
                    {
                        if (b != null && b.name == "Button_Start")
                        {
                            startGameButton = b;
                            break;
                        }
                    }
                }
                if (startGameButton == null)
                {
                    var go = GameObject.Find("Button_Start");
                    if (go != null) startGameButton = go.GetComponent<Button>();
                }
            }

            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(OnStartGameClicked);
                startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
                startGameButton.interactable = false; // 좌석 다 찰 때까지 비활성
            }
        }

        private void SetupButtons()
        {
            if (vehicle1ThrottleButton != null)
            {
                vehicle1ThrottleButton.onClick.AddListener(() => OnSeatButtonClicked(0, true));
                UpdateButtonText(vehicle1ThrottleButton, "Vehicle 1\nThrottle");
            }

            if (vehicle1SteeringButton != null)
            {
                vehicle1SteeringButton.onClick.AddListener(() => OnSeatButtonClicked(0, false));
                UpdateButtonText(vehicle1SteeringButton, "Vehicle 1\nSteering");
            }

            if (vehicle2ThrottleButton != null)
            {
                vehicle2ThrottleButton.onClick.AddListener(() => OnSeatButtonClicked(1, true));
                UpdateButtonText(vehicle2ThrottleButton, "Vehicle 2\nThrottle");
            }

            if (vehicle2SteeringButton != null)
            {
                vehicle2SteeringButton.onClick.AddListener(() => OnSeatButtonClicked(1, false));
                UpdateButtonText(vehicle2SteeringButton, "Vehicle 2\nSteering");
            }

            // Confirm 버튼은 제거됨
        }

        private void OnSeatButtonClicked(int vehicleIndex, bool isThrottle)
        {
            // 즉시 토글: 이미 내가 점유 중이면 해제 요청, 아니면 점유 요청
            ToggleSeat(vehicleIndex, isThrottle);
        }

        private void ToggleSeat(int vehicleIndex, bool isThrottle)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom) return;
            int myActor = PhotonNetwork.LocalPlayer.ActorNumber;

            // 현재 좌석 배정 상태 조회
            int v1Throttle = -1, v1Steering = -1, v2Throttle = -1, v2Steering = -1;
            var managers = FindObjectsOfType<VehicleSeatManager>();
            foreach (var m in managers)
            {
                var (t, s) = m.GetAssignedActors();
                if (m.name.Contains("Vehicle 1"))
                { v1Throttle = t; v1Steering = s; }
                else if (m.name.Contains("Vehicle 2"))
                { v2Throttle = t; v2Steering = s; }
            }

            bool iOwnThisSeat = false;
            if (vehicleIndex == 0)
            {
                iOwnThisSeat = isThrottle ? (v1Throttle == myActor) : (v1Steering == myActor);
            }
            else
            {
                iOwnThisSeat = isThrottle ? (v2Throttle == myActor) : (v2Steering == myActor);
            }

            var spawner = FindObjectOfType<MapVehicleSpawner>();
            if (spawner == null) { UpdateStatusText("Error: No Spawner"); return; }
            var spawnerPV = spawner.GetComponent<PhotonView>();
            if (spawnerPV == null) { UpdateStatusText("Error: No PhotonView"); return; }

            // 로컬 사전 규칙 체크: 다른 차량에 이미 배정되어 있으면 요청 차단
            bool iOwnOtherVehicle = false;
            if (vehicleIndex == 0)
            {
                // 요청 차량이 0인데, 차량 1의 어느 좌석이든 내가 가지고 있으면 금지
                iOwnOtherVehicle = (v2Throttle == myActor) || (v2Steering == myActor);
            }
            else
            {
                iOwnOtherVehicle = (v1Throttle == myActor) || (v1Steering == myActor);
            }
            if (!iOwnThisSeat && iOwnOtherVehicle)
            {
                UpdateStatusText("같은 차량 내 좌석만 선택할 수 있습니다.");
                return;
            }

            if (iOwnThisSeat)
            {
                // 내가 점유한 좌석이면 해제 요청
                spawnerPV.RPC("RPC_ReleaseSeat", RpcTarget.MasterClient, myActor, vehicleIndex, isThrottle);
                UpdateStatusText("Release request sent...");
            }
            else
            {
                // 점유 요청
                spawnerPV.RPC("RPC_RequestSeat", RpcTarget.MasterClient, myActor, vehicleIndex, isThrottle);
                UpdateStatusText("Request sent...");
            }
        }

        // 마스터가 게임 시작
        private void OnStartGameClicked()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (!AllFourUniqueSeatsFilled())
            {
                UpdateStatusText("Not all seats filled");
                return;
            }
            var spawner = FindObjectOfType<MapVehicleSpawner>();
            if (spawner != null)
            {
                var pv = spawner.GetComponent<PhotonView>();
                if (pv != null)
                {
                    pv.RPC("RPC_StartGame", RpcTarget.All);
                    UpdateStatusText("Game Starting!");
                }
            }
            // Start 버튼 즉시 숨김 (마스터 포함 모든 클라이언트에서는 RPC_StartGame에서 패널 닫힘 처리)
            if (startGameButton != null)
            {
                startGameButton.gameObject.SetActive(false);
            }
        }

        private void ApplySeatAssignment(int actorNumber, int vehicleIndex, bool isThrottle)
        {
            var spawner = FindObjectOfType<MapVehicleSpawner>();
            if (spawner != null)
            {
                spawner.AssignSeatManually(actorNumber, vehicleIndex, isThrottle);
            }
        }

        private void HighlightButton(int vehicleIndex, bool isThrottle)
        {
            // Reset all buttons to normal color
            ResetButtonColor(vehicle1ThrottleButton);
            ResetButtonColor(vehicle1SteeringButton);
            ResetButtonColor(vehicle2ThrottleButton);
            ResetButtonColor(vehicle2SteeringButton);

            // Highlight selected
            Button selectedButton = null;
            if (vehicleIndex == 0 && isThrottle) selectedButton = vehicle1ThrottleButton;
            else if (vehicleIndex == 0 && !isThrottle) selectedButton = vehicle1SteeringButton;
            else if (vehicleIndex == 1 && isThrottle) selectedButton = vehicle2ThrottleButton;
            else if (vehicleIndex == 1 && !isThrottle) selectedButton = vehicle2SteeringButton;

            if (selectedButton != null)
            {
                var colors = selectedButton.colors;
                colors.normalColor = Color.green;
                selectedButton.colors = colors;
            }
        }

        private void ResetButtonColor(Button btn)
        {
            if (btn == null) return;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            btn.colors = colors;
        }

        private void UpdateButtonText(Button btn, string text)
        {
            if (btn == null) return;
            var tmpText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null) tmpText.text = text;
        }

        private void UpdateStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private void Update()
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom) return;
            
            // 패널이 세션 중 잠겼으면 갱신 중지
            if (isPanelLockedForSession) return;
            
            refreshTimer += Time.deltaTime;
            if (refreshTimer >= 0.3f)
            {
                refreshTimer = 0f;
                RefreshSeatOccupancyVisuals();
            }
        }

        private void RefreshSeatOccupancyVisuals()
        {
            // 현재 모든 VehicleSeatManager로부터 좌석 정보 수집
            var managers = FindObjectsOfType<VehicleSeatManager>();
            int v1Throttle = -1, v1Steering = -1, v2Throttle = -1, v2Steering = -1;
            foreach (var m in managers)
            {
                if (m.name.Contains("Vehicle 1"))
                {
                    var (t,s) = m.GetAssignedActors();
                    v1Throttle = t; v1Steering = s;
                }
                else if (m.name.Contains("Vehicle 2"))
                {
                    var (t,s) = m.GetAssignedActors();
                    v2Throttle = t; v2Steering = s;
                }
            }

            ApplySeatColor(vehicle1ThrottleButton, v1Throttle, true);
            ApplySeatColor(vehicle1SteeringButton, v1Steering, false);
            ApplySeatColor(vehicle2ThrottleButton, v2Throttle, true);
            ApplySeatColor(vehicle2SteeringButton, v2Steering, false);

            // 시작 버튼 활성 조건
            if (startGameButton != null && PhotonNetwork.IsMasterClient)
            {
                bool allSeatsReady = AllFourUniqueSeatsFilled();
                startGameButton.interactable = allSeatsReady;
                
                if (allSeatsReady)
                {
                    UpdateStatusText("All seats ready - Master can start");
                }
                else
                {
                    // 몇 명이 배정되었는지 확인
                    System.Collections.Generic.HashSet<int> currentActors = new System.Collections.Generic.HashSet<int>();
                    foreach (var m in managers)
                    {
                        var (t, s) = m.GetAssignedActors();
                        if (t != -1) currentActors.Add(t);
                        if (s != -1) currentActors.Add(s);
                    }
                    
                    int filledSeats = 0;
                    if (v1Throttle != -1) filledSeats++;
                    if (v1Steering != -1) filledSeats++;
                    if (v2Throttle != -1) filledSeats++;
                    if (v2Steering != -1) filledSeats++;
                    
                    UpdateStatusText($"Waiting: {filledSeats}/4 seats filled, {currentActors.Count} unique players");
                }
            }
        }

        private void ApplySeatColor(Button btn, int actorNumber, bool throttle)
        {
            if (btn == null) return;
            var colors = btn.colors;
            string playerLabel = "";
            
            if (actorNumber == -1)
            {
                colors.normalColor = unoccupiedColor;
            }
            else
            {
                // 플레이어 리스트 순서 기반 인덱스 찾기
                var ordered = PhotonNetwork.PlayerList;
                int idx = 0;
                for (int i = 0; i < ordered.Length; i++)
                {
                    if (ordered[i].ActorNumber == actorNumber)
                    {
                        idx = i;
                        playerLabel = $"P{i + 1}";
                        break;
                    }
                }
                
                // 로컬 플레이어인지 여부
                if (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
                {
                    colors.normalColor = localHighlightColor;
                }
                else
                {
                    colors.normalColor = playerColors[Mathf.Clamp(idx,0,playerColors.Length-1)];
                }
            }
            
            btn.colors = colors;
            
            // 버튼 텍스트에 플레이어 라벨 추가
            UpdateButtonPlayerLabel(btn, playerLabel);
        }
        
        private void UpdateButtonPlayerLabel(Button btn, string playerLabel)
        {
            if (btn == null) return;
            var tmpText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null)
            {
                // 항상 기본 형식으로 재구성 (중복 방지)
                string vehicleText = "";
                string roleText = "";
                
                // 버튼 식별
                if (btn == vehicle1ThrottleButton) { vehicleText = "Vehicle 1"; roleText = "Throttle"; }
                else if (btn == vehicle1SteeringButton) { vehicleText = "Vehicle 1"; roleText = "Steering"; }
                else if (btn == vehicle2ThrottleButton) { vehicleText = "Vehicle 2"; roleText = "Throttle"; }
                else if (btn == vehicle2SteeringButton) { vehicleText = "Vehicle 2"; roleText = "Steering"; }
                
                if (string.IsNullOrEmpty(playerLabel))
                {
                    tmpText.text = $"{vehicleText}\n{roleText}";
                }
                else
                {
                    tmpText.text = $"{vehicleText}\n{roleText} [{playerLabel}]";
                }
            }
        }

        private bool AllFourUniqueSeatsFilled()
        {
            var managers = FindObjectsOfType<VehicleSeatManager>();
            int v1Throttle = -1, v1Steering = -1, v2Throttle = -1, v2Steering = -1;
            
            foreach (var m in managers)
            {
                var (t, s) = m.GetAssignedActors();
                if (m.name.Contains("Vehicle 1"))
                {
                    v1Throttle = t;
                    v1Steering = s;
                }
                else if (m.name.Contains("Vehicle 2"))
                {
                    v2Throttle = t;
                    v2Steering = s;
                }
            }
            
            // 모든 좌석이 배정되어 있어야 함 (빈 좌석 없음)
            if (v1Throttle == -1 || v1Steering == -1 || v2Throttle == -1 || v2Steering == -1)
            {
                return false;
            }
            
            // 각 차량별로 누가 배정되었는지 확인
            System.Collections.Generic.HashSet<int> vehicle1Actors = new System.Collections.Generic.HashSet<int> { v1Throttle, v1Steering };
            System.Collections.Generic.HashSet<int> vehicle2Actors = new System.Collections.Generic.HashSet<int> { v2Throttle, v2Steering };
            
            // 차량 1과 차량 2의 플레이어가 겹치면 안 됨 (한 사람이 두 차량 동시 제어 불가)
            foreach (int actor in vehicle1Actors)
            {
                if (vehicle2Actors.Contains(actor))
                {
                    return false; // 같은 사람이 두 차량에 배정됨
                }
            }
            
            // 2, 3, 4명 어떤 조합이든 허용 (이미 위에서 교차 제어 금지 검증됨)
            return true;
        }

        [PunRPC]
        void RPC_SeatRequestResult(bool success, string message)
        {
            OnSeatRequestResult(success, message);
        }

        public void OnSeatRequestResult(bool success, string message)
        {
            // 패널은 숨기지 않고 계속 토글 가능
            UpdateStatusText(message);
        }

        public void HidePanel()
        {
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
                if (startGameButton != null)
                {
                    startGameButton.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 외부에서 UI 강제 업데이트 (리스폰 후 등)
        /// </summary>
        public void ForceRefreshUI()
        {
            if (!isPanelLockedForSession && PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                RefreshSeatOccupancyVisuals();
            }
        }

        public override void OnLeftRoom()
        {
            selectedVehicle = -1;
            isPanelLockedForSession = false; // 방을 나가면 잠금 해제
        }
    }
}
