using UnityEngine;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

namespace Ilumisoft.SkillDrive.Game
{
    public class SpeedUpCollectableManager : MonoBehaviour
    {
        [SerializeField]
        private GameObject speedUpCollectablePrefab;

        [SerializeField]
        private int speedUpCount = 3;

        [SerializeField]
        private Vector2 spawnAreaSize = new Vector2(10f, 10f);

        private void Start()
        {
#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.IsMasterClient)
            {
                SpawnSpeedUpCollectables();
            }
#else
            SpawnSpeedUpCollectables();
#endif
        }

        private void SpawnSpeedUpCollectables()
        {
            for (int i = 0; i < speedUpCount; i++)
            {
                Vector3 spawnPosition = transform.position + new Vector3(
                    Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f),
                    0.5f,
                    Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f)
                );
#if PHOTON_UNITY_NETWORKING
                PhotonNetwork.Instantiate("SpeedUpCollectable", spawnPosition, Quaternion.identity);
#else
                Instantiate(speedUpCollectablePrefab, spawnPosition, Quaternion.identity);
#endif
            }
        }
    }
}
