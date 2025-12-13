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

        [SerializeField, Tooltip("해당 아이템을 먹은 후 재생성까지 대기 시간(초). 0 이하이면 재생성 안 함")]
        private float respawnDelay = 8.0f;

        [SerializeField, Tooltip("아이템 수집 시 재생할 사운드")]
        private AudioClip collectSound = null;

        [SerializeField, Tooltip("사운드 볼륨")]
        private float soundVolume = 1.0f;

        public UnityAction OnCollect = null;

        private void Start()
        {
            // Ground 레이어 위에 자동으로 위치 조정 (Collectable과 동일 방식)
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 100f, 1 << groundLayer))
                {
                    var pos = transform.position;
                    // 콜라이더가 중앙 피벗이면 반높이만큼 올려서 파묻힘 방지 + 추가 여유 높이
                    float lift = 0.05f; // 기본 여유
                    var col = GetComponent<Collider>();
                    if (col != null)
                    {
                        lift += col.bounds.extents.y; // 반높이
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

                // 사운드 재생 (먹은 차량의 AudioSource에서)
                if (collectSound != null)
                {
                    var audioSource = other.GetComponent<AudioSource>();
                    if (audioSource != null)
                    {
                        audioSource.PlayOneShot(collectSound, soundVolume);
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
                        // 마스터 클라이언트에서만 재생성 예약
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

        /// <summary>
        /// 씬 내 ItemSpawner를 찾아 재생성을 예약한다.
        /// </summary>
        private void TryScheduleRespawn()
        {
            if (respawnDelay <= 0f) return;
            var spawner = FindObjectOfType<ItemSpawner>();
            if (spawner != null)
            {
                // 개별 아이템 교체: 전체 batch 대신 하나만 리스폰
                spawner.RespawnOneAfterDelay(respawnDelay);
            }
        }
    }
}
