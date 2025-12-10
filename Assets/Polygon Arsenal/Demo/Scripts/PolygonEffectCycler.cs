using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace PolygonArsenal
{
    public class PolygonEffectCycler : MonoBehaviour
    {
        [SerializeField]
        List<GameObject> listOfEffects;

        [Header("Loop length in seconds")]
        [SerializeField]
        float loopTimeLength = 5f;

        [Header("Start offset (초기 지연)")]
        [SerializeField]
        [Tooltip("레이저 시작 지연 시간 (Row/Col 타이밍 엇갈리게)")]
        float startOffset = 0f;

        [Header("FireCenter 발사 효과음")]
        [SerializeField]
        [Tooltip("FireCenter 발사 시 재생될 효과음")]
        AudioClip fireCenterSound = null;

        double timeOfLastInstantiate;
        AudioSource audioSource;

        GameObject instantiatedEffect;
        Coroutine activeColliderRoutine; // 현재 실행 중인 코루틴 추적

        int effectIndex = 0;
        bool usePhotonTime = false; // Photon 시간 사용 여부

        // Use this for initialization

        void Start()
        {
            // 멀티플레이 시 Photon 시간 사용
            usePhotonTime = PhotonNetwork.IsConnected;

            // AudioSource 컴포넌트 가져오기 또는 생성
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && fireCenterSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.5f; // 반반 블렌드 (적당한 거리감)
                audioSource.volume = 1.0f;
            }

            // 초기 지연 설정 (startOffset만큼 시간을 뒤로 당김)
            timeOfLastInstantiate = GetCurrentTime() - loopTimeLength + startOffset;
        }

        double GetCurrentTime()
        {
            return usePhotonTime ? PhotonNetwork.Time : Time.time;
        }

        void Update()
        {
            // 모든 클라이언트에서 Photon 시간 기준으로 레이저 생성
            if (GetCurrentTime() >= timeOfLastInstantiate + loopTimeLength)
            {
                // 이전 효과 정리
                if (activeColliderRoutine != null)
                {
                    StopCoroutine(activeColliderRoutine);
                    activeColliderRoutine = null;
                }

                if (instantiatedEffect != null)
                {
                    ActivateFireCenterCollider(instantiatedEffect, false);
                    Destroy(instantiatedEffect);
                }

                // 새 파티클 생성 및 새 코루틴 시작
                instantiatedEffect = Instantiate(listOfEffects[effectIndex], transform.position, transform.rotation) as GameObject;
                ActivateFireCenterCollider(instantiatedEffect, false);
                activeColliderRoutine = StartCoroutine(FireCenterColliderRoutine(instantiatedEffect));
                timeOfLastInstantiate = GetCurrentTime();

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
                    float startDelay = main.startDelay.constant; // FireCenter의 startDelay (2초)

                    // startDelay 동안 대기 (충전 중 - 안전)
                    ActivateFireCenterCollider(effectRoot, false);
                    yield return new WaitForSeconds(startDelay);

                    // 레이저 발사 시작 - Collider 활성화 (2~2.7초 구간만)
                    ActivateFireCenterCollider(effectRoot, true);

                    // FireCenter 발사 효과음 재생
                    if (audioSource != null && fireCenterSound != null)
                    {
                        audioSource.PlayOneShot(fireCenterSound);
                    }

                    yield return new WaitForSeconds(0.7f); // 0.7초간만 위험

                    // 레이저 발사 끝 - Collider 비활성화 (파티클은 계속 재생됨)
                    ActivateFireCenterCollider(effectRoot, false);
                }
            }
        }

        // FireCenter라는 이름의 자식 오브젝트에서 BoxCollider를 제거하거나 추가
        void ActivateFireCenterCollider(GameObject effectRoot, bool enable)
        {
            if (effectRoot == null) return;

            var fireCenter = effectRoot.transform.Find("FireCenter");
            if (fireCenter == null) return;

            var existingCollider = fireCenter.GetComponent<BoxCollider>();

            if (enable)
            {
                // Collider가 없으면 새로 생성
                if (existingCollider == null)
                {
                    var newCollider = fireCenter.gameObject.AddComponent<BoxCollider>();
                    newCollider.isTrigger = true;
                    newCollider.size = new Vector3(10, 10, 700);
                    newCollider.center = Vector3.zero;

                    // LaserKillZone 스크립트도 추가
                    if (fireCenter.GetComponent<Ilumisoft.SkillDrive.Game.LaserKillZone>() == null)
                    {
                        fireCenter.gameObject.AddComponent<Ilumisoft.SkillDrive.Game.LaserKillZone>();
                    }
                }
                else
                {
                    // 이미 있으면 활성화
                    existingCollider.enabled = true;
                }
            }
            else
            {
                // Collider 완전히 제거
                if (existingCollider != null)
                {
                    Destroy(existingCollider);
                }
            }
        }
    }
}
