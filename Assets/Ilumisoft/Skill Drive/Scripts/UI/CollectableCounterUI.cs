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
            // MapVehicleSpawner가 있으면 절대 차량 생성하지 않음
            if (FindObjectOfType<Ilumisoft.SkillDrive.Network.MapVehicleSpawner>() != null)
            {
                Debug.Log("[PlayerSpawner] MapVehicleSpawner detected in scene. Skipping per-player spawn.");
                Destroy(this);
                return;
            }

            SpawnLocalPlayer();
        }

        void SpawnLocalPlayer()
        {
            if (!PhotonNetwork.IsConnected) return;

            int index = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
            var sp = spawnPoints[index];
            PhotonNetwork.Instantiate(vehiclePrefabName, sp.position, sp.rotation);
        }
    }
}
