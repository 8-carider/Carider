using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace Ilumisoft.SkillDrive.Game
{
    public class GameResultUI : MonoBehaviour
    {
        [Header("플레이어 점수 텍스트 생성 위치 (1P~4P)")]
        public Transform[] textPositions;

        [Header("점수 표시용 TextMeshPro 프리팹")]
        public GameObject resultTextPrefab;

        [Header("깃발 생성 시스템")]
        public FlagSpawner flagSpawner;

        private void Start()
        {
            DisplayResults();
        }

        private void DisplayResults()
        {
            // 게임 최종 점수 가져오기
            List<PlayerRankData> ranking = GameSessionManager.Instance.GetFinalRanking();

            // rank 번호 정리
            for (int i = 0; i < ranking.Count; i++)
                ranking[i].Rank = i + 1;

            // 결과 UI 표시
            SpawnResultTexts(ranking);

            // 깃발 스폰
            if (flagSpawner != null)
                flagSpawner.SpawnFlags(ranking);
        }

        private void SpawnResultTexts(List<PlayerRankData> ranking)
        {
            if (resultTextPrefab == null || textPositions.Length == 0)
            {
                Debug.LogWarning("[GameResultUI] 텍스트 위치 또는 프리팹이 배정되지 않았습니다.");
                return;
            }

            for (int i = 0; i < ranking.Count && i < textPositions.Length; i++)
            {
                CreateResultText(ranking[i], textPositions[i]);
            }
        }

        private void CreateResultText(PlayerRankData data, Transform position)
        {
            Vector3 spawnPos = position.position + new Vector3(0, 0.5f, 0);

            GameObject uiObj = Instantiate(resultTextPrefab, spawnPos, Quaternion.identity);

            TextMeshPro tmp = uiObj.GetComponent<TextMeshPro>();
            tmp.text = $"P{data.ActorNumber}\n{data.TotalCollected}";
            tmp.fontSize = 4;
            tmp.alignment = TextAlignmentOptions.Center;

            // 카메라 방향 맞추기
            uiObj.transform.LookAt(Camera.main.transform);
            uiObj.transform.Rotate(0, 180, 0);
        }
    }
}
