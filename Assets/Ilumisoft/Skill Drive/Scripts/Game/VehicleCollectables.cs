using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI; // 추가: UI 네임스페이스

namespace Ilumisoft.SkillDrive.Game
{
    public class VehicleCollectables : MonoBehaviour
    {
        public int CollectedCount { get; private set; }
        public UnityAction OnCollectableCollected;

        private void Awake()
        {
            CollectedCount = 0;
        }

        public void AddCollectable()
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