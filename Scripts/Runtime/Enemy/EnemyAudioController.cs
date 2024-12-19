using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using SFEscape.Runtime.Audio;
using UnityEngine;
using R3;

namespace SFEscape.Runtime.Enemy
{
    public class EnemyAudioController : MonoBehaviour
    {
        [SerializeField]
        private PlayerAudioController playerAudioController;
        
        [SerializeField]
        private AudioSource crySeSource;
        [SerializeField]
        private AudioClip monsterCryClip;

        [SerializeField]
        private Vector2 monsterCryPitch;
        
        private EnemyAI enemyAI;

        private void Start()
        {
            enemyAI = this.GetComponent<EnemyAI>();

            enemyAI.state
                .Subscribe(state =>
                {
                    switch (state)
                    {
                        case EnemyState.PlayerChase:
                            playerAudioController.ChangeChaseBGM();
                            break;
                        case EnemyState.Patrol:
                            playerAudioController.ChangeNormalBGM();
                            break;
                    }
                }).AddTo(destroyCancellationToken);
            
            enemyAI.isPoweredUp
                .Skip(1)
                .Subscribe(this, static (isPowerUp, self) =>
                {
                    if (isPowerUp)
                    {
                        self.crySeSource.pitch = self.monsterCryPitch.x;
                        self.crySeSource.PlayOneShot(self.monsterCryClip);
                    }
                    else
                    {
                        self.crySeSource.pitch = self.monsterCryPitch.y;
                        self.crySeSource.PlayOneShot(self.monsterCryClip);
                    }
                }).AddTo(destroyCancellationToken);
        }
    }
}