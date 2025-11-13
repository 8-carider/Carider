using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Text;

namespace Ilumisoft.SkillDrive.Game
{
    /// <summary>
    /// 화면 오른쪽 위에 모든 플레이어의 누적 수집 개수를 표시
    /// </summary>
    public class PlayerStatsUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private float updateInterval = 0.5f; // 0.5초마다 업데이트

        private float updateTimer = 0f;

        private void Awake()
        {
            if (statsText == null)
            {
                // Auto-find TextMeshProUGUI if not assigned
                statsText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        private void Update()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                UpdateStatsDisplay();
            }
        }

        private void UpdateStatsDisplay()
        {
            if (statsText == null || !PhotonNetwork.IsConnected)
            {
                return;
            }

            // 모든 플레이어의 수집 개수를 표시
            StringBuilder sb = new StringBuilder();
            
            // 현재 맵 진행 상황 표시
            if (GameSessionManager.Instance.IsSessionStarted)
            {
                int playedMaps = GameSessionManager.Instance.PlayedMapsCount + 1; // 현재 플레이 중인 맵 포함
                int totalMaps = GameSessionManager.Instance.TotalMaps;
                sb.AppendLine($"Playing {playedMaps}/{totalMaps}");
                sb.AppendLine("==================");
            }
            else
            {
                sb.AppendLine("=== Collected Stats ===");
            }

            // Photon 룸에 있는 모든 플레이어 순회
            var players = PhotonNetwork.PlayerList;
            foreach (var player in players)
            {
                int actorNumber = player.ActorNumber;
                int collected = PlayerCollectableStats.Instance.GetPlayerCollectedCount(actorNumber);
                
                // 로컬 플레이어는 강조 표시
                if (PhotonNetwork.LocalPlayer != null && actorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    sb.AppendLine($"<color=yellow>P{actorNumber}: {collected}</color>");
                }
                else
                {
                    sb.AppendLine($"P{actorNumber}: {collected}");
                }
            }

            statsText.text = sb.ToString();
        }
    }
}
