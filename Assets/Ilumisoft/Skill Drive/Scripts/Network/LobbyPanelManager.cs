using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

namespace Ilumisoft.SkillDrive.Network
{
    /// <summary>
    /// Menu 씬의 Lobby 패널 관리
    /// Start 버튼 → 네트워크 접속 및 방 생성/참가 → 플레이어 대기 UI 표시 → Enter → 맵 선택
    /// </summary>
    public class LobbyPanelManager : MonoBehaviourPunCallbacks
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private TextMeshProUGUI playerListText;
        [SerializeField] private Button enterButton;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("References")]
        [SerializeField] private Game.MapSelectionUI mapSelectionUI;

        private void Start()
        {
            if (enterButton != null)
            {
                enterButton.onClick.AddListener(OnEnterClicked);
                enterButton.interactable = false; // 초기에는 비활성화
            }

            UpdatePlayerList();
        }

        /// <summary>
        /// 외부에서 호출 (Menu의 Start 버튼 등)하여 Lobby 패널 표시
        /// </summary>
        public void ShowLobbyPanel()
        {
            if (lobbyPanel != null)
            {
                lobbyPanel.SetActive(true);
            }
            UpdatePlayerList();
            UpdateEnterButton();
        }

        public void HideLobbyPanel()
        {
            if (lobbyPanel != null)
            {
                lobbyPanel.SetActive(false);
            }
        }

        public override void OnJoinedRoom()
        {
            UpdatePlayerList();
            UpdateEnterButton();
            UpdateStatusText("Room joined successfully");
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            UpdatePlayerList();
            UpdateEnterButton();
            UpdateStatusText($"{newPlayer.NickName} joined");
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            UpdatePlayerList();
            UpdateEnterButton();
            UpdateStatusText($"{otherPlayer.NickName} left");
        }

        private void UpdatePlayerList()
        {
            if (playerListText == null) return;

            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            {
                playerListText.text = "Not connected";
                return;
            }

            var players = PhotonNetwork.PlayerList;
            string text = $"Players ({players.Length}/4):\n";
            
            foreach (var player in players)
            {
                string role = player.IsMasterClient ? " (Master)" : "";
                text += $"- {player.NickName}{role}\n";
            }

            playerListText.text = text;
        }

        private void UpdateEnterButton()
        {
            if (enterButton == null) return;

            // 2~4명이 접속했을 때만 활성화
            int playerCount = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.PlayerCount : 0;
            enterButton.interactable = PhotonNetwork.IsMasterClient && playerCount >= 2 && playerCount <= 4;
        }

        private void UpdateStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private void OnEnterClicked()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                UpdateStatusText("Only master can start");
                return;
            }

            int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
            if (playerCount < 2 || playerCount > 4)
            {
                UpdateStatusText("Need 2-4 players");
                return;
            }

            // Lobby 패널 숨기고 맵 선택 UI 표시
            HideLobbyPanel();

            if (mapSelectionUI != null)
            {
                mapSelectionUI.ShowMapSelectionUI();
            }
            else
            {
                Debug.LogError("[LobbyPanelManager] MapSelectionUI reference not set!");
            }
        }
    }
}
