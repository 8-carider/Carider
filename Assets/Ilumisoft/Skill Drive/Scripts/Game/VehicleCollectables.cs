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

        private void Awake()
        {
            CollectedCount = 0;
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

            // 네트워크로 동기화
            if (photonView != null && photonView.IsMine)
            {
                photonView.RPC("RPC_AddCollectable", RpcTarget.OthersBuffered);
            }
        }

        [PunRPC]
        private void RPC_AddCollectable()
        {
            CollectedCount++;
            OnCollectableCollected?.Invoke();
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