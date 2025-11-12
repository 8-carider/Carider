using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections.Generic;

namespace Ilumisoft.SkillDrive.Network
{
    /// <summary>
    /// 씬에 미리 배치된 2대의 차량에 대해 좌석 배정만 담당하는 스포너(관리자).
    /// - 차량 생성 없음
    /// - 규칙: 같은 차량의 두 좌석은 가능, 서로 다른 차량 동시 점유 불가
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class MapVehicleSpawner : MonoBehaviourPunCallbacks
    {
        [Header("Scene References")]
        [SerializeField] private GameObject vehicle1;
        [SerializeField] private GameObject vehicle2;

        private GameObject[] vehicles = new GameObject[2];
        private bool seatsLocked = false;

        private struct PlayerSeatRecord { public int vehicle; public bool throttle; public bool steering; }
        private Dictionary<int, PlayerSeatRecord> playerSeats = new Dictionary<int, PlayerSeatRecord>();

        private void Awake()
        {
            if (vehicle1 == null) vehicle1 = GameObject.Find("Vehicle 1");
            if (vehicle2 == null) vehicle2 = GameObject.Find("Vehicle 2");
            if (vehicle1 == null || vehicle2 == null)
            {
                Debug.LogError("[MapVehicleSpawner] Vehicle 1 또는 Vehicle 2가 씬에 없습니다.");
                return;
            }

            vehicles[0] = vehicle1;
            vehicles[1] = vehicle2;
            SetupVehicle(vehicle1, 0);
            SetupVehicle(vehicle2, 1);
        }

        private void SetupVehicle(GameObject v, int index)
        {
            if (v == null) return;
            if (v.GetComponent<VehicleSeatManager>() == null) v.AddComponent<VehicleSeatManager>();
            if (v.GetComponent<VehicleCameraManager>() == null) v.AddComponent<VehicleCameraManager>();
            var respawn = v.GetComponent<VehicleRespawnHandler>();
            if (respawn == null) respawn = v.AddComponent<VehicleRespawnHandler>();
            var sp = new GameObject($"SpawnPoint_Vehicle{index+1}").transform;
            sp.position = v.transform.position; sp.rotation = v.transform.rotation; sp.parent = v.transform.parent;
            respawn.SetSpawnPoint(sp);
        }

        private void Start()
        {
            if (PhotonNetwork.IsMasterClient) BroadcastSeats();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (PhotonNetwork.IsMasterClient) BroadcastSeats();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (playerSeats.Remove(otherPlayer.ActorNumber))
            {
                Ilumisoft.SkillDrive.SkillDriveDebug.LogNetwork($"[MapVehicleSpawner] Removed seat record of Actor {otherPlayer.ActorNumber}");
            }
            BroadcastSeats();
        }

        // SeatSelectionUI에서 직접/간접 호출
        public void AssignSeatManually(int actorNumber, int vehicleIndex, bool isThrottle)
        {
            if (!PhotonNetwork.IsMasterClient || seatsLocked) return;
            if (vehicleIndex < 0 || vehicleIndex > 1) return;

            // 좌석 점유 검사 (타 플레이어가 이미 점유중이면 거부)
            foreach (var kvp in playerSeats)
            {
                if (kvp.Key == actorNumber) continue;
                var r = kvp.Value;
                if (r.vehicle == vehicleIndex)
                {
                    if ((isThrottle && r.throttle) || (!isThrottle && r.steering))
                    {
                        SendSeatRequestResult(actorNumber, false, "이미 다른 플레이어가 선택한 좌석입니다.");
                        return;
                    }
                }
            }

            // 동일 플레이어의 다른 차량 배정 여부 검사
            if (playerSeats.TryGetValue(actorNumber, out var rec) && rec.vehicle != vehicleIndex)
            {
                SendSeatRequestResult(actorNumber, false, "이미 다른 차량을 선택했습니다. 같은 차량의 좌석만 가능합니다.");
                return;
            }

            if (!playerSeats.TryGetValue(actorNumber, out rec))
            {
                rec = new PlayerSeatRecord { vehicle = vehicleIndex, throttle = false, steering = false };
            }
            if (isThrottle) rec.throttle = true; else rec.steering = true;
            rec.vehicle = vehicleIndex;
            playerSeats[actorNumber] = rec;

            SendSeatRequestResult(actorNumber, true, "좌석 배정 성공");
            BroadcastSeats();
        }

        private void BroadcastSeats()
        {
            int v1T=-1, v1S=-1, v2T=-1, v2S=-1;
            foreach (var kvp in playerSeats)
            {
                var r = kvp.Value;
                if (r.vehicle == 0)
                {
                    if (r.throttle) v1T = kvp.Key;
                    if (r.steering) v1S = kvp.Key;
                }
                else if (r.vehicle == 1)
                {
                    if (r.throttle) v2T = kvp.Key;
                    if (r.steering) v2S = kvp.Key;
                }
            }
            ApplySeatsToVehicle(vehicles[0], v1T, v1S);
            ApplySeatsToVehicle(vehicles[1], v2T, v2S);
        }

        private void ApplySeatsToVehicle(GameObject v, int throttleActor, int steeringActor)
        {
            if (v == null) return;
            var mgr = v.GetComponent<VehicleSeatManager>();
            if (mgr != null)
            {
                mgr.photonView.RPC("RPC_SetSeats", RpcTarget.AllBuffered, throttleActor, steeringActor);
            }
        }

        [PunRPC]
        void RPC_RequestSeat(int actorNumber, int vehicleIndex, bool isThrottle)
        {
            AssignSeatManually(actorNumber, vehicleIndex, isThrottle);
        }

        private void SendSeatRequestResult(int actorNumber, bool success, string message)
        {
            var ui = FindObjectOfType<SeatSelectionUI>();
            if (ui == null) return;
            var uiPv = ui.GetComponent<PhotonView>();
            var player = PhotonNetwork.CurrentRoom?.GetPlayer(actorNumber);
            if (uiPv != null && player != null)
            {
                uiPv.RPC("RPC_SeatRequestResult", player, success, message);
            }
        }

        [PunRPC]
        void RPC_StartGame()
        {
            seatsLocked = true;
        }
    }
}
