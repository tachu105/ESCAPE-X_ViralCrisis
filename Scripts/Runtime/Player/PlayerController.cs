using UnityEngine;
using SFEscape.Runtime.Utilities;
using UnityEngine.AI;
using R3;
using SFEscape.Runtime.Audio;

namespace SFEscape.Runtime.Player
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField, Tooltip("歩行時移動速度")]
        private float walkSpeed = 5.0f;
        [SerializeField, Tooltip("スプリント時移動速度")]
        private float sprintSpeed = 10.0f;
        [SerializeField, Tooltip("匍匐時移動速度")]
        private float crawlSpeed = 1.0f;
        [SerializeField, Tooltip("カメラ感度")]
        private Vector2 cameraSensitivity;
        [SerializeField, Tooltip("カメラ上下操作の反転")]
        private bool invertMouseY = true;

        [SerializeField]
        private Transform cameraTransform;
        [SerializeField, Tooltip("Playerのボディ（見た目）")]
        private Transform bodyTransform;

        [SerializeField, Tooltip("カメラ上下回転のクランプ角度（min, max)")]
        private Vector2 cameraXRotationLimits = new Vector2(-60, 60);
        [SerializeField] private bool isCrawling = false;
        
        [SerializeField]
        private PlayerAudioController audioController;

        private Vector3 moveDirection;
        private float moveSpeed;
        private Vector2 mouseInputValue;
        private bool isPosed = false;
        private Transform playerTransform;
        private NavMeshAgent navAgent;

        // 直立時と匍匐時のカメラ位置とボディ位置
        private readonly Vector3 normalCameraPos = new Vector3(0, 2f, 0);
        private readonly Vector3 crawlingCameraPos = new Vector3(0, 0.3f, 0);
        private readonly Vector3 normalBodyPos = Vector3.zero;
        private readonly Vector3 crawlingBodyPos = new Vector3(0, 0, -1.3f);

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            cameraTransform = cameraTransform ? cameraTransform : Camera.main.transform;
            playerTransform = this.transform;
            navAgent = this.GetComponent<NavMeshAgent>();
            moveSpeed = walkSpeed;

            // Player移動処理
            Observable
                .EveryUpdate(destroyCancellationToken)
                .Where(this, static (_, self) => !self.isPosed)
                .Subscribe(this, static (_, self) =>
                {
                    self.navAgent.Move(
                        self.playerTransform.TransformDirection(
                            self.moveDirection * (self.isCrawling ? self.crawlSpeed : self.moveSpeed) * Time.deltaTime
                            )
                        );
                });

            // カメラの回転処理
            Observable
                .EveryUpdate(UnityFrameProvider.PostLateUpdate, destroyCancellationToken)
                .Where(this, static (_, self) => !self.isPosed)
                .Subscribe(this, static (_, self) =>
                {
                    self.RotatePlayer(self.mouseInputValue);
                });
        }

        /// <summary>
        /// プレイヤーの移動制御メソッド
        /// </summary>
        /// <param name="inputValue"> 移動の入力値 </param>
        public void MovePlayer(Vector2 inputValue)
        {
            moveDirection = inputValue.ToVector3XZ();
            ChangeFootstepSound();
        }
        
        /// <summary>
        /// マウスの移動量を設定するメソッド
        /// </summary>
        /// <param name="inputValue"> マウスの移動量 </param>
        public void SetMouseInputValue(Vector2 inputValue)
        {
            mouseInputValue = inputValue;
        }

        /// <summary>
        /// カメラの回転制御メソッド
        /// </summary>
        /// <param name="inputValue"> 回転の入力値 </param>
        private void RotatePlayer(Vector2 inputValue)
        {
            Vector3 cameraRotation = AngleUtility.RotationTo180(
                new Vector3(cameraTransform.localEulerAngles.x, playerTransform.localEulerAngles.y)
                );

            cameraRotation += new Vector3(
                    inputValue.y * (invertMouseY ? 1 : -1) * cameraSensitivity.y * cameraSensitivity.y,
                    inputValue.x * cameraSensitivity.x
                    ) * Time.deltaTime;
            cameraRotation.x = Mathf.Clamp(cameraRotation.x, cameraXRotationLimits.x, cameraXRotationLimits.y);

            // 左右回転反映
            playerTransform.localEulerAngles = new Vector3(
                playerTransform.localEulerAngles.x,
                AngleUtility.To360(cameraRotation.y),
                playerTransform.localEulerAngles.z
                );
            // 上下回転反映
            cameraTransform.localEulerAngles = new Vector3(
                AngleUtility.To180(cameraRotation.x),
                cameraTransform.localEulerAngles.y,
                cameraTransform.localEulerAngles.z
                );
        }

        /// <summary>
        /// プレイヤーのスプリント状態を設定するメソッド
        /// </summary>
        public void SprintPlayer(bool isSprint)
        {
            moveSpeed = isSprint ? sprintSpeed : walkSpeed;
            ChangeFootstepSound();
        }

        /// <summary>
        /// 匍匐移動状態を設定するメソッド
        /// </summary>
        public void SetCrawling(bool newValue)
        {
            isCrawling = newValue;

            // 匍匐状態の場合、カメラとプレイヤーの位置を変更
            if (newValue)
            {
                cameraTransform.localPosition = crawlingCameraPos;
                bodyTransform.localPosition = crawlingBodyPos;
                bodyTransform.localEulerAngles = new Vector3(90, 0, 0);
            }
            else
            {
                cameraTransform.localPosition = normalCameraPos;
                bodyTransform.localPosition = normalBodyPos;
                bodyTransform.localEulerAngles = Vector3.zero;
            }
        }
        
        /// <summary>
        /// 足音の再生を切り替えるメソッド
        /// </summary>
        private void ChangeFootstepSound()
        {
            if (moveDirection.magnitude == 0) audioController.FootStepEffect(false, false).Forget();
            else audioController.FootStepEffect(true, Mathf.Approximately(moveSpeed, sprintSpeed)).Forget();
        }
        
        
        public void ChangeCameraSensitiveX(float value)
        {
            cameraSensitivity.x = value;
        }
        public void ChangeCameraSensitiveY(float value)
        {
            cameraSensitivity.y = value;
        }
        public void ChangeInvertY(bool value)
        {
            invertMouseY = value;
        }
        public void PosePlayerInput(bool value)
        {
            isPosed = value;
        }
    }
}
