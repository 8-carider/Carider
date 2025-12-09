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
        private GameObject particlesPrefab = null;

        public UnityAction OnCollect = null;

        private void Start()
        {
            // Ground 레이어 위 자동 위치 조정
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0)
            {
                if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit hit, 100f, 1 << groundLayer))
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

            // 파티클 생성 (컬러 분리: Collectable2는 지정된 프리팹 사용)
            if (particlesPrefab != null)
            {
                // 파티클은 네트워크 동기화가 필요 없으므로 로컬 생성만 수행
                Instantiate(particlesPrefab, transform.position, transform.rotation);
            }

            // 점수 +5
            var vehicleCollectables = other.GetComponent<VehicleCollectables>();
            if (vehicleCollectables != null)
            {
                vehicleCollectables.AddCollectable(5);
            }

            OnCollect?.Invoke();

            // 🔥 리스폰: CollectableManager 중앙 관리로 통합
            Debug.Log($"[Collectable2] OnTriggerEnter: InRoom={PhotonNetwork.InRoom}, IsMaster={PhotonNetwork.IsMasterClient}");
#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.InRoom)
            {
                CollectableManager.Instance?.OnCollectable2PickedUp(this);
            }
            else
            {
                Debug.Log("[Collectable2] Offline mode: calling CollectableManager");
                CollectableManager.Instance?.OnCollectable2PickedUp(this);
            }
#else
            Debug.Log("[Collectable2] No Photon: calling CollectableManager");
            CollectableManager.Instance?.OnCollectable2PickedUp(this);
#endif

            // === 파괴 처리 ===
            Debug.Log("[Collectable2] Starting destroy sequence");
#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.InRoom)
            {
                PhotonView pv = PhotonView.Get(this);

                if (pv != null && !pv.IsMine)
                {
                    Debug.Log("[Collectable2] Not owner, requesting ownership");
                    pv.RequestOwnership();
                    return;
                }

                if (!PhotonNetwork.IsMasterClient)
                {
                    Debug.Log("[Collectable2] Non-master requesting destroy via RPC");
                    if (pv != null)
                        pv.RPC("RPC_RequestDestroy", RpcTarget.MasterClient);
                }
                else
                {
                    Debug.Log("[Collectable2] Master destroying gameobject");
                    PhotonNetwork.Destroy(gameObject);
                }
            }
            else
            {
                Debug.Log("[Collectable2] Offline mode: destroying gameobject");
                Destroy(gameObject);
            }
#else
            Debug.Log("[Collectable2] No Photon: destroying gameobject");
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
