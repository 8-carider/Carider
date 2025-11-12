using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Linq;

namespace Ilumisoft.SkillDrive.Network
{
    // Put this in each GAME LEVEL scene (not in Level Selection).
    // Spawns exactly 2 shared vehicles for all players.
    // Players can control vehicles through seat assignments (same vehicle or different vehicles).
    // Master client spawns both vehicles; all players receive them via Photon.
    [RequireComponent(typeof(PhotonView))]
    public class MapVehicleSpawner : MonoBehaviourPunCallbacks
    {
        [SerializeField] private string vehiclePrefabName = "Vehicle";
        [SerializeField] private Transform[] vehicleSpawnPoints; // Exactly 2 spawn points
        [SerializeField] private bool autoSpawnOnStart = true;

        private GameObject[] spawnedVehicles; // 항상 2대 고정
        private bool hasSpawned = false; // 중복 스폰 완전 방지

    // 좌석 상태 캐시 (마스터만 결정, RPC로 반영)
    private struct SeatAssignment { public int throttle; public int steering; }
    private SeatAssignment[] seatAssignments = new SeatAssignment[2]; // 2대 차량
    
    // 수동 좌석 할당: key = actorNumber, value = (vehicleIndex, isThrottle)
    private System.Collections.Generic.Dictionary<int, (int vehicle, bool isThrottle)> manualAssignments 
        = new System.Collections.Generic.Dictionary<int, (int vehicle, bool isThrottle)>();

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
                    Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork("[MapVehicleSpawner] Auto-assigned spawn points from Point1/Point2.");
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
                // 나간 플레이어의 수동 배정 제거
                if (manualAssignments.ContainsKey(otherPlayer.ActorNumber))
                {
                    var assignment = manualAssignments[otherPlayer.ActorNumber];
                    manualAssignments.Remove(otherPlayer.ActorNumber);
                    Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork(
                        $"[MapVehicleSpawner] Removed seat assignment for departed player {otherPlayer.ActorNumber} from Vehicle {assignment.vehicle} ({(assignment.isThrottle ? "Throttle" : "Steering")})");
                }
                
