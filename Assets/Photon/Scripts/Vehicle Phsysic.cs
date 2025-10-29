using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehiclePhysic : MonoBehaviour
{
    [SerializeField] float _maxTorq = 100f;
    [SerializeField] float _maxSteerAngle = 30f;
    [SerializeField] Wheel[] _motorWheel;
    [SerializeField] Wheel[] _steerWheel;

    private Vector3 _startPosition;
    private Quaternion _startRotation;

    void Start()
    {
        _startPosition = transform.position;
        _startRotation = transform.rotation;
    }

    void Update()
    {
        float gas = Input.GetAxisRaw("Vertical");
        float steer = Input.GetAxis("Horizontal");
        MoveCar(gas, steer);

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCar();
        }
    }

    void MoveCar(float gas, float steer)
    {
        foreach (Wheel item in _motorWheel)
        {
            item.AddTorque(gas * _maxTorq);
        }
        foreach (Wheel item in _steerWheel)
        {
            item.AddSteerAngle(steer * _maxSteerAngle);
        }
    }

    void ResetCar()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = _startPosition;
        transform.rotation = _startRotation;
        FindObjectOfType<HUDManager>().IncrementRespawn();
    }
}