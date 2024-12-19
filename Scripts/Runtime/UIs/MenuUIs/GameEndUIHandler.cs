using System;
using SFEscape.Runtime.Systems;
using UnityEngine;

namespace SFEscape.Runtime.UIs.MenuUIs
{
    public class GameEndUIHandler : MonoBehaviour
    {
        private void Start()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void OnBackTitleButtonClicked()
        {
            SceneLoader.LoadTitle();
        }
    }
}