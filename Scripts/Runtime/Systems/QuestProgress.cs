using R3;

namespace SFEscape.Runtime.Systems
{
    /// <summary>
    /// クエストの進行状況を管理するクラス（シングルトン）
    /// </summary>
    public class QuestProgress
    {
        public int QuestMaxCount { get; private set; }
        public readonly ReactiveProperty<int> QuestClearedCount = new(0);
        public bool IsQuestCleared => QuestClearedCount.Value == QuestMaxCount;
        
        public static readonly QuestProgress Instance = new QuestProgress();

        private QuestProgress(){}
        
        public void InitializeProgress(int questMaxCount)
        {
            this.QuestMaxCount = questMaxCount;
            QuestClearedCount.Value = 0;
        }
    }
}
