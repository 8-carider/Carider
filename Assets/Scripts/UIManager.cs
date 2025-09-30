using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject lobbyPanel;

    public void ToggleLobbyPanel()
    {
        if (lobbyPanel != null)
            lobbyPanel.SetActive(!lobbyPanel.activeSelf);
    }
}
