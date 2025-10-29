using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrivingCamera : MonoBehaviour
{
    public Transform target;             // 따라갈 차량
    public Vector3 offset = new Vector3(3, 5, -12);   // 차량 뒤에서 카메라 위치 오프셋

    public float positionSmoothTime = 0.3f;   // 위치 보간 속도
    private Vector3 positionVelocity = Vector3.zero;

    public float rotationSmoothTime = 0.1f;   // 회전 보간 속도
    private float rotationVelocity;

    // 고정된 피치 각도(카메라가 위에서 바라보도록)
    public float fixedPitchAngle = 10f;

    void LateUpdate()
    {
        // 차량 위치와 회전 반영한 목표 위치
        Vector3 desiredPosition = target.position + target.rotation * offset;

        // 위치 부드럽게 변환(SmoothDamp)
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime);

        // 차량의 y축 회전을 기준으로 회전 보간(SmoothDampAngle)
        float targetAngle = target.eulerAngles.y;
        float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, rotationSmoothTime);

        // 카메라 회전 설정 (x: 피치 고정, y: 보간된 차량 회전, z: 0으로 롤 제거)
        transform.rotation = Quaternion.Euler(fixedPitchAngle, smoothAngle, 0);
    }
}
