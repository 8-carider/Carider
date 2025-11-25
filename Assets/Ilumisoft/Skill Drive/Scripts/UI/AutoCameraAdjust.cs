using UnityEngine;
using System.Collections;

public class AutoCameraAdjust : MonoBehaviour
{
    [Header("카메라 이동 속도")]
    public float moveSpeed = 0.05f;

    private Coroutine moveRoutine;

    public void AdjustCameraToHeight(float targetHeight)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveRoutine(targetHeight));
    }

    private IEnumerator MoveRoutine(float targetHeight)
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = new Vector3(startPos.x, targetHeight, startPos.z);

        float t = 0;

        while (true)
        {
            t += Time.deltaTime * moveSpeed;

            // EaseOut (부드럽게 감속)
            float smoothT = Mathf.SmoothStep(0, 1, t);

            transform.position = Vector3.Lerp(startPos, targetPos, smoothT);

            // 오버슈팅 또는 거의 도달 시 종료
            if (Vector3.Distance(transform.position, targetPos) < 0.01f)
                break;

            yield return null;
        }

        // 목표 위치에 정확히 고정
        transform.position = targetPos;
        moveRoutine = null;
    }
}
