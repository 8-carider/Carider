using Ilumisoft.SkillDrive.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Ilumisoft.SkillDrive.UI
{
    [RequireComponent(typeof(Button))]
    public class LevelButton : MonoBehaviour
    {
        [SerializeField]
        TMPro.TextMeshProUGUI text = null;

        [SerializeField]
        Image lockImage = null;

        int sceneIndex = 0;

        Button button;

        SceneLoader sceneLoader;

        int levelIndex;

        private void Awake()
        {
            sceneLoader = FindAnyObjectByType<SceneLoader>();

            button = GetComponent<Button>();

            if (button != null)
            {
                button.onClick.AddListener(OnClick);
            }
        }

        private void Start()
        {
            // --- 원본 코드 (잠금 확인 및 적용) ---
            // bool isLocked = LevelLockManager.IsLocked(levelIndex);
            // text.gameObject.SetActive(!isLocked);
            // lockImage.gameObject.SetActive(isLocked);
            // button.interactable = !isLocked;

            // --- 수정된 코드: 모든 맵을 항상 잠금 해제 상태로 설정 ---

            // 1. 잠금 이미지를 비활성화합니다.
            lockImage.gameObject.SetActive(false);

            // 2. 텍스트 (레벨 번호/이름)를 활성화합니다.
            text.gameObject.SetActive(true);

            // 3. 버튼을 항상 클릭 가능하게 설정합니다.
            button.interactable = true;
        }

        private void OnClick()
        {
            GameManager.IsRetry = false;
            sceneLoader.LoadScene(sceneIndex);
        }

        public void SetText(string buttonText)
        {
            this.text.text = buttonText;
        }

        public void SetLevelIndex(int levelIndex)
        {
            this.levelIndex = levelIndex;
        }

        public void SetSceneIndex(int index)
        {
            sceneIndex = index;
        }
    }
}