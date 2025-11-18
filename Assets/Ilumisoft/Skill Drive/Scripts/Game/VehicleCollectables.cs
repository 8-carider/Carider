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

        public void ResetCollectableCount()
        {
            CollectedCount = 0;
            OnCollectableCollected?.Invoke();
        }

        private void Awake()
        {
            // CollectedCount = 0; // 이 줄은 삭제해도 됨
            seatManager = GetComponent<Network.VehicleSeatManager>();
            if (seatManager == null)
            {
                Debug.LogWarning("[VehicleCollectables] VehicleSeatManager not found on this vehicle!");
            }
        }

        public void AddCollectable(int amountToAdd)
        {
            // 네트워크 오브젝트인 경우, 소유자만 카운트 증가 가능
            if (photonView != null && !photonView.IsMine)
            {
                return;
            }

            CollectedCount+= amountToAdd;
            OnCollectableCollected?.Invoke();

            // 현재 운전자/조향자 정보 확인 및 플레이어 통계에 기록
            if (seatManager != null)
            {
                var (throttleActor, steeringActor) = seatManager.GetAssignedActors();
                bool counted = false;
                // 운전자와 조향자가 모두 있을 때(동일인물일 수도 있음)
                if (throttleActor != -1)
                {
                    PlayerCollectableStats.Instance.AddCollectableForPlayer(throttleActor, amountToAdd);
                    Debug.Log($"[VehicleCollectables] Player {throttleActor} (throttle) collected on {gameObject.name}");
                    counted = true;
                }
                if (steeringActor != -1 && steeringActor != throttleActor)
                {
                    PlayerCollectableStats.Instance.AddCollectableForPlayer(steeringActor, amountToAdd);
                    Debug.Log($"[VehicleCollectables] Player {steeringActor} (steering) collected on {gameObject.name}");
                }
            }

            // 네트워크로 동기화
            if (photonView != null && photonView.IsMine)
            {
                // 운전자/조향자 정보 함께 전송
                int throttleActor = -1;
                int steeringActor = -1;
                if (seatManager != null)
                {
                    var actors = seatManager.GetAssignedActors();
                    throttleActor = actors.throttle;
                    steeringActor = actors.steering;
                }
                photonView.RPC("RPC_AddCollectable", RpcTarget.OthersBuffered, throttleActor, steeringActor, amountToAdd);
            }
        }

        [PunRPC]
        private void RPC_AddCollectable(int throttleActor, int steeringActor, int amountToAdd)
        {
            CollectedCount += amountToAdd;
            OnCollectableCollected?.Invoke();

            // 두 명 모두에게 카운트(동일인물 중복 방지)
            if (throttleActor != -1)
            {
                PlayerCollectableStats.Instance.AddCollectableForPlayer(throttleActor, amountToAdd);
            }
            if (steeringActor != -1 && steeringActor != throttleActor)
            {
                PlayerCollectableStats.Instance.AddCollectableForPlayer(steeringActor, amountToAdd);
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