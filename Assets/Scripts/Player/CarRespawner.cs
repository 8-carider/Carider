using UnityEngine;

public class CarRespawner : MonoBehaviour
{
    public Transform respawnPoint; // ������ ��ġ
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Respawn();
        }
    }

    void Respawn()
    {
        if (respawnPoint == null || rb == null) return;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = respawnPoint.position;
        transform.rotation = respawnPoint.rotation;

        // HUDManager�� ������ Ƚ�� ���� ��û
        HUDManager hud = FindObjectOfType<HUDManager>();
        if (hud != null)
        {
            hud.IncrementRespawn();
        }
    }
}