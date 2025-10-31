using UnityEngine;

public class MultiVehiclePhysics : MonoBehaviour
{
    private Vector3 _startPosition;
    private Quaternion _startRotation;

    void Start()
    {
        _startPosition = transform.position;
        _startRotation = transform.rotation;
    }

    public void ResetCar()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = _startPosition;
        transform.rotation = _startRotation;
    }
}
