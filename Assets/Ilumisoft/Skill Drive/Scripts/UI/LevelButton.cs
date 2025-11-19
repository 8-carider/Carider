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
            // 모든 레벨을 자동으로 잠금 해제(언락) 처리
            // 마지막 레벨 번호를 실제 프로젝트에 맞게 수정하세요 (예: 10)
            int lastLevelNumber = 5;
            for (int i = 2; i <= lastLevelNumber; i++)
            {
                Ilumisoft.SkillDrive.LevelLockManager.UnlockLevel(i);
            }

            // 원본 잠금 체크 코드 사용
            bool isLocked = Ilumisoft.SkillDrive.LevelLockManager.IsLocked(levelIndex);
            text.gameObject.SetActive(!isLocked);
            lockImage.gameObject.SetActive(isLocked);
            button.interactable = !isLocked;
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