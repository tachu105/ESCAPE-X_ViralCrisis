using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using R3;

namespace SFEscape.Runtime.UIs
{
    public class DamageVignetteUIHandler : MonoBehaviour
    {
        [SerializeField, Tooltip("心拍振動の強さ")] 
        private float beatStrength = 0.2f;
        [SerializeField, Tooltip("心拍の周期(s)")] 
        private float beatCycleTime = 1f;
        [SerializeField, Tooltip("1拍動の速さ")]
        private int beatSharpness = 4;
        [SerializeField, Tooltip("フェードイン・アウトの速さ")] private float fadeSpeed = 0.5f;
        
        [SerializeField] 
        private Image damageVignetteImage;

        private IDisposable heartbeatDisposable;
        private float beatElapsedTime = 0;
        
        private readonly Lazy<int> spotSize = new Lazy<int>(static () => Shader.PropertyToID("_SpotSize"));
        
        /// <summary>
        /// ダメージ表現を有効化する
        /// </summary>
        public void EnableDamageVignette()
        {
            HeartbeatEffect(true);
            FadeVignette(true);
        }

        /// <summary>
        /// ダメージ表現を無効化する
        /// </summary>
        public void DisableDamageVignette()
        {
            HeartbeatEffect(false);
            FadeVignette(false);
        }

        /// <summary>
        /// 心拍表現のオンオフ切り替え
        /// </summary>
        private void HeartbeatEffect(bool isStart)
        {
            if(isStart)
            {
                heartbeatDisposable = Observable
                    .EveryUpdate(destroyCancellationToken)
                    .Subscribe(this, static (_, self) =>
                    {
                        // 心拍表現の計算
                        // s(t) = 1 + A * (SIN(2πt / P)^n)
                        float scale
                            = 1 + self.beatStrength * Mathf.Pow(
                                Mathf.Sin(2 * Mathf.PI * self.beatElapsedTime / self.beatCycleTime),
                                self.beatSharpness
                            );
                        self.beatElapsedTime += Time.deltaTime;

                        self.damageVignetteImage.transform.localScale = new Vector3(scale, scale, 1);
                    });
            }
            else
            {
                heartbeatDisposable?.Dispose();
            }
        }
        
        /// <summary>
        /// ダメージ表現のスポットサイズを設定する
        /// </summary>
        /// <param name="spotSizeValue"> スポットサイズの値 </param>
        public void SetVignetteSpotSize(float spotSizeValue)
        {
            damageVignetteImage.material.SetFloat(spotSize.Value, spotSizeValue);
        }

        /// <summary>
        /// アルファ値により、ダメージ表現をフェードイン・アウトする
        /// </summary>
        /// <param name="isFadeIn"> フェードイン=>true, フェードアウト=>false </param>
        private void FadeVignette(bool isFadeIn)
        {
            int endValue = isFadeIn ? 1 : 0;
            
            damageVignetteImage.DOKill();
            damageVignetteImage
                .DOFade(endValue, fadeSpeed)
                .SetSpeedBased()
                .SetEase(Ease.Linear)
                .OnStart(() =>
                {
                    if (isFadeIn) damageVignetteImage.enabled = true;
                })
                .OnComplete(() =>
                {
                    if (!isFadeIn) damageVignetteImage.enabled = false;
                });
        }
    }
}