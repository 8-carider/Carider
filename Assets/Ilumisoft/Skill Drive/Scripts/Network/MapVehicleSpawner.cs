using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Linq;

namespace Ilumisoft.SkillDrive.Network
{
    // Put this in each GAME LEVEL scene (not in Level Selection).
    // Provide two spawn points for two shared vehicles.
    // Master client spawns both vehicles; all players receive them via Photon.
    public class MapVehicleSpawner : MonoBehaviourPunCallbacks
    {
        [SerializeField] private string vehiclePrefabName = "Vehicle";
        [SerializeField] private Transform[] vehicleSpawnPoints; // Expect length >= 2
        [SerializeField] private bool autoSpawnOnStart = true;

        private GameObject[] spawnedVehicles;
        private bool hasSpawned = false; // 중복 스폰 완전 방지

    // 좌석 상태 캐시 (마스터만 결정, RPC로 반영)
    private struct SeatAssignment { public int throttle; public int steering; }
    private SeatAssignment[] seatAssignments = new SeatAssignment[2]; // 2대 차량

        private void Awake()
        {
            // 자동으로 'Point1', 'Point2'를 찾아서 세팅 (인스펙터 미지정 시)
            if ((vehicleSpawnPoints == null || vehicleSpawnPoints.Length < 2))
            {
                var p1 = GameObject.Find("Point1");
                var p2 = GameObject.Find("Point2");
                if (p1 != null && p2 != null)
                {
                    vehicleSpawnPoints = new Transform[2] { p1.transform, p2.transform };
                    Debug.Log("[MapVehicleSpawner] Auto-assigned spawn points from Point1/Point2.");
                }
            }
        }

        private void Start()
        {
            if (!autoSpawnOnStart) return;
            TrySpawnVehicles();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (PhotonNetwork.IsMasterClient && hasSpawned)
            {
                RecalculateAndBroadcastSeats();
            }
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            if (PhotonNetwork.IsMasterClient && hasSpawned)
            {
                RecalculateAndBroadcastSeats();
            }
        }

        public void TrySpawnVehicles()
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            {
                Debug.LogWarning("[MapVehicleSpawner] Not in room.");
                return;
            }

            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.Log("[MapVehicleSpawner] Only master spawns vehicles.");
                return;
            }

            // 이미 스폰했으면 절대 다시 안 함
            if (hasSpawned)
            {
                Debug.Log("[MapVehicleSpawner] Already spawned vehicles. Ignoring duplicate spawn request.");
                return;
            }

            if (vehicleSpawnPoints == null || vehicleSpawnPoints.Length < 2)
            {
                Debug.LogError("[MapVehicleSpawner] Need at least 2 spawn points for two vehicles.");
                return;
            }

