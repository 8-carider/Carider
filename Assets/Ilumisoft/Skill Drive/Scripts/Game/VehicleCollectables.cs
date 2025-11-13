using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI; // 추가: UI 네임스페이스
using Photon.Pun; // 추가: Photon 네임스페이스

namespace Ilumisoft.SkillDrive.Game
{
    public class VehicleCollectables : MonoBehaviourPun
    {
        public int CollectedCount { get; private set; }
        public UnityAction OnCollectableCollected;

        private Network.VehicleSeatManager seatManager;

        private void Awake()
        {
            CollectedCount = 0;
            seatManager = GetComponent<Network.VehicleSeatManager>();
            if (seatManager == null)
            {
                Debug.LogWarning("[VehicleCollectables] VehicleSeatManager not found on this vehicle!");
            }
        }

        public void AddCollectable()
        {
            // 네트워크 오브젝트인 경우, 소유자만 카운트 증가 가능
            if (photonView != null && !photonView.IsMine)
            {
                return;
            }

            CollectedCount++;
            OnCollectableCollected?.Invoke();

            // 현재 운전자 정보 확인 및 플레이어 통계에 기록
            if (seatManager != null)
            {
                var (throttleActor, steeringActor) = seatManager.GetAssignedActors();
                
                // 가속 페달 조작자에게 수집 기록 (또는 조향 조작자 선택 가능)
                if (throttleActor != -1)
                {
                    PlayerCollectableStats.Instance.AddCollectableForPlayer(throttleActor, 1);
                    Debug.Log($"[VehicleCollectables] Player {throttleActor} collected on {gameObject.name}");
                }
                // 조향만 담당하는 경우도 기록하려면 아래 주석 해제
                else if (steeringActor != -1)
                {
                    PlayerCollectableStats.Instance.AddCollectableForPlayer(steeringActor, 1);
                    Debug.Log($"[VehicleCollectables] Player {steeringActor} (steering only) collected on {gameObject.name}");
                }
            }

            // 네트워크로 동기화
            if (photonView != null && photonView.IsMine)
            {
                // 운전자 정보 함께 전송
                int driverActor = -1;
                if (seatManager != null)
                {
                    var (throttleActor, steeringActor) = seatManager.GetAssignedActors();
                    driverActor = throttleActor != -1 ? throttleActor : steeringActor;
                }
                photonView.RPC("RPC_AddCollectable", RpcTarget.OthersBuffered, driverActor);
            }
        }

        [PunRPC]
        private void RPC_AddCollectable(int driverActor)
        {
            CollectedCount++;
            OnCollectableCollected?.Invoke();

            // 다른 클라이언트에서도 플레이어 통계 업데이트
            if (driverActor != -1)
            {
                PlayerCollectableStats.Instance.AddCollectableForPlayer(driverActor, 1);
            }
        }
    }

    public class CollectableCounterUI : MonoBehaviour
    {
        [SerializeField] private VehicleCollectables vehicleCollectables;
        [SerializeField] private Text counterText; // 수정: Text 컴포넌트 추가

        private void OnEnable()
        {
            vehicleCollectables.OnCollectableCollected += UpdateCounterText;
        }

        private void OnDisable()
        {
            vehicleCollectables.OnCollectableCollected -= UpdateCounterText;
        }

        private void UpdateCounterText()
        {
            if (counterText != null && vehicleCollectables != null)
            {
                Debug.Log($"UI 업데이트: {vehicleCollectables.CollectedCount}");
                counterText.text = $"{vehicleCollectables.CollectedCount}";
            }
        }
    }
}