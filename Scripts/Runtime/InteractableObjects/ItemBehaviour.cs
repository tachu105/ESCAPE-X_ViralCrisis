using R3;
using SFEscape.Runtime.Player;
using UnityEngine;

namespace SFEscape.Runtime.InteractableObjects
{
    /// <summary>
    /// アイテムの挙動を管理するクラス
    /// </summary>
    public class ItemBehaviour : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private ItemType itemType;
        private readonly ReactiveProperty<InteractableObjState> state = new(InteractableObjState.ItemSuppliable);

        public bool IsDestroyable => true;
        public ReadOnlyReactiveProperty<InteractableObjState> State => state;
        public ItemType RequiredItem => ItemType.None;
        public ItemType SupplyItem => itemType;
        public PlayerCondition InteractableCondition => PlayerCondition.Normal | PlayerCondition.Infected;

        public string GetStateDisplayText(InteractableObjState keyState)
        {
            return keyState switch
            {
                InteractableObjState.ItemSuppliable => "取得",
                _ => ""
            };
        }
        
        public void Interact(GameObject interactor)
        {
        }
    }
}
