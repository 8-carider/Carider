using Photon.Pun;
using UnityEngine;

namespace Ilumisoft.SkillDrive.Network
{
    // Simple local input reader - sends player input to all VehicleSeatManagers
    // Each manager filters by actor number to only accept from assigned seats
    public class PlayerSeatInput : MonoBehaviourPun
    {
        private float sendInterval = 0.05f; // 20Hz update rate
        private float lastSendTime = 0f;
        
        private Vector2 lastInput = Vector2.zero;
        private VehicleSeatManager[] cachedManagers;
        private float cacheRefreshTime = 1f;
        private float lastCacheTime = 0f;

        private void Update()
        {
            // Only read local input
            if (!PhotonNetwork.IsConnected || PhotonNetwork.LocalPlayer == null) return;

            float horizontal = UnityEngine.Input.GetAxis("Horizontal");
            float vertical = UnityEngine.Input.GetAxis("Vertical");
            Vector2 currentInput = new Vector2(horizontal, vertical);

            // Refresh manager cache periodically
            if (Time.time - lastCacheTime > cacheRefreshTime || cachedManagers == null)
            {
                cachedManagers = FindObjectsOfType<VehicleSeatManager>();
                lastCacheTime = Time.time;
            }

            // Only send if input changed significantly or enough time passed
            bool inputChanged = Vector2.Distance(currentInput, lastInput) > 0.01f;
            bool shouldSend = inputChanged || (Time.time - lastSendTime > sendInterval);

            if (shouldSend && cachedManagers != null)
            {
                int myActor = PhotonNetwork.LocalPlayer.ActorNumber;

                foreach (var m in cachedManagers)
                {
                    if (m != null && m.photonView != null)
                    {
                        // Send steering and throttle to vehicle owner
                        m.photonView.RPC("RPC_SetSteering", RpcTarget.All, horizontal, myActor);
                        m.photonView.RPC("RPC_SetThrottle", RpcTarget.All, vertical, myActor);
                    }
                }

                lastInput = currentInput;
                lastSendTime = Time.time;
            }
        }
    }
}
