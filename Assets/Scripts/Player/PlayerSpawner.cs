using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [Header("플레이어 프리팹")]
    public GameObject playerPrefab;

    [Header("스폰 위치들")]
    public Transform[] spawnPoints;

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
        // PlayerSlot 정보 없으면 새로 할당
        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("PlayerSlot"))
        {
            SpawnMyPlayer();
        }
    }

    private void SpawnMyPlayer()
    {
        int myIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1; 
        Vector3 spawnPos = spawnPoints[myIndex].position;

        GameObject myBall = PhotonNetwork.Instantiate("Prefabs/PlayerSphere", spawnPos, Quaternion.identity);

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["PlayerSlot"] = myIndex;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        // PlayerController 초기화
        PlayerController pc = myBall.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.SetSpawnPosition(spawnPos);
        }
    }
}
