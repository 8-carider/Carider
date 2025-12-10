using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Ilumisoft.SkillDrive.Network
{
    public class NetworkManager : MonoBehaviourPunCallbacks
    {
        [SerializeField] private string gameVersion = "1.0";
        [SerializeField] private byte maxPlayersPerRoom = 4;

        private void Start()
        {
            // Photon 서버에 연결
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();
        }

        public override void OnConnectedToMaster()
        {
            // 로비에 참가
            PhotonNetwork.JoinLobby();
        }

        public override void OnJoinedLobby()
        {
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarning($"Photon 서버 연결 끊김: {cause}");
        }

        // 랜덤 룸에 참가 (없으면 생성)
        public void JoinOrCreateRoom()
        {
            if (!PhotonNetwork.IsConnected)
            {
                Debug.LogError("[NetworkManager] Photon에 연결되지 않았습니다!");
                return;
            }

            if (!PhotonNetwork.InLobby)
            {
                Debug.LogError("[NetworkManager] 로비에 들어가지 않았습니다!");
                return;
            }

            PhotonNetwork.JoinRandomRoom();
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            
            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = maxPlayersPerRoom,
                IsVisible = true,
                IsOpen = true
            };

            PhotonNetwork.CreateRoom(null, roomOptions);
        }

        public override void OnJoinedRoom()
        {
            // 모든 클라이언트를 Level Selection 씬으로 동기화 이동
            // PhotonNetwork.AutomaticallySyncScene 가 TRUE인지 확인 필요 (Project Settings or 코드에서 설정)
            if (!PhotonNetwork.AutomaticallySyncScene)
            {
                PhotonNetwork.AutomaticallySyncScene = true;
            }

            // Create PlayerSeatInput once for local player
            if (FindObjectOfType<PlayerSeatInput>() == null)
            {
                GameObject inputObj = new GameObject("PlayerSeatInput");
                inputObj.AddComponent<PlayerSeatInput>();
                DontDestroyOnLoad(inputObj);
            }

            // 마스터 클라이언트만 씬 로드 호출 (자동 동기화됨)
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel("Level Selection");
            }
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
        }
    }
}
