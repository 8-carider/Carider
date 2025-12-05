using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;

// 이 스크립트를 자동차의 최상위 부모(Rigidbody가 있는 곳)에 추가하세요.
public class AntiGhostBump : MonoBehaviour
{
    // 내 자동차에 포함된 모든 콜라이더의 ID를 저장할 집합
    private HashSet<int> myColliderIDs = new HashSet<int>();

    private void Awake()
    {
        // 1. 내 자동차에 있는 모든 콜라이더를 찾습니다. (자식 오브젝트 포함)
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (var col in colliders)
        {
            // 트리거(Trigger)는 물리 충돌이 아니므로 제외
            if (col.isTrigger) continue;

            // 2. 물리 엔진이 충돌 전에 "잠깐! 수정할 거 있어?"라고 물어보게 설정
            col.hasModifiableContacts = true; 
            
            // ID 등록 (나중에 이 ID로 내 차인지 확인)
            myColliderIDs.Add(col.GetInstanceID());
        }
    }

    private void OnEnable()
    {
        // 물리 엔진의 충돌 수정 이벤트에 내 함수를 등록
        Physics.ContactModifyEvent += OnContactModify;
    }

    private void OnDisable()
    {
        // 꺼질 때 이벤트 해제 (메모리 누수 방지)
        Physics.ContactModifyEvent -= OnContactModify;
    }

    // 물리 엔진이 충돌을 계산하기 직전에 호출되는 함수
    private void OnContactModify(PhysicsScene scene, NativeArray<ModifiableContactPair> pairs)
    {
        // 현재 씬에서 발생한 모든 충돌 쌍을 검사
        foreach (var pair in pairs)
        {
            // 이 충돌이 '내 자동차'와 관련된 것인지 확인
            // (A가 내 차거나, B가 내 차라면)
            bool isMyCarInvolved = myColliderIDs.Contains(pair.colliderInstanceID) || 
                                   myColliderIDs.Contains(pair.otherColliderInstanceID);

            if (!isMyCarInvolved) continue;

            // 충돌 지점들을 하나씩 검사
            for (int i = 0; i < pair.contactCount; i++)
            {
                // pair.GetSeparation(i)
                // 음수(-): 서로 파고듦 (진짜 쾅! 충돌)
                // 양수(+): 아주 미세하게 떨어져 있거나 닿을락 말락 함 (이음새 문제의 원인)

                // 거리가 0보다 크다면 (즉, 바닥 틈새나 미세한 턱이라면)
                if (pair.GetSeparation(i) > 0.001f) // 아주 약간의 여유값(0.001f)을 줌
                {
                    // "이 충돌은 무시해라!"라고 명령
                    pair.IgnoreContact(i);
                }
            }
        }
    }
}