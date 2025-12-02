using UnityEngine;
using Ilumisoft.SkillDrive;

namespace Ilumisoft.SkillDrive.Game
{
    /// <summary>
    /// 레이저에 닿으면 차량 리스폰 처리
    /// </summary>
    public class LaserKillZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            var respawnHandler = other.GetComponent<VehicleRespawnHandler>();
            if (respawnHandler != null)
            {
                respawnHandler.RequestRespawn();
            }
        }
    }
}
