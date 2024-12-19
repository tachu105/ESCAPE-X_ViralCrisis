using SFEscape.Runtime.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SFEscape.Runtime.UIs.MenuUIs
{
    public class TitleUIHandler : MonoBehaviour
    {
        [SerializeField]
        private GameObject titleUI;
        [SerializeField]
        private GameObject guideUI;
        
        [SerializeField]
        private Image guideImage;
        [SerializeField]
        private Sprite[] guidePages;
        [SerializeField]
        private TextMeshProUGUI pageIndexText; 
        
        private int currentPageIndex = 0;
        
        public void OnStartButtonClicked()
        {
            SceneLoader.LoadGame();
        }
        public void OnGuideButtonClicked()
        {
            titleUI.SetActive(false);
            guideUI.SetActive(true);
            guideImage.sprite = guidePages[0];
            pageIndexText.SetText("{0} / {1}", currentPageIndex + 1, guidePages.Length);
        }
        public void OnTitleButtonClicked()
        {
            titleUI.SetActive(true);
            guideUI.SetActive(false);
        }
        public void OnNextPageButtonClicked()
        {
            RefreshPage(true);
        }
        public void OnPreviousPageButtonClicked()
        {
            RefreshPage(false);
        }
        public void OnQuitButtonClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        
        private void RefreshPage(bool isNext)
        {
            currentPageIndex = (currentPageIndex + (isNext ? 1 : -1)) % guidePages.Length;
            guideImage.sprite = guidePages[currentPageIndex];
            pageIndexText.SetText("{0} / {1}", currentPageIndex + 1, guidePages.Length);
        }
        
    }
}