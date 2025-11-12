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

        [Header("Settings")]
        [SerializeField] private bool showInLevelSelection = true;

        private int selectedVehicle = -1; // 0 or 1
        private bool selectedIsThrottle = true; // true = throttle, false = steering
        private bool hasConfirmed = false;

        private void Start()
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            {
                if (selectionPanel != null) selectionPanel.SetActive(false);
                return;
            }

            SetupButtons();
            UpdateStatusText("차량과 역할을 선택하세요");
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

            // Send seat request to master
            if (PhotonNetwork.IsMasterClient)
            {
                // Master approves own selection immediately
                ApplySeatAssignment(myActor, selectedVehicle, selectedIsThrottle);
            }
            else
            {
                // Send RPC to master for approval
                PhotonView pv = PhotonView.Get(this);
                if (pv == null)
                {
                    // Fallback: find any PhotonView in scene to route RPC
                    var spawner = FindObjectOfType<MapVehicleSpawner>();
                    if (spawner != null)
                    {
                        var spawnerPV = spawner.GetComponent<PhotonView>();
                        if (spawnerPV != null)
                        {
                            spawnerPV.RPC("RPC_RequestSeat", RpcTarget.MasterClient, myActor, selectedVehicle, selectedIsThrottle);
                        }
                    }
                }
                else
                {
                    pv.RPC("RPC_RequestSeat", RpcTarget.MasterClient, myActor, selectedVehicle, selectedIsThrottle);
                }
            }

            UpdateStatusText("요청 전송됨... 대기 중");
            if (confirmButton != null) confirmButton.interactable = false;
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
