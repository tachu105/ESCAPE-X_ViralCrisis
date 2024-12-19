using SFEscape.Runtime.UIs.MenuUIs;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;
using UnityEngine.InputSystem;

namespace SFEscape.Runtime.Player
{
    /// <summary>
    /// プレイヤーの操作入力を管理するクラス
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(InteractHandler))]
    [RequireComponent(typeof(PlayerInput))]
    public class InputHandler : MonoBehaviour
    {
        private PlayerInput playerInput;
        private PlayerController playerController;
        private InteractHandler interactHandler;
        private ItemHandler itemHandler;
        [SerializeField]
        private SettingMenuUIHandler settingMenuUIHandler;

        private const string Move = nameof(Move);
        private const string Look = nameof(Look);
        private const string Sprint = nameof(Sprint);
        private const string Interact = nameof(Interact);
        private const string DropItem = nameof(DropItem);
        private const string Pose = nameof(Pose);
        private const string Esc = nameof(Esc);

        private void Start()
        {
            playerInput = this.GetComponent<PlayerInput>();
            playerInput.onActionTriggered += context => OnActionTriggered(context);
            playerController = this.GetComponent<PlayerController>();
            interactHandler = this.GetComponent<InteractHandler>();
            itemHandler = this.GetComponent<ItemHandler>();
            
            // カーソルを非表示にする
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnActionTriggered(in CallbackContext context)
        {
            switch (context.action.name)
            {
                // 移動入力・解除時
                case Move when context.phase is InputActionPhase.Performed or InputActionPhase.Canceled:
                    playerController.MovePlayer(context.ReadValue<Vector2>());
                    break;
                // 視点移動時
                case Look:
                    playerController.SetMouseInputValue(context.ReadValue<Vector2>());
                    break;
                // スプリント入力時
                case Sprint when context.phase is InputActionPhase.Performed:
                    playerController.SprintPlayer(true);
                    break;
                // スプリント解除時
                case Sprint when context.phase is InputActionPhase.Canceled:
                    playerController.SprintPlayer(false);
                    break;
                // インタラクト入力時
                case Interact when context.phase is InputActionPhase.Performed:
                    interactHandler.InteractToObjects();
                    break;
                // インタラクト解除時
                case Interact when context.phase is InputActionPhase.Canceled:
                    interactHandler.StopInteraction();
                    break;
                // 手持ちアイテムドロップ入力時
                case DropItem when context.phase is InputActionPhase.Performed:
                    itemHandler.DropItem();
                    break;
                // ゲームポーズ入力時
                case Pose when context.phase is InputActionPhase.Performed:
                    settingMenuUIHandler.ChangeSettingMenuEnabled();
                    break;
                // Escキー入力時
                case Esc when context.phase is InputActionPhase.Performed:
                    settingMenuUIHandler.ChangeSettingMenuEnabled();
                    break;
            }
        }
    }
}