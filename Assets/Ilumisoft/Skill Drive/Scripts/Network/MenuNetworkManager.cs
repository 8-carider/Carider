using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

namespace Ilumisoft.SkillDrive.Network
{
    public class MenuNetworkManager : MonoBehaviourPunCallbacks
    {
        [Header("UI References")]
        [SerializeField] private Button startButton;
        [SerializeField] private LobbyPanelManager lobbyPanelManager;

        [Header("Network Settings")]
        [SerializeField] private string gameVersion = "1.0";
        [SerializeField] private byte maxPlayersPerRoom = 4;
        [Tooltip("고정 지역 서버 (asia, kr, jp 등). 비워두면 Best Region 자동 선택")]
        [SerializeField] private string fixedRegion = "asia";
        [SerializeField] private string roomNamePrefix = "GameRoom";

        private bool isConnecting = false;
        private float reconnectTimer = 0f;
        private int reconnectAttempts = 0;
        private const int MAX_RECONNECT_ATTEMPTS = 3;

        private void Start()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartButtonClicked);
            }
            
            // 네트워크 전송 빈도 설정 (반응성 향상)
            PhotonNetwork.SendRate = 30; // 초당 30회 전송
            PhotonNetwork.SerializationRate = 20; // 초당 20회 직렬화
            
            // FixedUpdate 빈도 증가 (물리 업데이트 빈도) - 레이싱 게임 최적화
            Time.fixedDeltaTime = 1f / 100f; // 50Hz → 100Hz (매우 부드러운 물리)
            
            Debug.Log($"[MenuNetworkManager] ⚡ High-Performance Mode: SendRate={PhotonNetwork.SendRate}, SerializationRate={PhotonNetwork.SerializationRate}, FixedDeltaTime={Time.fixedDeltaTime}ms ({1f/Time.fixedDeltaTime}Hz)");
        }

        private void OnStartButtonClicked()
        {
            if (isConnecting) return;

            if (!PhotonNetwork.IsConnected || PhotonNetwork.NetworkClientState == ClientState.Disconnected)
            {
                isConnecting = true;
                PhotonNetwork.GameVersion = gameVersion;
                PhotonNetwork.AutomaticallySyncScene = true;
                
                if (!string.IsNullOrEmpty(fixedRegion))
                {
                    PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = fixedRegion;
                    Debug.Log($"[MenuNetworkManager] Connecting to region: {fixedRegion}");
                }
                
                PhotonNetwork.ConnectUsingSettings();
                Debug.Log($"[MenuNetworkManager] Connecting... (Attempt {reconnectAttempts + 1})");
                return;
            }
            ShowLobby();
        }

        public override void OnConnectedToMaster()
        {
            Debug.Log($"[MenuNetworkManager] Connected! Region: {PhotonNetwork.CloudRegion}");
            reconnectAttempts = 0;
            
            string targetRoomName = roomNamePrefix + "_1";
            PhotonNetwork.JoinRoom(targetRoomName);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogWarning($"[MenuNetworkManager] Join failed: {message}");
            
            string roomName = roomNamePrefix + "_1";
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
            Debug.Log($"[MenuNetworkManager] Joined: {PhotonNetwork.CurrentRoom.Name} ({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})");
            ShowLobby();
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[MenuNetworkManager] Create failed: {message}");
            isConnecting = false;
            PhotonNetwork.JoinRandomRoom();
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarning($"[MenuNetworkManager] Disconnected: {cause}");
            isConnecting = false;
            
            if (reconnectAttempts < MAX_RECONNECT_ATTEMPTS && cause != DisconnectCause.DisconnectByClientLogic)
            {
                reconnectAttempts++;
                reconnectTimer = 2f;
                Debug.Log($"[MenuNetworkManager] Reconnecting in 2s... ({reconnectAttempts}/{MAX_RECONNECT_ATTEMPTS})");
            }
        }
        
        private void Update()
        {
            if (reconnectTimer > 0)
            {
                reconnectTimer -= Time.deltaTime;
                if (reconnectTimer <= 0)
                {
                    OnStartButtonClicked();
                }
            }
        }

        private void ShowLobby()
        {
            if (lobbyPanelManager != null)
            {
                lobbyPanelManager.ShowLobbyPanel();
            }
        }
    }
}
