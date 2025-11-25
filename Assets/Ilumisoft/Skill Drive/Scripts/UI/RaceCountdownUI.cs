using UnityEngine;
using TMPro;
using System.Collections;

namespace Ilumisoft.SkillDrive.UI
{
    public class RaceCountdownUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float countdownDuration = 1.0f;
        [SerializeField] private string[] countdownSequence = { "3", "2", "1", "START" };

        public System.Action OnCountdownFinished;

        public void StartCountdown()
        {
            StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
            canvasGroup.alpha = 1f;
            for (int i = 0; i < countdownSequence.Length; i++)
            {
                countdownText.text = countdownSequence[i];
                yield return new WaitForSeconds(countdownDuration);
            }
            canvasGroup.alpha = 0f;
            OnCountdownFinished?.Invoke();
        }
    }
}
