using TMPro;
using UnityEngine;
using Ilumisoft.SkillDrive.Game;

namespace Ilumisoft.SkillDrive
{
    public class CollectableCounterUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI counterText;
        private CollectableManager collectableManager;

        private void Awake()
        {
            if (collectableManager == null)
            {
                collectableManager = FindAnyObjectByType<CollectableManager>();
            }

            UpdateCounterText();
        }

        private void OnEnable()
        {
            // 이벤트 구독
            if (collectableManager != null)
            {
                collectableManager.OnCollectableCollected += OnCollectableCollected;
                collectableManager.OnCollectableSpawned += OnCollectableSpawned;
            }
        }

        private void OnDisable()
        {
            // 이벤트 구독 해제
            if (collectableManager != null)
            {
                collectableManager.OnCollectableCollected -= OnCollectableCollected;
                collectableManager.OnCollectableSpawned -= OnCollectableSpawned;
            }
        }

        private void OnCollectableCollected()
        {
            UpdateCounterText();
        }

        private void OnCollectableSpawned()
        {
            UpdateCounterText();
        }

        private void UpdateCounterText()
        {
            if (counterText != null && collectableManager != null)
            {
                counterText.text = $"{collectableManager.CollectedCount}";
            }
        }
    }
}
