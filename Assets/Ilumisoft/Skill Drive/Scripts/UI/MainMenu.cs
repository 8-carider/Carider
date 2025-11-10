using Ilumisoft.SkillDrive.LevelSelection;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Ilumisoft.SkillDrive; // MenuController 사용

namespace Ilumisoft.SkillDrive.UI
{
    public class MainMenu : UIPanel
    {
        [SerializeField]
        CanvasGroup canvasGroup = null;

        [SerializeField]
        Selectable selectable = null;

        SceneLoader sceneLoader;
    MenuController menuController;

        [SerializeField]
        Button playButton;

        [SerializeField]
        GameObject zoomInCam = null;

        [SerializeField]
        GameObject playCam = null;

        [SerializeField]
        AudioSource confirmAudioSource;

        private IEnumerator Start()
        {
            LevelSelectionManager.LastLevel = -1;

            playButton.onClick.AddListener(OnPlayButtonClick);

            sceneLoader = FindAnyObjectByType<SceneLoader>();
            menuController = FindAnyObjectByType<MenuController>();

            yield return null;

            zoomInCam.SetActive(true);
        }

        private void OnPlayButtonClick()
        {
            // 멀티플레이 전용: 로비가 있으면 로비 표시로 대체
            if (menuController != null)
            {
                menuController.ShowLobby();
                return;
            }

            // 기본 단일 플레이 흐름(로비가 없을 때만)
            StopAllCoroutines();
            StartCoroutine(LoadCoroutine());
        }

        public override void Show()
        {
            canvasGroup.interactable = true;

            if (selectable != null)
            {
                selectable.Select();
            }
        }

        public override void Hide()
        {
            canvasGroup.interactable = false;
        }

        IEnumerator LoadCoroutine()
        {
            playCam.SetActive(true);

            yield return new WaitForSecondsRealtime(0.25f);

            confirmAudioSource.Play();

            yield return new WaitForSecondsRealtime(1.25f);

            sceneLoader.LoadScene(1);
        }
    }
}