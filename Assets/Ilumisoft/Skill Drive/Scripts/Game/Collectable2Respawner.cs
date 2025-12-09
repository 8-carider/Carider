using UnityEngine;
using Photon.Pun;
using System.Collections;

public class Collectable2Respawner : MonoBehaviour
{
    [Tooltip("아이템 재생성 대기 시간 (초)")]
    public float respawnDelay = 30f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private GameObject originalPrefab;

    private void Awake()
    {
        // 원래 위치 기억
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // Resources 폴더에서 원본 프리팹 로드
        originalPrefab = Resources.Load<GameObject>("Collectable2");

        if (originalPrefab == null)
        {
            Debug.LogError("[Collectable2Respawner] Resources/Collectable2.prefab 을 찾지 못했습니다!");
        }
    }

    /// <summary>
    /// Collectable2.cs에서 먹었을 때 호출됨
    /// </summary>
    public void TriggerRespawn()
    {
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnDelay);

#if PHOTON_UNITY_NETWORKING
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Instantiate("Collectable2", originalPosition, originalRotation);
        }
#else
        Instantiate(originalPrefab, originalPosition, originalRotation);
#endif
    }
}
