using UnityEngine;
using TMPro;
using Photon.Pun;

namespace Ilumisoft.SkillDrive.Game
{
    /// <summary>
    /// LevelSelect 씬에서 세션 진행 상황 표시
    /// "맵 선택: 2/5 완료" 같은 정보 표시
    /// </summary>
    public class SessionProgressUI : MonoBehaviourPunCallbacks
    {
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI instructionText;


        private void Start()
        {
            UpdateProgressDisplay();
        }

        // Photon 룸 프로퍼티 변경 시 자동 갱신
        public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            UpdateProgressDisplay();
        }

        private void UpdateProgressDisplay()
        {
            if (!PhotonNetwork.IsConnected || !GameSessionManager.Instance.IsSessionStarted)
            {
                // 세션이 시작되지 않았으면 숨김
                if (progressText != null) progressText.gameObject.SetActive(false);
                if (instructionText != null) instructionText.gameObject.SetActive(false);
                return;
            }

            int played = GameSessionManager.Instance.PlayedMapsCount;
            int total = GameSessionManager.Instance.TotalMaps;

            if (progressText != null)
            {
                progressText.text = $"Session Progress: {played}/{total} maps completed";
                progressText.gameObject.SetActive(true);
            }

            if (instructionText != null)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    instructionText.text = "Select next level to play";
                }
                else
                {
                    instructionText.text = "Waiting for master to select level...";
                }
                instructionText.gameObject.SetActive(true);
            }
        }
    }
}
