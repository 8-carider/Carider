using UnityEngine;

public class FlagWaver : MonoBehaviour
{
    public float waveSpeed = 2f;
    public float waveStrength = 0.05f;

    private Vector3 basePos;

    private void Start()
    {
        basePos = transform.localPosition;
    }

    private void Update()
    {
        float offset = Mathf.Sin(Time.time * waveSpeed) * waveStrength;
        transform.localPosition = new Vector3(basePos.x - 0.3f * offset, basePos.y + 0.3f * offset, basePos.z + 0.1f * offset);
    }
}
