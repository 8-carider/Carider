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
            // 비 마스터 클라이언트: 네트워크 위치로 보간
            if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient && rb != null)
            {
                rb.MovePosition(Vector3.Lerp(rb.position, networkPosition, Time.fixedDeltaTime * 10f));
                rb.MoveRotation(Quaternion.Lerp(rb.rotation, networkRotation, Time.fixedDeltaTime * 10f));
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
