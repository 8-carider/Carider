using Photon.Pun;
using UnityEngine;
using System.Collections;

namespace Ilumisoft.SkillDrive
{
    /// <summary>
    /// Handles vehicle respawn/reset on F key press
    /// Teleports vehicle back to spawn point instead of creating new instance
    /// </summary>
    public class VehicleRespawnHandler : MonoBehaviourPun
    {
        [SerializeField] private KeyCode respawnKey = KeyCode.F;
        [SerializeField] private Transform spawnPoint; // Set by MapVehicleSpawner
        
        [Header("리스폰 이펙트")]
        [SerializeField]
        [Tooltip("차량이 터지는 파티클 프리팹")]
        private GameObject explosionParticlePrefab;

        [SerializeField]
        [Tooltip("이 차량의 리스폰 UI (차량 내 Canvas에 있는 VehicleRespawnUI)")]
        private VehicleRespawnUI respawnUI;

        [SerializeField]
        [Tooltip("리스폰 중 숨길 차량 모델 (비워두면 MeshRenderer 자동 검색)")]
        private GameObject[] vehicleModels;

        [SerializeField]
        [Tooltip("리스폰 중에도 유지할 파티클 (제트엔진 등, 비워두면 자동 제외)")]
        private ParticleSystem[] keepActiveParticles;

        private Rigidbody rb;
        private Vehicle vehicle;
        private Ilumisoft.SkillDrive.Network.VehicleSeatManager seatManager;
        private bool isRespawning = false;
        private MeshRenderer[] meshRenderers;
        private ParticleSystem[] particleSystems;
        private TrailRenderer[] trailRenderers;
        private Collider[] colliders;
        // 스폰포인트가 설정되지 않은 경우를 대비해 초기 위치/회전 저장
        private Vector3 initialPosition;
        private Quaternion initialRotation;

        [Header("Audio")]
        [SerializeField] private AudioSource respawnAudioSource;
        [SerializeField] private AudioClip respawnSfx;
        [SerializeField, Range(0f, 1f)] private float respawnSfxVolume = 1.0f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            vehicle = GetComponent<Vehicle>();
            seatManager = GetComponent<Ilumisoft.SkillDrive.Network.VehicleSeatManager>();
            if (respawnAudioSource == null)
            {
                respawnAudioSource = GetComponent<AudioSource>();
            }

            // 초기 위치/회전 기록 (스폰포인트 누락 시 사용)
            initialPosition = transform.position;
            initialRotation = transform.rotation;

            // RespawnUI가 설정되지 않았다면 자동으로 찾기
            if (respawnUI == null)
            {
                respawnUI = GetComponentInChildren<VehicleRespawnUI>();
            }

            // 차량 모델이 설정되지 않았다면 MeshRenderer 자동 검색
            if (vehicleModels == null || vehicleModels.Length == 0)
            {
                meshRenderers = GetComponentsInChildren<MeshRenderer>();
            }

            // 파티클과 트레일 찾기
            particleSystems = GetComponentsInChildren<ParticleSystem>();
            trailRenderers = GetComponentsInChildren<TrailRenderer>();
            
            // 충돌체 찾기
            colliders = GetComponentsInChildren<Collider>();
        }

        private void Update()
        {
            // 리스폰 중에는 입력 무시
            if (isRespawning) return;

            // Only process input from players assigned to this vehicle
            if (!CanRespawn()) return;

            if (UnityEngine.Input.GetKeyDown(respawnKey))
            {
                // 로컬에서 즉시 플래그 설정 (RPC 응답 대기하지 않음)
                isRespawning = true;
                
                // RPC로 리스폰 요청 - 모든 클라이언트에서 동기화
                if (PhotonNetwork.IsConnected && photonView != null)
                {
                    photonView.RPC("RPC_RequestRespawn", RpcTarget.All);
                }
                else
                {
                    RequestRespawn();
                }
            }
        }

        private bool CanRespawn()
        {
            // In multiplayer, only assigned players can respawn their vehicle
            if (PhotonNetwork.IsConnected)
            {
                var seatManager = GetComponent<Ilumisoft.SkillDrive.Network.VehicleSeatManager>();
                if (seatManager != null)
                {
                    // Check if local player is assigned to this vehicle (throttle or steering)
                    int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
                    // We'll add a public method to check assignment
                    return seatManager.IsPlayerAssigned(localActor);
                }
                // 좌석 매니저가 없으면 소유자에게는 허용, 아니면 거부
                return photonView != null ? photonView.IsMine : true;
            }
            
            // In single player, always allow
            return true;
        }

