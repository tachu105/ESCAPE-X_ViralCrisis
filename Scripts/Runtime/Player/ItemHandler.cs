using UnityEngine;
using R3;
using SFEscape.Runtime.Audio;
using SFEscape.Runtime.InteractableObjects;

namespace SFEscape.Runtime.Player
{
    /// <summary>
    /// プレイヤーのアイテム操作を管理するクラス
    /// </summary>
    public class ItemHandler : MonoBehaviour
    {
        [SerializeField]
        private ItemDatabase itemDatabase;
        [SerializeField]
        private PlayerAudioController audioController;
        private readonly ReactiveProperty<ItemType> holdingItemType = new(ItemType.None);

        /// <summary> 所持しているアイテムの種類 </summary>
        public ReadOnlyReactiveProperty<ItemType> HoldingItemType => holdingItemType;
        
        /// <summary>
        /// アイテムを取得する処理
        /// </summary>
        /// <param name="newItemType"> 取得したアイテムの種類 </param>
        /// <param name="itemObj"> 取得したアイテムのGameObject </param>
        public void PickUpItem(ItemType newItemType, GameObject itemObj)
        {
            if (newItemType == ItemType.None) return;
            if (holdingItemType.CurrentValue != ItemType.None) DropItem();

            holdingItemType.Value = newItemType;
            if (itemObj) Destroy(itemObj);
            
            audioController.DefaultGetEffect();
        }

        /// <summary>
        /// 所持しているアイテムをドロップする処理
        /// </summary>
        public void DropItem()
        {
            if(holdingItemType.CurrentValue == ItemType.None) return;

            GameObject droppingItem = itemDatabase.GetItemData(holdingItemType.CurrentValue).itemObj;
            Instantiate(
                droppingItem,
                this.transform.position,
                droppingItem.transform.rotation
                );
            holdingItemType.Value = ItemType.None;
            audioController.DefaultDropEffect();
        }

        /// <summary>
        /// 所持しているアイテムを使用（手放す）する処理
        /// </summary>
        public void UseItem(ItemType requiredItem)
        {
            if(holdingItemType.CurrentValue == requiredItem)
                holdingItemType.Value = ItemType.None;
        }
    }
}