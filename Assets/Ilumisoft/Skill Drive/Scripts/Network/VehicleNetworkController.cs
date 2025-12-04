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
        private VehicleSeatManager seatManager;
        private Rigidbody rb;
        
        // 네트워크 동기화용 변수
        private Vector3 networkPosition;
        private Quaternion networkRotation;
        private Vector3 networkVelocity;
        private Vector3 networkAngularVelocity;
        
        // 부드러운 동기화를 위한 추가 변수
        private float lerpSpeed = 15f; // 기본 보간 속도
        private float positionThreshold = 2f; // 텔레포트 임계값
        
        private void Awake()
        {
            seatManager = GetComponent<VehicleSeatManager>();
            rb = GetComponent<Rigidbody>();
            
            if (rb != null)
            {
                // 마스터만 물리 계산
                if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
                {
                    rb.isKinematic = true; // 비 마스터는 transform만 업데이트
                }
            }
        }
        
        private void FixedUpdate()
        {
            // 비 마스터 클라이언트: 네트워크 위치로 보간 + 외삽
            if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient && rb != null)
            {
                float distance = Vector3.Distance(rb.position, networkPosition);
                
                // 너무 멀리 떨어져 있으면 텔레포트 (워프)
                if (distance > positionThreshold)
                {
                    rb.position = networkPosition;
                    rb.rotation = networkRotation;
                    rb.linearVelocity = networkVelocity;
                }
                else
                {
                    // 거리에 따라 동적으로 보간 속도 조정
                    float dynamicLerpSpeed = Mathf.Lerp(lerpSpeed, lerpSpeed * 2f, distance / positionThreshold);
                    
                    // 외삽: 속도를 고려한 위치 예측 (더 공격적으로)
                    Vector3 extrapolatedPosition = networkPosition + networkVelocity * Time.fixedDeltaTime * 2f;
                    
                    // 부드러운 보간
                    rb.MovePosition(Vector3.Lerp(rb.position, extrapolatedPosition, Time.fixedDeltaTime * dynamicLerpSpeed));
                    rb.MoveRotation(Quaternion.Slerp(rb.rotation, networkRotation, Time.fixedDeltaTime * dynamicLerpSpeed));
                    
                    // 속도 동기화
                    rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, networkVelocity, Time.fixedDeltaTime * 10f);
                }
            }
        }
        
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // 마스터가 위치/회전/속도 전송
                stream.SendNext(rb.position);
                stream.SendNext(rb.rotation);
                stream.SendNext(rb.linearVelocity);
                stream.SendNext(rb.angularVelocity);
            }
            else
            {
                // 비 마스터가 수신
                networkPosition = (Vector3)stream.ReceiveNext();
                networkRotation = (Quaternion)stream.ReceiveNext();
                networkVelocity = (Vector3)stream.ReceiveNext();
                networkAngularVelocity = (Vector3)stream.ReceiveNext();
                
                // 지연 보정 (lag compensation)
                float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
                networkPosition += networkVelocity * lag;
            }
        }
    }
}
