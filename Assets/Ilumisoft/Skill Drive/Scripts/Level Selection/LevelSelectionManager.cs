using Ilumisoft.SkillDrive.Game;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Photon.Pun;
using Unity.Cinemachine;

namespace Ilumisoft.SkillDrive.LevelSelection
{
    public class LevelSelectionManager : MonoBehaviourPun
    {
        [Header("Preview Cameras")]
        [SerializeField]
        CinemachineVirtualCamera[] previewVCams = null;
        public static int LastLevel = -1;

        [Header("Button Setup")]
        [SerializeField]
        LevelSelectButton levelSelectButtonPrefab = null;

        [SerializeField]
        GameObject buttonContainer = null;

        [Header("Level Setup")]
        [SerializeField]
        LevelData levelData = null;

        [SerializeField, FormerlySerializedAs("levelSelects")]
        List<LevelPreview> levelPreviews = null;

        [Header("SFX")]
        [SerializeField]
        AudioSource confirmSFX = null;

        [SerializeField]
        AudioSource deniedSFX = null;

        [Header("Other")]
        [SerializeField]
        InputAction returnInputAction = null;

        SceneLoader sceneLoader;

        int currentIndex = 0;

        Coroutine levelSwitchCoroutine = null;

        private void OnEnable()
        {
            returnInputAction.Enable();
        }

        private void OnDisable()
        {
            returnInputAction.Disable();
        }

        private void Awake()
        {
            returnInputAction.started += (arg) =>
            {
                // Enter 키 입력 시 현재 선택된 맵으로 들어가기
                if (Keyboard.current != null && Keyboard.current.enterKey.isPressed)
                {
                    LoadLevel(currentIndex + 1);
                }
                else
                {
                    // 기존 메뉴 복귀 기능(다른 키 입력 시)
                    LoadMenu();
                }
            };
        }

        private IEnumerator Start()
        {
            sceneLoader = FindAnyObjectByType<SceneLoader>();

            // Optional debug (disabled by default via SkillDriveDebug flags)
            Ilumisoft.SkillDrive.SkillDriveDebug.LogUI($"[LevelSelectionManager] LastLevel = {LastLevel}");
            SetupLevelPreviewsAndButtons();
            currentIndex = 0;
            levelPreviews[currentIndex].Select();
            buttonContainer.transform.GetChild(currentIndex).GetComponent<Selectable>().Select();
            yield break;
        }

        void SetupLevelPreviewsAndButtons()
        {
            bool isMaster = Photon.Pun.PhotonNetwork.IsMasterClient;
            for (int i = 0; i < levelPreviews.Count; i++)
            {
                int levelNumber = i + 1;

                var buttonInstance = Instantiate(levelSelectButtonPrefab, buttonContainer.transform);
                buttonInstance.Initialize(this, levelNumber);

                // 마스터만 버튼 활성화
                var btn = buttonInstance.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = isMaster;
                }
            }
        }

        bool IsLevelLocked(int levelNumber) => LevelLockManager.IsLocked(levelNumber);
    public void LoadMenu()
        {
            // 포톤 방에서 나가기 및 서버 연결 끊기
            if (Photon.Pun.PhotonNetwork.InRoom)
            {
                Photon.Pun.PhotonNetwork.LeaveRoom();
            }
            if (Photon.Pun.PhotonNetwork.IsConnected)
            {
                Photon.Pun.PhotonNetwork.Disconnect();
            }
            sceneLoader.LoadScene(0);
        }

        public void GoToLevel(int levelNumber)
        {
            if (levelSwitchCoroutine != null)
            {
                StopCoroutine(levelSwitchCoroutine);
            }

            levelSwitchCoroutine = StartCoroutine(ExecuteDelayed(() =>
            {
                levelPreviews[currentIndex].Deselect();

                currentIndex = levelNumber - 1;

                levelPreviews[currentIndex].Select();

                // 마스터가 선택한 vcam 인덱스를 모든 클라이언트에 동기화
                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonView photonView = GetComponent<PhotonView>();
                    if (photonView != null)
                    {
                        photonView.RPC("RPC_SwitchPreviewVCam", RpcTarget.All, currentIndex);
                    }
                }
            }, 0.5f));
        }

        public void SelectNextLevel()
        {
            if (currentIndex < levelPreviews.Count - 1)
            {
                levelPreviews[currentIndex].Deselect();

                currentIndex++;

                levelPreviews[currentIndex].Select();
            }

            var button = buttonContainer.transform.GetChild(currentIndex);
            button.GetComponent<Selectable>().Select();
        }

        public void LoadLevel(int levelNumber)
        {
            StopAllCoroutines();
            
            // 마스터만 레벨 로드 RPC 호출
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonView photonView = GetComponent<PhotonView>();
                if (photonView != null)
                {
                    photonView.RPC("RPC_LoadLevel", RpcTarget.All, levelNumber);
                }
            }
        }

        [PunRPC]
        void RPC_LoadLevel(int levelNumber)
        {
            StartCoroutine(LoadLevelCoroutine(levelNumber));
        }

        IEnumerator LoadLevelCoroutine(int levelNumber)
        {
            LastLevel = levelNumber - 1;

            confirmSFX.Play();

            levelPreviews[levelNumber - 1].ZoomIn();

            yield return new WaitForSecondsRealtime(0.5f);

            int buildIndex = levelData.GetBuildIndex(levelNumber);

            sceneLoader.LoadScene(buildIndex);
        }

        IEnumerator ExecuteDelayed(UnityAction action, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            action?.Invoke();
        }

        [PunRPC]
        void RPC_SwitchPreviewVCam(int vcamIndex)
        {
            // 모든 preview vcam 중 선택된 것만 활성화
            for (int i = 0; i < previewVCams.Length; i++)
            {
                previewVCams[i].gameObject.SetActive(i == vcamIndex);
            }
        }
    }
}