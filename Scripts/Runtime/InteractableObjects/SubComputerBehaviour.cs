using UnityEngine;
using SFEscape.Runtime.Player;
using R3;
using SFEscape.Runtime.Systems;

namespace SFEscape.Runtime.InteractableObjects
{
    /// <summary>
    /// コンピュータの挙動を管理するクラス
    /// </summary>
    public class SubComputerBehaviour : MonoBehaviour, IInteractable
    {
        public bool IsDestroyable => false;

        private readonly ReactiveProperty<InteractableObjState> state = new(InteractableObjState.Accessible);
        public ReadOnlyReactiveProperty<InteractableObjState> State => state;

        public ItemType RequiredItem => ItemType.ChargedBattery;
        public ItemType SupplyItem => ItemType.None;
        public PlayerCondition InteractableCondition => PlayerCondition.Normal | PlayerCondition.Infected;

        private GameObject clearedObj;  // クリア済み時のオブジェクト
        private GameObject unClearedObj;    // 未クリア時のオブジェクト

        [Header("SE関連")]
        [SerializeField] 
        private AudioSource seSource;
        [SerializeField]
        private AudioClip batterySetClip;
        [SerializeField]
        private AudioClip pcStartClip;

        private void Start()
        {
            clearedObj = this.transform.Find("SubComputer_Cleared").gameObject;
            unClearedObj = this.transform.Find("SubComputer_UnCleared").gameObject;
        }
        
        public string GetStateDisplayText(InteractableObjState keyState)
        {
            return keyState switch
            {
                InteractableObjState.Accessible => "起動する",
                _ => ""
            };
        }

        public void Interact(GameObject interactor)
        {
            if(state.Value == InteractableObjState.Accessible)
            {
                // クリア処理
                unClearedObj.SetActive(false);
                clearedObj.SetActive(true);
                state.Value = InteractableObjState.Cleared;
                QuestProgress.Instance.QuestClearedCount.Value++;
                seSource.PlayOneShot(batterySetClip);
                seSource.PlayOneShot(pcStartClip);
            }
        }
    }
}
