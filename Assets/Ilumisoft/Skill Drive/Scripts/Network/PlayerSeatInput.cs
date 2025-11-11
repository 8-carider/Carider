using Photon.Pun;
using UnityEngine;

namespace Ilumisoft.SkillDrive.Network
{
    // Simple local input reader - sends player input to all VehicleSeatManagers
    // Each manager filters by actor number to only accept from assigned seats
    public class PlayerSeatInput : MonoBehaviourPun
    {
        private void Update()
        {
            // Only read local input
            if (!PhotonNetwork.IsConnected || PhotonNetwork.LocalPlayer == null) return;

            float horizontal = UnityEngine.Input.GetAxis("Horizontal");
            float vertical = UnityEngine.Input.GetAxis("Vertical");

            // Send to all seat managers (they filter by actor number)
            var managers = FindObjectsOfType<VehicleSeatManager>();
            int myActor = PhotonNetwork.LocalPlayer.ActorNumber;

            foreach (var m in managers)
            {
                if (m.photonView != null && m.photonView.Owner != null)
                {
                    // Send steering and throttle to vehicle owner
                    m.photonView.RPC("RPC_SetSteering", RpcTarget.All, horizontal, myActor);
                    m.photonView.RPC("RPC_SetThrottle", RpcTarget.All, vertical, myActor);
                }
            }
        }
    }
}
