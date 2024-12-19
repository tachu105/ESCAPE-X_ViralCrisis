using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using R3;
using SFEscape.Runtime.InteractableObjects;
using SFEscape.Runtime.Utilities.Debugger;
using SFEscape.Runtime.UIs;

namespace SFEscape.Runtime.Player
{
    [RequireComponent(typeof(ItemHandler))]
    public class InteractHandler : MonoBehaviour
    {
        [SerializeField]
        private new Camera camera;
        [SerializeField]
        private InteractUIHandler interactUIHandler;
        [SerializeField, Tooltip("検知距離")]
        private float interactableDistance = 5f;
        [SerializeField, Tooltip("検知判定の半径")]
        private float interactableRadius = 0.5f;
        [SerializeField, Tooltip("インタラクト検知可能なレイヤー")]
        private LayerMask interactableLayer;

        private ItemHandler itemHandler;
        private HealthConditionHandler healthConditionHandler;
        private bool isInteractable = false;    //プレイヤー側のインタラクト可否状態
        private GameObject interactableObject = null;   //検出中のインタラクト可能オブジェクト
        private IInteractable interactable = null; //検出中オブジェクトのIInteractableインターフェース

        // カメラのビューポート中心座標
        private readonly Vector3 viewPortCenter = new Vector3(0.5f, 0.5f, 0f);

#if UNITY_EDITOR
        [SerializeField, Tooltip("キャストのデバグ用可視化")]
        private bool isDrawCast = false;
#endif

        private void Start()
        {
            itemHandler = this.GetComponent<ItemHandler>();
            healthConditionHandler = this.GetComponent<HealthConditionHandler>();
            camera = camera ? camera : Camera.main;

            // インタラクト可能なオブジェクトを探す当たり判定の監視処理
            Observable
                .EveryUpdate(destroyCancellationToken)
                .Select(this, static (_, self) =>
                {
                    // Rayによる当たり判定処理
                    Ray ray = self.camera.ViewportPointToRay(self.viewPortCenter);
                    Physics.SphereCast(
                        ray,
                        self.interactableRadius,
                        out RaycastHit hit,
                        self.interactableDistance,
                        self.interactableLayer
                    );
#if UNITY_EDITOR
                    // キャストの可視化
                    if (self.isDrawCast)
                        DebugEx.DrawSphereCast(ray, self.interactableRadius, self.interactableDistance);
#endif
                    return (hit.collider?.gameObject, self);
                })
                .DistinctUntilChanged()
                .SubscribeAwait(static (arg, cancellation) =>
                {
                    var (obj, self) = arg;

                    self.StopInteraction();

                    self.interactableObject = obj;
                    self.interactable = obj?.GetComponent<IInteractable>();

                    self.UpdateInteractJudge();

                    // インタラクト対象物の状態変化の監視
                    if (self.interactable != null)
                    {
                        self.interactable.State
                            .Subscribe(self, static (_, self)  =>
                            {
                                self.UpdateInteractJudge();
                            }).AddTo(cancellation);
                    }
                    // 手持ちのアイテム変化を監視
                    self.itemHandler.HoldingItemType
                        .Subscribe(self, static (_, self) =>
                        {
                            self.UpdateInteractJudge();
                        }).AddTo(cancellation);

                    return new ValueTask();
                }, AwaitOperation.Switch);
        }

        /// <summary>
        /// Playerのインタラクト状態を更新するメソッド
        /// </summary>
        private void UpdateInteractJudge()
        {
            // インタラクト可能なオブジェクトがない場合
            if (interactable == null)
            {
                isInteractable = false;
                interactUIHandler.HideInteractUI();
                return;
            }

            // アイテムが必要かどうか
            bool isItemRequired = interactable.State.CurrentValue == InteractableObjState.Accessible &&
                                  interactable.RequiredItem != ItemType.None;
            // 必要なアイテムを持っているかどうか
            bool isValidItem = interactable.RequiredItem == itemHandler.HoldingItemType.CurrentValue;
            
            // インタラクト可能な健康状態かどうか
            bool isInteractableCondition
                = (interactable.InteractableCondition & healthConditionHandler.CurrentCondition) != 0;

            // 必要なアイテムを所持していない場合はインタラクト不可
            isInteractable = isInteractableCondition && (!isItemRequired || isValidItem);

            interactUIHandler.ShowInteractUI(interactable, isInteractable);
        }

        /// <summary>
        /// 対象物に対してインタラクトを行うメソッド
        /// </summary>
        public void InteractToObjects()
        {
            if (!isInteractable) return;
            switch (interactable.State.CurrentValue)
            {
                case InteractableObjState.ItemSuppliable:
                    interactable.Interact(this.gameObject);
                    itemHandler.PickUpItem(
                        interactable.SupplyItem,
                        interactable.IsDestroyable ? interactableObject : null
                    );
                    break;
                case InteractableObjState.Accessible:
                    interactable.Interact(this.gameObject);
                    itemHandler.UseItem(interactable.RequiredItem);
                    break;
                case InteractableObjState.HoldRequiring:
                    interactable.Interact(this.gameObject);
                    break;
                case InteractableObjState.Unavailable:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 対象物へのインタラクトを中断するメソッド
        /// </summary>
        public void StopInteraction()
        {
            if (interactable == null) return;
            if (interactable.State.CurrentValue != InteractableObjState.HoldRequiring) return;

            if (interactable is IHoldInteractable holdInteractable)
                holdInteractable.StopInteract();
            else
                throw new InvalidOperationException("長押し処理を実装する場合は、インターフェースIHoldInteractableを使用すること");
        }
    }
}
