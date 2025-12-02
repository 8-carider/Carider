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
        // FireCenter Collider를 2초~3초 구간(1초간)만 활성화
        IEnumerator FireCenterColliderRoutine(GameObject effectRoot)
        {
            ActivateFireCenterCollider(effectRoot, false);
            yield return new WaitForSeconds(2f);
            ActivateFireCenterCollider(effectRoot, true);
            yield return new WaitForSeconds(1f);
            ActivateFireCenterCollider(effectRoot, false);
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
