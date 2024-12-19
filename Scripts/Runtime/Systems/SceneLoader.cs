using UnityEngine.SceneManagement;

namespace SFEscape.Runtime.Systems
{
    public static class SceneLoader
    {
        private const string GameOverSceneName = "GameOverScene";
        private const string GameClearSceneName = "GameClearScene";
        private const string TitleSceneName = "TitleScene";
        private const string GameSceneName = "StageScene";
        public static void LoadGameOver()
        {
            SceneManager.LoadScene(GameOverSceneName);
        }
        public static void LoadGameClear()
        {
            SceneManager.LoadScene(GameClearSceneName);
        }
        public static void LoadTitle()
        {
            SceneManager.LoadScene(TitleSceneName);
        }
        public static void LoadGame()
        {
            SceneManager.LoadScene(GameSceneName);
        }
    }
}