using UnityEngine;

namespace Ilumisoft.SkillDrive
{
    /// <summary>
    /// Centralized debug logging toggles for the project.
    /// Flip these flags to silence specific categories without touching call sites.
    /// </summary>
    public static class SkillDriveDebug
    {
        // Global ON/OFF
        public static bool Enabled = true;

        // Categories
        public static bool Network = false; // MapVehicleSpawner, Photon-related chatter
        public static bool Vehicle = false; // Vehicle Awake/physics/seat logs
        public static bool UI = false;      // UI and Level Selection logs
        public static bool Gameplay = false; // GameManager countdown, collectables

        public static void LogNetwork(object message, Object context = null)
        {
            if (Enabled && Network) Debug.Log(message, context);
        }

        public static void LogVehicle(object message, Object context = null)
        {
            if (Enabled && Vehicle) Debug.Log(message, context);
        }

        public static void LogUI(object message, Object context = null)
        {
            if (Enabled && UI) Debug.Log(message, context);
        }

        public static void LogGameplay(object message, Object context = null)
        {
            if (Enabled && Gameplay) Debug.Log(message, context);
        }
    }
}
