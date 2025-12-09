using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine.SceneManagement;

namespace Ilumisoft.SkillDrive.Game
{
    public class GameResultUI : MonoBehaviourPunCallbacks
    {
        public Transform[] textPositions;
        public GameObject resultTextPrefab;
        public FlagSpawner flagSpawner;
        public AutoCameraAdjust cameraAdjuster;

        [Header("Audio")]
        [SerializeField] private AudioSource fireworksAudioSource;
        [SerializeField] private AudioClip fireworksSfx;
        [SerializeField] private float fireworksVolume = 1.0f;

        // 불꽃놀이
        public GameObject[] fireworksPrefabs;

        //  랭킹을 로컬에 복사해 두어서,
        //  중간에 누가 방을 나가도 결과는 그대로 유지
        private List<PlayerRankData> cachedRanking;

        private bool fireworksSoundPlayed = false; // 폭죽 사운드 재생 여부

        private bool canReturnToMenu = false;

        private void Start()
        {
            // 최종 랭킹 로컬 복사
            cachedRanking = GameSessionManager.Instance.GetFinalRanking();

            SpawnTexts(cachedRanking);

            if (flagSpawner != null)
                flagSpawner.SpawnFlags(cachedRanking);

            // 카메라 이동
            if (cameraAdjuster != null && flagSpawner != null)
                cameraAdjuster.AdjustCameraToHeight(flagSpawner.HighestFlagHeight * 1f);

            // 카메라가 멈추면 우승자 텍스트 & 불꽃놀이 & 안내문 실행
            if (cameraAdjuster != null)
                cameraAdjuster.OnCameraStop += () => OnCameraStopped(cachedRanking);
            else
                OnCameraStopped(cachedRanking);
        }

        private void Update()
        {
            if (!canReturnToMenu) return;

            if (UnityEngine.Input.GetKeyDown(KeyCode.S))
            {
                StartCoroutine(ReturnToMainCoroutine());
            }
        }

        private void SpawnTexts(List<PlayerRankData> ranking)
        {
            for (int i = 0; i < ranking.Count && i < textPositions.Length; i++)
            {
                Vector3 pos = textPositions[i].position + new Vector3(0, 1.2f, 0);

                GameObject obj = Instantiate(resultTextPrefab, pos, Quaternion.identity);
                TextMeshPro tmp = obj.GetComponent<TextMeshPro>();

                tmp.text = $"P{i + 1}\n{ranking[i].TotalCollected}";
                tmp.fontSize = 10;
                tmp.color = Color.black;
                tmp.outlineWidth = 0f;
                tmp.alignment = TextAlignmentOptions.Center;

                // ▶ 테두리 추가
                AddOutlineToTMP(tmp, 0.03f);

                obj.transform.LookAt(Camera.main.transform);
                obj.transform.Rotate(0, 180, 0);
            }
        }

        // 카메라가 멈추면 우승 텍스트 함수 & 불꽃놀이 함수 실행
        private void OnCameraStopped(List<PlayerRankData> ranking)
        {
            ShowWinnerText(ranking);
            ShowReturnHint();
            StartCoroutine(SpawnFireworksLoop());

            canReturnToMenu = true;
        }

        // 우승자 텍스트 출력
        private void ShowWinnerText(List<PlayerRankData> ranking)
        {
            int topScore = ranking[0].TotalCollected;
            List<int> winners = new List<int>(); // 동점자 고려

            foreach (var p in ranking)
            {
                if (p.TotalCollected == topScore)
                    winners.Add(p.ActorNumber);
            }

            string winnerText = "";

            if (winners.Count == 1)
            {
                winnerText = $"Winner is Player {winners[0]} !!!";
            }
            else
            {
                string listText = string.Join(", ", winners);
                winnerText = $"Winners are Players {listText} !!!";
            }

            GameObject winnerObj = new GameObject("WinnerText");
            TextMeshPro tmp = winnerObj.AddComponent<TextMeshPro>();

            tmp.text = winnerText;
            tmp.fontSize = 10;
            tmp.color = Color.black;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.outlineWidth = 0f;

            AddOutlineToTMP(tmp, 0.04f);

            Camera cam = Camera.main;
            winnerObj.transform.position =
                cam.transform.position +
                cam.transform.forward * 10f +
                cam.transform.up * 1.5f;

            winnerObj.transform.LookAt(cam.transform);
            winnerObj.transform.Rotate(0, 180f, 0);
        }

