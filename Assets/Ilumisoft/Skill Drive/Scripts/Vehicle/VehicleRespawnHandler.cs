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
        private bool isRespawning = false;
        private MeshRenderer[] meshRenderers;
        private ParticleSystem[] particleSystems;
        private TrailRenderer[] trailRenderers;
        private Collider[] colliders;
        // 스폰포인트가 설정되지 않은 경우를 대비해 초기 위치/회전 저장
        private Vector3 initialPosition;
        private Quaternion initialRotation;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            vehicle = GetComponent<Vehicle>();

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
                RequestRespawn();
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

            // 리스폰 시퀀스 시작
            isRespawning = true;

            // 폭발 파티클과 차량 숨김은 모든 클라이언트에 동기화
            if (PhotonNetwork.IsConnected && photonView != null)
            {
                photonView.RPC("RPC_ShowExplosionAndHide", RpcTarget.All);
            }
            else
            {
                ShowExplosionAndHide();
            }

            // UI 카운트다운은 로컬에서만 실행 (이 차량을 조종하는 플레이어만)
            bool showLocalUI = true;
            if (PhotonNetwork.IsConnected)
            {
                var seatManager = GetComponent<Ilumisoft.SkillDrive.Network.VehicleSeatManager>();
                int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
                showLocalUI = seatManager != null && seatManager.IsPlayerAssigned(localActor);
            }
            if (showLocalUI)
            {
                respawnUI.ShowRespawnSequence(() =>
                {
                    if (PhotonNetwork.IsConnected && photonView != null)
                    {
                        photonView.RPC("RPC_Respawn", RpcTarget.All);
                    }
                    else
                    {
                        PerformRespawn();
                    }
                    isRespawning = false;
                });
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
        }

        [PunRPC]
        private void RPC_Respawn()
        {
            PerformRespawn();
        }

        private void PerformRespawn()
        {
            if (spawnPoint == null)
            {
                // 스폰포인트가 누락된 경우 초기 위치로 리스폰
                Debug.LogWarning("[VehicleRespawnHandler] No spawn point assigned! Falling back to initial transform.");
                transform.position = initialPosition;
                transform.rotation = initialRotation;
            }
            else
            {
                // Teleport to spawn point
                transform.position = spawnPoint.position;
                transform.rotation = spawnPoint.rotation;
            }

            // 차량 다시 활성화
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

            Debug.Log($"[VehicleRespawnHandler] Vehicle respawned at {spawnPoint.position}");
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

            // 차량 입력/움직임 활성화
            if (vehicle != null)
            {
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
