using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;

namespace Ilumisoft.SkillDrive.Game
{
    /// <summary>
    /// Lobby 패널에서 Enter 버튼을 누른 후 표시되는 맵 개수 선택 UI
    /// 마스터만 선택 가능, 선택 완료 후 LevelSelection 씬으로 이동
    /// </summary>
    public class MapSelectionUI : MonoBehaviourPunCallbacks
    {
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private TMP_Dropdown mapCountDropdown;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI instructionText;

        private void Start()
        {
            // 초기에는 숨김 상태
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
            }

            // Confirm 버튼 연결
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmMapCount);
            }
        }

        /// <summary>
        /// 외부(Lobby의 Enter 버튼 등)에서 호출하여 맵 선택 UI 표시
        /// </summary>
        public void ShowMapSelectionUI()
        {
            Debug.Log("[MapSelectionUI] ShowMapSelectionUI called");
            
            if (selectionPanel == null)
            {
                Debug.LogError("[MapSelectionUI] Selection Panel is not assigned!");
                return;
            }

            Debug.Log($"[MapSelectionUI] Activating panel: {selectionPanel.name}");
            // 패널 활성화
            selectionPanel.SetActive(true);
            Debug.Log($"[MapSelectionUI] Panel active state: {selectionPanel.activeSelf}");

            if (PhotonNetwork.IsMasterClient)
            {
                // 마스터: 드롭다운 설정
                SetupMapCountDropdown();
                
                if (instructionText != null)
                {
                    instructionText.text = "Select total number of maps to play";
                }
                
                if (mapCountDropdown != null) mapCountDropdown.gameObject.SetActive(true);
                if (confirmButton != null) confirmButton.gameObject.SetActive(true);
            }
            else
            {
                // 마스터 아님: 대기 메시지
                if (instructionText != null)
                {
                    instructionText.text = "Waiting for master to select...";
                }
                
                if (mapCountDropdown != null) mapCountDropdown.gameObject.SetActive(false);
                if (confirmButton != null) confirmButton.gameObject.SetActive(false);
            }
        }

        public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
        {
            // 마스터 변경 시 UI 갱신
            if (selectionPanel != null && selectionPanel.activeSelf)
            {
                ShowMapSelectionUI();
            }
        }

        private void SetupMapCountDropdown()
        {
            if (mapCountDropdown == null) return;

            // 1~11개 맵 선택 옵션
            mapCountDropdown.ClearOptions();
            List<string> options = new List<string>();
            for (int i = 1; i <= 11; i++)
            {
                options.Add($"{i} Maps");
            }
            mapCountDropdown.AddOptions(options);
            mapCountDropdown.value = 2; // 기본값: 3 Maps
        }

        public void HideMapSelection()
        {
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
            }
        }

        private void OnConfirmMapCount()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            int selectedMapCount = mapCountDropdown.value + 1; // dropdown index 0 = 1 map
            
            // 각 맵마다 기본 제한시간 2분 설정
            float timeLimit = 120f;
            
            Debug.Log($"[MapSelectionUI] Master selected {selectedMapCount} maps with {timeLimit}s time limit per map");
            
            // 게임 세션 시작
            GameSessionManager.Instance.StartGameSession(selectedMapCount);
            
            // 제한시간 저장 (CustomProperties)
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { "TimeLimit", timeLimit }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            // UI 숨기기
            HideMapSelection();

            // LevelSelection 씬으로 이동 (씬 이름에 공백 있음)
            PhotonNetwork.LoadLevel("Level Selection");
        }
    }
}
