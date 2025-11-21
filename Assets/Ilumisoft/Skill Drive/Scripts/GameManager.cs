using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Ilumisoft.SkillDrive.Game
{
    [RequireComponent(typeof(CollectableManager), typeof(AudioSource))]
    public class GameManager : MonoBehaviour
    {
        [Header("Result UI Panels")]
        [SerializeField] private GameObject showWinUI;
        [SerializeField] private GameObject showLoseUI;
        [SerializeField] private float resultPanelDuration = 4f;

        [Header("Scene Management")]
        [SerializeField] private Ilumisoft.SkillDrive.SceneLoader sceneLoader = null; // 인스펙터에 Scene Loader 오브젝트 할당
    
        public static bool IsRetry = false;

        [SerializeField]
        LevelData levelData = null;

        [SerializeField, Header("Game")]
        Countdown countdown = null;

        [SerializeField, Tooltip("Delay before the countdown starts")]
        float startDelay = 0.0f;

        [SerializeField, Tooltip("Time limit of the countdown")]
        float timeLimit = 10.0f;

        [SerializeField, Header("SFX")]
        AudioClip successSFX = null;

        [SerializeField]
        AudioClip failSFX = null;


        [SerializeField]
        UnityEvent onComplete = null;

        [SerializeField]
        UnityEvent onFail = null;

        AudioSource audioSource;

        CollectableManager collectableManager = null;

        // Reference to the player
        GameObject player;

        bool isGameOver = false;

        public UnityEvent OnComplete { get => this.onComplete; set => this.onComplete = value; }
        public UnityEvent OnFail { get => this.onFail; set => this.onFail = value; }

        private void Awake()
        {
            player = GameObject.FindWithTag("Player");

            collectableManager = GetComponent<CollectableManager>();
            audioSource = GetComponent<AudioSource>();

            // 콜렉터블을 모두 먹어도 결과 패널을 띄우지 않음
            collectableManager.OnAllCollected += OnAllCollected;
            // 시간 초과 시에만 결과 패널을 띄움
            countdown.OnTimeOut += OnTimeOut;
        }

        private IEnumerator Start()
        {
            countdown.SetTimeLimit(timeLimit);

            yield return new WaitForSeconds(startDelay);

            // 멀티플레이(Photon 룸)에서는 카운트다운 비활성화 (무제한 시간)
            bool inMultiplayer = false;
#if PHOTON_UNITY_NETWORKING
            inMultiplayer = Photon.Pun.PhotonNetwork.IsConnected && Photon.Pun.PhotonNetwork.InRoom;
#endif

            if (!inMultiplayer)
            {
                countdown.StartCountdown();
                Ilumisoft.SkillDrive.SkillDriveDebug.LogGameplay("[GameManager] Countdown started (single player mode)");
            }
            else
            {
                countdown.StopTimer();
                Ilumisoft.SkillDrive.SkillDriveDebug.LogGameplay("[GameManager] Countdown disabled (multiplayer mode)");
            }

            isGameOver = false;
        }

        /// <summary>
        /// Callback invoked when the player collected all cubes
        /// </summary>
        private void OnAllCollected()
        {
            // 콜렉터블을 모두 먹어도 결과 패널을 띄우지 않음 (원한다면 여기서 EndLevel(true) 등으로 별도 처리 가능)
            Debug.Log("모든 콜렉터블을 수집했으나, 시간 종료가 아니므로 결과 패널을 띄우지 않음");
        }

        /// <summary>
        /// Callback invoked when the player run out of time
        /// </summary>
        private void OnTimeOut()
        {

            JudgeAndShowResult();
        }

        /// <summary>
        /// 차량별 수집품 개수 비교 후 결과 패널 표시 및 자동 씬 전환
        /// </summary>
        private void JudgeAndShowResult()
        {
            Debug.Log("JudgeAndShowResult 호출됨");
            // 1. 내 차량 찾기
            var myVehicle = FindMyVehicle();
            if (myVehicle == null)
            {
                Debug.LogWarning("[GameManager] 내 VehicleCollectables를 찾지 못했습니다.");
                EndLevel(false);
                return;
            }

            // 2. 모든 차량의 수집품 개수 비교
            var allVehicles = FindObjectsOfType<Ilumisoft.SkillDrive.Game.VehicleCollectables>();
            int myCount = myVehicle.CollectedCount;
            int maxOther = int.MinValue;
            foreach (var v in allVehicles)
            {
                if (v != myVehicle)
                {
                    if (v.CollectedCount > maxOther) maxOther = v.CollectedCount;
                }
            }

            // 3. 결과 판정
            string result = "draw";
            if (myCount > maxOther) result = "win";
            else if (myCount < maxOther) result = "lose";

            // 4. 결과 패널 표시
            if (showWinUI != null) showWinUI.SetActive(false);
            if (showLoseUI != null) showLoseUI.SetActive(false);
            if (result == "win" && showWinUI != null) {
                Debug.Log("Win 패널 활성화");
                showWinUI.SetActive(true);
            }
            else if (result == "lose" && showLoseUI != null) {
                Debug.Log("Lose 패널 활성화");
                showLoseUI.SetActive(true);
            }
            // draw일 때는 둘 다 안 띄움(원하면 별도 패널 추가 가능)

            // 5. 4초 후 자동 씬 전환
            Debug.Log("씬 전환 코루틴 시작, 딜레이: " + resultPanelDuration);
            StartCoroutine(GoToNextSceneAfterDelay(resultPanelDuration));
        }

        /// <summary>
        /// 내 VehicleCollectables 찾기 (내가 조종한 차량)
        /// </summary>
        private Ilumisoft.SkillDrive.Game.VehicleCollectables FindMyVehicle()
        {
            // Photon이 연결되어 있으면 ActorNumber로, 아니면 Player 태그로 찾기
#if PHOTON_UNITY_NETWORKING
            if (Photon.Pun.PhotonNetwork.IsConnected && Photon.Pun.PhotonNetwork.LocalPlayer != null)
            {
                int myActor = Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber;
                var all = FindObjectsOfType<Ilumisoft.SkillDrive.Game.VehicleCollectables>();
                foreach (var v in all)
                {
                    var seatMgr = v.GetComponent<Ilumisoft.SkillDrive.Network.VehicleSeatManager>();
                    if (seatMgr != null)
                    {
                        var (t, s) = seatMgr.GetAssignedActors();
                        if (t == myActor || s == myActor)
                            return v;
                    }
                }
            }
#endif
            // 싱글플레이어 등: Player 태그로 찾기
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                return playerObj.GetComponent<Ilumisoft.SkillDrive.Game.VehicleCollectables>();
            return null;
        }

        private System.Collections.IEnumerator GoToNextSceneAfterDelay(float delay)
        {
            Debug.Log("GoToNextSceneAfterDelay 시작, delay: " + delay);
            yield return new WaitForSeconds(delay);
            Debug.Log("GoToNextSceneAfterDelay 끝, 씬 전환 시도");

            // 결과 씬 또는 LevelSelection으로 이동 (마지막 맵이면 ResultScene)
            bool isLastMap = false;
            var gsm = Ilumisoft.SkillDrive.Game.GameSessionManager.Instance;
            if (gsm != null)
            {
                isLastMap = (gsm.PlayedMapsCount >= gsm.TotalMaps);
            }
            string nextScene = isLastMap ? "ResultScene" : "Level Selection";

#if PHOTON_UNITY_NETWORKING
            if (Photon.Pun.PhotonNetwork.IsMasterClient)
            {
                if (sceneLoader != null)
                {
                    sceneLoader.LoadScene(nextScene);
                }
                else
                {
                    Photon.Pun.PhotonNetwork.LoadLevel(nextScene);
                }
            }
#else
            if (sceneLoader != null)
            {
                sceneLoader.LoadScene(nextScene);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextScene);
            }
#endif
        }
        

        /// <summary>
        /// Ends the current level
        /// </summary>
        /// <param name="won"></param>
        public void EndLevel(bool won)
        {
            if (!isGameOver)
            {
                // Make sure this is only called once
                isGameOver = true;

                Ilumisoft.SkillDrive.SkillDriveDebug.LogGameplay($"[GameManager] EndLevel called. won={won}. (timeLimit may have expired or all collected)");

                var audioClip = won ? successSFX : failSFX;

                if (audioSource != null && audioClip != null)
                {
                    audioSource.PlayOneShot(audioClip);
                }

                var vehicle = player != null ? player.GetComponent<Vehicle>() : null;

                // 멀티플레이(Photon 룸)에서는 차량 이동을 강제로 막지 않음. 싱글/오프라인에서만 락.
                bool inMultiplayer = false;
#if PHOTON_UNITY_NETWORKING
                inMultiplayer = Photon.Pun.PhotonNetwork.IsConnected && Photon.Pun.PhotonNetwork.InRoom;
#endif
                if (vehicle != null && !inMultiplayer)
                {
                    vehicle.CanMove = false;
                    vehicle.Rigidbody.linearDamping = 2.0f;
                }
                else if (vehicle == null)
                {
                    Ilumisoft.SkillDrive.SkillDriveDebug.LogGameplay("[GameManager] Vehicle not found on 'player' object. Skipping movement lock.");
                }

                if(won)
                {
                    UnlockNextLevel();

                    OnComplete?.Invoke(); 
                }
                else
                {
                    OnFail?.Invoke();
                }
            }
        }

        void UnlockNextLevel()
        {
            // Get the level number of the current scene
            int currentLevelNumber = levelData.GetLevelNumber(gameObject.scene);

            // Unlock the next level
            LevelLockManager.UnlockLevel(currentLevelNumber + 1);
        }
    }
}