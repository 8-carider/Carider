using TMPro;
using UnityEngine;
using Ilumisoft.SkillDrive.Game;

namespace Ilumisoft.SkillDrive
{
    public class CollectableCounterUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI counterText;
        [SerializeField] private VehicleCollectables vehicleCollectables;

        private void Awake()
        {
            // counterText가 연결되지 않았다면 자동으로 찾기
            if (counterText == null)
            {
                counterText = GetComponent<TextMeshProUGUI>();
            }
            
            // vehicleCollectables가 연결되지 않았다면 자동으로 찾기
            if (vehicleCollectables == null)
            {
                var playerVehicle = GameObject.FindGameObjectWithTag("Player");
                if (playerVehicle != null)
                {
                    vehicleCollectables = playerVehicle.GetComponent<VehicleCollectables>();
                }
            }
            
            UpdateCounterText();
        }

        private void OnEnable()
        {
            if (vehicleCollectables != null)
            {
                vehicleCollectables.OnCollectableCollected += UpdateCounterText;
            }
        }

        private void OnDisable()
        {
            if (vehicleCollectables != null)
            {
                vehicleCollectables.OnCollectableCollected -= UpdateCounterText;
            }
        }

        private void UpdateCounterText()
        {
            if (counterText != null && vehicleCollectables != null)
            {
                counterText.text = $"{vehicleCollectables.CollectedCount}";
            }
        }
    }
}
