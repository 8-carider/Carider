using Ilumisoft.SkillDrive.Game;
using System.Collections;
using UnityEngine;

namespace Ilumisoft.SkillDrive.Effects
{
    /// <summary>
    /// Effect creating a dynamic engine sfx effect by adjusting the pitch of the engine audio source depending on the velocity of the vehicle
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class VehicleEngineAudioEffect : VehicleComponent
    {
        GameManager gameManager;
        Network.VehicleSeatManager seatManager;
        bool isDialogueActive = false; // Dialogue 활성 상태

        [SerializeField]
        float minPitch = 0.25f;

        [SerializeField]
        float maxPitch = 1.1f;

        [SerializeField]
        float multiplier = 1.2f;

        AudioSource audioSource;

        private void Awake()
        {
            gameManager = FindAnyObjectByType<GameManager>();
            audioSource = GetComponent<AudioSource>();
            // SeatManager는 상위(차량 루트)에 있을 수 있음
            seatManager = GetComponentInParent<Network.VehicleSeatManager>();
        }

        private void Start()
        {
            if (gameManager != null)
            {
                gameManager.OnComplete.AddListener(OnFinish);
                gameManager.OnFail.AddListener(OnFinish);
            }
        }

        private void OnFinish()
        {
            StartCoroutine(MuteCoroutine());
        }

        IEnumerator MuteCoroutine()
        {
            float duration = 1.0f;

            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                audioSource.volume = 1 - Mathf.Lerp(0, 1, elapsed / duration);

                yield return null;

                elapsed += Time.deltaTime;
            }
        }

        void Update()
        {
            if (Vehicle != null && audioSource != null)
            {
                // Time.timeScale이 0이면 Dialogue 중 (일시정지)
                CheckDialogueState();

                // Dialogue 중이거나 자리 선택 중에는 엔진 소리 끄기
                if (isDialogueActive || !IsLocallyControlled() || !HasPlayerAssigned())
                {
                    audioSource.mute = true;
                }
                else
                {
                    audioSource.mute = false;
                    audioSource.pitch = Mathf.Lerp(minPitch, maxPitch, Vehicle.NormalizedForwardSpeed * multiplier);
                }
            }
        }

        private void CheckDialogueState()
        {
            // Time.timeScale이 0이면 Dialogue 활성 (게임 일시정지)
            isDialogueActive = Time.timeScale == 0f;
        }

        private bool HasPlayerAssigned()
        {
#if PHOTON_UNITY_NETWORKING
            // Photon에 연결되어 있지 않으면 항상 true (싱글플레이)
            if (!Photon.Pun.PhotonNetwork.IsConnected)
            {
                return true;
            }

            // 좌석에 플레이어가 배정되었는지 확인
            if (seatManager != null)
            {
                var (t, s) = seatManager.GetAssignedActors();
                // 스로틀이나 조향 중 하나라도 배정되어 있으면 true
                return t != -1 || s != -1;
            }

            return true;
#else
            return true;
#endif
        }

        private bool IsLocallyControlled()
        {
#if PHOTON_UNITY_NETWORKING
            // Photon에 연결되어 있지 않으면 싱글플레이어로 간주
            if (!Photon.Pun.PhotonNetwork.IsConnected)
            {
                return true;
            }

            // Seat-based control: check assigned actors (우선)
            if (seatManager != null && Photon.Pun.PhotonNetwork.LocalPlayer != null)
            {
                var (t, s) = seatManager.GetAssignedActors();
                int local = Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber;
                if (t == local || s == local) return true;
            }

            // PhotonView 소유권 체크 (fallback)
            var pv = GetComponentInParent<Photon.Pun.PhotonView>();
            if (pv != null && pv.IsMine) return true;

            return false;
#else
            // 싱글플레이어
            return true;
#endif
        }
    }
}