        public void RequestRespawn()
        {
            // 이미 리스폰 중이면 무시 (중복 방지)
            if (isRespawning)
            {
                return;
            }

            // 플래그 설정
            isRespawning = true;

            // UI가 없으면 바로 리스폰
            if (respawnUI == null)
            {
                if (PhotonNetwork.IsConnected && photonView != null)
                {
                    photonView.RPC("RPC_Respawn", RpcTarget.All);
                }
                else
                {
                    PerformRespawn();
                }
                return;
            }

            // 폭발 파티클과 차량 숨김은 모든 클라이언트에 동기화
            if (PhotonNetwork.IsConnected && photonView != null)
            {
                photonView.RPC("RPC_ShowExplosionAndHide", RpcTarget.All);
            }
            else
            {
                ShowExplosionAndHide();
            }

            // 카운트다운 UI는 모든 클라이언트에서 보여줌 (RPC 동기화)
            if (PhotonNetwork.IsConnected && photonView != null)
            {
                photonView.RPC("RPC_ShowCountdown", RpcTarget.All);
            }
            else
            {
                StartCountdown();
            }
        }

        [PunRPC]
        private void RPC_RequestRespawn()
        {
            RequestRespawn();
        }

        [PunRPC]
        private void RPC_ShowCountdown()
        {
            StartCountdown();
        }

        private void StartCountdown()
        {
            // 로컬 플레이어가 이 차량에 배정되었는지 확인
            bool isLocalPlayer = true;
            if (PhotonNetwork.IsConnected)
            {
                var seatManager = GetComponent<Ilumisoft.SkillDrive.Network.VehicleSeatManager>();
                int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
                isLocalPlayer = seatManager != null && seatManager.IsPlayerAssigned(localActor);
            }

            // 이 차량을 조종하는 플레이어에게만 UI 표시
            if (isLocalPlayer && respawnUI != null)
            {
                respawnUI.ShowRespawnSequence(() =>
                {
                    // 카운트다운 완료 후 RPC로 리스폰 (마스터만 실행 또는 모든 클라이언트 동시 실행)
                    if (PhotonNetwork.IsConnected && photonView != null)
                    {
                        photonView.RPC("RPC_Respawn", RpcTarget.All);
                    }
                    else
                    {
                        PerformRespawn();
                    }
                });
            }
            else
            {
                // 다른 차량을 조종하는 플레이어는 카운트다운 안 보임
                // 하지만 마스터나 owner가 RPC_Respawn을 보낼 때까지 대기
                // (리스폰 플래그는 RPC_Respawn 완료 후 해제)
            }
        }

        [PunRPC]
        private void RPC_ShowExplosionAndHide()
        {
            ShowExplosionAndHide();
        }

        private void ShowExplosionAndHide()
        {
            // 차량 완전 비활성화
            DisableVehicle();

            // 폭발 파티클 생성 (모든 클라이언트에서 실행)
            if (explosionParticlePrefab != null)
            {
                GameObject explosion = Instantiate(explosionParticlePrefab, transform.position, Quaternion.identity);
                Destroy(explosion, 3f);
            }

            // 폭발 사운드 재생 (로컬 차량만)
            PlayExplosionSfxIfLocal();
        }

        [PunRPC]
        private void RPC_Respawn()
        {
            PerformRespawn();
            // RPC 실행 완료 후 리스폰 플래그 해제
            isRespawning = false;
        }

        private void PerformRespawn()
        {
            // 텔레포트 전에 차량이 완전히 비활성화되어 있는지 확인
            // (충돌체가 꺼져있어야 콜렉터블과 충돌 안 함)
            DisableVehicle();
            
            if (spawnPoint == null)
            {
                // 스폰포인트가 누락된 경우 초기 위치로 리스폰
                Debug.LogWarning("[VehicleRespawnHandler] No spawn point assigned! Falling back to initial transform.");
                transform.position = initialPosition;
                transform.rotation = initialRotation;
            }
            else
            {
                // Teleport to spawn point (비활성화 상태로 이동)
                transform.position = spawnPoint.position;
                transform.rotation = spawnPoint.rotation;
            }

            // 텔레포트 후 차량 다시 활성화
            EnableVehicle();

            // 리스폰 후 카메라가 다시 따라오도록 재활성화
            if (PhotonNetwork.IsConnected)
            {
                var seatManager = GetComponent<Ilumisoft.SkillDrive.Network.VehicleSeatManager>();
                var cameraManager = GetComponent<Ilumisoft.SkillDrive.Network.VehicleCameraManager>();
                
                if (seatManager != null && cameraManager != null)
                {
                    int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
                    // 로컬 플레이어가 이 차량에 배정되어 있으면 카메라 재활성화
                    if (seatManager.IsPlayerAssigned(localActor))
                    {
                        cameraManager.SetCameraActive(true, localActor);
                        Debug.Log($"[VehicleRespawnHandler] Camera reactivated after respawn for vehicle {gameObject.name}");
                    }
                }
                
                // 리스폰 후 UI 업데이트 (다른 플레이어 위치도 보이도록)
                var seatUI = FindObjectOfType<Ilumisoft.SkillDrive.Network.SeatSelectionUI>();
                if (seatUI != null)
                {
                    seatUI.ForceRefreshUI();
                }
            }

            Debug.Log($"[VehicleRespawnHandler] Vehicle respawned at spawn point");
        }

