using UnityEngine;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

namespace Ilumisoft.SkillDrive.Game
{
    public class SpeedUpCollectableSpawner : MonoBehaviour
    {
        [SerializeField]
        private GameObject speedUpCollectablePrefab;

        [SerializeField, Tooltip("생성할 위치들")]
        private Transform[] spawnPoints;

        private void Start()
        {
#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.IsMasterClient)
            {
                SpawnAtPoints();
            }
#else
            SpawnAtPoints();
#endif
        }

        private void SpawnAtPoints()
        {
            foreach (var point in spawnPoints)
            {
#if PHOTON_UNITY_NETWORKING
                PhotonNetwork.Instantiate("SpeedUpCollectable", point.position, point.rotation);
#else
                Instantiate(speedUpCollectablePrefab, point.position, point.rotation);
#endif
            }
        }
    }
}
