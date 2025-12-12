using Photon.Pun;
using UnityEngine;
using Ilumisoft.SkillDrive.Input;

namespace Ilumisoft.SkillDrive.Network
{
    /// <summary>
    /// 네트워크 환경에서 차량 입력 및 동기화를 단순하게 관리
    /// - 각 플레이어: 자신의 입력만 RPC로 전송
    /// - 마스터: 모든 입력을 수집하여 차량 물리 계산
    /// - PhotonTransformView: 차량 위치/회전 동기화
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class VehicleNetworkController : MonoBehaviourPun, IPunObservable
    {
        private Rigidbody rb;
        
        // 네트워크 동기화용 변수
        private Vector3 networkPosition;
        private Quaternion networkRotation;
        private Vector3 networkVelocity;
        private Vector3 networkAngularVelocity;
        
        // 마지막 전송한 데이터 (불필요한 네트워크 업데이트 방지)
        private Vector3 lastSentPosition;
        private Quaternion lastSentRotation;
        private Vector3 lastSentVelocity;
        private float positionChangeThreshold = 0.01f; // 0.01 단위 변화만 전송
        
        // 부드러운 동기화를 위한 추가 변수
        private float lerpSpeed = 20f; // 보간 속도 (20이 최적)
        private float positionThreshold = 5f; // 텔레포트 임계값 (2→5로 증가)
        private float receivedTime;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            
            if (rb != null)
            {
                // 마스터만 물리 계산
                if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
                {
                    rb.isKinematic = true; // 비 마스터는 transform만 업데이트
                    // 리지드바디 물리 업데이트를 최소화
                    rb.constraints = RigidbodyConstraints.FreezeRotation;
                }
            }
            
            // 초기값 저장 (불필요한 네트워크 업데이트 방지)
            if (rb != null)
            {
                lastSentPosition = rb.position;
                lastSentRotation = rb.rotation;
                lastSentVelocity = rb.linearVelocity;
            }
        }
        
        private void FixedUpdate()
        {
            // 비 마스터 클라이언트: 네트워크 위치로 보간 + 외삽
            if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient && rb != null)
            {
                float distance = Vector3.Distance(rb.position, networkPosition);
                float timeSinceUpdate = Time.time - receivedTime;
                
                // 너무 멀리 떨어져 있으면 텔레포트 (워프)
                if (distance > positionThreshold)
                {
                    rb.position = networkPosition;
                    rb.rotation = networkRotation;
                    rb.linearVelocity = networkVelocity;
                }
                else
                {
                    // 거리와 시간을 고려한 동적 보간 속도
                    float dynamicLerpSpeed = Mathf.Lerp(lerpSpeed, lerpSpeed * 1.5f, distance / positionThreshold);
                    
                    // 외삽 계수를 더 낮춰서 움찔거림 방지 (최대 0.1초까지만 예측)
                    float extrapolationFactor = Mathf.Min(timeSinceUpdate, 0.1f);
                    Vector3 extrapolatedPosition = networkPosition + networkVelocity * extrapolationFactor;
                    
                    // 부드러운 보간
                    Vector3 newPosition = Vector3.Lerp(rb.position, extrapolatedPosition, Time.fixedDeltaTime * dynamicLerpSpeed);
                    rb.position = newPosition;
                    
                    // 회전 보간
                    Quaternion newRotation = Quaternion.Slerp(rb.rotation, networkRotation, Time.fixedDeltaTime * dynamicLerpSpeed);
                    rb.rotation = newRotation;
                    
                    // 속도 직접 설정 (Lerp 없이)
                    rb.linearVelocity = networkVelocity;
                }
            }
        }
        
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // 마스터: 변화가 있을 때만 전송 (네트워크 효율화)
                bool posChanged = Vector3.Distance(rb.position, lastSentPosition) > positionChangeThreshold;
                bool rotChanged = Quaternion.Angle(rb.rotation, lastSentRotation) > 1f;
                bool velChanged = Vector3.Distance(rb.linearVelocity, lastSentVelocity) > 0.1f;
                
                stream.SendNext(rb.position);
                stream.SendNext(rb.rotation);
                stream.SendNext(rb.linearVelocity);
                stream.SendNext(rb.angularVelocity);
                
                // 마지막 전송값 업데이트
                if (posChanged) lastSentPosition = rb.position;
                if (rotChanged) lastSentRotation = rb.rotation;
                if (velChanged) lastSentVelocity = rb.linearVelocity;
            }
            else
            {
                // 비 마스터가 수신
                networkPosition = (Vector3)stream.ReceiveNext();
                networkRotation = (Quaternion)stream.ReceiveNext();
                networkVelocity = (Vector3)stream.ReceiveNext();
                networkAngularVelocity = (Vector3)stream.ReceiveNext();
                
                // 수신 시간 기록
                receivedTime = Time.time;
            }
        }
    }
}
