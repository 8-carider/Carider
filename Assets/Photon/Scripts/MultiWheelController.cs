using UnityEngine;
using Photon.Pun;

public class MultiWheelController : MonoBehaviourPun
{
    [SerializeField] WheelCollider _wheelCollider;
    [SerializeField] Transform _wheelTF;

    public float maxTorque = 100f;
    public float maxSteerAngle = 30f;

    public bool canSteer = false;
    public bool canDrive = false;

    void FixedUpdate()
    {
        _wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        _wheelTF.position = pos;
        _wheelTF.rotation = rot;

        if (photonView.IsMine)
        {
            float gas = Input.GetAxis("Vertical");
            float steer = Input.GetAxis("Horizontal");

            if (canDrive)
                _wheelCollider.motorTorque = gas * maxTorque;

            if (canSteer)
                _wheelCollider.steerAngle = steer * maxSteerAngle;
        }
    }
}
