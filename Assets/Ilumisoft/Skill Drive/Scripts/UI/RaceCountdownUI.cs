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
        [SerializeField] private string[] countdownSequence = { "3..", "2..", "1..", "Go!" };

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip tickClip; // 3,2,1용
        [SerializeField] private AudioClip goClip;   // Go! 용 (없으면 tickClip 재사용)
        [SerializeField] private float sfxVolume = 1.0f;

        public System.Action OnCountdownFinished;

        public void StartCountdown()
        {
            gameObject.SetActive(true);
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
            canvasGroup.alpha = 1f;
            for (int i = 0; i < countdownSequence.Length; i++)
            {
                countdownText.text = countdownSequence[i];

                // 사운드 재생
                if (audioSource != null)
                {
                    bool isLast = (i == countdownSequence.Length - 1);
                    var clip = isLast && goClip != null ? goClip : tickClip;
                    if (clip != null)
                    {
                        audioSource.PlayOneShot(clip, sfxVolume);
                    }
                }

                yield return new WaitForSeconds(countdownDuration);
            }
            canvasGroup.alpha = 0f;
            countdownText.text = "";
            gameObject.SetActive(false);
            OnCountdownFinished?.Invoke();
        }
    }
}
