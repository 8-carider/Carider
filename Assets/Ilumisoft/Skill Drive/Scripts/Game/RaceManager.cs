using UnityEngine;
using Ilumisoft.SkillDrive.UI;
using System.Collections;
using Photon.Pun;

namespace Ilumisoft.SkillDrive.Game
{
    public class RaceManager : MonoBehaviourPun
    {
        [SerializeField] private RaceCountdownUI countdownUI;
        [SerializeField] private Vehicle[] vehicles;
        [SerializeField] private float countdownDelay = 0.5f;

        public System.Action OnRaceStarted;
        public bool RaceStarted { get; private set; } = false;


        // 마스터가 Start 버튼을 누르면 호출 (예: MenuNetworkManager에서 호출)
        public void MasterStartRace()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_StartRace", RpcTarget.All);
            }
        }

        [PunRPC]
        private void RPC_StartRace()
        {
            StartCoroutine(RaceStartSequence());
        }

        private IEnumerator RaceStartSequence()
        {
            // 차량 조작 비활성화
            foreach (var v in vehicles)
            {
                v.CanMove = false;
            }

            yield return new WaitForSeconds(countdownDelay);
            countdownUI.StartCountdown();
            countdownUI.OnCountdownFinished += OnCountdownFinished;
        }

        private void OnCountdownFinished()
        {
            // 차량 조작 활성화
            foreach (var v in vehicles)
            {
                v.CanMove = true;
            }
            RaceStarted = true;
            OnRaceStarted?.Invoke();
        }
    }
}
