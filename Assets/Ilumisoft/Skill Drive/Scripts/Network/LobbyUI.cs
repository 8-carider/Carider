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

        private void Awake()
        {
            Debug.Log("[LobbyUI] Awake() 호출됨 - 스크립트 활성화됨!");
        }

        private void Start()
        {
            Debug.Log("[LobbyUI] Start() 호출됨");
            
            networkManager = FindAnyObjectByType<NetworkManager>();
            Debug.Log($"[LobbyUI] NetworkManager 찾기 결과: {(networkManager != null ? "성공" : "실패")}");
            
            if (joinButton != null)
            {
                Debug.Log("[LobbyUI] Join 버튼 리스너 등록");
                joinButton.onClick.AddListener(OnJoinButtonClicked);
            }
            else
            {
                Debug.LogError("[LobbyUI] joinButton이 null입니다! Inspector에서 할당하세요.");
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
            // 테스트용: J 키 누르면 강제로 Join 실행
            #if UNITY_EDITOR
            if (UnityEngine.Input.GetKeyDown(KeyCode.J))
            {
                Debug.Log("[LobbyUI] J 키로 강제 Join 호출!");
                OnJoinButtonClicked();
            }
            #endif

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
                if (joinButton != null)
                    joinButton.interactable = false;
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
            Debug.Log("[LobbyUI] Join 버튼 클릭됨!");
            
            if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
            {
                PhotonNetwork.NickName = playerNameInput.text;
                Debug.Log($"[LobbyUI] 플레이어 이름 설정: {playerNameInput.text}");
            }

            UpdateStatusText("게임 참가 중...");
            
            if (networkManager != null)
            {
                Debug.Log("[LobbyUI] NetworkManager.JoinOrCreateRoom() 호출");
                networkManager.JoinOrCreateRoom();
            }
            else
            {
                Debug.LogError("[LobbyUI] NetworkManager를 찾을 수 없습니다!");
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
