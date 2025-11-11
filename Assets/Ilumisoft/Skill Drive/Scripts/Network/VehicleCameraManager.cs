using Photon.Pun;
using UnityEngine;
using Unity.Cinemachine;

namespace Ilumisoft.SkillDrive.Network
{
    // Attach to each spawned vehicle - manages Cinemachine camera for assigned players
    [ExecuteAlways]
    public class VehicleCameraManager : MonoBehaviourPun
    {
        [SerializeField] private bool previewInEditMode = true; // 에디트 모드에서도 VCAM 프리뷰 생성
        private CinemachineCamera virtualCamera;
        private bool isAssigned = false;
        private bool createdByManager = false; // 에디트 모드 임시 생성 여부

        private void Awake()
        {
            EnsureVirtualCamera();
        }

        private void OnEnable()
        {
            // 에디트 모드에서도 프리뷰 보장
            if (!Application.isPlaying)
            {
                EnsureVirtualCamera();
                if (virtualCamera != null)
                {
                    virtualCamera.Priority.Value = 10; // 프리뷰 우선권
                }
            }
        }

        private void OnDisable()
        {
            // 에디트 모드에서 임시 생성한 VCAM은 제거 (씬에 저장되지 않도록)
            if (!Application.isPlaying && createdByManager && virtualCamera != null)
            {
                DestroyImmediate(virtualCamera.gameObject);
                virtualCamera = null;
                createdByManager = false;
            }
        }

        private void EnsureVirtualCamera()
        {
            // Find or create Cinemachine Virtual Camera for this vehicle
            virtualCamera = GetComponentInChildren<CinemachineCamera>(true);

            bool shouldCreate = (virtualCamera == null) && (Application.isPlaying || previewInEditMode);
            if (shouldCreate)
            {
                // Create a new virtual camera
                GameObject vcamObj = new GameObject("VehicleVCam");
                if (!Application.isPlaying)
                {
                    vcamObj.hideFlags = HideFlags.DontSave; // 편집 모드 임시 생성
                }
                vcamObj.transform.SetParent(transform);
                vcamObj.transform.localPosition = Vector3.zero;
                vcamObj.transform.localRotation = Quaternion.identity;

                virtualCamera = vcamObj.AddComponent<CinemachineCamera>();

                // Setup follow and look at
                virtualCamera.Follow = transform;
                virtualCamera.LookAt = transform;

                // Configure pipeline components for CM v3
                // Use Position Composer for body, Follow Zoom for dynamic FOV, and Rotation Composer to look at target
                vcamObj.AddComponent<CinemachinePositionComposer>();
                vcamObj.AddComponent<CinemachineFollowZoom>();
                vcamObj.AddComponent<CinemachineRotationComposer>();

                createdByManager = true;
                if (Application.isPlaying)
                {
                    Debug.Log($"[VehicleCameraManager] Created new virtual camera for {gameObject.name}");
                }
            }

            if (virtualCamera != null)
            {
                // 기본은 비활성 우선순위 (런타임)
                virtualCamera.Priority.Value = Application.isPlaying ? 0 : virtualCamera.Priority.Value;

                // Ensure required procedural components exist even if VCam pre-existed
                var vcamObj = virtualCamera.gameObject;
                if (vcamObj.GetComponent<CinemachinePositionComposer>() == null)
                {
                    vcamObj.AddComponent<CinemachinePositionComposer>();
                }
                if (vcamObj.GetComponent<CinemachineFollowZoom>() == null)
                {
                    vcamObj.AddComponent<CinemachineFollowZoom>();
                }
                // Critical: make sure we have a Rotation Composer so camera actually looks at the target
                if (vcamObj.GetComponent<CinemachineRotationComposer>() == null)
                {
                    vcamObj.AddComponent<CinemachineRotationComposer>();
                }
                // Reset transform so procedural components drive pose predictably
                vcamObj.transform.localRotation = Quaternion.identity;
            }

            // Ensure main camera has a CinemachineBrain so vcams can drive it (edit & play)
            var mainCam = Camera.main;
            if (mainCam != null && mainCam.GetComponent<CinemachineBrain>() == null)
            {
                mainCam.gameObject.AddComponent<CinemachineBrain>();
                if (Application.isPlaying)
                {
                    Debug.Log("[VehicleCameraManager] Added CinemachineBrain to Main Camera at runtime.");
                }
            }

            // Start disabled at runtime to avoid random camera selection before seat assignment
            if (Application.isPlaying && virtualCamera != null && PhotonNetwork.IsConnected)
            {
                virtualCamera.enabled = false;
            }
        }

        public void SetCameraActive(bool active, int actorNumber)
        {
            if (virtualCamera != null)
            {
                // Toggle component enabled state to opt-in/out of Brain selection
                virtualCamera.enabled = active;

                if (active)
                {
                    // Lower priority of other virtual cameras on this client
                    var cams = Object.FindObjectsOfType<CinemachineCamera>(true);
                    foreach (var cam in cams)
                    {
                        if (cam != virtualCamera)
                        {
                            cam.Priority.Value = 0;
                        }
                    }
                    // Promote our camera so Brain selects it
                    virtualCamera.Priority.Value = 100;
                }
                else
                {
                    virtualCamera.Priority.Value = 0;
                }
                isAssigned = active;
                Debug.Log($"[VehicleCameraManager] Camera {(active ? "enabled" : "disabled")} for actor {actorNumber} on vehicle {gameObject.name}");
            }
        }

        // 좌석 배정 시 호출: 이 차량에 배정된 플레이어면 카메라 활성화
        public void AssignCameraToPlayer(int throttleActor, int steeringActor)
        {
            int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
            
            // 내가 이 차량의 Throttle 또는 Steering을 맡았으면 카메라 활성화
            bool isMyVehicle = (localActor == throttleActor || localActor == steeringActor);
            SetCameraActive(isMyVehicle, localActor);
        }
    }
}
