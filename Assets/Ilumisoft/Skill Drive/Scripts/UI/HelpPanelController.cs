using UnityEngine;

namespace Ilumisoft.SkillDrive.UI
{
    public class HelpPanelController : MonoBehaviour
    {
        public GameObject helpPanel;
        private bool isVisible = false;

        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.H))
            {
                isVisible = !isVisible;
                helpPanel.SetActive(isVisible);
            }
        }
    }
}
