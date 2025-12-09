using UnityEngine;
using System.Collections;
using Photon.Pun;

public class GroundManager : MonoBehaviourPun
{
    [Header("문 연결")]
    public TrapDoorPair doorTL; 
    public TrapDoorPair doorTR; 
    public TrapDoorPair doorBL; 
    public TrapDoorPair doorBR; 

    [Header("타이머 설정")]
    public float interval = 5.0f; // 쿨타임

    [Header("모드 설정")]
    public bool isRandomMode = true; //체크하면 랜덤, 끄면 순서대로

    void Start()
    {
        // 마스터만 패턴을 계산하고 RPC로 전파한다
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(PatternLoop());
        }
    }

    void OpenDoors(bool tl, bool tr, bool bl, bool br, double startTime)
    {
        if (tl) doorTL.OpenScheduled(startTime);
        if (tr) doorTR.OpenScheduled(startTime);
        if (bl) doorBL.OpenScheduled(startTime);
        if (br) doorBR.OpenScheduled(startTime);
    }

    IEnumerator PatternLoop()
    {
                yield return new WaitForSeconds(5);
        while (true)
        {

            if (isRandomMode)
            {
                bool rndTL = Random.value > 0.5f;
                bool rndTR = Random.value > 0.5f;
                bool rndBL = Random.value > 0.5f;
                bool rndBR = Random.value > 0.5f;

                if (!rndTL && !rndTR && !rndBL && !rndBR)
                {
                    rndTL = true;
                    rndTR = true;
                    rndBL = true;
                    rndBR = true;
                }

                double startTime = PhotonNetwork.Time + 0.2f; // 약간의 여유를 두고 동시 시작
                photonView.RPC("RPC_OpenDoors", RpcTarget.All, rndTL, rndTR, rndBL, rndBR, startTime);

                yield return new WaitForSeconds(interval);
            }
            else
            {
                // [패턴 1] 아래 2개
                photonView.RPC("RPC_OpenDoors", RpcTarget.All, false, false, true, true, PhotonNetwork.Time + 0.2f);
                yield return new WaitForSeconds(interval);

                // [패턴 2] 위 2개
                photonView.RPC("RPC_OpenDoors", RpcTarget.All, true, true, false, false, PhotonNetwork.Time + 0.2f);
                yield return new WaitForSeconds(interval);

                // [패턴 3] 대각선
                photonView.RPC("RPC_OpenDoors", RpcTarget.All, true, false, false, true, PhotonNetwork.Time + 0.2f);
                yield return new WaitForSeconds(interval);  

                // [패턴 4] 대각선
                photonView.RPC("RPC_OpenDoors", RpcTarget.All, false, true, true, false, PhotonNetwork.Time + 0.2f);
                yield return new WaitForSeconds(interval);

                // [패턴 5] 전체
                photonView.RPC("RPC_OpenDoors", RpcTarget.All, true, true, true, true, PhotonNetwork.Time + 0.2f);
                yield return new WaitForSeconds(interval);
            }
        }
    }

    [PunRPC]
    void RPC_OpenDoors(bool tl, bool tr, bool bl, bool br, double startTime)
    {
        OpenDoors(tl, tr, bl, br, startTime);
    }
}