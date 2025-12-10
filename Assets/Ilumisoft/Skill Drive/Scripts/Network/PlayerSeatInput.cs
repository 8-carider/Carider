using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem; // New Input System (Keyboard/Gamepad)

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

        // Camera enforcement: ensure local client only follows assigned vehicle
        private VehicleSeatManager _myAssignedManager = null;
        private float cameraCheckInterval = 0.5f;
        private float lastCameraCheckTime = 0f;

        private void Update()
        {
            // Only read local input
            if (!PhotonNetwork.IsConnected || PhotonNetwork.LocalPlayer == null) return;

            // Read input from New Input System first (works when Project uses "Input System Package (New)"),
            // then fallback to legacy Input.GetAxis for compatibility
            float horizontal = 0f;
            float vertical = 0f;

            // Gamepad (left stick)
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                horizontal += gamepad.leftStick.x.ReadValue();
                vertical += gamepad.leftStick.y.ReadValue();
            }

            // Keyboard (WASD + Arrow keys)
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical += 1f;
            }

            // If no New Input System device provided a value, try legacy axes (when Both mode is enabled)
            if (Mathf.Approximately(horizontal, 0f) && Mathf.Approximately(vertical, 0f))
            {
                horizontal = UnityEngine.Input.GetAxis("Horizontal");
                vertical = UnityEngine.Input.GetAxis("Vertical");
            }

            // Clamp to [-1,1]
            horizontal = Mathf.Clamp(horizontal, -1f, 1f);
            vertical = Mathf.Clamp(vertical, -1f, 1f);
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
                        // VehicleSeatManager는 차량 GameObject에 붙어 있으므로 photonView가 차량의 PhotonView를 가리킴
                        m.photonView.RPC("RPC_ReceivePlayerInput", RpcTarget.All, horizontal, vertical, myActor);
                    }
                }

                lastInput = currentInput;
                lastSendTime = Time.time;
            }

            // Periodically enforce local camera to follow only the assigned vehicle
            if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null && cachedManagers != null)
            {
                if (Time.time - lastCameraCheckTime > cameraCheckInterval)
                {
                    int myActor = PhotonNetwork.LocalPlayer.ActorNumber;
                    VehicleSeatManager newlyAssigned = null;
                    foreach (var m in cachedManagers)
                    {
                        if (m != null && m.IsPlayerAssigned(myActor))
                        {
                            newlyAssigned = m;
                            break;
                        }
                    }

                    if (newlyAssigned != _myAssignedManager)
                    {
                        // Toggle cameras so only my assigned vehicle is active
                        foreach (var m in cachedManagers)
                        {
                            if (m == null) continue;
                            var cam = m.GetComponent<VehicleCameraManager>();
                            if (cam != null)
                            {
                                bool active = (m == newlyAssigned);
                                cam.SetCameraActive(active, myActor);
                            }
                        }
                        _myAssignedManager = newlyAssigned;
                    }
                    lastCameraCheckTime = Time.time;
                }
            }
        }
    }
}
