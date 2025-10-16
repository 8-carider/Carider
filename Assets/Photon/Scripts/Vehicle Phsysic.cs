using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehiclePhysic : MonoBehaviour
{
    [SerializeField] float _maxTorq = 100f; //모터 최대값 
    [SerializeField] float _maxSteerAngle = 30f; // 조향 최대값 - 임시로 할당 
    [SerializeField] Wheel[] _motorWheel; // 구동 바퀴들을 담는 배열 
    [SerializeField] Wheel[] _steerWheel; // 조향 바퀴들을 담는 배열 


    /*입력받기 */
    private void Update()
    {
        float gas = Input.GetAxisRaw("Vertical"); // 키보드의 위/아래 입력
        float steer = Input.GetAxis("Horizontal"); // 키보드의 좌/우 입력 
        MoveCar(gas, steer);
    }
    /*차량 이동*/
    void MoveCar(float gas, float steer)
    {
        foreach (Wheel item in _motorWheel) // 구동바퀴마다 토크값 전달
        {
            item.AddTorque(gas * _maxTorq); //위아래값에 최대토크를 곱해서 전달
        }
        foreach (Wheel item in _steerWheel) // 회전각도전달
        {
            item.AddSteerAngle(steer * _maxSteerAngle); // 회전값을 조향각을 곱해 전달
        }

    }
}