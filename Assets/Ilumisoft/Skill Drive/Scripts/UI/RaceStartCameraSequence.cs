using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

namespace Ilumisoft.SkillDrive.UI
{
    public class RaceStartCameraSequence : MonoBehaviour
    {
        [SerializeField] private CinemachineVirtualCamera raceCamera;
        [SerializeField] private Transform startViewTransform;
        [SerializeField] private Transform endViewTransform;
        [SerializeField] private float moveDuration = 2.0f;

        public System.Action OnCameraSequenceFinished;

        public void PlayCameraSequence()
        {
            StartCoroutine(CameraMoveRoutine());
        }

        private IEnumerator CameraMoveRoutine()
        {
            float elapsed = 0f;
            raceCamera.transform.position = startViewTransform.position;
            raceCamera.transform.rotation = startViewTransform.rotation;

            while (elapsed < moveDuration)
            {
                float t = elapsed / moveDuration;
                raceCamera.transform.position = Vector3.Lerp(startViewTransform.position, endViewTransform.position, t);
                raceCamera.transform.rotation = Quaternion.Slerp(startViewTransform.rotation, endViewTransform.rotation, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            raceCamera.transform.position = endViewTransform.position;
            raceCamera.transform.rotation = endViewTransform.rotation;
            OnCameraSequenceFinished?.Invoke();
        }
    }
}
