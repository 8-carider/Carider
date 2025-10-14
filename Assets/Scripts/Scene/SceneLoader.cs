using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
    }

    public void LoadInviteMemberScene()
    {
        SceneManager.LoadScene("InviteMemberScene");
    }

    public void LoadMapSelectionScene()
    {
        SceneManager.LoadScene("MapSelectionScene");
    }

    public void OnBackButtonPressed()
    {
        SceneManager.LoadScene("MainMenu");
    }
}