using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace SFEscape.Runtime.Audio
{
    public class PlayerAudioController : MonoBehaviour
    {
        [SerializeField]
        private AudioSource normalBGMSource;
        [SerializeField] 
        private AudioSource chaseBGMSource;
        [SerializeField]
        private AudioSource chaseIntroBGMSource;
        [SerializeField]
        private AudioSource playerSESource;
        [SerializeField]
        private AudioSource playerVignetteSource;
        [SerializeField]
        private AudioClip playerFootStepEffect;
        [SerializeField]
        private AudioClip batteryGetEffect;
        [SerializeField]  
        private AudioClip batteryDropEffect;
        [SerializeField]
        private AudioClip defaultGetEffect;
        [SerializeField]
        private AudioClip defaultDropEffect;
        [SerializeField]
        private AudioClip heartBeatEffect;
        
        [SerializeField]
        private float walkEffectInterval = 0.5f;
        [SerializeField]
        private float runEffectInterval = 0.3f;
        [SerializeField]
        private float footEffectPitchRange = 0.1f;
        [SerializeField]
        private float footEffectBasePitch = 1;
        
        [SerializeField]
        private float heartBeatEffectInterval = 0.5f;

        /// <summary>
        /// BGMをクロスフェードするCrossFadeBGMメソッドのキャンセルトークン
        /// </summary>
        private CancellationTokenSource bgmCts = new CancellationTokenSource();

        // BGM

        public void ChangeChaseBGM()
        {
            PlayChaseIntroThenLoop().Forget();
        }

        private async UniTask PlayChaseIntroThenLoop()
        {
            bgmCts.Cancel();
            bgmCts.Dispose();
            bgmCts = new CancellationTokenSource();

            // 現在のBGMをフェードアウト
            float initVol = normalBGMSource.volume;
            for (float time = 0; time < 1f; time += Time.deltaTime)
            {
                if (bgmCts.IsCancellationRequested) return;
                normalBGMSource.volume = Mathf.Lerp(initVol, 0, time);
                await UniTask.WaitForEndOfFrame(bgmCts.Token);
            }
            normalBGMSource.Stop();

            // イントロを再生
            chaseIntroBGMSource.volume = 1;
            chaseIntroBGMSource.Play();

            // イントロの長さだけ待機
            float introLength = chaseIntroBGMSource.clip.length;
            await UniTask.Delay((int)(introLength * 1000), cancellationToken: bgmCts.Token);

            // ループ部分を開始
            chaseBGMSource.volume = 1;
            chaseBGMSource.Play();
            chaseIntroBGMSource.Stop();
        }

        public void ChangeNormalBGM()
        {
            if (chaseIntroBGMSource.isPlaying)
            {
                CrossFadeBGM(chaseIntroBGMSource, normalBGMSource, 4f).Forget();
            }
            else if(chaseBGMSource.isPlaying)
            {
                CrossFadeBGM(chaseBGMSource, normalBGMSource, 4f).Forget();
            }
        }

        private async UniTask CrossFadeBGM(AudioSource fadeOutSource, AudioSource fadeInSource, float duration)
        {
            bgmCts.Cancel();
            bgmCts.Dispose();
            bgmCts = new CancellationTokenSource();

            fadeInSource.volume = 0;
            fadeInSource.Play();
            float initVol = fadeOutSource.volume;
            for (float time = 0; time < duration; time += Time.deltaTime)
            {
                if (bgmCts.IsCancellationRequested)
                {
                    return;
                }

                fadeInSource.volume = Mathf.Lerp(0, 1, time / duration);
                fadeOutSource.volume = Mathf.Lerp(initVol, 0, time / duration);

                await UniTask.WaitForEndOfFrame(bgmCts.Token);
            }

            fadeInSource.volume = 1;
            fadeOutSource.volume = 0;
        }

        private void OnDestroy()
        {
            if(!bgmCts.IsCancellationRequested) bgmCts.Cancel();
            if(!footstepCts.IsCancellationRequested) footstepCts.Cancel();
            if(!vignetteCts.IsCancellationRequested) vignetteCts.Cancel();
            bgmCts?.Dispose();
            footstepCts?.Dispose();
            vignetteCts?.Dispose();
        }


        // SoundEffect

        public void BatteryGetEffect()
        {
            playerSESource.PlayOneShot(batteryGetEffect);
        }

        public void BatteryDropEffect()
        {
            playerSESource.PlayOneShot(batteryDropEffect);
        }

        public void DefaultGetEffect()
        {
            playerSESource.pitch = 1;
            playerSESource.PlayOneShot(defaultGetEffect);
        }

        public void DefaultDropEffect()
        {
            playerSESource.pitch = 1;
            playerSESource.PlayOneShot(defaultDropEffect);
        }


        private bool isPlaying = false;
        private bool isRunning = false;
        private float currentInterval;
        private CancellationTokenSource footstepCts = CancellationTokenSource.CreateLinkedTokenSource(new CancellationToken(true));

        public async UniTaskVoid FootStepEffect(bool isPlay, bool isRun = false)
        {
            // 状態変化がない場合は処理をスキップ
            if (isPlaying == isPlay && isRunning == isRun) return;

            isRunning = isRun;
            currentInterval = isRunning ? runEffectInterval : walkEffectInterval;

            // 再生処理
            if (isPlay)
            {
                isPlaying = true;
                if (footstepCts.IsCancellationRequested)
                {
                    footstepCts = new CancellationTokenSource();

                    while (!footstepCts.Token.IsCancellationRequested)
                    {
                        if (playerSESource != null && playerFootStepEffect != null)
                        {
                            playerSESource.pitch = UnityEngine.Random.Range(footEffectBasePitch - footEffectPitchRange,
                                footEffectBasePitch + footEffectPitchRange);
                            playerSESource.PlayOneShot(playerFootStepEffect);
                        }

                        await UniTask.Delay(TimeSpan.FromSeconds(currentInterval),
                            cancellationToken: footstepCts.Token);
                    }
                }

                if (!isPlaying)
                {
                    footstepCts?.Cancel();
                    footstepCts?.Dispose();
                }
            }
            // 停止処理
            else
            {
                isPlaying = false;
                footstepCts?.Cancel();
                footstepCts?.Dispose();
            }
        }

        private CancellationTokenSource vignetteCts = CancellationTokenSource.CreateLinkedTokenSource(new CancellationToken(true));
        public async UniTaskVoid VignetteEffect(bool isPlay)
        {
            if (isPlay && !vignetteCts.IsCancellationRequested) return;

            // 再生処理
            if (isPlay)
            {
                vignetteCts = new CancellationTokenSource();

                while (!vignetteCts.Token.IsCancellationRequested)
                {
                    if (playerVignetteSource != null && heartBeatEffect != null)
                    {
                        playerVignetteSource.PlayOneShot(heartBeatEffect);
                    }

                    await UniTask.Delay(TimeSpan.FromSeconds(heartBeatEffectInterval),
                        cancellationToken: vignetteCts.Token);
                }

                vignetteCts?.Cancel();
                vignetteCts?.Dispose();
            }
            // 停止処理
            else
            {
                vignetteCts?.Cancel();
                vignetteCts?.Dispose();
            }
        }
    }
}
