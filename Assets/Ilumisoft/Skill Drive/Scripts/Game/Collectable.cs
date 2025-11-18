using UnityEngine;
using UnityEngine.Events;

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
                    vehicleCollectables.AddCollectable();
                }

                OnCollect?.Invoke();

#if PHOTON_UNITY_NETWORKING
                if (Photon.Pun.PhotonNetwork.InRoom && Photon.Pun.PhotonView.Get(this).IsMine)
                {
                    Photon.Pun.PhotonNetwork.Destroy(gameObject);
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
    }
}