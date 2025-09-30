using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitButtonController : MonoBehaviour
{
    public void OnExitButtonClicked()
    {
        // 1P만 버튼을 누를 수 있게 제한
        if (PhotonNetwork.LocalPlayer.ActorNumber == 1)
        {
            PhotonNetwork.LeaveRoom(); // 룸 나가기
            SceneManager.LoadScene("MapSelection"); // 맵 선택 씬으로 이동
        }
    }
}