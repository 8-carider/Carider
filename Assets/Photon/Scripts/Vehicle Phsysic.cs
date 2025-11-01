using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehiclePhysic : MonoBehaviour
{
    [SerializeField] float _maxTorq = 100f;
    [SerializeField] float _maxSteerAngle = 30f;
    [SerializeField] Wheel[] _motorWheel;
    [SerializeField] Wheel[] _steerWheel;

    private void Update()
    {
        float gas = Input.GetAxis("Vertical");
        float steer = Input.GetAxis("Horizontal");
        MoveCar(gas, steer);
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
}