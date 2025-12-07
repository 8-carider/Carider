using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PolygonArsenal
{
    public class PolygonEffectCycler : MonoBehaviour
    {
        [SerializeField]
        List<GameObject> listOfEffects;

        [Header("Loop length in seconds")]
        [SerializeField]
        float loopTimeLength = 5f;

        float timeOfLastInstantiate;

        GameObject instantiatedEffect;
        BoxCollider fireCenterCollider;
        Coroutine activeColliderRoutine; // 현재 실행 중인 코루틴 추적

        int effectIndex = 0;

        // Use this for initialization

        void Start()
        {
            instantiatedEffect = Instantiate(listOfEffects[effectIndex], transform.position, transform.rotation) as GameObject;
            // 즉시 Collider 비활성화 (프리팹의 기본값이 enabled=true일 수 있음)
            ActivateFireCenterCollider(instantiatedEffect, false);
            activeColliderRoutine = StartCoroutine(FireCenterColliderRoutine(instantiatedEffect));
            effectIndex++;
            timeOfLastInstantiate = Time.time;
        }

        void Update()
        {
            if (Time.time >= timeOfLastInstantiate + loopTimeLength)
            {
                // 이전 코루틴 중단 (확실한 정리)
                if (activeColliderRoutine != null)
                {
                    StopCoroutine(activeColliderRoutine);
                    activeColliderRoutine = null;
                }

                // Destroy 직전에 Collider 비활성화 (안전)
                if (instantiatedEffect != null)
                {
                    ActivateFireCenterCollider(instantiatedEffect, false);
                }
                Destroy(instantiatedEffect);

                // 새 파티클 생성 및 새 코루틴 시작
                instantiatedEffect = Instantiate(listOfEffects[effectIndex], transform.position, transform.rotation) as GameObject;
                // 즉시 Collider 비활성화 (프리팹의 기본값이 enabled=true일 수 있음)
                ActivateFireCenterCollider(instantiatedEffect, false);
                activeColliderRoutine = StartCoroutine(FireCenterColliderRoutine(instantiatedEffect));
                timeOfLastInstantiate = Time.time;
                if (effectIndex < listOfEffects.Count - 1)
                    effectIndex++;
                else
                    effectIndex = 0;
            }
        }
        // FireCenter의 startDelay를 고려하여 정확한 타이밍에 Collider 활성화
        IEnumerator FireCenterColliderRoutine(GameObject effectRoot)
        {
            if (effectRoot == null) yield break;

            // FireCenter 파티클 정보 가져오기
            var fireCenter = effectRoot.transform.Find("FireCenter");
            if (fireCenter != null)
            {
                var fireCenterPS = fireCenter.GetComponent<ParticleSystem>();
                if (fireCenterPS != null)
                {
                    var main = fireCenterPS.main;
                    float startDelay = main.startDelay.constant; // FireCenter의 startDelay
                    float duration = main.duration; // FireCenter의 실제 재생 시간

                    // startDelay 동안 대기 (충전 중 - 안전)
                    ActivateFireCenterCollider(effectRoot, false);
                    Debug.Log($"[{Time.time:F2}] Collider OFF - ChargeUp 시작");
                    yield return new WaitForSeconds(startDelay);

                    // 레이저 발사 시작 - Collider 활성화
                    ActivateFireCenterCollider(effectRoot, true);
                    Debug.Log($"[{Time.time:F2}] Collider ON - FireCenter 발사");
                    yield return new WaitForSeconds(duration);

                    // 레이저 발사 끝 - Collider 비활성화
                    ActivateFireCenterCollider(effectRoot, false);
                    Debug.Log($"[{Time.time:F2}] Collider OFF - FireCenter 종료");
                }
            }
        }

        // FireCenter라는 이름의 자식 오브젝트에서 BoxCollider를 찾아 활성/비활성
        void ActivateFireCenterCollider(GameObject effectRoot, bool enable)
        {
            if (effectRoot == null) return;
            var fireCenter = effectRoot.transform.Find("FireCenter");
            if (fireCenter != null)
            {
                var collider = fireCenter.GetComponent<BoxCollider>();
                if (collider != null)
                {
                    collider.enabled = enable;
                }
            }
        }
    }
}
