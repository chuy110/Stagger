using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Stagger.Core.Managers;
using Stagger.UI;

namespace Stagger.Boss
{
    /// <summary>
    /// CRASH-SAFE VERSION - Prevents crashes during boss death
    /// Ultra-defensive null checks and error handling
    /// </summary>
    [RequireComponent(typeof(BossHealth))]
    [RequireComponent(typeof(ThreadSystem))]
    public class BossController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BossData _bossData;
        
        [Header("References")]
        [SerializeField] private Transform _projectileSpawnPoint;
        [SerializeField] private Transform _ceilingAnchor;
        [SerializeField] private Transform _playerTransform;
        
        [Header("Projectile Pooling")]
        [SerializeField] private string _projectilePoolKey = "BossProjectile";
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private int _projectilePoolSize = 30;
        
        [Header("Execution")]
        [SerializeField] private float _executionRange = 3f;
        [SerializeField] private KeyCode _executionKey = KeyCode.E;
        
        [Header("Victory")]
        public UnityEvent OnBossVictory;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugGizmos = true;

        private BossHealth _health;
        private ThreadSystem _threadSystem;
        private StateMachine _stateMachine;
        private SpriteRenderer _spriteRenderer;
        private Animator _animator;

        private BossIdleState _idleState;
        private BossAttackingState _attackingState;
        private BossStunnedState _stunnedState;
        private BossThreadBreakState _threadBreakState;
        private BossExecutionState _executionState;
        private BossDeathState _deathState;

        private float _lastAttackTime;
        private int _currentAttackIndex = 0;
        private bool _isEnraged = false;
        private bool _isDying = false;
        private bool _deathSequenceStarted = false;
        private bool _isPaused = false;

        // Properties
        public BossData Data => _bossData;
        public BossHealth Health => _health;
        public ThreadSystem ThreadSystem => _threadSystem;
        public StateMachine StateMachine => _stateMachine;
        public Transform PlayerTransform => _playerTransform;
        public Transform ProjectileSpawnPoint => _projectileSpawnPoint;
        public bool IsEnraged => _isEnraged;
        public float AttackCooldown => _isEnraged ? _bossData.EnragedAttackInterval : _bossData.AttackInterval;

        public BossIdleState IdleState => _idleState;
        public BossAttackingState AttackingState => _attackingState;
        public BossStunnedState StunnedState => _stunnedState;
        public BossThreadBreakState ThreadBreakState => _threadBreakState;
        public BossExecutionState ExecutionState => _executionState;
        public BossDeathState DeathState => _deathState;

        private void Awake()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("[BossController] Awake() START");
            
            _health = GetComponent<BossHealth>();
            Debug.Log($"[BossController] Health component: {(_health != null ? "FOUND" : "NULL")}");
            
            _threadSystem = GetComponent<ThreadSystem>();
            Debug.Log($"[BossController] ThreadSystem component: {(_threadSystem != null ? "FOUND" : "NULL")}");
            
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();

            _stateMachine = new StateMachine();
            Debug.Log("[BossController] StateMachine created");
            
