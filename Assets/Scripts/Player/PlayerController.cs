using Photon.Pun;
using UnityEngine;

public class PlayerController : MonoBehaviourPun
{
    public float moveSpeed = 5f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // 내 구가 아니면 조작 스크립트 제거
        if (!photonView.IsMine)
        {
            Destroy(this);
        }
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = new Vector3(h, 0, v) * moveSpeed;
        rb.AddForce(move, ForceMode.Force);

        // 추락 탈락 처리
        if (transform.position.y < -10f)
        {
            PhotonNetwork.Destroy(gameObject); // 구 제거
        }
    }
}