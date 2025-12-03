using UnityEngine;
using UnityEngine.Events;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

namespace Ilumisoft.SkillDrive.Game
{
    public class Collectable2 : MonoBehaviour
    {
        [SerializeField]
        GameObject particlesPrefab = null;

        public UnityAction OnCollect = null;

        private void Start()
        {
            // Ground 레이어 위로 자동 위치 조정
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

            // RadarSystem 자동 등록
            StartCoroutine(RegisterToRadarDelayed());
        }

        private System.Collections.IEnumerator RegisterToRadarDelayed()
        {
            yield return new WaitForSeconds(0.1f);

            var locatableManagerType = System.Type.GetType("Ilumisoft.RadarSystem.LocatableManager, Ilumisoft.RadarSystem");
            if (locatableManagerType != null)
            {
                var registerMethod = locatableManagerType.GetMethod("Register", new[] { typeof(GameObject) });
                if (registerMethod != null)
                {
                    var managerInstance = GameObject.FindObjectOfType(locatableManagerType);
                    if (managerInstance != null)
                    {
                        registerMethod.Invoke(managerInstance, new object[] { this.gameObject });
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            // 파티클
#if PHOTON_UNITY_NETWORKING
            if (particlesPrefab != null)
            {
                if (PhotonNetwork.InRoom)
                    PhotonNetwork.Instantiate("Collect Particles", transform.position, transform.rotation);
                else
                    Instantiate(particlesPrefab, transform.position, transform.rotation);
            }
#else
            if (particlesPrefab != null)
                Instantiate(particlesPrefab, transform.position, transform.rotation);
#endif

            // 점수 +5
            var vehicleCollectables = other.GetComponent<VehicleCollectables>();
            if (vehicleCollectables != null)
            {
                vehicleCollectables.AddCollectable(5);
            }

            OnCollect?.Invoke();

            // Photon 파괴 처리
#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.InRoom)
            {
                PhotonView pv = PhotonView.Get(this);

                if (pv != null && !pv.IsMine)
                {
                    pv.RequestOwnership();
                    return;
                }

                if (!PhotonNetwork.IsMasterClient)
                {
                    if (pv != null)
                        pv.RPC("RPC_RequestDestroy", RpcTarget.MasterClient);
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
