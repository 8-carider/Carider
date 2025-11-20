    using UnityEngine;
    using Photon.Pun;

    public class Collectable2 : MonoBehaviourPun 
    {
        private void Start()
        {
            // Ground 레이어 위에 자동으로 위치 조정
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 100f, 1 << groundLayer))
                {
                    Vector3 pos = transform.position;
                    pos.y = hit.point.y;
                    transform.position = pos;
                }
            }
        }
    [Header("Settings")]
    [SerializeField] public int scoreAmount = 5; // 점수 설정

    [Header("Effects")]
    [SerializeField]
    GameObject particlesPrefab = null; // 파티클 프리팹 변수 (인스펙터 연결용)

    private void OnTriggerEnter(Collider other)
    {
        if (!gameObject.activeSelf) return;

        if (other.CompareTag("Player"))
        {
#if PHOTON_UNITY_NETWORKING
            if (particlesPrefab != null)
            {
                if (PhotonNetwork.InRoom)
                {
                    PhotonNetwork.Instantiate("Collect Particles 2", transform.position, transform.rotation);
                }
                else
                {
                    Instantiate(particlesPrefab, transform.position, transform.rotation);
                }
            }
#else
            if (particlesPrefab != null)
            {
                Instantiate(particlesPrefab, transform.position, transform.rotation);
            }
#endif

            var vehicle = other.GetComponentInParent<Ilumisoft.SkillDrive.Game.VehicleCollectables>();
            if (vehicle != null)
            {
                vehicle.AddCollectable(this.scoreAmount);
            }

#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.InRoom)
            {
                if (photonView != null && !photonView.IsMine)
                {
                    photonView.RequestOwnership();
                    return;
                }
                // 소유자(또는 마스터)가 파괴를 시도
                if (!PhotonNetwork.IsMasterClient)
                {
                    if (photonView != null)
                    {
                        photonView.RPC("RPC_RequestDestroy", RpcTarget.MasterClient);
                    }
                }
                else
                {
                    PhotonNetwork.Destroy(gameObject);
                }
            }
            else
            {
                gameObject.SetActive(false);
            }
#else
            gameObject.SetActive(false);
#endif
        }
    }


#if PHOTON_UNITY_NETWORKING
    [PunRPC]
    private void RPC_RequestDestroy()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
#endif
}