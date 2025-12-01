using UnityEngine;

namespace Ilumisoft.SkillDrive.Effects
{
    /// <summary>
    /// 차량 속도에 비례하여 제트엔진 파티클의 길이를 스케일로 조절합니다.
    /// </summary>
    public class JetEngineParticleScaler : VehicleComponent
    {
        [Header("제트엔진 파티클 설정")]
        [SerializeField]
        [Tooltip("제트엔진 파티클 시스템")]
        private ParticleSystem jetEngineParticle;

        [Header("길이 조절 설정")]
        [SerializeField]
        [Tooltip("최소 길이 스케일 (정지 시)")]
        private float minLength = 1f;

        [SerializeField]
        [Tooltip("최대 길이 스케일 (최고 속도 시)")]
        private float maxLength = 3.0f;

        [SerializeField]
        [Tooltip("길이 변화 속도")]
        private float lerpSpeed = 5f;

        private float currentLength;
        private Vector3 originalScale;

        private void Start()
        {
            if (jetEngineParticle != null)
            {
                originalScale = jetEngineParticle.transform.localScale;
                currentLength = minLength;
            }
            else
            {
                Debug.LogWarning("[JetEngineParticleScaler] 제트엔진 파티클이 할당되지 않았습니다!");
            }
        }

        private void Update()
        {
            if (Vehicle == null || jetEngineParticle == null)
                return;

            // 전진은 길어지고, 후진은 짧아지도록 (Abs 제거)
            float speedRatio = Vehicle.NormalizedForwardSpeed;

            // 스피드업 활성화 시 최대 길이 2배 증가
            float effectiveMaxLength = maxLength;
            if (Vehicle.IsSpeedUpActive)
            {
                effectiveMaxLength = maxLength * 3f;
            }

            // 목표 길이 계산 (전진: minLength~effectiveMaxLength, 후진: minLength 이하)
            float targetLength;
            if (speedRatio >= 0)
            {
                // 전진: 속도에 비례해서 길어짐
                targetLength = Mathf.Lerp(minLength, effectiveMaxLength, speedRatio);
            }
            else
            {
                // 후진: 속도에 비례해서 짧아짐
                targetLength = Mathf.Lerp(minLength, minLength * 0.3f, Mathf.Abs(speedRatio));
            }

            // 부드럽게 길이 변화
            currentLength = Mathf.Lerp(currentLength, targetLength, Time.deltaTime * lerpSpeed);

            // Z축 스케일을 조절하여 길이 변경 (파티클이 Z축 방향으로 발사되는 경우)
            jetEngineParticle.transform.localScale = new Vector3(
                originalScale.x,
                originalScale.y,
                originalScale.z * currentLength
            );
        }

        // 에디터에서 파티클 자동 할당
        private void Reset()
        {
            jetEngineParticle = GetComponent<ParticleSystem>();
        }
    }
}
