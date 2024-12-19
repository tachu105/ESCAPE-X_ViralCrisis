using System.Collections.Generic;
using UnityEngine;
using System;

namespace SFEscape.Runtime.InteractableObjects
{
    /// <summary>
    /// アイテムのデータ項目
    /// </summary>
    [Serializable]
    public class ItemData
    {
        public string itemName;
        public ItemType type;
        [TextArea(3, 10)]
        public string description;
        public Sprite icon;
        public GameObject itemObj;
    }

    /// <summary>
    /// アイテムのデータベース
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "Scriptable Objects/ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField]
        private List<ItemData> items = new List<ItemData>();
        private Dictionary<ItemType, ItemData> itemDictionary;

        private void OnEnable()
        {
            // 初期化時にディクショナリを構築
            BuildDictionary();
        }

        /// <summary>
        /// アイテムのenumとItemDataを接続するディクショナリを生成
        /// </summary>
        private void BuildDictionary()
        {
            itemDictionary = new Dictionary<ItemType, ItemData>();
            foreach (var item in items)
            {
                if(!itemDictionary.TryAdd(item.type, item))
                {
                    throw new ArgumentException($"ItemType {item.type} is already registered in the database.");
                }
            }
        }

        /// <summary>
        /// ItemTypeからItemDataを取得するメソッド
        /// </summary>
        /// <param name="type"> 取得したいデータのItemType </param>
        public ItemData GetItemData(ItemType type)
        {
            if (itemDictionary == null)
            {
                BuildDictionary();
            }

            return itemDictionary.GetValueOrDefault(type);
        }
    }
}