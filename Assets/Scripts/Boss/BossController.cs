using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Stagger.Boss
{
    // Simple object pool manager
    // This is a basic implementation - replace with your project's PoolManager if available
    public class SimplePoolManager : MonoBehaviour
    {
        private static SimplePoolManager _instance;
        public static SimplePoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("SimplePoolManager");
                    _instance = go.AddComponent<SimplePoolManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private Dictionary<string, Queue<Component>> _pools = new Dictionary<string, Queue<Component>>();
        private Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();

        public void CreatePool<T>(string key, T prefab, int size) where T : Component
        {
            if (_pools.ContainsKey(key))
            {
                Debug.LogWarning($"[SimplePoolManager] Pool '{key}' already exists!");
                return;
            }

            _prefabs[key] = prefab.gameObject;
            _pools[key] = new Queue<Component>();

            for (int i = 0; i < size; i++)
            {
                GameObject obj = Instantiate(prefab.gameObject);
                obj.SetActive(false);
                obj.transform.SetParent(transform);
                _pools[key].Enqueue(obj.GetComponent<T>());
            }

            Debug.Log($"[SimplePoolManager] Created pool '{key}' with {size} objects");
        }

        public T Spawn<T>(string key) where T : Component
        {
            if (!_pools.ContainsKey(key))
            {
                Debug.LogError($"[SimplePoolManager] Pool '{key}' doesn't exist!");
                return null;
            }

            Component obj;
            if (_pools[key].Count > 0)
            {
                obj = _pools[key].Dequeue();
            }
            else
            {
                // Pool exhausted, create new object
                GameObject newObj = Instantiate(_prefabs[key]);
                newObj.transform.SetParent(transform);
                obj = newObj.GetComponent<T>();
                Debug.LogWarning($"[SimplePoolManager] Pool '{key}' exhausted, created new object");
            }

            obj.gameObject.SetActive(true);
            return obj as T;
        }

        public void Despawn(string key, Component obj)
        {
            if (!_pools.ContainsKey(key))
            {
                Debug.LogError($"[SimplePoolManager] Pool '{key}' doesn't exist!");
                return;
            }

            obj.gameObject.SetActive(false);
            _pools[key].Enqueue(obj);
        }
    }
    
    // Main boss controller managing AI, states, and attacks.
    // Uses State pattern for behavior and Singleton pattern for pooling
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
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugGizmos = true;

        // Components
        private BossHealth _health;
        private ThreadSystem _threadSystem;
        private StateMachine _stateMachine;
        private SpriteRenderer _spriteRenderer;
        private Animator _animator;

        // States
        private BossIdleState _idleState;
        private BossAttackingState _attackingState;
        private BossStunnedState _stunnedState;
        private BossThreadBreakState _threadBreakState;
        private BossExecutionState _executionState;
        private BossDeathState _deathState;

        // Runtime state
        private float _lastAttackTime;
        private int _currentAttackIndex = 0;
        private bool _isEnraged = false;

        // Properties
        public BossData Data => _bossData;
        public BossHealth Health => _health;
        public ThreadSystem ThreadSystem => _threadSystem;
        public StateMachine StateMachine => _stateMachine;
        public Transform PlayerTransform => _playerTransform;
        public Transform ProjectileSpawnPoint => _projectileSpawnPoint;
        public bool IsEnraged => _isEnraged;
        public float AttackCooldown => _isEnraged ? _bossData.EnragedAttackInterval : _bossData.AttackInterval;

        // State properties
        public BossIdleState IdleState => _idleState;
        public BossAttackingState AttackingState => _attackingState;
        public BossStunnedState StunnedState => _stunnedState;
        public BossThreadBreakState ThreadBreakState => _threadBreakState;
        public BossExecutionState ExecutionState => _executionState;
        public BossDeathState DeathState => _deathState;

        private void Awake()
        {
            // Get components
            _health = GetComponent<BossHealth>();
            _threadSystem = GetComponent<ThreadSystem>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();

            // Initialize state machine
            _stateMachine = new StateMachine();
        }

        private void Start()
        {
            // Find player if not assigned
            if (_playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _playerTransform = player.transform;
                }
            }

            // Subscribe to health events (Observer pattern)
            if (_health != null)
            {
                _health.OnThreadBreakThreshold.AddListener(TriggerThreadBreak);
                _health.OnBossDefeated.AddListener(OnBossDefeated);
            }

            // Initialize with boss data
            if (_bossData != null)
            {
                Initialize(_bossData);
            }
            else
            {
                Debug.LogError("[BossController] No BossData assigned!");
            }
        }
        private void Update()
        {
            // Don't update state machine if boss is dead
            if (_health != null && _health.IsDead)
            {
                return;
            }
    
            _stateMachine?.Update();
    
            // Check for enrage
            if (!_isEnraged && _health.HealthPercent <= _bossData.EnrageThreshold)
            {
                Enrage();
            }
        }

        private void FixedUpdate()
        {
            if (_health != null && _health.IsDead)
            {
                return;
            }
            
            _stateMachine?.FixedUpdate();
        }
        
        // Initialize boss with data
        public void Initialize(BossData bossData)
        {
            _bossData = bossData;

            // Initialize health
            _health.Initialize(bossData);

            // Initialize thread system
            _threadSystem.Initialize(bossData, _ceilingAnchor);

            // Create projectile pool (Singleton pattern)
            if (_projectilePrefab != null)
            {
                Projectile projComponent = _projectilePrefab.GetComponent<Projectile>();
                if (projComponent != null)
                {
                    SimplePoolManager.Instance.CreatePool(_projectilePoolKey, projComponent, _projectilePoolSize);
                }
            }

            // Apply visuals
            if (_spriteRenderer != null && bossData.BossSprite != null)
            {
                _spriteRenderer.sprite = bossData.BossSprite;
            }

            if (_animator != null && bossData.AnimatorController != null)
            {
                _animator.runtimeAnimatorController = bossData.AnimatorController;
            }

            transform.localScale = Vector3.one * bossData.Scale;

            // Create state instances (State pattern)
            _idleState = new BossIdleState(this);
            _attackingState = new BossAttackingState(this);
            _stunnedState = new BossStunnedState(this);
            _threadBreakState = new BossThreadBreakState(this);
            _executionState = new BossExecutionState(this);
            _deathState = new BossDeathState(this);

            // Start in idle state
            _stateMachine.Initialize(_idleState);

            Debug.Log($"[BossController] Initialized: {bossData.BossName}");
        }
        
        // Fire a projectile using an attack pattern
        public void FireProjectile(AttackPattern pattern)
        {
            if (pattern == null || pattern.ProjectileData == null)
            {
                Debug.LogWarning("[BossController] Invalid attack pattern");
                return;
            }

            Vector3 spawnPos = _projectileSpawnPoint != null ? _projectileSpawnPoint.position : transform.position;

            // Calculate base direction
            Vector2 baseDirection;
            if (pattern.AimAtPlayer && _playerTransform != null)
            {
                baseDirection = (_playerTransform.position - spawnPos).normalized;
            }
            else
            {
                baseDirection = pattern.FixedDirection.normalized;
            }

            // Fire projectiles based on count
            if (pattern.ProjectileCount == 1)
            {
                // Single projectile
                SpawnProjectile(pattern.ProjectileData, spawnPos, baseDirection);
            }
            else
            {
                // Multiple projectiles with spread
                float angleStep = pattern.SpreadAngle / (pattern.ProjectileCount - 1);
                float startAngle = -pattern.SpreadAngle / 2f;

                for (int i = 0; i < pattern.ProjectileCount; i++)
                {
                    float angle = startAngle + (angleStep * i);
                    Vector2 direction = Quaternion.Euler(0, 0, angle) * baseDirection;
                    
                    // Delay between projectiles if specified
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
            Debug.Log($"[BossController] Fired attack: {pattern.PatternName}");
        }
        
        // Spawn a single projectile from pool
        private void SpawnProjectile(ProjectileData data, Vector3 position, Vector2 direction)
        {
            Projectile projectile = SimplePoolManager.Instance.Spawn<Projectile>(_projectilePoolKey);
            if (projectile != null)
            {
                projectile.transform.position = position;
                projectile.Initialize(data, direction);
            }
        }
        
        // Spawn projectile with delay
        private IEnumerator DelayedSpawn(ProjectileData data, Vector3 position, Vector2 direction, float delay)
        {
            yield return new WaitForSeconds(delay);
            SpawnProjectile(data, position, direction);
        }
        
        // Select a random attack pattern that's available (threads intact)
        public AttackPattern SelectRandomAttack()
        {
            if (_bossData.AttackPatterns == null || _bossData.AttackPatterns.Count == 0)
            {
                Debug.LogWarning("[BossController] No attack patterns available");
                return null;
            }

            // Filter available attacks based on thread states
            var availableAttacks = _bossData.AttackPatterns;
            
            // TODO: Filter based on broken threads
            // For now, just select randomly

            _currentAttackIndex = Random.Range(0, availableAttacks.Count);
            return availableAttacks[_currentAttackIndex];
        }
        
        // Enter enraged state
        private void Enrage()
        {
            _isEnraged = true;
            Debug.Log($"[BossController] <color=red>{_bossData.BossName} ENRAGED!</color>");
            
            // Visual feedback (could change color, animation, etc.)
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.red;
            }
        }
        
        /// Take damage and transition to stunned state
        public void OnDamaged(float damage)
        {
            _health.TakeDamage(damage);
            
            // Transition to stunned state
            if (_stateMachine.CurrentState != _stunnedState && 
                _stateMachine.CurrentState != _threadBreakState &&
                _stateMachine.CurrentState != _executionState)
            {
                _stateMachine.ChangeState(_stunnedState);
            }
        }
        
        // Trigger thread break QTE (called by Observer pattern event)
        public void TriggerThreadBreak(int thresholdIndex)
        {
            Debug.Log($"[BossController] Thread break triggered! Threshold: {thresholdIndex}");
            
            // Find next intact thread
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
        
        // Called when all threads are broken, ready for execution
        public void OnAllThreadsBroken()
        {
            Debug.Log($"[BossController] All threads broken - ready for execution!");
            // Boss becomes vulnerable to execution
            // Player can now press E to execute
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (_health != null)
            {
                _health.OnThreadBreakThreshold.RemoveListener(TriggerThreadBreak);
            }
        }

        // Debug visualization
        private void OnDrawGizmos()
        {
            if (!_showDebugGizmos) return;

            // Draw projectile spawn point
            if (_projectileSpawnPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_projectileSpawnPoint.position, 0.2f);
                Gizmos.DrawLine(_projectileSpawnPoint.position, _projectileSpawnPoint.position + Vector3.down);
            }

            // Draw ceiling anchor
            if (_ceilingAnchor != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_ceilingAnchor.position, 0.3f);
            }
        }

        [ContextMenu("Debug: Take 25% Damage")]
        private void DebugTakeDamage()
        {
            OnDamaged(_health.MaxHealth * 0.25f);
        }
        
        // Called when boss is defeated.
        public void OnBossDefeated()
        {
            Debug.Log($"[BossController] {_bossData.BossName} has been defeated!");
    
            // Transition to death state
            _stateMachine.ChangeState(_deathState);
        }
    }
}