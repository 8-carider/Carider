using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;

namespace Ilumisoft.SkillDrive.Game
{
    /// <summary>
    /// Lobby 패널에서 Enter 버튼을 누른 후 표시되는 맵 개수/제한시간 선택 UI
    /// 마스터만 선택 가능, 선택 완료 후 LevelSelect 씬으로 이동
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class MapSelectionUI : MonoBehaviourPunCallbacks
    {
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private TMP_Dropdown mapCountDropdown;
        [SerializeField] private TMP_Dropdown timeLimitDropdown;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI instructionText;

        private void Start()
        {
            // 초기에는 숨김 상태, ShowMapSelection()으로 수동 표시
            HideMapSelection();
        }

        /// <summary>
        /// 외부(Lobby의 Enter 버튼 등)에서 호출하여 맵 선택 UI 표시
        /// </summary>
        public void ShowMapSelectionUI()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                ShowMapSelection();
            }
            else
            {
                // 마스터가 아닌 플레이어는 대기 메시지만 표시
                if (selectionPanel != null) selectionPanel.SetActive(true);
                if (instructionText != null) instructionText.text = "Waiting for master to select...";
                
                // Dropdown과 버튼 비활성화
                if (mapCountDropdown != null) mapCountDropdown.gameObject.SetActive(false);
                if (timeLimitDropdown != null) timeLimitDropdown.gameObject.SetActive(false);
                if (confirmButton != null) confirmButton.gameObject.SetActive(false);
            }
        }

        public override void OnJoinedRoom()
        {
            // 방 참가 시 아무것도 안 함 (Enter 버튼 후에만 표시)
        }

        public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
        {
            // 마스터 변경 시 UI 갱신
            if (selectionPanel != null && selectionPanel.activeSelf)
            {
                ShowMapSelectionUI();
            }
        }

        private void ShowMapSelection()
        {
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(true);
            }

            if (mapCountDropdown != null)
            {
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

            if (timeLimitDropdown != null)
            {
                // 제한시간 선택 옵션
                timeLimitDropdown.ClearOptions();
                List<string> timeOptions = new List<string>
                {
                    "1 Minute",
                    "2 Minutes",
                    "3 Minutes",
                    "5 Minutes",
                    "10 Minutes"
                };
                timeLimitDropdown.AddOptions(timeOptions);
                timeLimitDropdown.value = 1; // 기본값: 2 Minutes
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmMapCount);
            }

            if (instructionText != null)
            {
                instructionText.text = "Select maps and time limit";
            }
        }

        private void HideMapSelection()
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
            
            // 제한시간 계산 (초 단위)
            float timeLimit = GetSelectedTimeLimit();
            
            Debug.Log($"[MapSelectionUI] Master selected {selectedMapCount} maps with {timeLimit}s time limit");
            
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

            // LevelSelect 씬으로 이동
            PhotonNetwork.LoadLevel("LevelSelect");
        }

        private float GetSelectedTimeLimit()
        {
            if (timeLimitDropdown == null) return 120f; // 기본 2분

            switch (timeLimitDropdown.value)
            {
                case 0: return 60f;   // 1분
                case 1: return 120f;  // 2분
                case 2: return 180f;  // 3분
                case 3: return 300f;  // 5분
                case 4: return 600f;  // 10분
                default: return 120f;
            }
        }
    }
}