            hasSpawned = true; // 스폰 시작 즉시 플래그 설정
            spawnedVehicles = new GameObject[2];
            for (int i = 0; i < 2; i++)
            {
                var sp = vehicleSpawnPoints[i];
                if (sp == null)
                {
                    Debug.LogError($"[MapVehicleSpawner] Spawn point {i} is null.");
                    continue;
                }

                var vehicle = PhotonNetwork.Instantiate(vehiclePrefabName, sp.position, sp.rotation);
                spawnedVehicles[i] = vehicle;

                // PlayerInput는 네트워크 좌석 시스템으로 대체되므로 비활성화하여 제어 스킴 경고 방지
                var playerInput = vehicle.GetComponent<UnityEngine.InputSystem.PlayerInput>();
                if (playerInput != null)
                {
                    playerInput.enabled = false;
                    Debug.Log($"[MapVehicleSpawner] Disabled PlayerInput on Vehicle {i} to avoid control scheme pairing.");
                }

                // 카메라 매니저 추가
                var cameraMgr = vehicle.GetComponent<VehicleCameraManager>();
                if (cameraMgr == null)
                {
                    cameraMgr = vehicle.AddComponent<VehicleCameraManager>();
                }

                // 좌석 매니저를 '루트 Vehicle GameObject의 PhotonView'에 부착하여 RPC 검색 이슈 방지
                var vpv = vehicle.GetComponent<PhotonView>();
                if (vpv == null)
                {
                    Debug.LogError($"[MapVehicleSpawner] PhotonView not found on Vehicle {i}. RPC will not work.");
                }
                else
                {
                    // 루트의 PhotonView가 붙은 같은 GO에 보장
                    var seatMgr = vpv.gameObject.GetComponent<VehicleSeatManager>();
                    if (seatMgr == null)
                    {
                        seatMgr = vpv.gameObject.AddComponent<VehicleSeatManager>();
                        Debug.Log($"[MapVehicleSpawner] VehicleSeatManager attached to ROOT PhotonView GameObject on Vehicle {i}");
                    }
                }

                // 리스폰 핸들러 추가 및 스폰 포인트 설정
                var respawnHandler = vehicle.GetComponent<Ilumisoft.SkillDrive.VehicleRespawnHandler>();
                if (respawnHandler == null)
                {
                    respawnHandler = vehicle.AddComponent<Ilumisoft.SkillDrive.VehicleRespawnHandler>();
                }
                respawnHandler.SetSpawnPoint(sp);
                Debug.Log($"[MapVehicleSpawner] Vehicle {i} respawn handler configured with spawn point: {sp.name}");

                // 이동 권한: 마스터가 물리 시뮬레이션을 하고 나머지는 동기화만 받도록 설정
                var vehicleComp = vehicle.GetComponent<Ilumisoft.SkillDrive.Vehicle>();
                if (vehicleComp != null)
                {
                    bool allowMove = !PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient;
                    vehicleComp.CanMove = allowMove;
                    if (!allowMove)
                    {
                        Debug.Log($"[MapVehicleSpawner] Vehicle {i} movement disabled locally (non-authoritative client). Waiting for network sync.");
                    }

                    // 커스텀 중력 값 조정
                    var physicsField = typeof(Ilumisoft.SkillDrive.Vehicle).GetField("physics",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (physicsField != null)
                    {
                        var physics = physicsField.GetValue(vehicleComp) as Ilumisoft.SkillDrive.VehiclePhysics;
                        if (physics != null)
                        {
                            physics.Gravity = 0f;
                            physics.FallGravity = 15f;
                            Debug.Log($"[MapVehicleSpawner] Vehicle {i} custom gravity adjusted: ground=0, fall=15");
                        }
                    }

                    // 지면 감지 레이캐스트 거리 조정
                    var groundDetectionField = typeof(Ilumisoft.SkillDrive.Vehicle).GetField("groundDetection",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (groundDetectionField != null)
                    {
                        var groundDetection = groundDetectionField.GetValue(vehicleComp) as Ilumisoft.SkillDrive.VehicleGroundDetection;
                        if (groundDetection != null)
                        {
                            groundDetection.RaycastDist = 5.0f;
                            int groundLayerIndex = LayerMask.NameToLayer("Ground");
                            LayerMask mask = Physics.DefaultRaycastLayers;
                            if (groundLayerIndex != -1)
                            {
                                mask |= (1 << groundLayerIndex);
                            }
                            groundDetection.GroundLayers = mask;
                            Debug.Log($"[MapVehicleSpawner] Vehicle {i} ground raycast distance set to {groundDetection.RaycastDist}");
                            Debug.Log($"[MapVehicleSpawner] Vehicle {i} ground layer mask set to {groundDetection.GroundLayers.value}");
                        }
                    }
                }

                // 비마스터에서 차량이 '멈춘 것처럼' 보이는 문제 해결을 위해 PhotonRigidbodyView 추가
                var rbView = vehicle.GetComponent<Photon.Pun.PhotonRigidbodyView>();
                if (rbView == null)
                {
                    rbView = vehicle.AddComponent<Photon.Pun.PhotonRigidbodyView>();
                    rbView.m_SynchronizeVelocity = true;
                    rbView.m_SynchronizeAngularVelocity = true;
                    Debug.Log($"[MapVehicleSpawner] PhotonRigidbodyView added to Vehicle {i} for network sync.");
                }

                // Rigidbody 설정 확인 및 조정
                var rb = vehicle.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.useGravity = true;
                    rb.mass = 1500f;
                    rb.linearDamping = 0.5f;
                    rb.angularDamping = 0.5f;
                    Debug.Log($"[MapVehicleSpawner] Vehicle {i} Rigidbody adjusted: gravity=true, mass={rb.mass}");
                }

                // 바닥에 맞춰 위치 정밀 조정
                if (Physics.Raycast(sp.position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 200f))
                {
                    var col = vehicle.GetComponentInChildren<Collider>();
                    if (col != null)
                    {
                        float bottomY = col.bounds.center.y - col.bounds.extents.y;
                        float deltaY = hit.point.y - bottomY;
                        vehicle.transform.position += new Vector3(0f, deltaY, 0f);
                        Debug.Log($"[MapVehicleSpawner] Vehicle {i} snapped to ground. spY={sp.position.y:F2}, hitY={hit.point.y:F2}, deltaY={deltaY:F2}");
                    }
                    else
                    {
                        vehicle.transform.position = hit.point;
                        Debug.Log($"[MapVehicleSpawner] Vehicle {i} grounded without collider info at Y={hit.point.y:F2}");
                    }
                }
            }

            // 스폰 완료 후 좌석 일괄 배정
            if (PhotonNetwork.IsMasterClient)
            {
                RecalculateAndBroadcastSeats();
            }
        }

