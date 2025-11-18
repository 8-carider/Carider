
using UnityEngine;
using UnityEngine.Events;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

namespace Ilumisoft.SkillDrive.Game
{
    public class Collectable : MonoBehaviour
    {
        [SerializeField]
        GameObject particlesPrefab = null;

        public UnityAction OnCollect = null;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (particlesPrefab != null)
                {
                    Instantiate(particlesPrefab, transform.position, transform.rotation);
                }

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
                        photonView.RequestOwnership();
                    }
                    // 마스터가 아니면 마스터에게 RPC로 제거 요청
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
}