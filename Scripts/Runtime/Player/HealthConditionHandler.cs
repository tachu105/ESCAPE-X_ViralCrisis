using System;
using R3;
using SFEscape.Runtime.UIs;
using UnityEngine;
using Cysharp.Threading.Tasks;
using R3.Triggers;
using SFEscape.Runtime.Audio;
using SFEscape.Runtime.Systems;

namespace SFEscape.Runtime.Player
{
    /// <summary>
    /// Playerの健康状態を管理するクラス
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class HealthConditionHandler : MonoBehaviour
    {
        [SerializeField, Tooltip("感染進捗率のリセットにかかる時間")]
        private float timeUntilInfectionReset = 2f;

        [SerializeField, Tooltip("瀕死状態になるまでの時間")]
        private float timeUntilCritical = 10;

        private float infectionElapsedTime = 0; // 現感染時間
        private readonly ReactiveProperty<float> infectionRate = new(0);    // 感染進捗率

        /// <summary> Playerの健康状態 </summary>
        public PlayerCondition CurrentCondition { get; private set; } = PlayerCondition.Normal;

        [SerializeField] private DamageVignetteUIHandler damageVignetteUIHandler;
        [SerializeField] private PlayerAudioController playerAudioController;
        private PlayerController playerController;

        private IDisposable rateUpdateDisposable = null;
        private IDisposable rateResetDisposable = null;
        
        private const string EnemyInfectAreaTag = "EnemyInfectArea";
        private const string EnemyAttackAreaTag = "EnemyAttackArea";

        private void Start()
        {
            playerController = this.GetComponent<PlayerController>();

            // 感染時の視界範囲変化の処理
            infectionRate
                .Subscribe(this, static (rate, self) =>
                {
                    self.damageVignetteUIHandler.SetVignetteSpotSize(1 - rate);
                })
                .AddTo(destroyCancellationToken);
            
            // 敵の感染範囲に接触した際の処理
            this.OnTriggerEnterAsObservable()
                .Where(static collider => collider.gameObject.CompareTag(EnemyInfectAreaTag))
                .Subscribe(this, static (_, self) =>
                {
                    if(self.CurrentCondition == PlayerCondition.Normal)
                        self.ChangeCondition(PlayerCondition.Infected);
                })
                .AddTo(destroyCancellationToken);

            // 敵の攻撃範囲に接触した際の処理
            this.OnTriggerEnterAsObservable()
                .Where(static collider => collider.gameObject.CompareTag(EnemyAttackAreaTag))
                .Subscribe(this, static (_, self) =>
                {
                    switch(self.CurrentCondition)
                    {
                        case PlayerCondition.Normal:
                            self.ChangeCondition(PlayerCondition.CriticalNormal);
                            break;
                        case PlayerCondition.Infected:
                            self.ChangeCondition(PlayerCondition.CriticalInfected);
                            break;
                        default:
                            break;
                    }
                });
        }

        /// <summary>
        /// PlayerConditionを変更するメソッド
        /// </summary>
        /// <param name="newCondition"> 変更先のPlayerCondition </param>
        /// <exception cref="System.ArgumentException"> 想定されていない状態変更が行われた場合 </exception>
        public void ChangeCondition(PlayerCondition newCondition)
        {
            switch (newCondition)
            {
                // 解毒 （感染→通常）
                case PlayerCondition.Normal when CurrentCondition is PlayerCondition.Infected:
                    damageVignetteUIHandler.DisableDamageVignette();
                    playerAudioController.VignetteEffect(false).Forget();
                    ResetInfectionRate();
                    break;
                // 治療 （通常瀕死→通常）
                case PlayerCondition.Normal when CurrentCondition is PlayerCondition.CriticalNormal:
                    playerController.SetCrawling(false);
                    break;
                // 感染 （通常→感染）
                case PlayerCondition.Infected when CurrentCondition is PlayerCondition.Normal:
                    damageVignetteUIHandler.EnableDamageVignette();
                    playerAudioController.VignetteEffect(true).Forget();
                    UpdateInfectionRate();
                    break;
                // 治療 （感染瀕死→通常）
                case PlayerCondition.Normal when CurrentCondition is PlayerCondition.CriticalInfected:
                    playerController.SetCrawling(false);
                    break;
                // 受傷 （通常→通常瀕死）
                case PlayerCondition.CriticalNormal when CurrentCondition is PlayerCondition.Normal:
                    SceneLoader.LoadGameOver();
                    playerController.SetCrawling(true);
                    break;
                // 感染時受傷 （感染→感染瀕死）
                case PlayerCondition.CriticalInfected when CurrentCondition is PlayerCondition.Infected:
                    SceneLoader.LoadGameOver();
                    playerController.SetCrawling(true);
                    break;
                // 既に同じ状態になっている場合
                case var _ when newCondition == CurrentCondition:
                    return;
                default:
                    throw new System.ArgumentException("Invalid condition change");
            }

            CurrentCondition = newCondition;
        }

        /// <summary>
        /// 感染進捗率を更新するメソッド
        /// </summary>
        private void UpdateInfectionRate()
        {
            rateResetDisposable?.Dispose();
            rateUpdateDisposable = Observable
                .EveryUpdate(destroyCancellationToken)
                .Select(this, static (_, self) => self)
                .TakeUntil(static self => self.infectionElapsedTime >= self.timeUntilCritical)
                .Subscribe(this,
                    onNext: static (_, self) =>
                    {
                        self.infectionElapsedTime += Time.deltaTime;
                        self.infectionRate.Value =
                            Mathf.Clamp(self.infectionElapsedTime / self.timeUntilCritical, 0, 1);
                    },
                    onCompleted: static (_, self) =>
                    {
                        self.ChangeCondition(PlayerCondition.CriticalInfected);
                        self.rateUpdateDisposable = null;
                    });
        }

        /// <summary>
        /// 感染進捗率をリセットするメソッド
        /// </summary>
        private void ResetInfectionRate()
        {
            rateUpdateDisposable?.Dispose();
            rateResetDisposable = Observable
                .EveryUpdate(destroyCancellationToken)
                .Select(this, static (_, self) => self)
                .TakeUntil(static self => self.infectionElapsedTime <= 0)
                .Subscribe(this,
                    onNext: static (_, self) =>
                    {
                        self.infectionElapsedTime -=
                            Time.deltaTime * self.timeUntilCritical / self.timeUntilInfectionReset;
                        self.infectionRate.Value =
                            Mathf.Clamp(self.infectionElapsedTime / self.timeUntilCritical, 0, 1);
                    },
                    onCompleted: static (_, self) => { self.rateResetDisposable = null; });
        }
    }
    
    
    /// <summary>
    /// Playerの状態を表す列挙型
    /// </summary>
    [Flags]
    public enum PlayerCondition
    {
        /// <summary> 通常状態 </summary>
        Normal = 1 << 0,

        /// <summary> 感染状態（通常移動） </summary>
        Infected = 1 << 1,

        /// <summary> 非感染時瀕死状態（匍匐移動） </summary>
        CriticalNormal = 1 << 2,

        /// <summary> 感染時瀕死状態（匍匐移動） </summary>
        CriticalInfected = 1 << 3,
    }
}