using UnityEngine;

namespace Ilumisoft.ArcardeRacingKit.Effects
{
    /// <summary>
    /// Smoothly blends in a given post processing volume when the vehicle accelerates to create a nice visual speed effect.
    /// </summary>
    public class SpeedPostProcessingEffect : MonoBehaviour
    {
        // [SerializeField]
        // PostProcessVolume postProcessVolume = null; // URP에서는 Volume으로 교체 필요

        // private void Update()
        // {
        //     if (postProcessVolume != null && Vehicle != null)
        //     {
        //         float speed = Vehicle.CanMove ? Vehicle.NormalizedForwardSpeed : 0.0f;
        //
        //         // Adjust the weight of the volume according to the normalized speed of the vehicle
        //         postProcessVolume.weight = speed;
        //     }
        // }
    }
}