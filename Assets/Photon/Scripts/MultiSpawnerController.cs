using Photon.Pun;
using UnityEngine;

public class MultiSpawnerController : MonoBehaviourPunCallbacks
{
    private MultiWheelController[] allWheels;
    private int nextWheelIndex = 0;

    void Start()
    {
        // 씬에 존재하는 모든 WheelController 가져오기
        allWheels = FindObjectsOfType<MultiWheelController>();
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AssignWheel(PhotonNetwork.LocalPlayer);
        }
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AssignWheel(newPlayer);
        }
    }

    void AssignWheel(Photon.Realtime.Player player)
    {
        // 이미 배정된 바퀴면 넘어감
        while (nextWheelIndex < allWheels.Length &&
               allWheels[nextWheelIndex].GetComponent<PhotonView>().Owner != null)
        {
            nextWheelIndex++;
        }

        if (nextWheelIndex >= allWheels.Length) return;

        PhotonView pv = allWheels[nextWheelIndex].GetComponent<PhotonView>();
        pv.TransferOwnership(player);

        nextWheelIndex++;
    }
}
