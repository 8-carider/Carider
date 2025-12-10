using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

namespace Ilumisoft.SkillDrive.Game
{
    /// <summary>
    /// 플레이어별 누적 수집 개수를 추적하는 싱글톤 매니저
    /// 레벨 전환 시에도 데이터 유지
    /// </summary>
    public class PlayerCollectableStats : MonoBehaviour
    {
        private static PlayerCollectableStats instance;
        public static PlayerCollectableStats Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("PlayerCollectableStats");
                    instance = go.AddComponent<PlayerCollectableStats>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        // 플레이어 ActorNumber별 누적 수집 개수
        private Dictionary<int, int> playerCollectedCounts = new Dictionary<int, int>();

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
        /// 특정 플레이어의 수집 개수 추가
        /// </summary>
        public void AddCollectableForPlayer(int actorNumber, int amount = 1)
        {
            if (!playerCollectedCounts.ContainsKey(actorNumber))
            {
                playerCollectedCounts[actorNumber] = 0;
            }

            playerCollectedCounts[actorNumber] += amount;
        }

        /// <summary>
        /// 특정 플레이어의 총 수집 개수 반환
        /// </summary>
        public int GetPlayerCollectedCount(int actorNumber)
        {
            if (playerCollectedCounts.ContainsKey(actorNumber))
            {
                return playerCollectedCounts[actorNumber];
            }
            return 0;
        }

        /// <summary>
        /// 로컬 플레이어의 총 수집 개수 반환
        /// </summary>
        public int GetLocalPlayerCollectedCount()
        {
            if (PhotonNetwork.LocalPlayer != null)
            {
                return GetPlayerCollectedCount(PhotonNetwork.LocalPlayer.ActorNumber);
            }
            return 0;
        }

        /// <summary>
        /// 모든 플레이어 통계 초기화 (새 게임 시작 시)
        /// </summary>
        public void ResetAllStats()
        {
            playerCollectedCounts.Clear();
        }
    }
}
