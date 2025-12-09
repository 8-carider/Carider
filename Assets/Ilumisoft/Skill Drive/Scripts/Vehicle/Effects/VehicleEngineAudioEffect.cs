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
                if (IsLocallyControlled())
                {
                    audioSource.mute = false;
                    audioSource.pitch = Mathf.Lerp(minPitch, maxPitch, Vehicle.NormalizedForwardSpeed * multiplier);
                }
                else
                {
                    audioSource.mute = true;
                }
            }
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