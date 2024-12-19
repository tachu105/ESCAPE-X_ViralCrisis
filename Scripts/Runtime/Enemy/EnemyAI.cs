using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;
using UnityEngine.AI;
using Cysharp.Threading.Tasks.Linq;
using System.Threading;
using System;

namespace SFEscape.Runtime.Enemy
{
    /// <summary>
    /// 敵キャラクターの状態を表す列挙型
    /// </summary>
    public enum EnemyState
    {
        /// <summary>周囲を警戒して待機している状態</summary>
        Idle,
        /// <summary>指定されたポイントを巡回している状態</summary>
        Patrol,
        /// <summary>プレイヤーを直接視認して追跡している状態</summary>
        PlayerChase,
        /// <summary>音の発生源を追跡している状態</summary>
        SoundChase,
        /// <summary>強化状態でプレイヤーを追跡している状態</summary>
        PoweredChase,
        /// <summary>プレイヤーへの攻撃状態</summary>
        Attack
    }

    /// <summary>
    /// AIで制御される敵キャラクターの振る舞いを管理するコンポーネント。
    /// プレイヤーの追跡、巡回行動、強化状態など、
    /// 敵キャラクターの複雑な行動パターンを制御する。
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyAI : MonoBehaviour
    {
        #region Serialized Fields
        /// <summary>追跡対象となるプレイヤーのTransform</summary>
        [SerializeField, Tooltip("プレイヤーのTransform")]
        private Transform player;

        /// <summary>プレイヤーを検知できる最大距離</summary>
        [SerializeField, Tooltip("検知距離")]
        private float detectionRange = 10f;

        /// <summary>プレイヤーを検知できる視野角（度数）</summary>
        [SerializeField, Tooltip("視野角")]
        private float detectionAngle = 35f;

        /// <summary>プレイヤーを見失うと判定する距離</summary>
        [SerializeField, Tooltip("プレイヤーを見失うまでの距離")]
        private float missPlayerRange = 10f;

        /// <summary>視界を遮る障害物のレイヤーマスク</summary>
        [SerializeField, Tooltip("障害物のLayer")]
        private LayerMask obstacleMask;

        /// <summary>強化状態に移行するまでの時間（秒）</summary>
        [SerializeField, Tooltip("強化状態に移行するまでの時間")]
        private float powerUpTime = 10f;

        /// <summary>強化状態の継続時間（秒）</summary>
        [SerializeField, Tooltip("強化状態の持続時間")]
        private float powerUpDuration = 10f;

        /// <summary>強化状態中のプレイヤー位置通知間隔（秒）</summary>
        [SerializeField, Tooltip("強化状態中の通知間隔")]
        private float notificationInterval = 5f;

        /// <summary>通常時の移動速度</summary>
        [SerializeField, Tooltip("通常時の移動速度")]
        private float normalSpeed = 3.0f;

        /// <summary>強化状態時の速度倍率</summary>
        [SerializeField, Tooltip("強化状態での速度強化倍率")]
        private float poweredSpeedMultiplier = 1.2f;

        /// <summary>巡回する地点のリスト</summary>
        [SerializeField, Tooltip("巡回ポイントリスト")]
        private Transform[] patrolPoints;

        /// <summary>アニメーション制御用のAnimatorコンポーネント</summary>
        [SerializeField, Tooltip("アニメーター")]
        private Animator animator;

        /// <summary>強化状態時の感染攻撃範囲</summary>
        [SerializeField, Tooltip("感染攻撃範囲")]
        private GameObject infectArea;
        
        [SerializeField, Tooltip("感染攻撃エフェクト")]
        private ParticleSystem infectEffect;
        #endregion

        #region Private Fields
        /// <summary>経路探索用のNavMeshAgent</summary>
        private NavMeshAgent agent;

        /// <summary>最後に通知されたプレイヤーの位置</summary>
        private Vector3 lastNotifiedPlayerPosition;

        /// <summary>現在の巡回ポイントのインデックス</summary>
        private int currentPatrolIndex = 0;

        /// <summary>各種状態管理用のキャンセレーショントークンソース</summary>
        private CancellationTokenSource stateCts, playerPosNotificationCts, destroyCts;

        /// <summary>状態変更中かどうか</summary>
        private bool isChangingState = false;
        #endregion
        
        /// <summary>現在の敵の状態を管理するReactiveProperty</summary>
        public AsyncReactiveProperty<EnemyState> state = new AsyncReactiveProperty<EnemyState>(EnemyState.Idle);
        
        /// <summary>強化状態かどうかを示すフラグ</summary>
        public ReactiveProperty<bool> isPoweredUp = new ReactiveProperty<bool>(false);

        /// <summary>
        /// コンポーネントの初期化処理を行う。
        /// 必要なコンポーネントの取得、NavMeshの初期化、
        /// 各種タイマーやイベントの設定を行う。
        /// </summary>
        private void Start()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
                agent.speed = normalSpeed;
            }

