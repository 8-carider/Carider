using UnityEngine;
using TMPro;

public class HUDManager : MonoBehaviour
{
    public TextMeshProUGUI respawnText;
    public TextMeshProUGUI timeText;

    private int _respawnCount = 0;
    private float _elapsedTime = 0f;

    public void IncrementRespawn()
    {
        _respawnCount++;
    }

    void Update()
    {
        _elapsedTime += Time.deltaTime;

        respawnText.text = $"Respawns: {_respawnCount}";
        timeText.text = $"Time: {FormatTime(_elapsedTime)}";
    }

    string FormatTime(float time)
    {
        int hours = Mathf.FloorToInt(time / 3600f);
        int minutes = Mathf.FloorToInt((time % 3600f) / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
    }
}