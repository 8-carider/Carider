using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace Ilumisoft.SkillDrive.Game
{
    public class GameResultUI : MonoBehaviour
    {
        public Transform[] textPositions;
        public GameObject resultTextPrefab;
        public FlagSpawner flagSpawner;
        public AutoCameraAdjust cameraAdjuster;

        private void Start()
        {
            List<PlayerRankData> ranking = GameSessionManager.Instance.GetFinalRanking();

            SpawnTexts(ranking);

            if (flagSpawner != null)
                flagSpawner.SpawnFlags(ranking);

            // 🔥 카메라 이동 함수 호출 (이제 이름 일치함)
            if (cameraAdjuster != null && flagSpawner != null)
                cameraAdjuster.AdjustCameraToHeight(flagSpawner.HighestFlagHeight * 1f);
        }

        private void SpawnTexts(List<PlayerRankData> ranking)
        {
            for (int i = 0; i < ranking.Count && i < textPositions.Length; i++)
            {
                Vector3 pos = textPositions[i].position + new Vector3(0, 1.2f, 0);

                GameObject obj = Instantiate(resultTextPrefab, pos, Quaternion.identity);
                TextMeshPro tmp = obj.GetComponent<TextMeshPro>();

                tmp.text = $"P{i + 1}\n{ranking[i].TotalCollected}";
                tmp.fontSize = 10;
                tmp.alignment = TextAlignmentOptions.Center;

                obj.transform.LookAt(Camera.main.transform);
                obj.transform.Rotate(0, 180, 0);
            }
        }
    }
}
