using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;
using System.Text;

namespace Ilumisoft.SkillDrive.Game
{
    /// <summary>
    /// 모든 맵 완료 후 최종 결과 및 랭킹 표시
    /// </summary>
    public class GameResultUI : MonoBehaviour
    {
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI rankingText;
        [SerializeField] private TextMeshProUGUI winnerText;
        [SerializeField] private Button returnToLobbyButton;

        private void Start()
        {
            DisplayResults();

            if (returnToLobbyButton != null)
            {
                returnToLobbyButton.onClick.AddListener(OnReturnToLobby);
            }
        }

        private void DisplayResults()
        {
            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
            }

            // 최종 랭킹 가져오기
            List<PlayerRankData> ranking = GameSessionManager.Instance.GetFinalRanking();

            // 순위 설정
            for (int i = 0; i < ranking.Count; i++)
            {
                ranking[i].Rank = i + 1;
            }

            // 타이틀 표시
            if (titleText != null)
            {
                int totalMaps = GameSessionManager.Instance.TotalMaps;
                titleText.text = $"=== GAME COMPLETE ===\n{totalMaps} Maps Finished";
            }

            // 우승자 표시
            if (winnerText != null && ranking.Count > 0)
            {
                var winner = ranking[0];
                winnerText.text = $"<size=48><color=yellow>🏆 WINNER 🏆</color></size>\n" +
                                  $"<size=36>Player {winner.ActorNumber}</size>\n" +
                                  $"<size=28>Collected: {winner.TotalCollected}</size>";
            }

            // 전체 랭킹 표시
            if (rankingText != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("=== FINAL RANKING ===\n");

                foreach (var player in ranking)
                {
                    string medalIcon = GetMedalIcon(player.Rank);
                    string colorTag = GetRankColor(player.Rank);

                    sb.AppendLine($"<color={colorTag}>{medalIcon} #{player.Rank} - P{player.ActorNumber}: {player.TotalCollected} collected</color>");
                }

                rankingText.text = sb.ToString();
            }

            Debug.Log("[GameResultUI] Final results displayed");
        }

        private string GetMedalIcon(int rank)
        {
            switch (rank)
            {
                case 1: return "🥇";
                case 2: return "🥈";
                case 3: return "🥉";
                default: return "  ";
            }
        }

        private string GetRankColor(int rank)
        {
            switch (rank)
            {
                case 1: return "#FFD700"; // Gold
                case 2: return "#C0C0C0"; // Silver
                case 3: return "#CD7F32"; // Bronze
                default: return "white";
            }
        }

        private void OnReturnToLobby()
        {
            // 게임 세션 종료
            GameSessionManager.Instance.EndGameSession();

            // Menu 씬으로 복귀 (빌드 인덱스 0 또는 "Menu")
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel(0); // 또는 "Menu"
            }
        }
    }
}
