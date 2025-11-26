using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

namespace Ilumisoft.SkillDrive.UI
{
    public class RaceCameraDistanceSequence : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera targetVCam;
        [SerializeField] private float startDistance = 120f;
        [SerializeField] private float endDistance = 60f;
        [SerializeField] private float moveDuration = 2.0f;

        public System.Action OnCameraSequenceFinished;

        public void PlayCameraDistanceSequence()
        {
            StartCoroutine(CameraDistanceRoutine());
        }

        private IEnumerator CameraDistanceRoutine()
        {
            var composer = targetVCam.GetComponent<CinemachinePositionComposer>();
            if (composer == null)
            {
                Debug.LogError("RaceCameraDistanceSequence: CinemachinePositionComposer not found!");
                yield break;
            }
            float elapsed = 0f;
            float initialDistance = startDistance;
            float finalDistance = endDistance;
            composer.CameraDistance = initialDistance;
            while (elapsed < moveDuration)
            {
                float t = elapsed / moveDuration;
                composer.CameraDistance = Mathf.Lerp(initialDistance, finalDistance, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            composer.CameraDistance = finalDistance;
            OnCameraSequenceFinished?.Invoke();
        }
    }
}
