using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

namespace Ilumisoft.SkillDrive.Game
{
    /// <summary>
    /// 맵 제한시간 관리 및 타이머 표시
    /// 시간 종료 시 자동으로 다음 맵으로 이동
    /// </summary>
    public class MapTimerManager : MonoBehaviourPunCallbacks
    {
        [Header("Timer Settings")]
        [SerializeField] private float mapDuration = 120f; // 기본 2분
        
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Image timerFillImage; // 선택적: 타이머 바
        
        private float remainingTime;
        private bool timerRunning = false;
        private bool mapCompleted = false;

        private void Start()
        {
            // Room CustomProperties에서 제한시간 불러오기
            if (PhotonNetwork.CurrentRoom != null &&
                PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("TimeLimit"))
            {
                mapDuration = (float)PhotonNetwork.CurrentRoom.CustomProperties["TimeLimit"];
                Debug.Log($"[MapTimer] Loaded time limit from room: {mapDuration}s");
            }

            remainingTime = mapDuration;

            // 타이머 텍스트 자동 바인딩
            if (timerText == null)
            {
                // 1) 자신 하위에서 탐색
                timerText = GetComponentInChildren<TextMeshProUGUI>();

                // 2) 씬에 존재하는 기존 Countdown UI 재사용 시도
                if (timerText == null)
                {
                    var countdown = FindAnyObjectByType<Countdown>();
                    if (countdown != null)
                    {
                        var tmp = countdown.GetComponentInChildren<TextMeshProUGUI>();
                        if (tmp != null)
                        {
                            timerText = tmp;
                            Debug.Log("[MapTimer] Bound to existing Countdown UI text");
                        }
                    }
                }
            }
        }

        private void Update()
        {
            if (!timerRunning || mapCompleted) return;

            // 마스터만 시간 체크
            if (PhotonNetwork.IsMasterClient)
            {
                remainingTime -= Time.deltaTime;

                if (remainingTime <= 0f)
                {
                    remainingTime = 0f;
                    OnTimeUp();
                }

                // 모든 클라이언트에 시간 동기화
                photonView.RPC("RPC_SyncTimer", RpcTarget.Others, remainingTime);
            }

            UpdateTimerDisplay();
        }

        /// <summary>
        /// 타이머 시작 (게임 시작 시 호출)
        /// </summary>
        public void StartTimer()
        {
            timerRunning = true;
            Debug.Log($"[MapTimer] Timer started: {mapDuration} seconds");
        }

        /// <summary>
        /// 타이머 정지
        /// </summary>
        public void StopTimer()
        {
            timerRunning = false;
        }

        /// <summary>
        /// 시간 종료 시 처리
        /// </summary>
        private void OnTimeUp()
        {
            if (mapCompleted) return;
            mapCompleted = true;
            timerRunning = false;

            Debug.Log("[MapTimer] Time's up! Moving to next map...");

            // 모든 클라이언트에 시간 종료 알림
            photonView.RPC("RPC_OnTimeUp", RpcTarget.All);

            // 다음 맵으로 이동 (마스터만)
            if (PhotonNetwork.IsMasterClient)
            {
                Invoke(nameof(MoveToNextMap), 2f); // 2초 후 이동
            }
        }

        private void MoveToNextMap()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            GameSessionManager.Instance.CompleteCurrentMap();
        }

        [PunRPC]
        private void RPC_SyncTimer(float time)
        {
            remainingTime = time;
        }

        [PunRPC]
        private void RPC_OnTimeUp()
        {
            mapCompleted = true;
            timerRunning = false;
            Debug.Log("[MapTimer] Time up notification received");
            
            // 시간 종료 효과 (선택적)
            if (timerText != null)
            {
                timerText.color = Color.red;
            }

            // 시간 종료 시 모든 차량 정지 (클라이언트 포함)
            var vehicles = FindObjectsOfType<Ilumisoft.SkillDrive.Vehicle>();
            foreach (var v in vehicles)
            {
                v.CanMove = false;
                if (v.Rigidbody != null)
                {
                    v.Rigidbody.linearDamping = 2.0f;
                }
            }
        }

        /// <summary>
        /// 타이머 UI 업데이트
        /// </summary>
        private void UpdateTimerDisplay()
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(remainingTime / 60f);
                int seconds = Mathf.FloorToInt(remainingTime % 60f);
                
                timerText.text = $"Time: {minutes:00}:{seconds:00}";

                // 시간이 30초 이하면 빨간색
                if (remainingTime <= 30f && remainingTime > 0f)
                {
                    timerText.color = Color.Lerp(Color.yellow, Color.red, 1f - (remainingTime / 30f));
                }
                else
                {
                    timerText.color = Color.white;
                }
            }

            // 타이머 바 업데이트 (있는 경우)
            if (timerFillImage != null)
            {
                timerFillImage.fillAmount = remainingTime / mapDuration;
            }
        }

        /// <summary>
        /// 제한시간 설정 (게임 시작 전)
        /// </summary>
        public void SetMapDuration(float seconds)
        {
            mapDuration = seconds;
            remainingTime = seconds;
        }

        public float GetRemainingTime()
        {
            return remainingTime;
        }
    }
}
