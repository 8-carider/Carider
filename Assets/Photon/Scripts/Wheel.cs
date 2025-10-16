using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wheel : MonoBehaviour
{
    [SerializeField] WheelCollider _wheelCollider; // 휠 콜라이더 컴포넌트 
    [SerializeField] Transform _wheelTF; // 바퀴의 3D모델 회전에 따라 바뀜 

    private void FixedUpdate() // 물리 업데이트 주기 
    {
        _wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        //휠 콜라이더의 값을 3D 모델에 그대로 할당하여 일치하도록함 
        _wheelTF.position = pos;
        _wheelTF.rotation = rot;
    }

    public void AddTorque (float torq) //바퀴 충돌체에 모터 토크 설정 
    {
        _wheelCollider.motorTorque = torq;
    }

    public void AddSteerAngle(float angle) //조향 각도 설정 
    {
        _wheelCollider.steerAngle = angle;
    }
}
