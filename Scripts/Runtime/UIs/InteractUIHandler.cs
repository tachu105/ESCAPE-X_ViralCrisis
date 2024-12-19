using R3;
using SFEscape.Runtime.InteractableObjects;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SFEscape.Runtime.UIs
{
    /// <summary>
    /// オブジェクトへのインタラクト関連のUIを制御するクラス
    /// </summary>
    public class InteractUIHandler : MonoBehaviour
    {
        private CancellationTokenRegistration progressDisposable;  // 進捗メーターの購読解除用
        
        [SerializeField]
        private Color32 normalUIColor = Color.cyan;
        [SerializeField]
        private Color32 bannedUIColor = Color.red;
        
        [Header("UIアイコン画像")]
        [SerializeField, Tooltip("インタラクト可能状態のUI")]
        private Sprite normalIcon;
        [SerializeField, Tooltip("進捗メーターのUI")]
        private Sprite progressIcon;
        [SerializeField, Tooltip("インタラクト不可状態のUI")]
        private Sprite bannedIcon;

        [Header("InteractUI Components")]
        [SerializeField]
        private Image interactIconImage;
        [SerializeField]
        private Image progressGageImage;
        [SerializeField]
        private TextMeshProUGUI interactDisplayText;
        [SerializeField]
        private TextMeshProUGUI keyOperationText;

        private const string InteractOperationText = "Press E";
        private const string HoldOperationText = "Hold E";

        /// <summary>
        /// インタラクトUIを表示する
        /// </summary>
        /// <param name="interactable"> インタラクト対象のインターフェース </param>
        /// <param name="playerInteractableState"> プレイヤーのインタラクト可否状態 </param>
        public void ShowInteractUI(IInteractable interactable, bool playerInteractableState)
        {
            switch (interactable.State.CurrentValue)
            {
                // インタラクト不可状態の場合
                case var _ when !playerInteractableState:
                case InteractableObjState.Unavailable:
                    ShowBannedUI(interactable);
                    return;

                // クリア済みの場合
                case InteractableObjState.Cleared:
                    HideInteractUI();
                    return;

                // 長押し処理が必要な場合
                case InteractableObjState.HoldRequiring:
                    if (interactable is IHoldInteractable holdInteractable)
                    {
                        ShowProgressUI(holdInteractable);
                    }
                    else
                    {
                        throw new InvalidOperationException("長押し処理を実装する場合は、インターフェースIHoldInteractableを使用すること");
                    }
                    return;
                
                // その他インタラクト可能状態の場合
                default:
                    ShowNormalUI(interactable);
                    return;
            }
        }

        /// <summary>
        /// インタラクトUIを非表示にする
        /// </summary>
        public void HideInteractUI()
        {
            // UIの非表示
            interactIconImage.enabled = false;
            progressGageImage.enabled = false;
            interactIconImage.sprite = null;
            interactIconImage.color = normalUIColor;
            interactDisplayText.text = "";
            interactDisplayText.color = normalUIColor;
            keyOperationText.text = "";
            keyOperationText.color = normalUIColor;

            // 進捗メーターの購読解除
            if(progressDisposable != default) progressDisposable.Dispose();
            progressDisposable = default;
        }

        /// <summary>
        /// 進捗メーター付きのUIを表示する
        /// </summary>
        /// <param name="interactable"> インタラクト対象のインターフェース </param>
        private void ShowProgressUI(IHoldInteractable interactable)
        {
            // UIの表示・設定
            interactIconImage.enabled = true;
            interactIconImage.sprite = progressIcon;
            interactDisplayText.text = interactable.GetStateDisplayText(interactable.State.CurrentValue);
            progressGageImage.enabled = true;
            keyOperationText.text = HoldOperationText;

            // 進捗メーターの購読
            progressDisposable = interactable.CurrentHoldProgress
                .Subscribe((progressGageImage, interactable), static (value, arg) =>
                {
                    arg.progressGageImage.fillAmount = value / arg.interactable.RequiredHoldTime;
                }).AddTo(destroyCancellationToken);
        }

        /// <summary>
        /// インタラクト可能状態のUIを表示する
        /// </summary>
        /// <param name="interactable"> インタラクト対象のインターフェース </param>
        private void ShowNormalUI(IInteractable interactable)
        {
            // UIの表示・設定
            interactIconImage.enabled = true;
            progressGageImage.enabled = false;
            interactIconImage.sprite = normalIcon;
            interactIconImage.color = normalUIColor;
            interactDisplayText.text = interactable.GetStateDisplayText(interactable.State.CurrentValue);
            interactDisplayText.color = normalUIColor;
            keyOperationText.text = InteractOperationText;
            keyOperationText.color = normalUIColor;
        }

        /// <summary>
        /// インタラクト不可状態のUIを表示する
        /// </summary>
        /// <param name="interactable"> インタラクト対象のインターフェース </param>
        private void ShowBannedUI(IInteractable interactable)
        {
            // UIの表示・設定
            interactIconImage.enabled = true;
            progressGageImage.enabled = false;
            interactIconImage.sprite = bannedIcon;
            interactIconImage.color = bannedUIColor;
            interactDisplayText.text = interactable.GetStateDisplayText(interactable.State.CurrentValue);
            interactDisplayText.color = bannedUIColor;
            keyOperationText.text = "";
            keyOperationText.color = bannedUIColor;
        }
    }
}

