using UnityEngine;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;

namespace Ilumisoft.SkillDrive.Game
{
    public class GameResultUI : MonoBehaviour
    {
        [Header("단상 위치")]
        public Transform goldPos;
        public Transform silverPos;
        public Transform bronzePos;

        [Header("단상 위에 표시할 텍스트 프리팹 (3D TextMeshPro)")]
        public GameObject resultTextPrefab;

        private void Start()
        {
            DisplayResults();
        }

        private void DisplayResults()
        {
            // 최종 랭킹 계산
            List<PlayerRankData> ranking = GameSessionManager.Instance.GetFinalRanking();

            // 순위 번호 붙이기
            for (int i = 0; i < ranking.Count; i++)
                ranking[i].Rank = i + 1;

            // 단상 위에 텍스트 생성
            SpawnTextOnPodiums(ranking);

            Debug.Log("[GameResultUI] Podium results displayed");
        }

        private void SpawnTextOnPodiums(List<PlayerRankData> ranking)
        {
            if (resultTextPrefab == null) return;

            if (ranking.Count > 0 && goldPos != null)
                CreateText(ranking[0], goldPos);

            if (ranking.Count > 1 && silverPos != null)
                CreateText(ranking[1], silverPos);

            if (ranking.Count > 2 && bronzePos != null)
                CreateText(ranking[2], bronzePos);
        }

        private void CreateText(PlayerRankData data, Transform podium)
        {
            float podiumHeight = podium.localScale.y;
            Vector3 spawnPos = podium.position + new Vector3(0, podiumHeight + 0.1f, 0);

            GameObject go = Instantiate(resultTextPrefab, spawnPos, Quaternion.identity);
            TextMeshPro tmp = go.GetComponent<TextMeshPro>();

            tmp.text = $"P{data.ActorNumber}\n{data.TotalCollected}";
            tmp.fontSize = 4;
            tmp.alignment = TextAlignmentOptions.Center;

            // 카메라를 바라보게 회전
            go.transform.LookAt(Camera.main.transform);
            go.transform.Rotate(0, 180, 0);
        }
    }
}