                RecalculateAndBroadcastSeats();
            }
        }

        public void TrySpawnVehicles()
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            {
                Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork("[MapVehicleSpawner] Not in room.");
                return;
            }

            if (!PhotonNetwork.IsMasterClient)
            {
                Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork("[MapVehicleSpawner] Only master spawns vehicles.");
                return;
            }

            // 이미 스폰했으면 절대 다시 안 함
            if (hasSpawned)
            {
                Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork("[MapVehicleSpawner] Already spawned vehicles. Ignoring duplicate spawn request.");
                return;
            }

            if (vehicleSpawnPoints == null || vehicleSpawnPoints.Length < 2)
            {
                Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork("[MapVehicleSpawner] Need exactly 2 spawn points. Game always uses 2 vehicles.");
                return;
            }

            hasSpawned = true; // 스폰 시작 즉시 플래그 설정
            spawnedVehicles = new GameObject[2]; // 항상 2대만
            
            Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork("[MapVehicleSpawner] Spawning 2 vehicles (fixed). Players will share these vehicles.");
            
            for (int i = 0; i < 2; i++)
            {
                var sp = vehicleSpawnPoints[i];
                if (sp == null)
                {
                    Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork($"[MapVehicleSpawner] Spawn point {i} is null.");
                    continue;
                }

                var vehicle = PhotonNetwork.Instantiate(vehiclePrefabName, sp.position, sp.rotation);
                spawnedVehicles[i] = vehicle;
                
                // Set vehicle name to include number (Vehicle 1, Vehicle 2)
                vehicle.name = $"Vehicle {i + 1}";
                // Broadcast name to all clients to ensure consistency (late joiners & remote instantiation differences)
                var pvRename = vehicle.GetComponent<PhotonView>();
                if (pvRename != null)
                {
                    pvRename.RPC("RPC_SetRuntimeName", RpcTarget.AllBuffered, vehicle.name);
                }

                // PlayerInput는 네트워크 좌석 시스템으로 대체되므로 비활성화하여 제어 스킴 경고 방지
                var playerInput = vehicle.GetComponent<UnityEngine.InputSystem.PlayerInput>();
                if (playerInput != null)
                {
                    playerInput.enabled = false;
                    Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork($"[MapVehicleSpawner] Disabled PlayerInput on Vehicle {i} to avoid control scheme pairing.");
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
                        Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork($"[MapVehicleSpawner] VehicleSeatManager attached to ROOT PhotonView GameObject on Vehicle {i}");
                    }
                }

                // 리스폰 핸들러 추가 및 스폰 포인트 설정
                var respawnHandler = vehicle.GetComponent<Ilumisoft.SkillDrive.VehicleRespawnHandler>();
                if (respawnHandler == null)
                {
                    respawnHandler = vehicle.AddComponent<Ilumisoft.SkillDrive.VehicleRespawnHandler>();
                }
                respawnHandler.SetSpawnPoint(sp);
                Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork($"[MapVehicleSpawner] Vehicle {i} respawn handler configured with spawn point: {sp.name}");

                // 이동 권한: 마스터가 물리 시뮬레이션을 하고 나머지는 동기화만 받도록 설정
                var vehicleComp = vehicle.GetComponent<Ilumisoft.SkillDrive.Vehicle>();
                if (vehicleComp != null)
                {
                    bool allowMove = !PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient;
                    vehicleComp.CanMove = allowMove;
                    if (!allowMove)
                    {
                        Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork($"[MapVehicleSpawner] Vehicle {i} movement disabled locally (non-authoritative client). Waiting for network sync.");
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
                            Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork($"[MapVehicleSpawner] Vehicle {i} custom gravity adjusted: ground=0, fall=15");
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
                            Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork($"[MapVehicleSpawner] Vehicle {i} ground raycast distance set to {groundDetection.RaycastDist}");
                            Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork($"[MapVehicleSpawner] Vehicle {i} ground layer mask set to {groundDetection.GroundLayers.value}");
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
                    Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork($"[MapVehicleSpawner] PhotonRigidbodyView added to Vehicle {i} for network sync.");
                }

                // Rigidbody 설정 확인 및 조정
                var rb = vehicle.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.useGravity = true;
                    rb.mass = 1500f;
                    rb.linearDamping = 0.5f;
                    rb.angularDamping = 0.5f;
                    Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork($"[MapVehicleSpawner] Vehicle {i} Rigidbody adjusted: gravity=true, mass={rb.mass}");
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
                        Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork($"[MapVehicleSpawner] Vehicle {i} snapped to ground. spY={sp.position.y:F2}, hitY={hit.point.y:F2}, deltaY={deltaY:F2}");
                    }
                    else
                    {
                        vehicle.transform.position = hit.point;
                        Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork($"[MapVehicleSpawner] Vehicle {i} grounded without collider info at Y={hit.point.y:F2}");
                    }
                }
            }

            // 스폰 완료 후 좌석 일괄 배정
            if (PhotonNetwork.IsMasterClient)
            {
                RecalculateAndBroadcastSeats();
            }
        }

        // 좌석 재계산: 수동 배정 우선, 빈 자리는 join 순서로 자동 채움
        private void RecalculateAndBroadcastSeats()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            var players = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();

            // 초기화: 모든 좌석 비움
            seatAssignments[0].throttle = -1;
            seatAssignments[0].steering = -1;
            seatAssignments[1].throttle = -1;
            seatAssignments[1].steering = -1;

            // 1단계: 수동 배정 적용 (플레이어가 방을 나가도 좌석 유지)
            foreach (var kvp in manualAssignments)
            {
                int actorNumber = kvp.Key;
                int vIndex = kvp.Value.vehicle;
                bool isThrottle = kvp.Value.isThrottle;

                if (vIndex < 0 || vIndex >= seatAssignments.Length) continue;
                
                // 플레이어가 방에 없으면 수동 배정에서 제거
                if (!players.Any(p => p.ActorNumber == actorNumber))
                {
                    continue; // 좌석은 비워둠 (자동 재배정하지 않음)
                }

                if (isThrottle)
                    seatAssignments[vIndex].throttle = actorNumber;
                else
                    seatAssignments[vIndex].steering = actorNumber;
            }

            // 2단계: 자동 배정 - 수동 배정되지 않은 플레이어들을 빈 자리에 채움
            var unassignedPlayers = players
                .Where(p => !manualAssignments.ContainsKey(p.ActorNumber))
                .OrderBy(p => p.ActorNumber)
                .ToArray();

            int autoIndex = 0;
            // Vehicle 0 throttle
            if (seatAssignments[0].throttle == -1 && autoIndex < unassignedPlayers.Length)
                seatAssignments[0].throttle = unassignedPlayers[autoIndex++].ActorNumber;
            // Vehicle 0 steering
            if (seatAssignments[0].steering == -1 && autoIndex < unassignedPlayers.Length)
                seatAssignments[0].steering = unassignedPlayers[autoIndex++].ActorNumber;
            // Vehicle 1 throttle
            if (seatAssignments[1].throttle == -1 && autoIndex < unassignedPlayers.Length)
                seatAssignments[1].throttle = unassignedPlayers[autoIndex++].ActorNumber;
            // Vehicle 1 steering
            if (seatAssignments[1].steering == -1 && autoIndex < unassignedPlayers.Length)
                seatAssignments[1].steering = unassignedPlayers[autoIndex++].ActorNumber;

            // 3단계: 혼자 조작 규칙 제거 - 플레이어가 나가면 그 역할은 비워둠
            // (다른 플레이어가 자동으로 두 역할을 다 맡지 않도록)
            // if (seatAssignments[0].throttle != -1 && seatAssignments[0].steering == -1)
            //     seatAssignments[0].steering = seatAssignments[0].throttle;
            // if (seatAssignments[1].throttle != -1 && seatAssignments[1].steering == -1)
            //     seatAssignments[1].steering = seatAssignments[1].throttle;

            // RPC 브로드캐스트
            for (int i = 0; i < spawnedVehicles.Length; i++)
            {
                if (spawnedVehicles[i] == null) continue;
                var pv = spawnedVehicles[i].GetComponent<PhotonView>();
                if (pv == null) continue;
                var throttle = seatAssignments[i].throttle;
                var steering = seatAssignments[i].steering;
                // Always broadcast seats (even -1) so non-assigned vehicles disable their cameras on all clients
                pv.RPC("RPC_SetSeats", RpcTarget.AllBuffered, throttle, steering);
                Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork($"[MapVehicleSpawner] Reassign vehicle {i} -> throttle:{throttle}, steering:{steering}");
            }
        }

        #region Player Join/Leave Hooks (manual add since not inheriting from callback interface in file)
        // Public method to be called externally by a small Photon callback bridge if needed.
        public void OnPlayerListChanged() { if (PhotonNetwork.IsMasterClient) RecalculateAndBroadcastSeats(); }
        #endregion

        // 수동 좌석 배정 (마스터 클라이언트만 호출, UI에서 사용)
        public void AssignSeatManually(int actorNumber, int vehicleIndex, bool isThrottle)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            
            // 기존 배정 제거 (한 플레이어는 한 자리만)
            manualAssignments.Remove(actorNumber);
            
            // 타인이 이미 동일 좌석을 점유했다면 제거하여 좌석 중복 방지
            var toRemove = new System.Collections.Generic.List<int>();
            foreach (var kvp in manualAssignments)
            {
                if (kvp.Value.vehicle == vehicleIndex && kvp.Value.isThrottle == isThrottle)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var key in toRemove)
            {
                manualAssignments.Remove(key);
                Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork($"[MapVehicleSpawner] Removed previous occupant Actor {key} from Vehicle {vehicleIndex} {(isThrottle ? "Throttle" : "Steering")} seat");
            }
            
            // 새 배정 추가
            manualAssignments[actorNumber] = (vehicleIndex, isThrottle);
            
            Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork(
                $"[MapVehicleSpawner] Manual seat assigned: Actor {actorNumber} -> Vehicle {vehicleIndex} ({(isThrottle ? "Throttle" : "Steering")})");
            
            // 좌석 재계산 및 브로드캐스트
            RecalculateAndBroadcastSeats();
        }

        private bool seatsLocked = false; // 게임 시작 후 좌석 변경 금지

        [PunRPC]
        void RPC_RequestSeat(int actorNumber, int vehicleIndex, bool isThrottle)
        {
            // 마스터 클라이언트만 처리
            if (!PhotonNetwork.IsMasterClient) return;

            if (seatsLocked)
            {
                SendSeatRequestResult(actorNumber, false, "게임이 이미 시작되어 좌석 변경이 잠겼습니다.");
                return;
            }

            Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork(
                $"[MapVehicleSpawner] RPC_RequestSeat received: Actor {actorNumber} requests Vehicle {vehicleIndex} ({(isThrottle ? "Throttle" : "Steering")})");

            // 1. 동일한 좌석을 다른 플레이어가 차지했는지 검사
            foreach (var kvp in manualAssignments)
            {
                if (kvp.Value.vehicle == vehicleIndex && kvp.Value.isThrottle == isThrottle && kvp.Key != actorNumber)
                {
                    SendSeatRequestResult(actorNumber, false, "이미 다른 플레이어가 선택한 좌석입니다.");
                    return;
                }
            }

            // 2. 요청한 플레이어가 이미 다른 차량을 선택했는지 검사
            if (manualAssignments.ContainsKey(actorNumber))
            {
                var currentAssignment = manualAssignments[actorNumber];
                // 다른 차량의 좌석을 이미 가지고 있으면 거부
                if (currentAssignment.vehicle != vehicleIndex)
                {
                    SendSeatRequestResult(actorNumber, false, "이미 다른 차량을 선택했습니다. 같은 차량의 좌석만 선택 가능합니다.");
                    return;
                }
            }

            // 좌석 배정 실행
            AssignSeatManually(actorNumber, vehicleIndex, isThrottle);
            SendSeatRequestResult(actorNumber, true, "좌석 배정 성공");
        }

        private void SendSeatRequestResult(int actorNumber, bool success, string message)
        {
            var target = PhotonNetwork.CurrentRoom?.GetPlayer(actorNumber);
            if (target == null) return;
            
            var pv = GetComponent<PhotonView>();
            if (pv != null)
            {
                pv.RPC("RPC_SeatRequestResult", target, success, message);
            }
        }

        [PunRPC]
        void RPC_SeatRequestResult(bool success, string message)
        {
            // SeatSelectionUI로 전달
            var ui = FindObjectOfType<SeatSelectionUI>();
            if (ui != null)
            {
                ui.OnSeatRequestResult(success, message);
            }
            Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork($"[MapVehicleSpawner] Seat request result -> success:{success} message:{message}");
        }

        [PunRPC]
        void RPC_StartGame()
        {
            if (!seatsLocked)
            {
                seatsLocked = true;
                Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork("[MapVehicleSpawner] Game start triggered. Seats locked.");
            }
        }

        [PunRPC]
        void RPC_LogSpawn()
        {
            Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork("[MapVehicleSpawner] Vehicles spawned (RPC sync). Ready for seat assignment.");
        }

        [PunRPC]
        void RPC_AssignCamera(int vehicleViewID, int throttleActor, int steeringActor)
        {
            // ViewID로 직접 차량 GameObject 찾기 (클라이언트마다 배열 상태 상이할 수 있음)
            PhotonView targetPV = PhotonView.Find(vehicleViewID);
            if (targetPV == null)
            {
                Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork($"[MapVehicleSpawner] RPC_AssignCamera: PhotonView {vehicleViewID} not found on client.");
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
                Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork($"[MapVehicleSpawner] RPC_AssignCamera: VehicleCameraManager missing on ViewID {vehicleViewID}");
            }
        }
    }
}