        // 좌석 재계산: 플레이어 join 순서 기반 (추후 커스터마이즈를 위한 단일 진입점)
        private void RecalculateAndBroadcastSeats()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            var players = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();

            // 규칙:
            // index0 -> Vehicle0 throttle+steering (혼자일 때)
            // index1 -> Vehicle0 steering (둘 이상), index0은 Vehicle0 throttle
            // index2 -> Vehicle1 throttle (셋 이상)
            // index3 -> Vehicle1 steering (넷 이상)

            seatAssignments[0].throttle = players.Length >= 1 ? players[0].ActorNumber : -1;
            seatAssignments[0].steering = players.Length >= 2 ? players[1].ActorNumber : seatAssignments[0].throttle; // 1명일 때 혼자 둘 다
            seatAssignments[1].throttle = players.Length >= 3 ? players[2].ActorNumber : -1;
            seatAssignments[1].steering = players.Length >= 4 ? players[3].ActorNumber : seatAssignments[1].throttle; // 3명일 때 혼자 둘 다

            for (int i = 0; i < spawnedVehicles.Length; i++)
            {
                if (spawnedVehicles[i] == null) continue;
                var pv = spawnedVehicles[i].GetComponent<PhotonView>();
                if (pv == null) continue;
                var throttle = seatAssignments[i].throttle;
                var steering = seatAssignments[i].steering;
                // Always broadcast seats (even -1) so non-assigned vehicles disable their cameras on all clients
                pv.RPC("RPC_SetSeats", RpcTarget.AllBuffered, throttle, steering);
                Debug.Log($"[MapVehicleSpawner] Reassign vehicle {i} -> throttle:{throttle}, steering:{steering}");
            }
        }

        #region Player Join/Leave Hooks (manual add since not inheriting from callback interface in file)
        // Public method to be called externally by a small Photon callback bridge if needed.
        public void OnPlayerListChanged() { if (PhotonNetwork.IsMasterClient) RecalculateAndBroadcastSeats(); }
        #endregion

        [PunRPC]
        void RPC_LogSpawn()
        {
            Debug.Log("[MapVehicleSpawner] Vehicles spawned (RPC sync). Ready for seat assignment.");
        }

        [PunRPC]
        void RPC_AssignCamera(int vehicleViewID, int throttleActor, int steeringActor)
        {
            // ViewID로 직접 차량 GameObject 찾기 (클라이언트마다 배열 상태 상이할 수 있음)
            PhotonView targetPV = PhotonView.Find(vehicleViewID);
            if (targetPV == null)
            {
                Debug.LogWarning($"[MapVehicleSpawner] RPC_AssignCamera: PhotonView {vehicleViewID} not found on client.");
                return;
            }
            var vehicle = targetPV.gameObject;
            var cameraMgr = vehicle.GetComponent<VehicleCameraManager>();
            if (cameraMgr != null)
            {
                cameraMgr.AssignCameraToPlayer(throttleActor, steeringActor);
            }
            else
            {
                Debug.LogWarning($"[MapVehicleSpawner] RPC_AssignCamera: VehicleCameraManager missing on ViewID {vehicleViewID}");
            }
        }
    }
}
