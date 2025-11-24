using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class PlayerStatsUI : MonoBehaviourPunCallbacks
{
    public TextMeshProUGUI statsText; // UI 텍스트 컴포넌트
    public float updateInterval = 0.5f; // UI 업데이트 간격
    private float updateTimer = 0f; // 업데이트 타이머

    void Awake()
    {
        // statsText가 연결되지 않은 경우 자동으로 찾기
        if (statsText == null)
        {
            statsText = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

     void Start()
    {
        UpdatePlayerNamesDisplay();
    }

    // 룸 입장 시 닉네임 자동 할당
    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.LocalPlayer != null)
        {
            int actorNum = PhotonNetwork.LocalPlayer.ActorNumber;
            PhotonNetwork.NickName = $"P{actorNum}";
            Debug.Log($"[PlayerStatsUI] ActorNumber: {actorNum}, NickName: {PhotonNetwork.NickName}");
            UpdatePlayerNamesDisplay(); // 닉네임 할당 후 즉시 UI 갱신
        }
    }

    void Update()
    {
        updateTimer += Time.deltaTime;

        if (updateTimer >= updateInterval)
        {
            UpdatePlayerNamesDisplay();
            updateTimer = 0f;
        }
    }

    void UpdatePlayerNamesDisplay()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
        {
            statsText.text = "No player info";
            return;
        }

        if (string.IsNullOrEmpty(PhotonNetwork.LocalPlayer.NickName))
        {
            Debug.LogWarning("[PlayerStatsUI] NickName is empty!");
        }

        statsText.text = $"You: {PhotonNetwork.LocalPlayer.NickName}";
    }
}