using UnityEngine;
using R3;
using SFEscape.Runtime.Player;
using System;

namespace SFEscape.Runtime.InteractableObjects
{
    /// <summary>
    /// 充電ボックスの挙動を管理するクラス
    /// </summary>
    public class ChargeBoxBehaviour : MonoBehaviour, IHoldInteractable
    {
        [SerializeField, Tooltip("チャージ所要時間")]
        private float requiredHoldTime = 3f;
        /// <summary> チャージ所要時間 </summary>
        public float RequiredHoldTime => requiredHoldTime;
        
        /// <summary> 現在のチャージ進捗 </summary>
        public ReadOnlyReactiveProperty<float> CurrentHoldProgress => currentHoldProgress;
        private readonly ReactiveProperty<float> currentHoldProgress = new(0);

        /// <summary> チャージボックスの状態 </summary>
        public ReadOnlyReactiveProperty<InteractableObjState> State => state;
        private readonly ReactiveProperty<InteractableObjState> state = new (InteractableObjState.Accessible);

        public bool IsDestroyable => false;
        public ItemType RequiredItem => ItemType.EmptyBattery;
        public ItemType SupplyItem => ItemType.ChargedBattery;
        public PlayerCondition InteractableCondition => PlayerCondition.Normal | PlayerCondition.Infected;
        
        private int chargeSpeedOdds = 0;   //充電速度の倍率
        private IDisposable chargeDisposable = null;
        
        [SerializeField]
        private ParticleSystem[] chargeEffects;
        [SerializeField]
        private GameObject chargedBatteryObj;
        [SerializeField]
        private GameObject emptyBatteryObj;
        
        [Header("SE関連")]
        [SerializeField]
        private AudioSource seOneShotSource;
        [SerializeField]
        private AudioSource seLoopSource;
        [SerializeField]
        private AudioClip batterySetClip;
        [SerializeField]
        private AudioClip chargingClip;
        [SerializeField]
        private AudioClip chargeCompleteClip;
        
        /// <summary>
        /// チャージボックスの表示状態
        /// </summary>
        private enum DisplayState
        {
            /// <summary> 空状態 </summary>
            None,   
            /// <summary> 空のバッテリーがセットされている状態 </summary>
            InjectedBattery,
            /// <summary> バッテリー充電中の状態 </summary>
            Charging,
            /// <summary> バッテリー充電完了の状態 </summary>
            ChargeCompleted
        }

        public string GetStateDisplayText(InteractableObjState keyState)
        {
            return keyState switch 
            {
                InteractableObjState.Accessible => "バッテリーをセット",
                InteractableObjState.HoldRequiring => "チャージする",
                InteractableObjState.ItemSuppliable => "バッテリーを取り出す",
                _ => ""
            };
        }

        public void Interact(GameObject interactor)
        {
            switch (state.Value)
            {
                case InteractableObjState.Accessible:
                    currentHoldProgress.Value = 0;
                    state.Value = InteractableObjState.HoldRequiring;
                    SetDisplayObject(DisplayState.InjectedBattery);
                    seOneShotSource.PlayOneShot(batterySetClip);
                    Interact(interactor);
                    break;
                case InteractableObjState.HoldRequiring:
                    if (chargeSpeedOdds == 0)
                    {
                        StartCharge();
                        SetDisplayObject(DisplayState.Charging);
                        seLoopSource.clip = chargingClip;
                        seLoopSource.Play();
                    }
                    chargeSpeedOdds++;
                    break;
                case InteractableObjState.ItemSuppliable:
                    state.Value = InteractableObjState.Accessible;
                    SetDisplayObject(DisplayState.None);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void StopInteract()
        {
            if (chargeSpeedOdds < 1 || state.Value != InteractableObjState.HoldRequiring) return;

            chargeSpeedOdds--;
            if (chargeSpeedOdds == 0)
            {
                chargeDisposable.Dispose();
                chargeDisposable = null;
                SetDisplayObject(DisplayState.InjectedBattery);
                seLoopSource.Stop();
            }
        }

        /// <summary>
        /// バッテリーの充電処理
        /// </summary>
        private void StartCharge()
        {
            chargeDisposable?.Dispose();
            chargeDisposable = Observable
                .EveryUpdate(destroyCancellationToken)
               .Select(this, static (_, self) => self)
               .TakeUntil(static self => self.currentHoldProgress.Value >= self.RequiredHoldTime)
               .Subscribe(this,
                    onNext:
                    static (_, self) =>
                    {
                        self.currentHoldProgress.Value += Time.deltaTime * self.chargeSpeedOdds;
                    },
                    onCompleted:
                    static (_, self) =>
                    {
                        self.state.Value = InteractableObjState.ItemSuppliable;
                        self.chargeSpeedOdds = 0;
                        self.SetDisplayObject(DisplayState.ChargeCompleted);
                        self.seLoopSource.Stop();
                        self.seOneShotSource.PlayOneShot(self.chargeCompleteClip);
                    }
               );
        }
        
        /// <summary>
        /// オブジェクトの表示を切り替える
        /// </summary>
        /// <param name="newState"> 表示状態 </param>
        private void SetDisplayObject(DisplayState newState)
        {
            switch (newState)
            {
                case DisplayState.None:
                    chargedBatteryObj.SetActive(false);
                    emptyBatteryObj.SetActive(false);
                    foreach (var effect in chargeEffects) effect.Stop();
                    break;
                case DisplayState.InjectedBattery:
                    chargedBatteryObj.SetActive(false);
                    emptyBatteryObj.SetActive(true);
                    foreach (var effect in chargeEffects) effect.Stop();
                    break;
                case DisplayState.Charging:
                    chargedBatteryObj.SetActive(false);
                    emptyBatteryObj.SetActive(true);
                    foreach (var effect in chargeEffects) effect.Play();
                    break;
                case DisplayState.ChargeCompleted:
                    chargedBatteryObj.SetActive(true);
                    emptyBatteryObj.SetActive(false);
                    foreach (var effect in chargeEffects) effect.Stop();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
            }
        }
    }
}
