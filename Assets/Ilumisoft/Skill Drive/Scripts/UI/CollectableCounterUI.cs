using TMPro;
using UnityEngine;
using Ilumisoft.SkillDrive.Game;
using Photon.Pun;

namespace Ilumisoft.SkillDrive
{
    public class CollectableCounterUI : MonoBehaviourPunCallbacks
    {
        [SerializeField] private TextMeshProUGUI counterText;
        [SerializeField] private VehicleCollectables vehicleCollectables;

        private void Awake()
        {
            // counterText가 연결되지 않았다면 자동으로 찾기
            if (counterText == null)
            {
                counterText = GetComponent<TextMeshProUGUI>();
            }
            
            // vehicleCollectables가 연결되지 않았다면 자동으로 찾기
            if (vehicleCollectables == null)
            {
                var playerVehicle = GameObject.FindGameObjectWithTag("Player");
                if (playerVehicle != null)
                {
                    vehicleCollectables = playerVehicle.GetComponent<VehicleCollectables>();
                }
            }
            
            UpdateCounterText();
        }

        private void OnEnable()
        {
            if (vehicleCollectables != null)
            {
                vehicleCollectables.OnCollectableCollected += UpdateCounterText;
            }
        }

        private void OnDisable()
        {
            if (vehicleCollectables != null)
            {
                vehicleCollectables.OnCollectableCollected -= UpdateCounterText;
            }
        }

        private void UpdateCounterText()
        {
            if (counterText != null && vehicleCollectables != null)
            {
                counterText.text = $"{vehicleCollectables.CollectedCount}";
            }
        }

        public override void OnJoinedRoom()
        {
            Debug.Log($"룸 참가 성공. 현재 인원: {PhotonNetwork.CurrentRoom.PlayerCount}");
            // 레벨 셀렉트 씬으로 전환 (모든 클라이언트 동기화)
            PhotonNetwork.LoadLevel("Level Selection");
        }
    }

    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] private string vehiclePrefabName = "Vehicle"; // 실제 프리팹 이름으로 교체
        [SerializeField] private Transform[] spawnPoints;

        private void Start()
        {
            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
            {
                // 마스터가 모든 플레이어 스폰 위치 결정 또는 그냥 각자 스폰
            }

            SpawnLocalPlayer();
        }

        void SpawnLocalPlayer()
        {
            if (!PhotonNetwork.IsConnected) return;

            // 간단한 스폰 위치 선택 (플레이어 ID 기반)
            int index = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
            var sp = spawnPoints[index];
            PhotonNetwork.Instantiate(vehiclePrefabName, sp.position, sp.rotation);
        }
    }
}