        // Press S 안내문
        private void ShowReturnHint()
        {
            Camera cam = Camera.main;

            GameObject hintObj = new GameObject("ReturnHintText");
            TextMeshPro tmp = hintObj.AddComponent<TextMeshPro>();

            tmp.text = "Press S to return to Main Menu";
            tmp.fontSize = 8;
            tmp.color = Color.black;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.outlineWidth = 0f;

            AddOutlineToTMP(tmp, 0.03f);

            hintObj.transform.position =
                cam.transform.position +
                cam.transform.forward * 10f +
                cam.transform.up * -0.4f;

            hintObj.transform.LookAt(cam.transform);
            hintObj.transform.Rotate(0, 180f, 0);
        }

        // 불꽃놀이(무한 반복) 
        private IEnumerator SpawnFireworksLoop()
        {
            Camera cam = Camera.main;
            fireworksSoundPlayed = false;

            while (true)
            {
                if (fireworksPrefabs.Length > 0)
                {
                    // 랜덤 위치 (카메라가 보고있는 화면 내부)
                    float x = Random.Range(-8f, 8f);
                    float y = Random.Range(2f, 6f);

                    Vector3 spawnPos = cam.transform.position
                                     + cam.transform.forward * 10f
                                     + cam.transform.right * x
                                     + cam.transform.up * y;

                    GameObject prefab = fireworksPrefabs[Random.Range(0, fireworksPrefabs.Length)];
                    Instantiate(prefab, spawnPos, Quaternion.identity);

                    // 폭죽 사운드 재생 (처음 한 번만)
                    if (!fireworksSoundPlayed && fireworksAudioSource != null && fireworksSfx != null)
                    {
                        fireworksAudioSource.PlayOneShot(fireworksSfx, fireworksVolume);
                        fireworksSoundPlayed = true;
                    }
                }

                yield return new WaitForSeconds(0.1f); // 반복 속도
            }
        }

        //  S 키로 메인으로 돌아가기
        private IEnumerator ReturnToMainCoroutine()
        {
            canReturnToMenu = false;

#if PHOTON_UNITY_NETWORKING
            // 현재 방에서 완전히 나가야됨
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom(false);
                PhotonNetwork.Disconnect();

                // 이건 사실 잘 모르는데 ai가 이거도 추가하래요
                while (PhotonNetwork.InRoom)
                {
                    yield return null;
                }
            }

            //방을 나가자마자 같은 방에 재입장되어 씬이 강제로 동기화되는 것 방지
            PhotonNetwork.AutomaticallySyncScene = false;
#endif
            // 다음 게임을 위해 플레이어 수집 통계 초기화
            PlayerCollectableStats.Instance.ResetAllStats();
            SceneManager.LoadScene("Menu");
        }

        private void AddOutlineToTMP(TextMeshPro main, float offset)
        {
            if (main == null) return;

            Transform parent = main.transform;
            string text = main.text;
            float size = main.fontSize;
            var alignment = main.alignment;
            var font = main.font;
            Vector3[] offsets = new Vector3[]
            {
                new Vector3( offset, 0, 0),
                new Vector3(-offset, 0, 0),
                new Vector3(0,  offset, 0),
                new Vector3(0, -offset, 0),
                new Vector3( offset,  offset, 0),
                new Vector3(-offset,  offset, 0),
                new Vector3( offset, -offset, 0),
                new Vector3(-offset, -offset, 0),
            };

            foreach (var off in offsets)
            {
                GameObject go = new GameObject("Outline");
                go.transform.SetParent(parent, false);
                go.transform.localPosition = off;

                var t = go.AddComponent<TextMeshPro>();
                t.text = text;
                t.fontSize = size;
                t.alignment = alignment;
                t.color = Color.white;
                t.font = font;
            }
        }
    }
}