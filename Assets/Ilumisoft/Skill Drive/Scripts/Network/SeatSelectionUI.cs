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
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button startGameButton; // 마스터 전용 시작 버튼

        [Header("Settings")]
        [SerializeField] private bool showInLevelSelection = true;

        private int selectedVehicle = -1; // 0 or 1
        private bool selectedIsThrottle = true; // true = throttle, false = steering
        private bool hasConfirmed = false;

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
            UpdateStatusText("차량과 역할을 선택하세요");

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
                UpdateButtonText(vehicle1ThrottleButton, "Vehicle 1\n가속");
            }

            if (vehicle1SteeringButton != null)
            {
                vehicle1SteeringButton.onClick.AddListener(() => OnSeatButtonClicked(0, false));
                UpdateButtonText(vehicle1SteeringButton, "Vehicle 1\n조향");
            }

            if (vehicle2ThrottleButton != null)
            {
                vehicle2ThrottleButton.onClick.AddListener(() => OnSeatButtonClicked(1, true));
                UpdateButtonText(vehicle2ThrottleButton, "Vehicle 2\n가속");
            }

            if (vehicle2SteeringButton != null)
            {
                vehicle2SteeringButton.onClick.AddListener(() => OnSeatButtonClicked(1, false));
                UpdateButtonText(vehicle2SteeringButton, "Vehicle 2\n조향");
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
                confirmButton.interactable = false;
            }
        }

        private void OnSeatButtonClicked(int vehicleIndex, bool isThrottle)
        {
            selectedVehicle = vehicleIndex;
            selectedIsThrottle = isThrottle;

            string vehicleName = $"Vehicle {vehicleIndex + 1}";
            string role = isThrottle ? "가속" : "조향";
            UpdateStatusText($"선택: {vehicleName} {role}");

            if (confirmButton != null)
            {
                confirmButton.interactable = true;
            }

            // Highlight selected button
            HighlightButton(vehicleIndex, isThrottle);
        }

        private void OnConfirmClicked()
        {
            if (selectedVehicle < 0 || hasConfirmed) return;

            hasConfirmed = true;
            int myActor = PhotonNetwork.LocalPlayer.ActorNumber;

            // Always send through MapVehicleSpawner's PhotonView
            var spawner = FindObjectOfType<MapVehicleSpawner>();
            if (spawner != null)
            {
                var spawnerPV = spawner.GetComponent<PhotonView>();
                if (spawnerPV != null)
                {
                    spawnerPV.RPC("RPC_RequestSeat", RpcTarget.MasterClient, myActor, selectedVehicle, selectedIsThrottle);
                    UpdateStatusText("요청 전송됨... 대기 중");
                    if (confirmButton != null) confirmButton.interactable = false;
                }
                else
                {
                    UpdateStatusText("오류: MapVehicleSpawner PhotonView 없음");
                    hasConfirmed = false;
                }
            }
            else
            {
                UpdateStatusText("오류: MapVehicleSpawner 없음");
                hasConfirmed = false;
            }
        }

        // 마스터가 게임 시작
        private void OnStartGameClicked()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (!AllFourUniqueSeatsFilled())
            {
                UpdateStatusText("모든 좌석이 채워지지 않았습니다.");
                return;
            }
            var spawner = FindObjectOfType<MapVehicleSpawner>();
            if (spawner != null)
            {
                var pv = spawner.GetComponent<PhotonView>();
                if (pv != null)
                {
                    pv.RPC("RPC_StartGame", RpcTarget.All);
                    UpdateStatusText("게임 시작!");
                    if (selectionPanel != null) selectionPanel.SetActive(false);
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
                startGameButton.interactable = AllFourUniqueSeatsFilled();
            }

            if (AllFourUniqueSeatsFilled())
            {
                UpdateStatusText("모든 좌석 준비 완료 - 마스터 시작 가능");
            }
        }

        private void ApplySeatColor(Button btn, int actorNumber, bool throttle)
        {
            if (btn == null) return;
            var colors = btn.colors;
            if (actorNumber == -1)
            {
                colors.normalColor = unoccupiedColor;
            }
            else
            {
                // 로컬 플레이어인지 여부
                if (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
                {
                    colors.normalColor = localHighlightColor;
                }
                else
                {
                    // 플레이어 리스트 순서 기반 색상
                    var ordered = PhotonNetwork.PlayerList; // 이미 ActorNumber 오름차순으로 가정
                    int idx = 0;
                    for (int i = 0; i < ordered.Length; i++)
                    {
                        if (ordered[i].ActorNumber == actorNumber)
                        {
                            idx = i; break;
                        }
                    }
                    colors.normalColor = playerColors[Mathf.Clamp(idx,0,playerColors.Length-1)];
                }
            }
            btn.colors = colors;
        }

        private bool AllFourUniqueSeatsFilled()
        {
            var managers = FindObjectsOfType<VehicleSeatManager>();
            System.Collections.Generic.HashSet<int> actors = new System.Collections.Generic.HashSet<int>();
            foreach (var m in managers)
            {
                var (t,s) = m.GetAssignedActors();
                if (m.name.Contains("Vehicle 1")) { if (t!=-1) actors.Add(t); if (s!=-1) actors.Add(s); }
                else if (m.name.Contains("Vehicle 2")) { if (t!=-1) actors.Add(t); if (s!=-1) actors.Add(s); }
            }
            if (actors.Count != 4) return false;
            // 각 좌석이 -1 아니고 중복 없음 확인
            return actors.Count == 4;
        }

        [PunRPC]
        void RPC_SeatRequestResult(bool success, string message)
        {
            OnSeatRequestResult(success, message);
        }

        public void OnSeatRequestResult(bool success, string message)
        {
            if (!success)
            {
                hasConfirmed = false; // 다시 선택 가능
                if (confirmButton != null) confirmButton.interactable = true;
            }
            UpdateStatusText(message);
        }

        public void HidePanel()
        {
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
            }
        }

        public override void OnLeftRoom()
        {
            hasConfirmed = false;
            selectedVehicle = -1;
        }
    }
}
