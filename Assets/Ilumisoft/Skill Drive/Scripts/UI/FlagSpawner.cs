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

    // 카메라 거리 보정 계수 (중요)
    [Header("점수 텍스트 스케일 보정")]
    public float textScaleFactor = 0.05f;

    private readonly Color[] playerColors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.yellow,
        new Color(0.2f, 1f, 0.2f)
    };

    private readonly List<TextMeshPro> scoreTexts = new List<TextMeshPro>();

    public void SpawnFlags(List<PlayerRankData> ranking)
    {
        HighestFlagHeight = 1f;
        scoreTexts.Clear();

        foreach (var playerData in ranking)
        {
            int playerIndex = Mathf.Clamp(
                playerData.ActorNumber - 1,
                0,
                spawnPositions.Length - 1
            );

            float visualHeight = SpawnSingleFlag(
                playerData,
                spawnPositions[playerIndex],
                playerIndex
            );

            if (visualHeight > HighestFlagHeight)
                HighestFlagHeight = visualHeight;
        }

        Debug.Log($"[FlagSpawner] Highest visual height: {HighestFlagHeight}");
    }

    private float SpawnSingleFlag(
        PlayerRankData playerData,
        Transform spawnPos,
        int playerIndex
    )
    {
        GameObject flag = Instantiate(
            flagPrefab,
            spawnPos.position,
            Quaternion.identity
        );

        Transform pole = flag.transform.Find("Pole");
        Transform cloth = flag.transform.Find("FFlag");

        float height = Mathf.Max(
            1,
            playerData.TotalCollected * heightMultiplier
        );

        // 폴대 높이 조절
        if (pole != null)
        {
            Vector3 scale = pole.localScale;
            scale.y = height;
            pole.localScale = scale;
            pole.localPosition = new Vector3(0, height, 0);
        }

        float visualHeight = height * 2f;

        // 깃발 위치 + 색상
        if (cloth != null)
        {
            cloth.localPosition = new Vector3(
                cloth.localPosition.x - 0.1f,
                visualHeight + 0.1f,
                cloth.localPosition.z
            );

            Renderer r = cloth.GetComponent<Renderer>();
            if (r != null)
                r.material.color = playerColors[playerIndex];

            cloth.gameObject.AddComponent<FlagWaver>();
        }

        // ===== 점수 텍스트 (월드 독립)
        GameObject textObj = new GameObject("ScoreText");
        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();

        tmp.text = playerData.TotalCollected.ToString();
        tmp.fontSize = 10;
        tmp.color = playerColors[playerIndex];
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.outlineWidth = 0f;

        textObj.transform.position =
            flag.transform.position +
            new Vector3(0.3f, visualHeight + 1.2f, 0);

        scoreTexts.Add(tmp);

        return visualHeight;
    }

    private void LateUpdate()
    {
        if (Camera.main == null) return;

        Camera cam = Camera.main;

        foreach (var tmp in scoreTexts)
        {
            if (tmp == null) continue;

            Transform t = tmp.transform;

            // 카메라 바라보기
            t.LookAt(cam.transform);
            t.Rotate(0, 180f, 0);

            // ⭐ 카메라 거리 기반 스케일 보정
            float distance = Vector3.Distance(
                cam.transform.position,
                t.position
            );

            t.localScale = Vector3.one * distance * textScaleFactor;
        }
    }
}
