using SFEscape.Runtime.InteractableObjects;
using UnityEngine;
using UnityEngine.UI;
using SFEscape.Runtime.Player;
using R3;
using Cysharp.Threading.Tasks;

namespace SFEscape.Runtime.UIs
{
    /// <summary>
    /// アイテムUIの挙動を管理するクラス
    /// </summary>
    public class HoldingItemUIHandler : MonoBehaviour
    {
        [SerializeField]
        private ItemHandler itemHandler;

        [SerializeField]
        private ItemDatabase itemDatabase;

        [SerializeField]
        private Image itemImage;

        private void Start()
        {
            // 所持アイテムの種類が変更された際にアイテム画像を更新
            itemHandler.HoldingItemType.Subscribe(this, static (itemType, self) =>
            {
                self.itemImage.sprite = self.itemDatabase.GetItemData(itemType).icon;
            }).AddTo(destroyCancellationToken);
        }
    }
}