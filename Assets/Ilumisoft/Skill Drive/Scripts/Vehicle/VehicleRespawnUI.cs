using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Ilumisoft.SkillDrive
{
    /// <summary>
    /// 리스폰 시 화면 페이드아웃 및 카운트다운 UI 표시
    /// 각 차량마다 독립적으로 존재 (차량별 Canvas)
    /// </summary>
    public class VehicleRespawnUI : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField]
        [Tooltip("화면을 덮을 검은색 반투명 이미지")]
        private Image fadeImage;

        [SerializeField]
        [Tooltip("카운트다운 숫자를 표시할 텍스트")]
        private TextMeshProUGUI countdownText;

        [Header("설정")]
        [SerializeField]
        [Tooltip("카운트다운 시작 숫자")]
        private int countdownFrom = 5;

        [SerializeField]
        [Tooltip("페이드 이미지의 최대 알파값")]
        private float maxFadeAlpha = 0.7f;

        [SerializeField]
        [Tooltip("페이드 인/아웃 속도")]
        private float fadeSpeed = 2f;

        private Canvas canvas;

        private void Awake()
        {
            // Canvas 컴포넌트 가져오기
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            // Canvas를 Screen Space - Camera 모드로 설정하고 Main Camera 할당
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                // Main Camera는 Start에서 할당 (Cinemachine이 초기화된 후)
            }

            // 초기 상태 설정
            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = 0;
                fadeImage.color = c;
                fadeImage.gameObject.SetActive(false);
            }

            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            // Cinemachine이 Main Camera를 제어하므로 Main Camera를 Canvas에 할당
            if (canvas != null && canvas.worldCamera == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    canvas.worldCamera = mainCam;
                    canvas.planeDistance = 1f;
                }
                else
                {
                    Debug.LogWarning("[VehicleRespawnUI] Main Camera not found! Canvas may not render correctly.");
                }
            }

            // 카운트다운 텍스트 색상을 흰색으로 명시적으로 설정
            if (countdownText != null)
            {
                countdownText.color = Color.white;
            }
        }

        /// <summary>
        /// 리스폰 시퀀스 시작
        /// </summary>
        public void ShowRespawnSequence(System.Action onComplete)
        {
            StartCoroutine(RespawnSequence(onComplete));
        }

        private IEnumerator RespawnSequence(System.Action onComplete)
        {
            // 1. UI 활성화
            if (fadeImage != null)
                fadeImage.gameObject.SetActive(true);
            if (countdownText != null)
                countdownText.gameObject.SetActive(true);

            // 2. 화면 어둡게 페이드인
            yield return StartCoroutine(FadeIn());

            // 3. 카운트다운 표시
            for (int i = countdownFrom; i > 0; i--)
            {
                if (countdownText != null)
                {
                    countdownText.text = i.ToString();
                    // 숫자가 바뀔 때마다 약간 크기 변화 효과
                    countdownText.transform.localScale = Vector3.one * 1.2f;
                }
                
                yield return new WaitForSeconds(0.2f);
                
                if (countdownText != null)
                {
                    countdownText.transform.localScale = Vector3.one;
                }
                
                yield return new WaitForSeconds(0.8f);
            }

            // 4. 리스폰 실행
            onComplete?.Invoke();

            // 5. 화면 밝게 페이드아웃
            yield return StartCoroutine(FadeOut());

            // 6. UI 비활성화
            if (fadeImage != null)
                fadeImage.gameObject.SetActive(false);
            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(false);
                countdownText.text = "";
            }
        }

        private IEnumerator FadeIn()
        {
            if (fadeImage == null) yield break;

            Color c = fadeImage.color;
            c.a = 0;
            fadeImage.color = c;

            while (c.a < maxFadeAlpha)
            {
                c.a += Time.deltaTime * fadeSpeed;
                fadeImage.color = c;
                yield return null;
            }

            c.a = maxFadeAlpha;
            fadeImage.color = c;
        }

        private IEnumerator FadeOut()
        {
            if (fadeImage == null) yield break;

            Color c = fadeImage.color;

            while (c.a > 0)
            {
                c.a -= Time.deltaTime * fadeSpeed;
                fadeImage.color = c;
                yield return null;
            }

            c.a = 0;
            fadeImage.color = c;
        }
    }
}
