using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using ExitGames.Client.Photon;
using System.Collections;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [Header("플레이어 프리팹")]
    public GameObject playerPrefab;

    [Header("스폰 위치들")]
    public Transform[] spawnPoints;

    private bool[] playerSlots = new bool[4]; // 마스터 클라이언트만 사용
    private GameObject currentBall;           // 내 공 추적용
    private bool isRespawning = false;        // 중복 리스폰 방지

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinOrCreateRoom("PracticeRoom", new RoomOptions { MaxPlayers = 4 }, null);
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            int assignedSlot = FindAvailableSlot();
            if (assignedSlot == -1)
            {
                Debug.LogWarning("슬롯이 모두 찼습니다.");
                return;
            }

            playerSlots[assignedSlot] = true;

            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["PlayerSlot"] = assignedSlot;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            // props에서 직접 꺼내서 출력 (비동기 문제 방지)
            int confirmedSlot = (int)props["PlayerSlot"];
            Debug.Log($"현재 {confirmedSlot + 1}P입니다!");

            Vector3 spawnPos = spawnPoints[confirmedSlot].position;
            currentBall = PhotonNetwork.Instantiate("Prefabs/PlayerSphere", spawnPos, Quaternion.identity);
        }
        else
        {
            StartCoroutine(WaitForSlotAssignment());
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (otherPlayer.CustomProperties.ContainsKey("PlayerSlot"))
        {
            int slot = (int)otherPlayer.CustomProperties["PlayerSlot"];
            playerSlots[slot] = false;
            Debug.Log($"플레이어 {otherPlayer.NickName}가 나가서 슬롯 {slot} 해제됨");
        }
    }

    private int FindAvailableSlot()
    {
        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (!playerSlots[i])
                return i;
        }
        return -1;
    }

    private IEnumerator WaitForSlotAssignment()
    {
        while (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("PlayerSlot"))
        {
            yield return null;
        }

        int slot = (int)PhotonNetwork.LocalPlayer.CustomProperties["PlayerSlot"];
        Debug.Log($"현재 {slot + 1}P입니다!");

        Vector3 spawnPos = spawnPoints[slot].position;
        currentBall = PhotonNetwork.Instantiate("Prefabs/PlayerSphere", spawnPos, Quaternion.identity);
    }

    void Update()
    {
        if (currentBall == null || isRespawning || !PhotonNetwork.IsConnected)
            return;

        PhotonView view = currentBall.GetComponent<PhotonView>();
        if (view == null || !view.IsMine)
            return;

        if (currentBall.transform.position.y < -10f)
        {
            isRespawning = true;
            StartCoroutine(RespawnBall());
        }
    }

    private IEnumerator RespawnBall()
    {
        PhotonNetwork.Destroy(currentBall);
        yield return new WaitForSeconds(0.5f);

        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("PlayerSlot"))
        {
            Debug.LogWarning("리스폰 실패: PlayerSlot 없음");
            isRespawning = false;
            yield break;
        }

        int slot = (int)PhotonNetwork.LocalPlayer.CustomProperties["PlayerSlot"];
        Vector3 spawnPos = spawnPoints[slot].position;
        currentBall = PhotonNetwork.Instantiate("Prefabs/PlayerSphere", spawnPos, Quaternion.identity);

        isRespawning = false;
    }
}