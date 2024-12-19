using Cysharp.Threading.Tasks;
using R3;
using SFEscape.Runtime.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SFEscape.Runtime.UIs
{
    /// <summary>
    /// クエスト進行状況のUIを制御するクラス
    /// </summary>
    public class QuestProgressUIHandler : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI questProgressText;
        
        [SerializeField]
        private TextMeshProUGUI questInstructionText;
        
        [SerializeField]
        private Image portalIcon;
        
        [SerializeField]
        private Color32 clearedColor = Color.cyan;
        
        private const string ClearedInstruction = "ポータル起動";

        private void Start()
        {
            // クエスト進行状況の変更を購読
            QuestProgress.Instance.QuestClearedCount
                .Subscribe(this, UpdateQuestProgress)
                .AddTo(destroyCancellationToken);
        }
        
        /// <summary>
        /// クエスト進行状況のUIを更新
        /// </summary>
        /// <param name="clearedCount"> クリア済みのクエスト数 </param>
        /// <param name="self"> 自身のインスタンス </param>
        private static void UpdateQuestProgress(int clearedCount, QuestProgressUIHandler self)
        {
            self.questProgressText.SetText("{0} / {1}", clearedCount, QuestProgress.Instance.QuestMaxCount);

            if (QuestProgress.Instance.IsQuestCleared)
            {
                self.questProgressText.color = self.clearedColor;
                self.portalIcon.color = self.clearedColor;
                self.questInstructionText.color = self.clearedColor;
                self.questInstructionText.text = ClearedInstruction;
            }
        }
    }
}