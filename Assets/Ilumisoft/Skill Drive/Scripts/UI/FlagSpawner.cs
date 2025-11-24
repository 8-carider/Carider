using UnityEngine;
using System.Collections.Generic;
using Ilumisoft.SkillDrive.Game;

public class FlagSpawner : MonoBehaviour
{
    [Header("깃발 프리팹 (이름: Flag 1)")]
    public GameObject flagPrefab;

    [Header("플레이어 깃발 생성 위치 (1~4P]")]
    public Transform[] spawnPositions;

    [Header("높이 스케일 배율 (추천: 0.5)")]
    public float heightMultiplier = 0.5f;

    public void SpawnFlags(List<PlayerRankData> ranking)
    {
        if (flagPrefab == null)
        {
            Debug.LogError("[FlagSpawner] Flag prefab이 연결되지 않음!");
            return;
        }

        if (spawnPositions == null || spawnPositions.Length == 0)
        {
            Debug.LogError("[FlagSpawner] Spawn positions 설정되지 않음!");
            return;
        }

        for (int i = 0; i < ranking.Count && i < spawnPositions.Length; i++)
        {
            SpawnSingleFlag(ranking[i], spawnPositions[i]);
        }
    }

    private void SpawnSingleFlag(PlayerRankData playerData, Transform position)
    {
        GameObject flag = Instantiate(flagPrefab, position.position, Quaternion.identity);

        Transform pole = flag.transform.Find("Pole");
        Transform cloth = flag.transform.Find("FFlag");

        float height = Mathf.Max(1, playerData.TotalCollected * heightMultiplier);

        if (pole != null)
        {
            // Pole scale 적용
            Vector3 poleScale = pole.localScale;
            poleScale.y = height;
            pole.localScale = poleScale;

            // Cylinder pivot 중앙 → 바닥에 붙도록 위치 보정
            pole.localPosition = new Vector3(0, height, 0);
        }

        if (cloth != null)
        {
            // 기존 x,z 유지, y만 pole 높이만큼 이동
            float x = cloth.localPosition.x;
            float z = cloth.localPosition.z;
            float poleVisualHeight = height * 2f; // Cylinder mesh scale 적용

            cloth.localPosition = new Vector3(
                x,
                poleVisualHeight + 0.1f,
                z
            );
        }

        Debug.Log($"[FlagSpawner] Player {playerData.ActorNumber} | Score: {playerData.TotalCollected} | Height: {height}");
    }
}
