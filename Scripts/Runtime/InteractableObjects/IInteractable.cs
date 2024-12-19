using R3;
using SFEscape.Runtime.Player;
using UnityEngine;

namespace SFEscape.Runtime.InteractableObjects
{
    /// <summary>
    /// プレイヤーがインタラクト可能なオブジェクトに実装するインターフェース
    /// </summary>
    public interface IInteractable
    {
        /// <summary> 破壊可能オブジェクトかどうか </summary>
        public bool IsDestroyable { get; }

        /// <summary> オブジェクトのインタラクト状態 </summary>
        public ReadOnlyReactiveProperty<InteractableObjState> State { get; }

        /// <summary> インタラクトに必要なアイテム </summary>
        public ItemType RequiredItem { get; }

        /// <summary> 取得できるアイテム </summary>
        public ItemType SupplyItem { get; }
        
        /// <summary> インタラクト可能なプレイヤーの状態(ビット演算で複数指定可能) </summary>
        public PlayerCondition InteractableCondition { get; }
        
        /// <summary>
        ///  UI表示用のテキストを取得するメソッド
        /// </summary>
        public string GetStateDisplayText(InteractableObjState keyState);

        /// <summary>
        /// オブジェクトのインタラクト処理を行うメソッド
        /// </summary>
        /// <param name="interactor"> インタラクトを行うプレイヤー </param>
        public void Interact(GameObject interactor);
    }


    /// <summary>
    /// 長押しインタラクションが必要なオブジェクトを定義するインターフェース
    /// </summary>
    public interface IHoldInteractable : IInteractable
    {      
        /// <summary> 現在の長押し進捗を取得 </summary>
        public ReadOnlyReactiveProperty<float> CurrentHoldProgress { get; }

        /// <summary> インタラクション完了までに必要な時間（秒） </summary>
        public float RequiredHoldTime { get; }

        /// <summary>
        /// インタラクションを終了する処理
        /// </summary>
        public void StopInteract();
    }

    /// <summary>
    /// インタラクト可能オブジェクトの状態
    /// </summary>
    public enum InteractableObjState
    {
        /// <summary> インタラクト不可 </summary>
        Unavailable,
        /// <summary> インタラクト待機 </summary>
        Accessible,
        /// <summary> 再インタラクト待機 </summary>
        ReAccessible,
        /// <summary> 攻略済み </summary>
        Cleared,
        /// <summary> 長押しインタラクト待機 </summary>
        HoldRequiring,
        /// <summary> アイテム取得可能 </summary>
        ItemSuppliable,
    }
}
