using UnityEngine;
using UnityEngine.Events;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

namespace Ilumisoft.SkillDrive.Game
{
    public class RoleSwapCollectable : MonoBehaviour
    {
        [SerializeField]
        GameObject particlesPrefab = null;

        [SerializeField, Tooltip("역할 교체 지속 시간(초)")]
        private float swapDuration = 5.0f;

        [SerializeField, Tooltip("해당 아이템을 먹은 후 재생성까지 대기 시간(초). 0 이하이면 재생성 안 함")]
        private float respawnDelay = 10.0f;

        public UnityAction OnCollect = null;

        private void Start()
        {
            // Ground 레이어 위에 자동으로 위치 조정
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 100f, 1 << groundLayer))
                {
                    var pos = transform.position;
                    float lift = 0.05f;
                    var col = GetComponent<Collider>();
                    if (col != null)
                    {
                        lift += col.bounds.extents.y;
                    }
                    pos.y = hit.point.y + lift;
                    transform.position = pos;
                }
            }
        }

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
                        PhotonNetwork.Instantiate("RoleSwapParticles", transform.position, transform.rotation);
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

                // 아이템을 먹은 차량을 제외한 다른 모든 차량의 역할 교체
                var myVehicle = other.GetComponent<Vehicle>();
                if (myVehicle != null)
                {
                    var allVehicles = FindObjectsOfType<Vehicle>();
                    foreach (var vehicle in allVehicles)
                    {
                        if (vehicle != myVehicle)
                        {
                            var seatManager = vehicle.GetComponent<Ilumisoft.SkillDrive.Network.VehicleSeatManager>();
                            if (seatManager != null)
                            {
                                seatManager.SwapRoles(swapDuration);
                            }
                        }
                    }
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
                        TryScheduleRespawn();
                        PhotonNetwork.Destroy(gameObject);
                    }
                }
                else
                {
                    TryScheduleRespawn();
                    Destroy(gameObject);
                }
#else
                TryScheduleRespawn();
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
                TryScheduleRespawn();
                PhotonNetwork.Destroy(gameObject);
            }
        }
#endif

        private void TryScheduleRespawn()
        {
            if (respawnDelay <= 0f) return;
            var spawner = FindObjectOfType<ItemSpawner>();
            if (spawner != null)
            {
                spawner.RespawnOneAfterDelay(respawnDelay);
            }
        }
    }
}
