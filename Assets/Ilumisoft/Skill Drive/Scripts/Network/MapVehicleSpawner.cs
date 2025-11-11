using Photon.Pun;
using UnityEngine;

namespace Ilumisoft.SkillDrive.Network
{
    // Put this in each GAME LEVEL scene (not in Level Selection).
    // Provide two spawn points for two shared vehicles.
    // Master client spawns both vehicles; all players receive them via Photon.
    public class MapVehicleSpawner : MonoBehaviourPun
    {
        [SerializeField] private string vehiclePrefabName = "Vehicle";
        [SerializeField] private Transform[] vehicleSpawnPoints; // Expect length >= 2
        [SerializeField] private bool autoSpawnOnStart = true;

        private GameObject[] spawnedVehicles;
        private bool hasSpawned = false; // 중복 스폰 완전 방지

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

                // 좌석 매니저 추가
                var seatMgr = vehicle.GetComponent<VehicleSeatManager>();
                if (seatMgr == null)
                {
                    seatMgr = vehicle.AddComponent<VehicleSeatManager>();
                }

                // 리스폰 핸들러 추가 및 스폰 포인트 설정
                var respawnHandler = vehicle.GetComponent<Ilumisoft.SkillDrive.VehicleRespawnHandler>();
                if (respawnHandler == null)
                {
                    respawnHandler = vehicle.AddComponent<Ilumisoft.SkillDrive.VehicleRespawnHandler>();
                }
                respawnHandler.SetSpawnPoint(sp);
                Debug.Log($"[MapVehicleSpawner] Vehicle {i} respawn handler configured with spawn point: {sp.name}");
                
                // 마스터 클라이언트만 좌석 배정
                if (PhotonNetwork.IsMasterClient)
                {
                    // 4명 플레이어를 2대 차량에 분산 배정
                    // Vehicle 0: Player 1 (throttle), Player 2 (steering)
                    // Vehicle 1: Player 3 (throttle), Player 4 (steering)
                    
                    int throttleActor = -1;
                    int steeringActor = -1;
                    
                    if (i == 0) // 첫 번째 차량
                    {
                        // Player 1 (index 0) -> throttle
                        throttleActor = PhotonNetwork.PlayerList.Length >= 1 
                            ? PhotonNetwork.PlayerList[0].ActorNumber 
                            : -1;
                        
                        // Player 2 (index 1) -> steering
                        steeringActor = PhotonNetwork.PlayerList.Length >= 2 
                            ? PhotonNetwork.PlayerList[1].ActorNumber 
                            : throttleActor; // 혼자면 둘 다 담당
                    }
                    else if (i == 1) // 두 번째 차량
                    {
                        // Player 3 (index 2) -> throttle
                        throttleActor = PhotonNetwork.PlayerList.Length >= 3 
                            ? PhotonNetwork.PlayerList[2].ActorNumber 
                            : -1;
                        
                        // Player 4 (index 3) -> steering
                        steeringActor = PhotonNetwork.PlayerList.Length >= 4 
                            ? PhotonNetwork.PlayerList[3].ActorNumber 
                            : throttleActor; // 3명이면 Player 3이 둘 다 담당
                    }

                    // 좌석 배정 (버퍼링되어 나중에 합류한 플레이어도 동일 상태 유지)
                    if (throttleActor != -1)
                    {
                        seatMgr.photonView.RPC("RPC_SetSeats", RpcTarget.AllBuffered, throttleActor, steeringActor);

                        // 차량의 PhotonView ID를 이용해 모든 클라이언트가 정확히 해당 차량을 찾도록 함
                        var vpv = vehicle.GetComponent<PhotonView>();
                        if (vpv != null)
                        {
                            photonView.RPC("RPC_AssignCamera", RpcTarget.AllBuffered, vpv.ViewID, throttleActor, steeringActor);
                            Debug.Log($"[MapVehicleSpawner] Vehicle {i} (ViewID {vpv.ViewID}) assigned -> throttle:{throttleActor}, steering:{steeringActor}");
                        }
                        else
                        {
                            Debug.LogWarning($"[MapVehicleSpawner] PhotonView missing on spawned vehicle index {i}");
                        }
                    }
                    else
                    {
                        Debug.Log($"[MapVehicleSpawner] Vehicle {i} has no players assigned (not enough players in room)");
                    }
                }

                // Vehicle 이동은 오너(마스터)가 담당
                var vehicleComp = vehicle.GetComponent<Ilumisoft.SkillDrive.Vehicle>();
                if (vehicleComp != null)
                {
                    vehicleComp.CanMove = PhotonNetwork.IsMasterClient;
                    
                    // 커스텀 중력 값 조정 (너무 강한 중력이 차량을 땅에 눌러서 끌림 현상 발생)
                    // Reflection을 통해 physics 필드에 접근
                    var physicsField = typeof(Ilumisoft.SkillDrive.Vehicle).GetField("physics", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (physicsField != null)
                    {
                        var physics = physicsField.GetValue(vehicleComp) as Ilumisoft.SkillDrive.VehiclePhysics;
                        if (physics != null)
                        {
                            physics.Gravity = 0f; // Unity의 기본 중력 사용 (Rigidbody.useGravity = true)
                            physics.FallGravity = 15f; // 공중에서만 추가 중력
                            Debug.Log($"[MapVehicleSpawner] Vehicle {i} custom gravity adjusted: ground=0, fall=15");
                        }
                    }
                }

                // Rigidbody 설정 확인 및 조정 (중력 문제 해결)
                var rb = vehicle.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.useGravity = true; // 유니티 기본 중력 활성화
                    rb.mass = 1500f; // 차량 무게 (기본값이 너무 가벼우면 조정)
                    rb.linearDamping = 0.5f; // 공기 저항 (너무 높으면 끌림)
                    rb.angularDamping = 0.5f; // 회전 저항
                    Debug.Log($"[MapVehicleSpawner] Vehicle {i} Rigidbody adjusted: gravity=true, mass={rb.mass}");
                }

                // 바닥에 맞춰 위치 정밀 조정: 콜라이더 하단을 지면에 맞춤
                if (Physics.Raycast(sp.position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 200f))
                {
                    var col = vehicle.GetComponentInChildren<Collider>();
                    if (col != null)
                    {
                        // 현재 콜라이더의 월드 좌표에서 하단 Y를 계산
                        float bottomY = col.bounds.center.y - col.bounds.extents.y;
                        float deltaY = hit.point.y - bottomY;
                        vehicle.transform.position += new Vector3(0f, deltaY, 0f);
                        Debug.Log($"[MapVehicleSpawner] Vehicle {i} snapped to ground. spY={sp.position.y:F2}, hitY={hit.point.y:F2}, deltaY={deltaY:F2}");
                    }
                    else
                    {
                        // 콜라이더가 없으면 약간 내리기 (보수적)
                        vehicle.transform.position = hit.point;
                        Debug.Log($"[MapVehicleSpawner] Vehicle {i} grounded without collider info at Y={hit.point.y:F2}");
                    }
                }
            }

            photonView.RPC("RPC_LogSpawn", RpcTarget.AllBuffered);
        }

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
