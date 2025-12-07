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

        int effectIndex = 0;

        // Use this for initialization

        void Start()
        {
            instantiatedEffect = Instantiate(listOfEffects[effectIndex], transform.position, transform.rotation) as GameObject;
            StartCoroutine(FireCenterColliderRoutine(instantiatedEffect));
            effectIndex++;
            timeOfLastInstantiate = Time.time;
        }

        void Update()
        {
            if (Time.time >= timeOfLastInstantiate + loopTimeLength)
            {
                // Destroy 직전에 Collider 비활성화 (안전)
                if (instantiatedEffect != null)
                {
                    ActivateFireCenterCollider(instantiatedEffect, false);
                }
                Destroy(instantiatedEffect);
                instantiatedEffect = Instantiate(listOfEffects[effectIndex], transform.position, transform.rotation) as GameObject;
                StartCoroutine(FireCenterColliderRoutine(instantiatedEffect));
                timeOfLastInstantiate = Time.time;
                if (effectIndex < listOfEffects.Count - 1)
                    effectIndex++;
                else
                    effectIndex = 0;
            }
        }
        // FireCenter Collider를 파티클이 살아있는 동안만 활성화
        IEnumerator FireCenterColliderRoutine(GameObject effectRoot)
        {
            if (effectRoot == null) yield break;

            // 파티클 시스템의 실제 지속 시간 가져오기
            var fireCenter = effectRoot.transform.Find("FireCenter");
            if (fireCenter != null)
            {
                var particleSystem = fireCenter.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    // 파티클이 시작되는 순간부터 활성화
                    ActivateFireCenterCollider(effectRoot, true);

                    // 파티클 지속 시간만큼 대기
                    yield return new WaitForSeconds(particleSystem.main.duration);

                    // 파티클이 끝나면 즉시 비활성화
                    ActivateFireCenterCollider(effectRoot, false);
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
