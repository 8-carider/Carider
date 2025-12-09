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
                return;
            }

            // PhotonNetwork가 연결되어 있지 않거나, Disconnected 상태면 항상 재접속 시도
            if (!PhotonNetwork.IsConnected || PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.Disconnected)
            {
                isConnecting = true;
                PhotonNetwork.GameVersion = gameVersion;
                PhotonNetwork.AutomaticallySyncScene = true;
                PhotonNetwork.ConnectUsingSettings();
                return;
            }
            ShowLobby();
        }

        public override void OnConnectedToMaster()
        {
            // 열려있는 방 중에 게임이 시작되지 않은 방 찾기 (IsOpen=true)
            PhotonNetwork.JoinRandomRoom();
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
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
