using UnityEngine;
using System.Collections;

public class TrapDoorPair : MonoBehaviour
{
    [Header("연결 1: 문 경첩 오브젝트")]
    public Transform leftHinge;  
    public Transform rightHinge;

    [Header("연결 2: 색깔 바꿀 문짝들 (Cube)")]
    public MeshRenderer[] doorMeshes; 


    [Header("설정 1: 경고(Warning)")]
    public float warningTime = 1.0f;     
    [Tooltip("진한 빨강 시간")] public float redDuration = 0.3f;     
    [Tooltip("연한 빨강 시간")] public float lightRedDuration = 0.1f; 
    public Material redMat;          
    public Material lightRedMat;     

    [Header("설정 2: 열릴 때")]
    public float openSpeed = 1.5f;     
    public float targetAngle = -90.0f; 

    [Header("설정 3: 유지 및 닫힐 때")]
    public float stayOpenTime = 2.0f; 
    public float closeSpeed = 0.5f;   

    [Header("--- 정보 ---")]
    public float totalDuration; 

    private bool isRunning = false; 
    private Material originalMat; 

    // 인스펙터 값 바뀔 때마다 시간 자동 계산
    void OnValidate()
    {
        float openTime = (openSpeed > 0) ? Mathf.Abs(targetAngle) / (openSpeed * 100) : 0;
        float closeTime = (closeSpeed > 0) ? Mathf.Abs(targetAngle) / (closeSpeed * 100) : 0;
        totalDuration = warningTime + openTime + stayOpenTime + closeTime;
    }

    void Start()
    {
        // 원래 문 색깔 기억하기
        if (doorMeshes.Length > 0 && doorMeshes[0] != null)
            originalMat = doorMeshes[0].material;
    }

    public void Open()
    {
        if (!isRunning)
        {
            isRunning = true;
            StartCoroutine(DoorSequence());
        }
    }

    IEnumerator DoorSequence()
    {
        
        float timer = 0f;
        while (timer < warningTime)
        {
            ChangeMaterial(redMat);
            yield return new WaitForSeconds(redDuration);
            timer += redDuration;
            if (timer >= warningTime) break;

            ChangeMaterial(lightRedMat);
            yield return new WaitForSeconds(lightRedDuration);
            timer += lightRedDuration;
        }
        ChangeMaterial(originalMat);


        
        float currentAngle = 0f;
        while (Mathf.Abs(currentAngle - targetAngle) > 0.1f)
        {
            currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, openSpeed * 100 * Time.deltaTime);
            ApplyRotation(currentAngle);
            yield return null;
        }
        ApplyRotation(targetAngle); 

        // 2단계: 유지
        yield return new WaitForSeconds(stayOpenTime);

        // 3단계: 문 닫기 애니메이션
        while (Mathf.Abs(currentAngle - 0f) > 0.1f)
        {
            currentAngle = Mathf.MoveTowards(currentAngle, 0f, closeSpeed * 100 * Time.deltaTime);
            ApplyRotation(currentAngle);
            yield return null;
        }
        ApplyRotation(0f); 



        isRunning = false; 
    }

    void ChangeMaterial(Material mat)
    {
        foreach (MeshRenderer mesh in doorMeshes)
        {
            if (mesh != null) mesh.material = mat;
        }
    }

    void ApplyRotation(float angle)
    {
        if (leftHinge != null) leftHinge.localRotation = Quaternion.Euler(-angle, 0, 0);
        if (rightHinge != null) rightHinge.localRotation = Quaternion.Euler(angle, 0, 0);
    }
}