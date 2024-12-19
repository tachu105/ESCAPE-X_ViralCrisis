using R3;
using SFEscape.Runtime.Player;
using SFEscape.Runtime.Systems;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace SFEscape.Runtime.InteractableObjects
{
    /// <summary>
    /// ポータルの挙動を管理するクラス
    /// </summary>
    public class PortalBehaviour : MonoBehaviour, IInteractable
    {
        public bool IsDestroyable => false;
        
        private readonly ReactiveProperty<InteractableObjState> state = new(InteractableObjState.Unavailable);
        public ReadOnlyReactiveProperty<InteractableObjState> State => state;
        public ItemType RequiredItem => ItemType.None;
        public ItemType SupplyItem => ItemType.None;
        public PlayerCondition InteractableCondition => PlayerCondition.Normal | PlayerCondition.Infected;
        
        [SerializeField]
        private AudioSource seSource;
        [SerializeField] 
        private AudioClip portalClip;
        private GameObject energyCircle;
        
        public string GetStateDisplayText(InteractableObjState keyState)
        {
            return keyState switch
            {
                InteractableObjState.Unavailable => "脱出",
                InteractableObjState.Accessible => "脱出",
                _ => ""
            };
        }

        public void Interact(GameObject interactor)
        {
            if (state.Value == InteractableObjState.Accessible)
            {
                SceneLoader.LoadGameClear();
            }
        }

        private void Start()
        {
            energyCircle = this.transform.Find("EnergyCircle").gameObject;
            energyCircle.SetActive(false);
            
            // クエストクリア時の処理
            QuestProgress.Instance.QuestClearedCount
                .Where(static _ => QuestProgress.Instance.IsQuestCleared)
                .Subscribe(this, static (_, self) =>
                {
                    self.state.Value = InteractableObjState.Accessible;
                    self.energyCircle.SetActive(true);
                    self.seSource.clip = self.portalClip;
                    self.seSource.Play();
                })
                .AddTo(destroyCancellationToken);
        }
    }
}