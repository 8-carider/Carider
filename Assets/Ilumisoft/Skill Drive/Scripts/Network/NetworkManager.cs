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
            
            Debug.Log("Photon 서버에 연결 중...");
        }

        public override void OnConnectedToMaster()
        {
            Debug.Log("Photon 서버에 연결됨!");
            // 로비에 참가
            PhotonNetwork.JoinLobby();
        }

        public override void OnJoinedLobby()
        {
            Debug.Log("로비에 참가함!");
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarning($"Photon 서버 연결 끊김: {cause}");
        }

        // 랜덤 룸에 참가 (없으면 생성)
        public void JoinOrCreateRoom()
        {
            Debug.Log("[NetworkManager] JoinOrCreateRoom 호출됨");
            
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

            Debug.Log("[NetworkManager] 랜덤 룸 참가 시도...");
            PhotonNetwork.JoinRandomRoom();
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log("랜덤 룸 참가 실패, 새 룸 생성 중...");
            
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
            Debug.Log($"룸에 참가함! 플레이어 수: {PhotonNetwork.CurrentRoom.PlayerCount}");

            // 모든 클라이언트를 Level Selection 씬으로 동기화 이동
            // PhotonNetwork.AutomaticallySyncScene 가 TRUE인지 확인 필요 (Project Settings or 코드에서 설정)
            if (!PhotonNetwork.AutomaticallySyncScene)
            {
                PhotonNetwork.AutomaticallySyncScene = true;
            }

            // 마스터 클라이언트만 씬 로드 호출 (자동 동기화됨)
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("마스터 클라이언트 - Level Selection 씬 로드");
                PhotonNetwork.LoadLevel("Level Selection");
            }
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"새 플레이어 참가: {newPlayer.NickName}");
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"플레이어 퇴장: {otherPlayer.NickName}");
        }
    }
}
