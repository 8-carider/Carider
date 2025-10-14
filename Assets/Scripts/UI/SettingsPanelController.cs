using UnityEngine;

public class SettingsPanelController : MonoBehaviour
{
    public GameObject settingsPanel;

    public void TogglePanel()
    {
        bool isActive = settingsPanel.activeSelf;
        settingsPanel.SetActive(!isActive);
    }

    public void ClosePanel()
    {
        settingsPanel.SetActive(false);
    }
}