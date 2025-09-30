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
        // Photon 서버 연결
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        // 룸 생성 또는 참가
        PhotonNetwork.JoinOrCreateRoom("PracticeRoom", new RoomOptions { MaxPlayers = 4 }, null);
    }

    public override void OnJoinedRoom()
    {
        // 플레이어 번호 기반 위치 배정
        int index = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        Vector3 spawnPos = spawnPoints[index % spawnPoints.Length].position;

        // 플레이어 구 생성 (프리팹 경로는 Resources 기준 문자열로 입력)
        PhotonNetwork.Instantiate("Prefabs/PlayerSphere", spawnPos, Quaternion.identity);
    }
}