using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DualControlVehicle : MonoBehaviour
{
    [Header("앞 바퀴 (Player 1 - Steering)")]
    [SerializeField] Wheel[] frontWheels; // 앞 바퀴 2개
    [SerializeField] string steerAxis = "Horizontal"; // 좌우 조향
    [SerializeField] float maxSteerAngle = 30f;

    [Header("뒤 바퀴 (Player 2 - Gas/Brake)")]
    [SerializeField] Wheel[] rearWheels; // 뒤 바퀴 2개
    [SerializeField] KeyCode forwardKey = KeyCode.UpArrow;
    [SerializeField] KeyCode backwardKey = KeyCode.DownArrow;
    [SerializeField] float maxTorque = 100f;

    private Vector3 _startPos;
    private Quaternion _startRot;

    void Start()
    {
        _startPos = transform.position;
        _startRot = transform.rotation;
    }

    void Update()
    {
        // Player 1 입력: 조향
        float steerInput = Input.GetAxisRaw(steerAxis);
        foreach (var wheel in frontWheels)
        {
            wheel.AddSteerAngle(steerInput * maxSteerAngle);
        }

        // Player 2 입력: 가속/브레이크
        float torqueInput = 0f;
        if (Input.GetKey(forwardKey)) torqueInput = 1f;
        else if (Input.GetKey(backwardKey)) torqueInput = -1f;

        foreach (var wheel in rearWheels)
        {
            wheel.AddTorque(torqueInput * maxTorque);
        }

        // Reset
        if (Input.GetKeyDown(KeyCode.R))
            ResetCar();
    }

    void ResetCar()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = _startPos;
        transform.rotation = _startRot;
    }
}
