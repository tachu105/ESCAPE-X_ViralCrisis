using SFEscape.Runtime.Player;
using SFEscape.Runtime.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace SFEscape.Runtime.UIs.MenuUIs
{
    public class SettingMenuUIHandler : MonoBehaviour
    {
        [SerializeField]
        private PlayerController playerController;
        [SerializeField]
        private GameObject settingUICanvas;

        [SerializeField] 
        private Slider horizontalSlider;
        [SerializeField] 
        private Slider verticalSlider;
        
        private bool isOpened = false;
        
        public void HorizontalSensitivityChanged()
        {
            playerController.ChangeCameraSensitiveX(horizontalSlider.value);
        }
        public void VerticalSensitivityChanged()
        {
            playerController.ChangeCameraSensitiveY(verticalSlider.value);
        }
        public void OnInvertYToggleChanged(bool value)
        {
            playerController.ChangeInvertY(value);
        }
        public void OnBackTitleClicked()
        {
            SceneLoader.LoadTitle();
        }
        public void ChangeSettingMenuEnabled()
        {
            isOpened = !isOpened;
            Cursor.lockState = isOpened ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isOpened;
            settingUICanvas.SetActive(isOpened);
            playerController.PosePlayerInput(isOpened);
        }
    }
}