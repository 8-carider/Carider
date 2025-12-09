using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace Ilumisoft.SkillDrive
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField]
        PlayableDirector loadedPlayableDirector = null;

        [SerializeField]
        PlayableDirector playableDirector = null;

        private void Start()
        {
            loadedPlayableDirector.Play();
        }

        public void LoadScene(int index)
        {
            StopAllCoroutines();
            StartCoroutine(LoadSceneCoroutine(index));
        }

        public void LoadScene(string name)
        {
            StopAllCoroutines();
            StartCoroutine(LoadSceneCoroutine(name));
        }

        IEnumerator LoadSceneCoroutine(string name)
        {
            yield return PlayTimelineAndWait();

#if PHOTON_UNITY_NETWORKING
            if (Photon.Pun.PhotonNetwork.IsConnected && Photon.Pun.PhotonNetwork.InRoom)
            {
                // 멀티플레이: 마스터만 실제 씬 전환 (클라이언트는 페이드 연출만 재생)
                if (Photon.Pun.PhotonNetwork.IsMasterClient)
                {
                    Photon.Pun.PhotonNetwork.LoadLevel(name);
                }
                // 클라이언트는 마스터의 LoadLevel에 의해 자동으로 씬이 전환됨
            }
            else
            {
                SceneManager.LoadScene(name);
            }
#else
            SceneManager.LoadScene(name);
#endif
        }

        IEnumerator LoadSceneCoroutine(int index)
        {
            yield return PlayTimelineAndWait();

            SceneManager.LoadScene(index);
        }

        IEnumerator PlayTimelineAndWait()
        {
            if (playableDirector != null)
            {
                playableDirector.gameObject.SetActive(true);
                playableDirector.Stop();
                playableDirector.time = 0;
                playableDirector.Evaluate();
                playableDirector.Play();

                yield return new WaitForSecondsRealtime((float)playableDirector.duration);
            }
        }
    }
}