using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

namespace Ilumisoft.SkillDrive.Network
{
    /// <summary>
    /// Menu 씬의 Start 버튼 클릭 시 Photon 네트워크 접속 및 방 생성/참가 처리
    /// </summary>
    public class MenuNetworkManager : MonoBehaviourPunCallbacks
    {
        [Header("UI References")]
        [SerializeField] private Button startButton;
        [SerializeField] private LobbyPanelManager lobbyPanelManager;

        [Header("Network Settings")]
        [SerializeField] private string gameVersion = "1.0";
        [SerializeField] private byte maxPlayersPerRoom = 4;

        private bool isConnecting = false;

        private void Start()
        {
            // Start 버튼 이벤트 연결
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartButtonClicked);
            }
        }

        private void OnStartButtonClicked()
        {
            if (isConnecting)
            {
                Debug.Log("[MenuNetworkManager] Already connecting...");
                return;
            }

            if (PhotonNetwork.IsConnected)
            {
                Debug.Log("[MenuNetworkManager] Already connected, showing lobby");
                ShowLobby();
                return;
            }

            // Photon 네트워크 접속 시작
            isConnecting = true;
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.ConnectUsingSettings();

            Debug.Log("[MenuNetworkManager] Connecting to Photon...");
        }

        public override void OnConnectedToMaster()
        {
            Debug.Log("[MenuNetworkManager] Connected to Master Server");

            // 자동으로 방 생성 또는 참가 시도
            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = maxPlayersPerRoom,
                IsVisible = true,
                IsOpen = true
            };

            // 랜덤 방 참가 시도, 없으면 새로 생성
            PhotonNetwork.JoinRandomRoom();
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log($"[MenuNetworkManager] No room available, creating new room...");

            // 방이 없으면 새로 생성
            string roomName = "Room_" + Random.Range(1000, 9999);
            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = maxPlayersPerRoom,
                IsVisible = true,
                IsOpen = true
            };

            PhotonNetwork.CreateRoom(roomName, roomOptions);
        }

        public override void OnJoinedRoom()
        {
            Debug.Log($"[MenuNetworkManager] Joined room: {PhotonNetwork.CurrentRoom.Name}");
            isConnecting = false;

            // LobbyPanel 표시
            ShowLobby();
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[MenuNetworkManager] Failed to create room: {message}");
            isConnecting = false;
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarning($"[MenuNetworkManager] Disconnected: {cause}");
            isConnecting = false;
        }

        private void ShowLobby()
        {
            if (lobbyPanelManager != null)
            {
                lobbyPanelManager.ShowLobbyPanel();
            }
            else
            {
                Debug.LogError("[MenuNetworkManager] LobbyPanelManager reference not set!");
            }
        }
    }
}
