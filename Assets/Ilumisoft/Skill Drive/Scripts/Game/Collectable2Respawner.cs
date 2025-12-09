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
        // Run respawn on an external helper so it survives this object being destroyed
        Collectable2RespawnRunner.ScheduleRespawn(originalPosition, originalRotation, respawnDelay, originalPrefab);
    }
}

/// <summary>
/// Helper runner that survives the original collectable destruction and respawns a new one after delay.
/// </summary>
internal class Collectable2RespawnRunner : MonoBehaviour
{
    private Vector3 pos;
    private Quaternion rot;
    private float delay;
    private GameObject prefab;

    public static void ScheduleRespawn(Vector3 position, Quaternion rotation, float respawnDelay, GameObject fallbackPrefab)
    {
        var go = new GameObject("Collectable2RespawnRunner");
        // ⚠️ DO NOT use DontDestroyOnLoad - let it be destroyed when scene unloads
        var runner = go.AddComponent<Collectable2RespawnRunner>();
        runner.pos = position;
        runner.rot = rotation;
        runner.delay = respawnDelay;
        runner.prefab = fallbackPrefab;
        runner.StartCoroutine(runner.Run());
    }

    private IEnumerator Run()
    {
        yield return new WaitForSeconds(delay);

#if PHOTON_UNITY_NETWORKING
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
        {
            try
            {
                PhotonNetwork.Instantiate("Collectable2", pos, rot);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Collectable2RespawnRunner] PhotonNetwork.Instantiate failed: {ex.Message}. Falling back to local instantiate");
                if (prefab != null)
                {
                    Instantiate(prefab, pos, rot);
                }
            }
        }

#else
        if (prefab != null)
        {
            Instantiate(prefab, pos, rot);
        }
        else
        {
            Debug.LogError("[Collectable2RespawnRunner] Offline mode but prefab is null!");
        }
#endif

        Destroy(gameObject);
    }
}