            if (!agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                    Debug.Log("エージェントを NavMesh 上にワープさせました。");

                }
                else
                {
                    Debug.LogError("エージェントを NavMesh 上に戻せませんでした。");
                    return;
                }
            }

            destroyCts = new CancellationTokenSource();


            // 強化状態へのタイマー
            Observable.Timer(TimeSpan.FromSeconds(powerUpTime),TimeSpan.FromSeconds(powerUpTime + powerUpDuration))
                .SelectMany(_ =>
                    Observable.Concat(
                        // PowerUpをtrueにする
                        Observable.Return(Unit.Default)
                            .Do(_ => {
                                if (playerPosNotificationCts != null && !playerPosNotificationCts.IsCancellationRequested)
                                {
                                    playerPosNotificationCts.Cancel();
                                    playerPosNotificationCts.Dispose();
                                }

                                playerPosNotificationCts = new CancellationTokenSource();

                                isPoweredUp.Value = true;
                                Debug.Log("敵が強化状態になりました！");
                                NotifyPlayerPositionPeriodically(playerPosNotificationCts.Token).Forget();
                            }),
                        // 強化状態が終了するまで待機
                        Observable.Timer(TimeSpan.FromSeconds(powerUpDuration)),
                        // PowerUpをfalseにする
                        Observable.Return(Unit.Default)
                            .Do(_ => {
                                isPoweredUp.Value = false;
                                if (playerPosNotificationCts != null && !playerPosNotificationCts.IsCancellationRequested)
                                {
                                    playerPosNotificationCts.Cancel();
                                    playerPosNotificationCts.Dispose();
                                }
                                Debug.Log("敵の強化状態が終了しました。");
                            })
                    )
                )
                .Subscribe()
                .AddTo(this);

            // プレイヤーの距離と視野を監視
            Observable.EveryUpdate(destroyCts.Token)
                .Select(_ => Vector3.Distance(transform.position, player.position))
                .Subscribe(distance =>
                {
                    if (IsPlayerInSight() && state.Value != EnemyState.PlayerChase)
                    {
                        ChangeState(EnemyState.PlayerChase);
                    }
                }).AddTo(this);

            // 状態の変化に応じた動作を実行
            state.DistinctUntilChanged()
                .Subscribe(async newState =>
                {
                    if(stateCts != null)
                    {
                        stateCts.Cancel();
                        stateCts.Dispose();
                    }
                    stateCts = new CancellationTokenSource();
                    CancellationToken token = stateCts.Token;

                    switch (newState)
                    {
                        case EnemyState.Idle:
                            animator.SetBool("IsMoving", false);
                            await IdleBehavior(token);
                            break;
                        case EnemyState.Patrol:
                            animator.SetBool("IsMoving", true);
                            await PatrolBehavior(token);
                            break;
                        case EnemyState.PlayerChase:
                            animator.SetBool("IsMoving", true);
                            await PlayerChaseBehavior(token);
                            break;
                        case EnemyState.SoundChase:
                            animator.SetBool("IsMoving", true);
                            await SoundChaseBehavior(token);
                            break;
                        case EnemyState.PoweredChase:
                            animator.SetBool("IsMoving", true);
                            await PoweredChaseBehavior(token);
                            break;
                        case EnemyState.Attack:
                            await AttackBehavior(token);
                            break;
                    }
                }).AddTo(this);

            //  強化状態になったらプレイヤーを追跡し始める
            isPoweredUp.Subscribe(powered =>
            {
                if (powered)
                {
                    agent.speed = normalSpeed * poweredSpeedMultiplier;
                    state.Value = IsPlayerInSight() ? EnemyState.PlayerChase : EnemyState.PoweredChase;
                    infectArea.SetActive(true); // 感染攻撃を有効化
                    infectEffect.Play();
                }
                else
                {
                    infectArea.SetActive(false);    // 感染攻撃を無効化
                    infectEffect.Stop();
                    if (!IsPlayerInSight())
                    {
                        agent.speed = normalSpeed;
                        state.Value = EnemyState.Idle;
                    }
                }
            }).AddTo(this);
        }

        /// <summary>
        /// アニメーションの速度パラメータの更新を行う。
        /// </summary>
        private void Update()
        {
            animator.SetFloat("MoveSpeed", agent.velocity.magnitude / agent.speed);
        }

        /// <summary>
        /// EnemyStateを変更する。
        /// 変更先が同じStateなら変更しない。
        /// </summary>
        /// <param name="newState">変更先のEnemyState</param>
        private void ChangeState(EnemyState newState)
        {
            if (this == null) return;
            if (state.Value == newState) return;

            if (isChangingState) return;
            isChangingState = true;

            try
            {
                Debug.Log($"敵の状態が変更されました {state.Value} to {newState}");
                state.Value = newState;
            }
            finally
            {
                isChangingState = false;
            }
        }

        /// <summary>
        /// プレイヤーが敵の視界内にいるかどうかを判定する。
        /// 距離、角度、障害物の有無を考慮して判定を行う。
        /// </summary>
        /// <returns>プレイヤーが視界内にいる場合はtrue、それ以外はfalse</returns>
        private bool IsPlayerInSight()
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer <= detectionRange)
            {
                float angle = Vector3.Angle(transform.forward, directionToPlayer);
                if (angle <= detectionAngle / 2)
                {
                    if (!Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleMask))
                    {
                        return true; // 視認成功
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 待機状態の行動を制御。
        /// 一定時間待機した後、巡回状態に移行する。
        /// </summary>
        /// <param name="token">処理のキャンセル用トークン</param>

        private async UniTask IdleBehavior(CancellationToken token)
        {
            Debug.Log("Idle: 周囲を警戒中...");
            agent.isStopped = true; // 移動を停止
            await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken:token);
            ChangeState(EnemyState.Patrol);
        }

        /// <summary>
        /// 巡回状態の行動を制御。
        /// 設定された巡回ポイントを順番に移動する。
        /// </summary>
        /// <param name="token">処理のキャンセル用トークン</param>
        private async UniTask PatrolBehavior(CancellationToken token)
        {
            Debug.Log("Patrol: 巡回中...");
            agent.isStopped = false;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);

            while (state.Value == EnemyState.Patrol && !token.IsCancellationRequested)
            {
                // NavMesh上にNavMeshAgentがあるか確認
                if (!agent.isOnNavMesh)
                {
                    if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
                    {
                        agent.Warp(hit.position);
                        Debug.Log("エージェントを NavMesh 上にワープさせました。");

                    }
                    else
                    {
                        Debug.LogError("エージェントを NavMesh 上に戻せませんでした。");
                        return;
                    }
                }


                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    // 次の巡回ポイントへ
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                    Debug.Log($"次の巡回ポイント: {patrolPoints[currentPatrolIndex].name}");
                    ChangeState(EnemyState.Idle);
                    break;
                }
                await UniTask.Yield(token);
            }
        }

        /// <summary>
        /// プレイヤー追跡状態の行動を制御。
        /// プレイヤーの現在位置に向かって直接追跡を行う。
        /// </summary>
        /// <param name="token">処理のキャンセル用トークン</param>
        private async UniTask PlayerChaseBehavior(CancellationToken token)
        {
            try
            {
                Debug.Log("PlayerChase: プレイヤーを直接追跡中...");
                agent.isStopped = false;

                while (state.Value == EnemyState.PlayerChase && !token.IsCancellationRequested)
                {
                    if (this == null) return;

                    if (!agent.isOnNavMesh)
                    {
                        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
                        {
                            agent.Warp(hit.position);
                            Debug.Log("エージェントを NavMesh 上にワープさせました。");
                        }
                        else
                        {
                            Debug.LogError("エージェントを NavMesh 上に戻せませんでした。");
                            return;
                        }
                    }

                    agent.SetDestination(player.position);

                    if (missPlayerRange < (player.position - transform.position).magnitude)
                    {
                        Debug.Log("敵がプレイヤーを見失いました");
                        await UniTask.NextFrame(token);
                        var newState = isPoweredUp.Value ? EnemyState.PoweredChase : EnemyState.Idle;
                        ChangeState(newState);
                    }

                    await UniTask.Yield(token);
                }
            }
            catch (System.OperationCanceledException)
            {
                // キャンセル時は正常終了
            }
        }

        /// <summary>
        /// 強化状態での追跡行動を制御。
        /// 定期的に通知されるプレイヤーの位置情報を元に追跡を行う。
        /// </summary>
        /// <param name="token">処理のキャンセル用トークン</param>
        private async UniTask PoweredChaseBehavior(CancellationToken token)
        {
            try
            {
                Debug.Log("PoweredChase: 強化状態で追跡中...");
                agent.isStopped = false;

                while (state.Value == EnemyState.PoweredChase && !token.IsCancellationRequested)
                {
                    if (this == null) return;

                    if (!agent.isOnNavMesh)
                    {
                        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
                        {
                            agent.Warp(hit.position);
                            Debug.Log("エージェントを NavMesh 上にワープさせました。");
                        }
                        else
                        {
                            Debug.LogError("エージェントを NavMesh 上に戻せませんでした。");
                            return;
                        }
                    }

                    // プレイヤーが視界に入ったら直接追跡に切り替え
                    if (IsPlayerInSight())
                    {
                        ChangeState(EnemyState.PlayerChase);
                        continue;
                    }

                    agent.SetDestination(lastNotifiedPlayerPosition);

                    await UniTask.Yield(token);
                }
            }
            catch (System.OperationCanceledException)
            {
                // キャンセル時は正常終了
            }
        }

        /// <summary>
        /// 音を追跡する状態の行動を制御。
        /// 最後に通知された音の発生位置まで移動する。
        /// </summary>
        /// <param name="token">処理のキャンセル用トークン</param>
        private async UniTask SoundChaseBehavior(CancellationToken token)
        {
            try
            {
                Debug.Log("SoundChase: 音を追跡中...");
                agent.isStopped = false;

                while (state.Value == EnemyState.SoundChase && !token.IsCancellationRequested)
                {
                    if (this == null) return;

                    if (!agent.isOnNavMesh)
                    {
                        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
                        {
                            agent.Warp(hit.position);
                            Debug.Log("エージェントを NavMesh 上にワープさせました。");
                        }
                        else
                        {
                            Debug.LogError("エージェントを NavMesh 上に戻せませんでした。");
                            return;
                        }
                    }

                    // プレイヤーが視界に入ったら直接追跡に切り替え
                    if (IsPlayerInSight())
                    {
                        ChangeState(EnemyState.PlayerChase);
                        continue;
                    }

                    agent.SetDestination(lastNotifiedPlayerPosition);

                    if (!agent.pathPending && agent.remainingDistance < 0.5f)
                    {
                        var newState = isPoweredUp.Value ? EnemyState.PoweredChase : EnemyState.Idle;
                        ChangeState(newState);
                        continue;
                    }

                    await UniTask.Yield(token);
                }
            }
            catch (System.OperationCanceledException)
            {
                // キャンセル時は正常終了
            }
        }

        /// <summary>
        /// 攻撃処理を行う。
        /// 攻撃アニメーションを再生し、一定時間後に待機状態に戻る。
        /// </summary>
        /// <param name="token">処理のキャンセル用トークン</param>
        private async UniTask AttackBehavior(CancellationToken token)
        {
            Debug.Log("Attack: 攻撃中...");
            agent.isStopped = true; // 攻撃中は移動を停止
            animator.SetBool("IsAttacking", true);
            await UniTask.Delay(1000, cancellationToken:token);
            animator.SetBool("IsAttacking", false);
            ChangeState(EnemyState.Idle);
        }

        /// <summary>
        /// 強化状態中のプレイヤー位置通知処理を行う。
        /// 定期的にプレイヤーの現在位置を記録し、追跡に使用する。
        /// </summary>
        /// <param name="token">処理のキャンセル用トークン</param>
        private async UniTask NotifyPlayerPositionPeriodically(CancellationToken token)
        {
            try
            {
                while (isPoweredUp.Value && !token.IsCancellationRequested)
                {
                    if (this == null) return;

                    lastNotifiedPlayerPosition = player.position;
                    Debug.Log($"プレイヤーの位置: {lastNotifiedPlayerPosition}");

                    if (state.Value != EnemyState.PlayerChase)
                    {
                        ChangeState(EnemyState.PoweredChase);
                    }
                    await UniTask.Delay((int)(notificationInterval * 1000), cancellationToken: token);
                }
            }
            catch (System.OperationCanceledException)
            {
                // キャンセル時は正常終了
            }
        }

        /// <summary>
        /// 使用中のCancellationTokenSourceを適切に破棄。
        /// </summary>
        private void OnDestroy()
        {
            if (destroyCts != null && !destroyCts.IsCancellationRequested)
            {
                destroyCts.Cancel();
                destroyCts.Dispose();
            }

            if (stateCts != null && !stateCts.IsCancellationRequested)
            {
                stateCts.Cancel();
                stateCts.Dispose();
            }

            if (playerPosNotificationCts != null && !playerPosNotificationCts.IsCancellationRequested)
            {
                playerPosNotificationCts.Cancel();
                playerPosNotificationCts.Dispose();
            }
        }
    }
}

