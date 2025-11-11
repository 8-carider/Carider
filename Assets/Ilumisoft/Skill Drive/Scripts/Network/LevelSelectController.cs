using Photon.Pun;
using UnityEngine;

namespace Ilumisoft.SkillDrive.Network
{
    // Attach this on a GameObject in the "Level Selection" scene.
    // Wire your UI buttons to call LoadMap("Level 1"), LoadMap("Level 2"), etc.
    public class LevelSelectController : MonoBehaviour
    {
        [SerializeField] private string[] availableScenes; // Optional: for validation/logging

        public void LoadMap(string sceneName)
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
            {
                Debug.LogWarning("[LevelSelectController] Not in room; cannot load map.");
                return;
            }

            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.Log("[LevelSelectController] Only the master client can load the map. Ignoring.");
                return;
            }

            if (availableScenes != null && availableScenes.Length > 0)
            {
                bool found = false;
                foreach (var s in availableScenes)
                {
                    if (s == sceneName) { found = true; break; }
                }
                if (!found)
                {
                    Debug.LogWarning($"[LevelSelectController] Scene '{sceneName}' not listed in availableScenes (this is just a warning).");
                }
            }

            // With AutomaticallySyncScene=true, this will move everyone to the chosen map.
            Debug.Log($"[LevelSelectController] Master loading scene: {sceneName}");
            PhotonNetwork.LoadLevel(sceneName);
        }
    }
}
