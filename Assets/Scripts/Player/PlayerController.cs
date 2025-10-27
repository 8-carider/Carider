using Photon.Pun;
using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviourPun
{
    public float moveSpeed = 5f;
    private Rigidbody rb;
    private bool isRespawning = false;
    private Vector3 spawnPos;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (!photonView.IsMine) return;

        // 시작 위치 저장
        spawnPos = transform.position;
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = new Vector3(h, 0, v) * moveSpeed;
        rb.AddForce(move, ForceMode.Force);

        // 바닥 아래로 떨어지면 리스폰
        if (transform.position.y < -10f && !isRespawning)
        {
            isRespawning = true;
            StartCoroutine(Respawn());
        }
    }

    public void SetSpawnPosition(Vector3 pos)
    {
        spawnPos = pos;
    }

    private IEnumerator Respawn()
    {
        // 기존 공 제거
        PhotonNetwork.Destroy(gameObject);
        yield return new WaitForSeconds(0.5f);

        // 새 공 생성
        GameObject newBall = PhotonNetwork.Instantiate("Prefabs/PlayerSphere", spawnPos, Quaternion.identity);

        // 새 공에 PlayerController 초기화
        PlayerController pc = newBall.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.SetSpawnPosition(spawnPos);
            pc.isRespawning = false;
        }
    }
}