        /// <summary>
        /// Called by MapVehicleSpawner to set the spawn point
        /// </summary>
        public void SetSpawnPoint(Transform point)
        {
            spawnPoint = point;
        }

        /// <summary>
        /// 차량 완전 비활성화 (물리, 입력, 이펙트 모두 정지)
        /// </summary>
        private void DisableVehicle()
        {
            // 물리 정지
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true; // 물리 완전 정지
            }

            // 차량 입력/움직임 비활성화
            if (vehicle != null)
            {
                vehicle.CanMove = false;
            }

            // 모든 파티클 정지 (제트엔진, 부스트 등)
            if (particleSystems != null)
            {
                foreach (var ps in particleSystems)
                {
                    if (ps != null && ps.isPlaying)
                    {
                        // keepActiveParticles에 포함되어 있으면 건너뛰기
                        if (keepActiveParticles != null && System.Array.Exists(keepActiveParticles, p => p == ps))
                        {
                            continue;
                        }
                        
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                }
            }

            // 모든 트레일 초기화 (바퀴자국 등)
            if (trailRenderers != null)
            {
                foreach (var trail in trailRenderers)
                {
                    if (trail != null)
                    {
                        trail.Clear();
                        trail.emitting = false;
                    }
                }
            }

            // 모든 충돌체 비활성화
            if (colliders != null)
            {
                foreach (var col in colliders)
                {
                    if (col != null)
                    {
                        col.enabled = false;
                    }
                }
            }

            // 차량 모델 숨기기
            HideVehicleModel();
        }

        /// <summary>
        /// 차량 다시 활성화
        /// </summary>
        private void EnableVehicle()
        {
            // 물리 재활성화
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // 스피드업 효과 초기화 (리스폰 시 효과 제거)
            if (vehicle != null)
            {
                vehicle.ResetSpeedUp();
                vehicle.CanMove = true;
            }

            // 파티클 다시 시작 (제트엔진 등)
            if (particleSystems != null)
            {
                foreach (var ps in particleSystems)
                {
                    if (ps != null)
                    {
                        // keepActiveParticles에 포함된 것만 건너뛰기 (이미 실행 중)
                        if (keepActiveParticles != null && System.Array.Exists(keepActiveParticles, p => p == ps))
                        {
                            continue;
                        }
                        
                        // 정지되었던 파티클 다시 시작
                        if (!ps.isPlaying)
                        {
                            ps.Play();
                        }
                    }
                }
            }

            // 트레일 다시 활성화
            if (trailRenderers != null)
            {
                foreach (var trail in trailRenderers)
                {
                    if (trail != null)
                    {
                        trail.emitting = true;
                    }
                }
            }

            // 모든 충돌체 재활성화
            if (colliders != null)
            {
                foreach (var col in colliders)
                {
                    if (col != null)
                    {
                        col.enabled = true;
                    }
                }
            }

            // 차량 모델 다시 보이기
            ShowVehicleModel();
        }

        /// <summary>
        /// 로컬이 조종하는 차량일 때만 폭발 SFX 재생 (F 누를 때)
        /// </summary>
        private void PlayExplosionSfxIfLocal()
        {
            if (respawnAudioSource == null || respawnSfx == null) return;

#if PHOTON_UNITY_NETWORKING
            // SeatManager에 로컬 Actor가 배정된 경우만 재생
            if (seatManager != null && PhotonNetwork.LocalPlayer != null)
            {
                int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
                if (seatManager.IsPlayerAssigned(localActor))
                {
                    respawnAudioSource.PlayOneShot(respawnSfx, respawnSfxVolume);
                }
            }
#else
            respawnAudioSource.PlayOneShot(respawnSfx, respawnSfxVolume);
#endif
        }

        /// <summary>
        /// 차량 모델만 숨기기
        /// </summary>
        private void HideVehicleModel()
        {
            if (vehicleModels != null && vehicleModels.Length > 0)
            {
                foreach (var model in vehicleModels)
                {
                    if (model != null)
                        model.SetActive(false);
                }
            }
            else if (meshRenderers != null)
            {
                foreach (var renderer in meshRenderers)
                {
                    if (renderer != null)
                        renderer.enabled = false;
                }
            }
        }

        /// <summary>
        /// 차량 모델만 다시 보이기
        /// </summary>
        private void ShowVehicleModel()
        {
            if (vehicleModels != null && vehicleModels.Length > 0)
            {
                foreach (var model in vehicleModels)
                {
                    if (model != null)
                        model.SetActive(true);
                }
            }
            else if (meshRenderers != null)
            {
                foreach (var renderer in meshRenderers)
                {
                    if (renderer != null)
                        renderer.enabled = true;
                }
            }
        }
    }
}
