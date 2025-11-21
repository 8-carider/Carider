
using UnityEngine;
using UnityEngine.Events;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

namespace Ilumisoft.SkillDrive.Game
{
    public class Collectable : MonoBehaviour
    {
        private void Start()
        {
            // Ground 레이어 위에 자동으로 위치 조정
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0)
            {
                RaycastHit hit;
                // 위에서 아래로 Raycast (Collectable이 공중에 있어도 동작)
                if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 100f, 1 << groundLayer))
                {
                    Vector3 pos = transform.position;
                    pos.y = hit.point.y;
                    transform.position = pos;
                }
            }
        }

        [SerializeField]
        GameObject particlesPrefab = null;

        public UnityAction OnCollect = null;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {

#if PHOTON_UNITY_NETWORKING
                if (particlesPrefab != null)
                {
                    if (PhotonNetwork.InRoom)
                    {
                        PhotonNetwork.Instantiate("Collect Particles", transform.position, transform.rotation);
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


                // 차량의 VehicleCollectables 컴포넌트 업데이트
                var vehicleCollectables = other.GetComponent<VehicleCollectables>();
                if (vehicleCollectables != null)
                {
                    vehicleCollectables.AddCollectable(1);
                }

                OnCollect?.Invoke();

            #if PHOTON_UNITY_NETWORKING
                if (PhotonNetwork.InRoom)
                {
                    PhotonView photonView = PhotonView.Get(this);
                    if (photonView != null && !photonView.IsMine)
                    {
                        // 소유권이 없으면 먼저 요청만 하고, 파괴는 소유권이 넘어온 후에만 시도
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
                    Destroy(gameObject);
                }
#else
                Destroy(gameObject);
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
}