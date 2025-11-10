using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

namespace Ilumisoft.SkillDrive.Network
{
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private Button joinButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject lobbyPanel;

        private NetworkManager networkManager;

        private void Start()
        {
            networkManager = FindAnyObjectByType<NetworkManager>();
            
            if (joinButton != null)
            {
                joinButton.onClick.AddListener(OnJoinButtonClicked);
            }

            // 기본 플레이어 이름 설정
            if (playerNameInput != null && string.IsNullOrEmpty(playerNameInput.text))
            {
                playerNameInput.text = "Player" + Random.Range(1000, 9999);
            }

            UpdateStatusText("연결 대기 중...");
        }

        private void Update()
        {
            // 연결 상태 업데이트
            if (PhotonNetwork.IsConnected && PhotonNetwork.InLobby)
            {
                UpdateStatusText("준비 완료! 게임 참가 가능");
                if (joinButton != null)
                    joinButton.interactable = true;
            }
            else if (PhotonNetwork.IsConnected)
            {
                UpdateStatusText("로비 참가 중...");
            }
            else
            {
                UpdateStatusText("서버 연결 중...");
                if (joinButton != null)
                    joinButton.interactable = false;
            }

            // 게임에 참가하면 로비 UI 숨김
            if (PhotonNetwork.InRoom && lobbyPanel != null)
            {
                lobbyPanel.SetActive(false);
            }
        }

        private void OnJoinButtonClicked()
        {
            if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
            {
                PhotonNetwork.NickName = playerNameInput.text;
            }

            UpdateStatusText("게임 참가 중...");
            
            if (networkManager != null)
            {
                networkManager.JoinOrCreateRoom();
            }
        }

        private void UpdateStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
    }
}
