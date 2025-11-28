using UnityEngine;
using UnityEngine.Events;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

namespace Ilumisoft.SkillDrive.Game
{
    public class SpeedUpCollectable : MonoBehaviour
    {
        [SerializeField]
        GameObject particlesPrefab = null;

        [SerializeField, Tooltip("스피드업 지속 시간(초)")]
        private float speedUpDuration = 3.0f;

        [SerializeField, Tooltip("스피드업 배율")]
        private float speedUpMultiplier = 1.5f;

        public UnityAction OnCollect = null;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // 파티클 생성
#if PHOTON_UNITY_NETWORKING
                if (particlesPrefab != null)
                {
                    if (PhotonNetwork.InRoom)
                    {
                        PhotonNetwork.Instantiate("SpeedUpParticles", transform.position, transform.rotation);
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

                // 차량에 스피드업 효과 적용
                var vehicle = other.GetComponent<Vehicle>();
                if (vehicle != null)
                {
                    vehicle.ApplySpeedUp(speedUpDuration, speedUpMultiplier);
                }

                OnCollect?.Invoke();

#if PHOTON_UNITY_NETWORKING
                if (PhotonNetwork.InRoom)
                {
                    PhotonView photonView = PhotonView.Get(this);
                    if (photonView != null && !photonView.IsMine)
                    {
                        photonView.RequestOwnership();
                        return;
                    }
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
