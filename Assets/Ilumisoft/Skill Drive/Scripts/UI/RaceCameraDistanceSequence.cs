using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

namespace Ilumisoft.SkillDrive.UI
{
    public class RaceCameraDistanceSequence : MonoBehaviour
    {
        [SerializeField] private Unity.Cinemachine.CinemachineCamera targetVCam;
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
            var transposer = targetVCam.GetComponent<Unity.Cinemachine.CinemachineTransposer>();
            if (transposer == null)
            {
                Debug.LogError("RaceCameraDistanceSequence: CinemachineTransposer not found!");
                yield break;
            }
            float elapsed = 0f;
            float initialDistance = startDistance;
            float finalDistance = endDistance;
            Vector3 offset = transposer.m_FollowOffset;
            offset.z = initialDistance;
            transposer.m_FollowOffset = offset;
            while (elapsed < moveDuration)
            {
                float t = elapsed / moveDuration;
                offset.z = Mathf.Lerp(initialDistance, finalDistance, t);
                transposer.m_FollowOffset = offset;
                elapsed += Time.deltaTime;
                yield return null;
            }
            offset.z = finalDistance;
            transposer.m_FollowOffset = offset;
            OnCameraSequenceFinished?.Invoke();
        }
    }
}
