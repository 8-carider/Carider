using UnityEngine;
using System.Collections;

public class GroundManager : MonoBehaviour
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
        StartCoroutine(PatternLoop());
    }

    void OpenDoors(bool tl, bool tr, bool bl, bool br)
    {
        if (tl) doorTL.Open();
        if (tr) doorTR.Open();
        if (bl) doorBL.Open();
        if (br) doorBR.Open();
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

                OpenDoors(rndTL, rndTR, rndBL, rndBR);
            
            yield return new WaitForSeconds(interval);
            }
            else
            {
                
                // [패턴 1] 아래 2개
                OpenDoors(false, false, true, true);
                yield return new WaitForSeconds(interval);

                // [패턴 2] 위 2개
                OpenDoors(true, true, false, false);
                yield return new WaitForSeconds(interval);

                // [패턴 3] 대각선
                OpenDoors(true, false, false, true);
                yield return new WaitForSeconds(interval);  

                // [패턴 4] 대각선
                OpenDoors(false, true, true, false);
                yield return new WaitForSeconds(interval);

                // [패턴 5] 전체
                OpenDoors(true, true, true, true);
                
            }
        }
    }
}