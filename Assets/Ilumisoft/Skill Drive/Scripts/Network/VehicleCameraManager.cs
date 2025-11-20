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

                // Setup follow and look at - 항상 부모 transform을 추적
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
                // Inspector에서 이미 세팅된 값이 있으면 덮어쓰지 않음
                if (virtualCamera.Follow == null)
                    virtualCamera.Follow = transform;
                if (virtualCamera.LookAt == null)
                    virtualCamera.LookAt = transform;

                // 기본 우선순위: Inspector 값 유지, 없으면 0
                if (virtualCamera.Priority == null || (virtualCamera.Priority != null && virtualCamera.Priority.Value == 0))
                    virtualCamera.Priority.Value = Application.isPlaying ? 0 : virtualCamera.Priority.Value;

                // 필요한 컴포넌트가 없을 때만 추가
                var vcamObj = virtualCamera.gameObject;
                if (vcamObj.GetComponent<CinemachinePositionComposer>() == null)
                {
                    vcamObj.AddComponent<CinemachinePositionComposer>();
                }
                // CinemachineFollowZoom 자동 추가 제거: Inspector에서 직접 붙인 경우만 사용
                if (vcamObj.GetComponent<CinemachineRotationComposer>() == null)
                {
                    vcamObj.AddComponent<CinemachineRotationComposer>();
                }
                // Inspector에서 세팅된 localRotation이 있으면 유지
                // (덮어쓰지 않음)

                // 항상 Rotation Control을 None으로 강제 설정 (3인칭 고정 시점)
#if UNITY_EDITOR
                // 에디터에서만 SerializedObject 사용
                UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(virtualCamera);
                var prop = so.FindProperty("m_RotationControl");
                if (prop != null)
                {
                    prop.enumValueIndex = 0; // 0: None
                    so.ApplyModifiedProperties();
                }
#else
                // 런타임에서는 Inspector 세팅값만 유지 (코드로 강제 불가)
#endif
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
                bool wasAssigned = isAssigned;

                // 카메라가 활성화될 때마다 Follow/LookAt을 무조건 덮어쓰지 않고, null일 때만 세팅
                if (active)
                {
                    if (virtualCamera.Follow == null)
                        virtualCamera.Follow = transform;
                    if (virtualCamera.LookAt == null)
                        virtualCamera.LookAt = transform;
                }

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
                // Log when this camera becomes the active one for the local view
                if (active && !wasAssigned)
                {
                    Debug.Log($"[Camera] Now following {gameObject.name}");
                }
                isAssigned = active;
                // Debug.Log($"[VehicleCameraManager] Camera {(active ? "enabled" : "disabled")} for actor {actorNumber} on vehicle {gameObject.name}");
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
