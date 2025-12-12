using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Ilumisoft.SkillDrive.Game;
using Ilumisoft.SkillDrive.UI;

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
        [SerializeField] private Button startGameButton; // Master-only start button

        [Header("Settings")]
        [SerializeField] private bool showInLevelSelection = true;

    private int selectedVehicle = -1; // 0 or 1 (for temporary highlight)
    private bool selectedIsThrottle = true; // true = throttle, false = steering (for temporary highlight)
        private bool isPanelLockedForSession = false; // Once confirmed, cannot reopen until level clear

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
            UpdateStatusText("Select your seat (Throttle or Steering)");

            // Auto-bind Start button (find by name if field not assigned)
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
                startGameButton.interactable = false; // Inactive until all seats filled
            }
        }

        private void SetupButtons()
        {
            if (vehicle1ThrottleButton != null)
            {
                vehicle1ThrottleButton.onClick.AddListener(() => OnSeatButtonClicked(0, true));
                UpdateButtonText(vehicle1ThrottleButton, "V1 Throttle W/S");
            }

            if (vehicle1SteeringButton != null)
            {
                vehicle1SteeringButton.onClick.AddListener(() => OnSeatButtonClicked(0, false));
                UpdateButtonText(vehicle1SteeringButton, "V1 Steer A/D");
            }

            if (vehicle2ThrottleButton != null)
            {
                vehicle2ThrottleButton.onClick.AddListener(() => OnSeatButtonClicked(1, true));
                UpdateButtonText(vehicle2ThrottleButton, "V2 Throttle W/S");
            }

            if (vehicle2SteeringButton != null)
            {
                vehicle2SteeringButton.onClick.AddListener(() => OnSeatButtonClicked(1, false));
                UpdateButtonText(vehicle2SteeringButton, "V2 Steer A/D");
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

            // Check current seat assignment status
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
                UpdateStatusText("ERROR: Cannot control both vehicles");
                return;
            }

            if (iOwnThisSeat)
            {
                // 내가 점유한 좌석이면 해제 요청
                spawnerPV.RPC("RPC_ReleaseSeat", RpcTarget.MasterClient, myActor, vehicleIndex, isThrottle);
                UpdateStatusText("Releasing seat...");
            }
            else
            {
                // 점유 요청
                spawnerPV.RPC("RPC_RequestSeat", RpcTarget.MasterClient, myActor, vehicleIndex, isThrottle);
                UpdateStatusText("Requesting seat...");
            }
        }

        // Master starts the game
        private void OnStartGameClicked()
        {
            Debug.Log("[SeatSelectionUI] Start button clicked!");
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[SeatSelectionUI] Not master client!");
                return;
            }
            Debug.Log("[SeatSelectionUI] Checking if all seats filled...");
            if (!AllFourUniqueSeatsFilled())
            {
                Debug.LogWarning("[SeatSelectionUI] Not all seats filled!");
                var managers = FindObjectsOfType<VehicleSeatManager>();
                Debug.Log($"[SeatSelectionUI] Found {managers.Length} VehicleSeatManagers");
                foreach (var m in managers)
                {
                    var (t, s) = m.GetAssignedActors();
                    Debug.Log($"[SeatSelectionUI] {m.name}: Throttle={t}, Steering={s}");
                }
                UpdateStatusText("ERROR: Not all seats filled");
                return;
            }

            // Call RPC to play camera sequence then countdown on all clients
            photonView.RPC("RPC_PlayCameraSequenceAndStartCountdown", RpcTarget.All);

            // Hide Start button immediately
            if (startGameButton != null)
            {
                startGameButton.gameObject.SetActive(false);
            }
        }

        [PunRPC]
        private void RPC_PlayCameraSequenceAndStartCountdown()
        {
            // Play camera sequence on all vehicles
            var cameraSequences = FindObjectsByType<RaceCameraDistanceSequence>(FindObjectsSortMode.None);
            if (cameraSequences != null && cameraSequences.Length > 0)
            {
                foreach (var seq in cameraSequences)
                {
                    seq.PlayCameraDistanceSequence();
                }
                UpdateStatusText("Camera sequence...");
                // Start countdown after sequence ends (2.5s later)
                Invoke(nameof(StartRaceCountdown), 2.5f);
            }
            else
            {
                // 연출이 없으면 바로 카운트다운 시작
                StartRaceCountdown();
            }
            // Hide selection panel
            if (selectionPanel != null)
                selectionPanel.SetActive(false);
        }

        private void StartRaceCountdown()
        {
            // Start race countdown UI
            var countdownUI = FindAnyObjectByType<RaceCountdownUI>();
            if (countdownUI != null)
            {
                countdownUI.OnCountdownFinished -= OnCountdownFinished;
                countdownUI.OnCountdownFinished += OnCountdownFinished;
                countdownUI.StartCountdown();
            }
            else
            {
                // No countdown UI, proceed immediately
                OnCountdownFinished();
            }
        }

        private void OnCountdownFinished()
        {
            // Start time limit countdown (begin race)
            float timeLimit = 120f;
            if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("TimeLimit"))
            {
                object v = PhotonNetwork.CurrentRoom.CustomProperties["TimeLimit"];
                if (v is float f) timeLimit = f;
                else if (v is int i) timeLimit = i;
                else if (float.TryParse(v.ToString(), out var parsed)) timeLimit = parsed;
            }
            photonView.RPC("RPC_BeginCountdown", RpcTarget.AllBuffered, timeLimit);

            // Enable vehicle controls
            var vehicles = FindObjectsByType<Ilumisoft.SkillDrive.Vehicle>(FindObjectsSortMode.None);
            foreach (var v in vehicles)
            {
                v.CanMove = true;
            }
        }

        [PunRPC]
        private void RPC_BeginCountdown(float timeLimit)
        {
            var countdown = FindAnyObjectByType<Countdown>();
            if (countdown != null)
            {
                // Use existing GameManager Countdown UI
                countdown.SetTimeLimit(timeLimit);
                // Prevent duplicate registration
                countdown.OnTimeOut -= HandleCountdownTimeout;
                countdown.OnTimeOut += HandleCountdownTimeout;
                countdown.StartCountdown();
            }
            else
            {
                Debug.LogWarning("[SeatSelectionUI] Countdown component not found in scene.");
            }
        }

        private void HandleCountdownTimeout()
        {
            // Master moves to next map
            if (PhotonNetwork.IsMasterClient)
            {
                if (GameSessionManager.Instance != null)
                {
                    GameSessionManager.Instance.CompleteCurrentMap();
                }
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
            // Collect seat info from all VehicleSeatManagers
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

            // Start button enabled condition
            if (startGameButton != null && PhotonNetwork.IsMasterClient)
            {
                bool allSeatsReady = AllFourUniqueSeatsFilled();
                startGameButton.interactable = allSeatsReady;
                
                if (allSeatsReady)
                {
                    UpdateStatusText("Ready to Start\nClick START button");
                }
                else
                {
                    // Check how many players are assigned
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
                // PlayerList 인덱스 사용하여 항상 P1, P2, P3, P4로 표시 (ActorNumber는 재사용 안됨)
                var ordered = PhotonNetwork.PlayerList;
                int idx = 0;
                for (int i = 0; i < ordered.Length; i++)
                {
                    if (ordered[i].ActorNumber == actorNumber)
                    {
                        idx = i;
                        playerLabel = $"P{i + 1}"; // 배열 순서로 P1~P4
                        break;
                    }
                }
                
                // Check if local player
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
            
            // Add player label to button text
            UpdateButtonPlayerLabel(btn, playerLabel);
        }
        
        private void UpdateButtonPlayerLabel(Button btn, string playerLabel)
        {
            if (btn == null) return;
            var tmpText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null)
            {
                // Reconstruct with base format (prevent duplication)
                string vehicleText = "";
                string roleText = "";
                
                // Identify button
                if (btn == vehicle1ThrottleButton) { vehicleText = "V1"; roleText = "Throttle W/S"; }
                else if (btn == vehicle1SteeringButton) { vehicleText = "V1"; roleText = "Steer A/D"; }
                else if (btn == vehicle2ThrottleButton) { vehicleText = "V2"; roleText = "Throttle W/S"; }
                else if (btn == vehicle2SteeringButton) { vehicleText = "V2"; roleText = "Steer A/D"; }
                
                if (string.IsNullOrEmpty(playerLabel))
                {
                    tmpText.text = $"{vehicleText} {roleText}";
                }
                else
                {
                    tmpText.text = $"{vehicleText} {roleText} [{playerLabel}]";
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
            
            // All seats must be assigned (no empty seats)
            if (v1Throttle == -1 || v1Steering == -1 || v2Throttle == -1 || v2Steering == -1)
            {
                return false;
            }
            
            // Check for player overlap across vehicles
            System.Collections.Generic.HashSet<int> vehicle1Actors = new System.Collections.Generic.HashSet<int> { v1Throttle, v1Steering };
            System.Collections.Generic.HashSet<int> vehicle2Actors = new System.Collections.Generic.HashSet<int> { v2Throttle, v2Steering };
            
            // Players cannot control both vehicles simultaneously
            foreach (int actor in vehicle1Actors)
            {
                if (vehicle2Actors.Contains(actor))
                {
                    return false; // Same player assigned to both vehicles
                }
            }
            
            // Allow any combination with 2, 3, or 4 unique players
            return true;
        }

        [PunRPC]
        void RPC_SeatRequestResult(bool success, string message)
        {
            OnSeatRequestResult(success, message);
        }

        public void OnSeatRequestResult(bool success, string message)
        {
            // Panel continues to be toggleable, not hidden
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
        /// Force UI update from external caller (e.g., after respawn)
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
            isPanelLockedForSession = false; // Unlock when leaving room
        }
    }
}