            Debug.Log("[BossController] Awake() END");
            Debug.Log("═══════════════════════════════════════");
        }

        private void Start()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("[BossController] Start() BEGIN");

            if (_playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _playerTransform = player.transform;
                    Debug.Log("[BossController] Player found");
                }
                else
                {
                    Debug.LogWarning("[BossController] Player NOT found!");
                }
            }

            if (_health != null)
            {
                Debug.Log("[BossController] Subscribing to health events...");
                _health.OnThreadBreakThreshold.AddListener(TriggerThreadBreak);
                _health.OnBossDefeated.AddListener(HandleBossDeath);
                Debug.Log("[BossController] ✓ Health events subscribed");
            }
            else
            {
                Debug.LogError("[BossController] ✗ Cannot subscribe - Health is NULL!");
            }

            if (_bossData != null)
            {
                Debug.Log("[BossController] Initializing with BossData...");
                Initialize(_bossData);
            }
            else
            {
                Debug.LogError("[BossController] No BossData assigned!");
            }
            
            Debug.Log("[BossController] Start() END");
            Debug.Log("═══════════════════════════════════════");
        }

        private void Update()
        {
            // Check game state - prevent crash during non-combat states
            if (GameManager.Instance != null)
            {
                try
                {
                    GameManager.GameState currentState = GameManager.Instance.CurrentState;
                    
                    if (currentState != GameManager.GameState.Combat)
                    {
                        _isPaused = true;
                        return;
                    }
                    else
                    {
                        _isPaused = false;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[BossController] Error checking game state: {e.Message}");
                }
            }
            
            if (_isDying)
            {
                return;
            }
            
            if (_health == null || !_health.enabled || _health.IsDead)
            {
                return;
            }

            if (_stateMachine != null)
            {
                try
                {
                    _stateMachine.Update();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[BossController] StateMachine.Update error: {e.Message}");
                }
            }

            if (_threadSystem != null && _threadSystem.AllThreadsBroken)
            {
                if (Input.GetKeyDown(_executionKey))
                {
                    TryExecute();
                }
            }

            if (!_isEnraged && _health != null && _health.HealthPercent <= _bossData.EnrageThreshold)
            {
                Enrage();
            }
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance != null && 
                GameManager.Instance.CurrentState != GameManager.GameState.Combat)
            {
                return;
            }
            
            if (_isDying) return;
            if (_health == null || !_health.enabled || _health.IsDead) return;
            
            if (_stateMachine != null)
            {
                try
                {
                    _stateMachine.FixedUpdate();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[BossController] StateMachine.FixedUpdate error: {e.Message}");
                }
            }
        }
        
        public void Initialize(BossData bossData)
        {
            Debug.Log("────────────────────────────────────────");
            Debug.Log($"[BossController] Initialize() START - Boss: {bossData.BossName}");
            
            _bossData = bossData;

            // RESET FLAGS
            _isDying = false;
            _deathSequenceStarted = false;
            _isEnraged = false;
            _isPaused = false;
            enabled = true;

            Debug.Log("[BossController] Initializing Health...");
            if (_health != null)
            {
                _health.Initialize(bossData);
            }
            
            Debug.Log("[BossController] Initializing ThreadSystem...");
            if (_threadSystem != null)
            {
                _threadSystem.Initialize(bossData, _ceilingAnchor);
            }

            if (_projectilePrefab != null)
            {
                Projectile projComponent = _projectilePrefab.GetComponent<Projectile>();
                if (projComponent != null && PoolManager.Instance != null)
                {
                    Debug.Log($"[BossController] Creating projectile pool: {_projectilePoolKey}");
                    
                    try
                    {
                        PoolManager.Instance.ClearPool(_projectilePoolKey);
                    }
                    catch
                    {
                        // Pool doesn't exist yet
                    }
                    
                    PoolManager.Instance.CreatePool(_projectilePoolKey, projComponent, _projectilePoolSize);
                    Debug.Log($"[BossController] ✓ Pool created");
                }
            }

            if (_spriteRenderer != null && bossData.BossSprite != null)
            {
                _spriteRenderer.sprite = bossData.BossSprite;
                _spriteRenderer.color = Color.white;
            }

            if (_animator != null && bossData.AnimatorController != null)
            {
                _animator.runtimeAnimatorController = bossData.AnimatorController;
            }

            transform.localScale = Vector3.one * bossData.Scale;

            Debug.Log("[BossController] Creating state instances...");
            _idleState = new BossIdleState(this);
            _attackingState = new BossAttackingState(this);
            _stunnedState = new BossStunnedState(this);
            _threadBreakState = new BossThreadBreakState(this);
            _executionState = new BossExecutionState(this);
            _deathState = new BossDeathState(this);

            Debug.Log("[BossController] Initializing state machine with IdleState...");
            _stateMachine.Initialize(_idleState);

            Debug.Log($"[BossController] ✓ Initialize() COMPLETE - {bossData.BossName} ready!");
            Debug.Log("────────────────────────────────────────");
        }
        
        public void FireProjectile(AttackPattern pattern)
        {
            if (_isPaused || _isDying) return;
            
            if (_health == null || _health.IsDead)
            {
                return;
            }
    
            if (pattern == null || pattern.ProjectileData == null)
            {
                return;
            }

            Vector3 spawnPos = _projectileSpawnPoint != null ? _projectileSpawnPoint.position : transform.position;

            Vector2 baseDirection;
            if (pattern.AimAtPlayer && _playerTransform != null)
            {
                baseDirection = (_playerTransform.position - spawnPos).normalized;
            }
            else
            {
                baseDirection = pattern.FixedDirection.normalized;
            }

            if (pattern.ProjectileCount == 1)
            {
                SpawnProjectile(pattern.ProjectileData, spawnPos, baseDirection);
            }
            else
            {
                float angleStep = pattern.SpreadAngle / (pattern.ProjectileCount - 1);
                float startAngle = -pattern.SpreadAngle / 2f;

                for (int i = 0; i < pattern.ProjectileCount; i++)
                {
                    float angle = startAngle + (angleStep * i);
                    Vector2 direction = Quaternion.Euler(0, 0, angle) * baseDirection;
            
                    if (pattern.ProjectileDelay > 0f)
                    {
                        StartCoroutine(DelayedSpawn(pattern.ProjectileData, spawnPos, direction, i * pattern.ProjectileDelay));
                    }
                    else
                    {
                        SpawnProjectile(pattern.ProjectileData, spawnPos, direction);
                    }
                }
            }

            _lastAttackTime = Time.time;
        }
        
        private void SpawnProjectile(ProjectileData data, Vector3 position, Vector2 direction)
        {
            if (_isPaused || _isDying) return;
            
            if (_health == null || !_health.enabled || _health.IsDead || !enabled || this == null)
            {
                return;
            }
    
            if (PoolManager.Instance == null) return;
            
            try
            {
                Projectile projectile = PoolManager.Instance.Spawn<Projectile>(_projectilePoolKey);
                if (projectile != null)
                {
                    projectile.transform.position = position;
                    projectile.SetPoolKey(_projectilePoolKey);
                    projectile.Initialize(data, direction);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BossController] Error spawning projectile: {e.Message}");
            }
        }
        
        private IEnumerator DelayedSpawn(ProjectileData data, Vector3 position, Vector2 direction, float delay)
        {
            yield return new WaitForSeconds(delay);
    
            if (_isPaused || _isDying || _health == null || _health.IsDead || !enabled || this == null)
            {
                yield break;
            }
    
            SpawnProjectile(data, position, direction);
        }
        
        public AttackPattern SelectRandomAttack()
        {
            if (_bossData == null || _bossData.AttackPatterns == null || _bossData.AttackPatterns.Count == 0)
            {
                return null;
            }

            var availableAttacks = _bossData.AttackPatterns;
            _currentAttackIndex = Random.Range(0, availableAttacks.Count);
            return availableAttacks[_currentAttackIndex];
        }
        
        private void Enrage()
        {
            _isEnraged = true;
            Debug.Log($"[BossController] <color=red>{_bossData.BossName} ENRAGED!</color>");
            
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.red;
            }
        }
        
        public void OnDamaged(float damage)
        {
            if (_health != null)
            {
                _health.TakeDamage(damage);
            }
            
            if (_stateMachine != null && _stateMachine.CurrentState != _stunnedState && 
                _stateMachine.CurrentState != _threadBreakState &&
                _stateMachine.CurrentState != _executionState &&
                _stateMachine.CurrentState != _deathState)
            {
                _stateMachine.ChangeState(_stunnedState);
            }
        }
        
        public void TriggerThreadBreak(int thresholdIndex)
        {
            Debug.Log($"[BossController] Thread break triggered! Threshold: {thresholdIndex}");
            
            if (_threadSystem == null) return;
            
            int threadIndex = -1;
            for (int i = 0; i < _threadSystem.TotalThreads; i++)
            {
                if (_threadSystem.IsThreadIntact(i))
                {
                    threadIndex = i;
                    break;
                }
            }

            if (threadIndex >= 0)
            {
                _stateMachine.ChangeState(_threadBreakState);
                _threadSystem.StartThreadBreakQTE(threadIndex);
            }
        }
        
        public void OnAllThreadsBroken()
        {
            Debug.Log($"[BossController] <color=cyan>★★★ ALL THREADS BROKEN! ★★★</color>");
            
            if (_health != null)
            {
                _health.OnAllThreadsBroken();
            }
            
            if (_stateMachine != null)
            {
                _stateMachine.ChangeState(_idleState);
            }
        }

        private void TryExecute()
        {
            if (_playerTransform == null) return;
            
            float distance = Vector3.Distance(transform.position, _playerTransform.position);
            
            if (distance <= _executionRange)
            {
                Debug.Log($"[BossController] <color=cyan>★★★ EXECUTION INITIATED! ★★★</color>");
                if (_stateMachine != null)
                {
                    _stateMachine.ChangeState(_executionState);
                }
            }
        }

        /// <summary>
        /// ULTRA-SAFE: Death handler with maximum crash prevention
        /// </summary>
        private void HandleBossDeath()
        {
            Debug.Log("████████████████████████████████████████");
            Debug.Log("█ HANDLE BOSS DEATH CALLED");
            Debug.Log("████████████████████████████████████████");
            
            // CRITICAL: Check if we're in a safe state to die
            if (this == null || gameObject == null)
            {
                Debug.LogError("[BossController] GameObject or component is null - aborting death");
                return;
            }
            
            if (_deathSequenceStarted)
            {
                Debug.LogWarning("[BossController] ⚠ Death sequence already started - IGNORING");
                return;
            }
            
            _deathSequenceStarted = true;
            _isDying = true;
            
            Debug.Log($"[BossController] Boss defeated: {(_bossData != null ? _bossData.BossName : "Unknown")}");
            
            // STEP 1: Stop coroutines safely
            Debug.Log("[BossController] STEP 1: Stopping coroutines...");
            try
            {
                StopAllCoroutines();
                Debug.Log("[BossController] ✓ Coroutines stopped");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BossController] Error stopping coroutines: {e.Message}");
            }
    
            // STEP 2: Change state safely
            Debug.Log("[BossController] STEP 2: Changing to death state...");
            try
            {
                if (_stateMachine != null && _deathState != null)
                {
                    _stateMachine.ChangeState(_deathState);
                    Debug.Log("[BossController] ✓ State changed to DeathState");
                }
                else
                {
                    Debug.LogWarning("[BossController] ⚠ StateMachine or DeathState is null");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BossController] Error changing state: {e.Message}");
            }
    
            // STEP 3: Animation safely
            Debug.Log("[BossController] STEP 3: Triggering death animation...");
            try
            {
                if (_animator != null)
                {
                    _animator.SetTrigger("Death");
                    Debug.Log("[BossController] ✓ Death animation triggered");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BossController] Error with animator: {e.Message}");
            }
            
            // STEP 4: Invoke victory event SAFELY - This is where crashes usually happen
            Debug.Log("[BossController] STEP 4: Invoking OnBossVictory event...");
            try
            {
                if (OnBossVictory != null)
                {
                    int listenerCount = OnBossVictory.GetPersistentEventCount();
                    Debug.Log($"[BossController] OnBossVictory has {listenerCount} persistent listeners");
                    
                    // Try to invoke
                    Debug.Log("[BossController] About to invoke event...");
                    OnBossVictory.Invoke();
                    Debug.Log("[BossController] ✓ OnBossVictory invoked successfully");
                }
                else
                {
                    Debug.LogWarning("[BossController] ⚠ OnBossVictory is NULL - creating new event");
                    OnBossVictory = new UnityEvent();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BossController] ✗✗✗ CRITICAL ERROR invoking victory event: {e.Message}");
                Debug.LogError($"[BossController] Stack trace: {e.StackTrace}");
                
                // Try to transition to results screen manually as fallback
                Debug.Log("[BossController] Attempting manual transition to results...");
                try
                {
                    if (GameManager.Instance != null && UIManager.Instance != null)
                    {
                        string bossName = _bossData != null ? _bossData.BossName : "Boss";
                        float battleTime = UIManager.Instance.GetBattleTime();
                        UIManager.Instance.ShowResultScreen(bossName, battleTime, new List<ArtifactData>());
                        Debug.Log("[BossController] ✓ Manual transition successful");
                    }
                }
                catch (System.Exception e2)
                {
                    Debug.LogError($"[BossController] Manual transition also failed: {e2.Message}");
                }
            }
    
            Debug.Log("[BossController] ✓✓✓ DEATH SEQUENCE COMPLETE ✓✓✓");
            Debug.Log("████████████████████████████████████████");
        }

        private void OnDestroy()
        {
            Debug.Log("[BossController] OnDestroy() called");
            
            try
            {
                if (_health != null)
                {
                    _health.OnThreadBreakThreshold.RemoveListener(TriggerThreadBreak);
                    _health.OnBossDefeated.RemoveListener(HandleBossDeath);
                }
                
                if (PoolManager.Instance != null)
                {
                    try
                    {
                        PoolManager.Instance.DespawnAll<Projectile>(_projectilePoolKey);
                    }
                    catch { }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BossController] Error in OnDestroy: {e.Message}");
            }
        }

        private void OnDrawGizmos()
        {
            if (!_showDebugGizmos) return;

            if (_projectileSpawnPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_projectileSpawnPoint.position, 0.2f);
            }

            if (_ceilingAnchor != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_ceilingAnchor.position, 0.3f);
            }
            
            if (Application.isPlaying && _threadSystem != null && _threadSystem.AllThreadsBroken)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, _executionRange);
            }
        }

        [ContextMenu("Debug: Take 25% Damage")]
        private void DebugTakeDamage()
        {
            if (_health != null)
            {
                OnDamaged(_health.MaxHealth * 0.25f);
            }
        }
        
        [ContextMenu("Debug: Kill Boss Instantly")]
        private void DebugKillBoss()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("[BossController] DEBUG: Instant kill command");
            Debug.Log("═══════════════════════════════════════");
            
            if (_health != null)
            {
                _health.TakeDamage(_health.MaxHealth * 10f);
            }
            else
            {
                Debug.LogError("[BossController] Cannot kill - Health is null!");
            }
        }
    }
}