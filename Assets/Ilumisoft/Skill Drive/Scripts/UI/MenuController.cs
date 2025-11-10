using UnityEngine;
using UnityEngine.UI;

namespace Ilumisoft.SkillDrive
{
    public class MenuController : MonoBehaviour
    {
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private bool showLobbyOnPlay = false; // 테스트용: 플레이 시작 시 로비 자동 표시

        private void Awake()
        {
            // 참조가 비어 있으면 자동으로 찾아 연결
            if (lobbyPanel == null)
            {
                lobbyPanel = GameObject.Find("LobbyPanel");
            }

            if (mainMenuPanel == null)
            {
                // 자주 쓰는 이름들 우선 탐색
                mainMenuPanel = GameObject.Find("Buttons")
                                 ?? GameObject.Find("Main Menu")
                                 ?? GameObject.Find("Menu");
            }

            Debug.Log($"[MenuController] Awake - lobbyPanel: {lobbyPanel?.name ?? "null"} (active={lobbyPanel?.activeSelf}), mainMenuPanel: {mainMenuPanel?.name ?? "null"} (active={mainMenuPanel?.activeSelf})");
        }

        private void Start()
        {
            if (showLobbyOnPlay)
            {
                ShowLobby();
            }
        }

        // 개발 편의용 디버그 단축키 (빌드에는 제외)
#if UNITY_EDITOR
        private void Update()
        {
            // Ilumisoft.SkillDrive.Input 네임스페이스와 충돌을 피하기 위해 UnityEngine.Input을 풀네임으로 사용
            if (UnityEngine.Input.GetKeyDown(KeyCode.L))
            {
                ShowLobby();
            }
        }
#endif

        public void ShowLobby()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }

            if (lobbyPanel != null)
            {
                // 만약 어떤 Canvas의 자식도 아니라면, 최상위 Canvas로 편성
                var parentCanvas = lobbyPanel.GetComponentInParent<Canvas>();
                if (parentCanvas == null)
                {
                    var topCanvas = Object.FindObjectOfType<Canvas>();
                    if (topCanvas == null)
                    {
                        var cGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                        topCanvas = cGo.GetComponent<Canvas>();
                        topCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        cGo.layer = LayerMask.NameToLayer("UI");
                    }
                    lobbyPanel.transform.SetParent(topCanvas.transform, false);
                }

                lobbyPanel.SetActive(true);

                // 강제 가시화: 위치/스케일/알파 초기화
                var rt = lobbyPanel.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    rt.localScale = Vector3.one;
                    rt.anchoredPosition3D = Vector3.zero;
                }

                var cg = lobbyPanel.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }

                // LobbyPanel이 별도 Canvas를 갖는다면 정렬 우선
                var panelCanvas = lobbyPanel.GetComponentInParent<Canvas>();
                if (panelCanvas != null)
                {
                    panelCanvas.sortingOrder = 1000; // 최상단 표시 보장
                }
            }

            Debug.Log($"[MenuController] ShowLobby() - lobbyPanel active={lobbyPanel?.activeSelf}, mainMenuPanel active={mainMenuPanel?.activeSelf}");
        }

        public void QuitGame()
        {
            Application.Quit();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
    }
}
