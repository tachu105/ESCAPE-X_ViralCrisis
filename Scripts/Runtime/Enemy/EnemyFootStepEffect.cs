using UnityEngine;

namespace SFEscape.Runtime.Enemy
{
    public class EnemyFootStepEffect : MonoBehaviour
    {
        [SerializeField]
        private AudioSource audioSource;
        [SerializeField]
        private AudioClip[] footStepClips;
        [SerializeField]
        private float footStepPitchRange = 0.1f;
        [SerializeField]
        private float footStepBasePitch = 1.0f;
        
        /// <summary>
        /// 足音の効果音を再生
        /// アニメーションイベントから呼び出し
        /// </summary>
        public void PlayFootStepEffect()
        {
            audioSource.pitch = footStepBasePitch + Random.Range(-footStepPitchRange, footStepPitchRange);
            audioSource.PlayOneShot(footStepClips[Random.Range(0, footStepClips.Length)]);
        }
    }
}