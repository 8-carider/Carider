using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wheel : MonoBehaviour
{
    [SerializeField] WheelCollider _wheelCollider;
    [SerializeField] Transform _wheelTF;

    private void FixedUpdate()
    {
        _wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        _wheelTF.position = pos;
        _wheelTF.rotation = rot;
    }

    public void AddTorque (float torq)
    {
        _wheelCollider.motorTorque = torq;
    }

    public void AddSteerAngle(float angle)
    {
        _wheelCollider.steerAngle = angle;
    }
}
