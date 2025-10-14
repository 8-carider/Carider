using UnityEngine;

public class QuitButtonController : MonoBehaviour
{
    public void QuitGame()
    {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // 에디터에서 실행 중지
#else
        Application.Quit(); // 빌드된 게임에서 종료
#endif
    }
}