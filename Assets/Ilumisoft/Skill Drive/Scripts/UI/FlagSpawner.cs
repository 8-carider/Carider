using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Ilumisoft.SkillDrive.Game;

public class FlagSpawner : MonoBehaviour
{
    [Header("깃발 프리팹 (Flag 1)")]
    public GameObject flagPrefab;

    [Header("깃발 출발 위치 (플레이어 순서 유지)")]
    public Transform[] spawnPositions;

    [Header("높이 배율")]
    public float heightMultiplier = 0.5f;

    // 👉 카메라가 참고해야 할 최종 높이
    public float HighestFlagHeight { get; private set; } = 1f;

    private readonly Color[] playerColors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.yellow,
        new Color(0.2f, 1f, 0.2f)
    };

    public void SpawnFlags(List<PlayerRankData> ranking)
    {
        HighestFlagHeight = 1f;

        for (int i = 0; i < ranking.Count && i < spawnPositions.Length; i++)
        {
            float visualHeight = SpawnSingleFlag(ranking[i], spawnPositions[i], i);

            if (visualHeight > HighestFlagHeight)
                HighestFlagHeight = visualHeight;
        }

        Debug.Log($"[FlagSpawner] Highest visual height: {HighestFlagHeight}");
    }

    private float SpawnSingleFlag(PlayerRankData playerData, Transform spawnPos, int playerIndex)
    {
        GameObject flag = Instantiate(flagPrefab, spawnPos.position, Quaternion.identity);

        Transform pole = flag.transform.Find("Pole");
        Transform cloth = flag.transform.Find("FFlag");

        float height = Mathf.Max(1, playerData.TotalCollected * heightMultiplier);

        if (pole != null)
        {
            Vector3 scale = pole.localScale;
            scale.y = height;
            pole.localScale = scale;

            pole.localPosition = new Vector3(0, height, 0);
        }

        float visualHeight = height * 2f;

        if (cloth != null)
        {
            cloth.localPosition = new Vector3(cloth.localPosition.x + 0.25f, visualHeight + 0.1f, cloth.localPosition.z);

            Renderer r = cloth.GetComponent<Renderer>();
            if (r != null)
                r.material.color = playerColors[playerIndex];

            cloth.gameObject.AddComponent<FlagWaver>();
        }

        GameObject textObj = new GameObject("ScoreText");
        textObj.transform.SetParent(flag.transform);

        TextMeshPro scoreText = textObj.AddComponent<TextMeshPro>();
        scoreText.text = $"{playerData.TotalCollected}";
        scoreText.fontSize = 10;
        scoreText.alignment = TextAlignmentOptions.Center;

        textObj.transform.localPosition = new Vector3(0.3f, visualHeight + 1.2f, 0);
        textObj.transform.LookAt(Camera.main.transform);
        textObj.transform.Rotate(0, 180, 0);

        return visualHeight;
    }
}
