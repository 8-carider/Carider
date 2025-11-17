using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;

namespace Ilumisoft.SkillDrive.Game
{
    /// <summary>
    /// 전체 게임 세션 관리: 총 맵 개수, 현재 맵 진행도, 결과 표시
    /// </summary>
    public class GameSessionManager : MonoBehaviourPunCallbacks
    {
        private static GameSessionManager instance;
        public static GameSessionManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("GameSessionManager");
                    instance = go.AddComponent<GameSessionManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        // 게임 세션 설정 (CustomProperties에 저장)
        private const string PROP_TOTAL_MAPS = "TotalMaps";
        private const string PROP_PLAYED_MAPS = "PlayedMaps"; // 플레이한 맵 개수
        private const string PROP_SESSION_STARTED = "SessionStarted";

        public int TotalMaps
        {
            get
            {
                if (PhotonNetwork.CurrentRoom != null && 
                    PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_TOTAL_MAPS))
                {
                    return (int)PhotonNetwork.CurrentRoom.CustomProperties[PROP_TOTAL_MAPS];
                }
                return 0;
            }
        }

        public int PlayedMapsCount
        {
            get
            {
                if (PhotonNetwork.CurrentRoom != null && 
                    PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_PLAYED_MAPS))
                {
                    return (int)PhotonNetwork.CurrentRoom.CustomProperties[PROP_PLAYED_MAPS];
                }
                return 0;
            }
        }

        public bool IsSessionStarted
        {
            get
            {
                if (PhotonNetwork.CurrentRoom != null && 
                    PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_SESSION_STARTED))
                {
                    return (bool)PhotonNetwork.CurrentRoom.CustomProperties[PROP_SESSION_STARTED];
                }
                return false;
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 마스터가 게임 세션 시작 (총 맵 개수 설정)
        /// </summary>
        public void StartGameSession(int totalMaps)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[GameSessionManager] Only master can start game session!");
                return;
            }

            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { PROP_TOTAL_MAPS, totalMaps },
                { PROP_PLAYED_MAPS, 0 },
                { PROP_SESSION_STARTED, true }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            // 플레이어 통계 초기화
            PlayerCollectableStats.Instance.ResetAllStats();

            Debug.Log($"[GameSessionManager] Session started: {totalMaps} maps");
        }

        /// <summary>
        /// 현재 맵 완료 후 LevelSelect로 복귀 또는 최종 결과 표시 (마스터만 호출)
        /// </summary>
        public void CompleteCurrentMap()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[GameSessionManager] Only master can complete map!");
                return;
            }

            int newPlayedCount = PlayedMapsCount + 1;

            // 플레이한 맵 개수 업데이트
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { PROP_PLAYED_MAPS, newPlayedCount }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            Debug.Log($"[GameSessionManager] Map completed. Progress: {newPlayedCount}/{TotalMaps}");

            if (newPlayedCount >= TotalMaps)
            {
                // 모든 맵 완료 - 결과 화면 표시
                ShowFinalResults();
            }
            else
            {
                // LevelSelect 씬으로 복귀
                Debug.Log($"[GameSessionManager] Returning to LevelSelect. {TotalMaps - newPlayedCount} maps remaining.");
                PhotonNetwork.LoadLevel("Level Selection");
            }
        }

        /// <summary>
        /// 최종 결과 표시 및 우승자 결정
        /// </summary>
        private void ShowFinalResults()
        {
            Debug.Log("[GameSessionManager] All maps completed! Showing final results...");

            // 결과 화면 씬으로 이동
            PhotonNetwork.LoadLevel("ResultScene");
        }

        /// <summary>
        /// 최종 랭킹 계산
        /// </summary>
        public List<PlayerRankData> GetFinalRanking()
        {
            List<PlayerRankData> ranking = new List<PlayerRankData>();

            if (PhotonNetwork.CurrentRoom == null) return ranking;

            foreach (var player in PhotonNetwork.PlayerList)
            {
                int collected = PlayerCollectableStats.Instance.GetPlayerCollectedCount(player.ActorNumber);
                ranking.Add(new PlayerRankData
                {
                    ActorNumber = player.ActorNumber,
                    PlayerName = player.NickName,
                    TotalCollected = collected
                });
            }

            // 수집 개수 내림차순 정렬
            ranking = ranking.OrderByDescending(p => p.TotalCollected).ToList();

            return ranking;
        }

        /// <summary>
        /// 게임 세션 종료 및 초기화
        /// </summary>
        public void EndGameSession()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { PROP_SESSION_STARTED, false },
                { PROP_PLAYED_MAPS, 0 },
                { PROP_TOTAL_MAPS, 0 }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            Debug.Log("[GameSessionManager] Session ended");
        }
    }

    /// <summary>
    /// 플레이어 랭킹 데이터 구조
    /// </summary>
    [System.Serializable]
    public class PlayerRankData
    {
        public int ActorNumber;
        public string PlayerName;
        public int TotalCollected;
        public int Rank;
    }
}
