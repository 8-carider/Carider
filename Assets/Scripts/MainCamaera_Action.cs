using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public class DrivingCamera : MonoBehaviour
//{
//    public Transform target;             // 따라갈 차량
//    public float height = 10f;           // 카메라 높이 (Y축)
//    public float positionSmoothTime = 0.3f;   // 위치 보간 속도
//    private Vector3 positionVelocity = Vector3.zero;

//    //void LateUpdate()
//    //{
//    //    // 차량의 X,Z 위치는 따라가고, Y는 고정
//    //    Vector3 desiredPosition = new Vector3(target.position.x, height, target.position.z);

//    //    // 부드럽게 위치 이동
//    //    transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime);

//    //    // 카메라 회전은 완전 탑뷰 (위에서 아래로 90도 회전)
//    //    transform.rotation = Quaternion.Euler(90f, 0f, 0f);
//    //}
//    void LateUpdate()
//    {
//        Vector3 desiredPosition = new Vector3(target.position.x, height, target.position.z);
//        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime);
//        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
//    }

//}

//public class TopDownCamera : MonoBehaviour
//{
//    public Transform target;  // 따라갈 대상

//    [SerializeField]
//    private Vector3 offset = new Vector3(0, 30, 0);  // 카메라 위치 오프셋 (인스펙터에 노출됨)

//    public float positionSmoothTime = 0.3f;  // 위치 보간 시간
//    private Vector3 positionVelocity = Vector3.zero;

//    void LateUpdate()
//    {
//        if (target == null) return;

//        Vector3 desiredPosition = target.position + offset;
//        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime);

//        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
//    }
//}


public class AdjustableAngleCamera : MonoBehaviour
{
    public Transform target;  // 따라갈 대상

    [SerializeField]
    private float distance = 10f;  // 차량 뒤로 얼마나 떨어질지 거리

    [SerializeField]
    private float height = 5f;     // 카메라 높이

    [SerializeField, Range(0f, 90f)]
    private float angle = 45f;     // 카메라 내려다보는 각도 (0 = 수평, 90 = 완전 위)

    public float positionSmoothTime = 0.3f;  // 위치 보간 시간
    private Vector3 positionVelocity = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;

        // 카메라 위치 계산: 타겟 위치에서 뒤쪽으로 distance 만큼 뒤로 가고 위로 height만큼 올라감
        // angle에 따라 y축 상승과 뒤쪽 거리 조절 (피타고라스)
        float radAngle = Mathf.Deg2Rad * angle;

        // 카메라가 뒤로 이동할 거리의 수평 거리 (cos), 높이 (sin)
        float horizontalDistance = distance * Mathf.Cos(radAngle);
        float verticalHeight = distance * Mathf.Sin(radAngle);

        // 타겟 뒤 방향: 타겟의 앞 방향 반대방향
        Vector3 behindDirection = -target.forward;

        // 원하는 위치 = 타겟 위치 + 뒤 방향 * horizontal 거리 + height 보정 + 수직 높이
        Vector3 desiredPosition = target.position + behindDirection * horizontalDistance + Vector3.up * (verticalHeight + height);

        // 부드럽게 이동
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime);

        // 회전은 타겟을 바라보도록
        // LookAt 함수는 카메라가 타겟을 바라보게 함
        transform.LookAt(target.position + Vector3.up * height);
    }
}

