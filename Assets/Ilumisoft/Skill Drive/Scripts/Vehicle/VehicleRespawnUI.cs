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
        private Coroutine sequenceCoroutine;

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
            if (sequenceCoroutine != null)
            {
                StopCoroutine(sequenceCoroutine); // 중복 재생 방지
            }
            sequenceCoroutine = StartCoroutine(RespawnSequence(() =>
            {
                sequenceCoroutine = null;
                onComplete?.Invoke();
            }));
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
                    // 처음에는 0 → 1.05까지 부드럽게 등장시키고, 이어서 수축
                    countdownText.transform.localScale = Vector3.zero;
                    var c = countdownText.color;
                    c.a = 0f;
                    countdownText.color = c;
                    // 이전 프레임 잔상 방지를 위해 텍스트 즉시 적용 후 한 프레임 대기
                    yield return null;
                }

                // 1) 팝인: 0 -> 1.05, 0 -> 1 (부드럽게 등장)
                const float popDuration = 0.1f;
                float t = 0f;
                while (t < popDuration)
                {
                    t += Time.deltaTime;
                    float lerp = Mathf.Clamp01(t / popDuration);
                    float smooth = Mathf.SmoothStep(0f, 1f, lerp);
                    float scale = Mathf.Lerp(0f, 1.05f, smooth);
                    float alpha = Mathf.Lerp(0f, 1f, smooth);

                    if (countdownText != null)
                    {
                        countdownText.transform.localScale = Vector3.one * scale;
                        var c = countdownText.color;
                        c.a = alpha;
                        countdownText.color = c;
                    }
                    yield return null;
                }

                // 2) 수축: 1.05 -> 0, 1 -> 0 (부드러운 연결)
                const float shrinkDuration = 0.6f;
                t = 0f;
                while (t < shrinkDuration)
                {
                    t += Time.deltaTime;
                    float lerp = Mathf.Clamp01(t / shrinkDuration);
                    float smooth = Mathf.SmoothStep(0f, 1f, lerp);
                    float scale = Mathf.Lerp(1.05f, 0f, smooth);
                    float alpha = Mathf.Lerp(1f, 0f, smooth);

                    if (countdownText != null)
                    {
                        countdownText.transform.localScale = Vector3.one * scale;
                        var c = countdownText.color;
                        c.a = alpha;
                        countdownText.color = c;
                    }
                    yield return null;
                }

                if (countdownText != null)
                {
                    countdownText.transform.localScale = Vector3.zero;
                    var c = countdownText.color;
                    c.a = 0f;
                    countdownText.color = c;
                }

                // 총 1초 템포 유지 (pop + shrink 소요 후 남은 시간 대기)
                float remain = Mathf.Max(0f, 1.0f - popDuration - shrinkDuration);
                if (remain > 0f) yield return new WaitForSeconds(remain);
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
