using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Stagger.Boss
{
    /// <summary>
    /// Projectile component - attach to projectile prefab.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private ProjectileData _data;
        private Vector2 _direction;
        private float _spawnTime;
        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null)
                _rb = gameObject.AddComponent<Rigidbody2D>();
            
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        public void Initialize(ProjectileData data, Vector2 direction)
        {
            _data = data;
            _direction = direction.normalized;
            _spawnTime = Time.time;
            
            // Apply visuals
            if (_spriteRenderer != null && data.ProjectileSprite != null)
            {
                _spriteRenderer.sprite = data.ProjectileSprite;
                _spriteRenderer.color = data.ProjectileColor;
            }
            
            transform.localScale = Vector3.one * data.Size;
            
            // Apply velocity
            if (_rb != null)
            {
                _rb.linearVelocity = _direction * data.Speed;
            }
        }

        public void OnSpawnFromPool()
        {
            _spawnTime = Time.time;
        }

        public void OnReturnToPool()
        {
            if (_rb != null)
                _rb.linearVelocity = Vector2.zero;
        }

        private void Update()
        {
            // Check lifetime
            if (Time.time - _spawnTime >= _data.Lifetime)
            {
                ReturnToPool();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Handle collisions (player parry, walls, etc.)
            if (other.CompareTag("Player"))
            {
                // Check if player is parrying
                // If not, damage player
                // If yes, reflect projectile
            }
        }

        private void ReturnToPool()
        {
            SimplePoolManager.Instance.Despawn("BossProjectile", this);
        }
    }

    /// <summary>
    /// Simple object pool manager (Singleton pattern).
    /// </summary>
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
                GameObject newObj = Instantiate(_prefabs[key]);
                newObj.transform.SetParent(transform);
                obj = newObj.GetComponent<T>();
                Debug.LogWarning($"[SimplePoolManager] Pool '{key}' exhausted, created new object");
            }

            obj.gameObject.SetActive(true);
            
            Projectile projectile = obj as Projectile;
            if (projectile != null)
            {
                projectile.OnSpawnFromPool();
            }
            
            return obj as T;
        }

        public void Despawn(string key, Component obj)
        {
            if (!_pools.ContainsKey(key))
            {
                Debug.LogError($"[SimplePoolManager] Pool '{key}' doesn't exist!");
                return;
            }

            Projectile projectile = obj as Projectile;
            if (projectile != null)
            {
                projectile.OnReturnToPool();
            }

            obj.gameObject.SetActive(false);
            _pools[key].Enqueue(obj);
        }
    }
    
    /// <summary>
    /// Main boss controller managing AI, states, and attacks.
    /// Uses State pattern for behavior and Singleton pattern for pooling.
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
        [Tooltip("Event raised when boss is defeated")]
        public UnityEvent OnBossVictory;
        
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
        private bool _isDying = false; // NEW: Flag to prevent multiple death calls

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
            _health = GetComponent<BossHealth>();
            _threadSystem = GetComponent<ThreadSystem>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();

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
                // FIXED: Use simple death handler that doesn't cause loops
                _health.OnBossDefeated.AddListener(HandleBossDeath);
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
            // CRITICAL: Stop all updates if dying
            if (_isDying) return;
            
            // Exit immediately if health component is disabled or null
            if (_health == null || !_health.enabled)
            {
                if (!enabled) return;
                enabled = false;
                Debug.Log("[BossController] Disabled due to boss death");
                return;
            }
    
            // Exit if dead flag is set
            if (_health.IsDead)
            {
                enabled = false;
                return;
            }

            _stateMachine?.Update();

            if (!_isEnraged && _health.HealthPercent <= _bossData.EnrageThreshold)
            {
                Enrage();
            }
        }

        private void FixedUpdate()
        {
            // CRITICAL: Stop all updates if dying
            if (_isDying) return;
            
            // Exit immediately if dead
            if (_health == null || !_health.enabled || _health.IsDead)
            {
                return;
            }
    
            _stateMachine?.FixedUpdate();
        }
        
        public void Initialize(BossData bossData)
        {
            _bossData = bossData;

            _health.Initialize(bossData);
            _threadSystem.Initialize(bossData, _ceilingAnchor);

            // Create projectile pool
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

            // Create state instances
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
        
        public void FireProjectile(AttackPattern pattern)
        {
            // CRITICAL: Don't spawn if dead or dying
            if (_isDying || _health == null || _health.IsDead)
            {
                Debug.Log("[BossController] FireProjectile blocked - boss is dead/dying");
                return;
            }
    
            if (pattern == null || pattern.ProjectileData == null)
            {
                Debug.LogWarning("[BossController] Invalid attack pattern");
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
            Debug.Log($"[BossController] Fired attack: {pattern.PatternName}");
        }
        
        private void SpawnProjectile(ProjectileData data, Vector3 position, Vector2 direction)
        {
            // CRITICAL: Exit if dying, health is null, disabled, or dead
            if (_isDying || _health == null || !_health.enabled || _health.IsDead)
            {
                return;
            }
    
            // Safety check - don't spawn if this component is disabled
            if (!enabled)
            {
                return;
            }
    
            Projectile projectile = SimplePoolManager.Instance.Spawn<Projectile>(_projectilePoolKey);
            if (projectile != null)
            {
                projectile.transform.position = position;
                projectile.Initialize(data, direction);
            }
        }
        
        private IEnumerator DelayedSpawn(ProjectileData data, Vector3 position, Vector2 direction, float delay)
        {
            yield return new WaitForSeconds(delay);
    
            // CRITICAL: Check if boss died/dying during delay
            if (_isDying || _health == null || !_health.enabled || _health.IsDead || !enabled)
            {
                yield break; // Exit coroutine silently
            }
    
            SpawnProjectile(data, position, direction);
        }
        
        public AttackPattern SelectRandomAttack()
        {
            if (_bossData.AttackPatterns == null || _bossData.AttackPatterns.Count == 0)
            {
                Debug.LogWarning("[BossController] No attack patterns available");
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
            _health.TakeDamage(damage);
            
            if (_stateMachine.CurrentState != _stunnedState && 
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
            Debug.Log($"[BossController] Boss is vulnerable to EXECUTION!");
            Debug.Log($"[BossController] Get close and press {_executionKey} to finish the boss!");
            
            // Notify health system
            if (_health != null)
            {
                _health.OnAllThreadsBroken();
            }
            
            // Transition to idle (but boss is invulnerable)
            _stateMachine.ChangeState(_idleState);
        }

        private void TryExecute()
        {
            if (_playerTransform == null)
            {
                Debug.LogWarning("[BossController] No player reference for execution!");
                return;
            }
            
            float distance = Vector3.Distance(transform.position, _playerTransform.position);
            
            if (distance <= _executionRange)
            {
                Debug.Log($"[BossController] <color=cyan>★★★ EXECUTION INITIATED! ★★★</color>");
                _stateMachine.ChangeState(_executionState);
            }
            else
            {
                Debug.Log($"[BossController] Too far for execution! Distance: {distance:F1}m (need < {_executionRange}m)");
            }
        }

        /// <summary>
        /// FIXED: Simple death handler that stops everything cleanly
        /// </summary>
        private void HandleBossDeath()
        {
            // Prevent multiple calls
            if (_isDying) return;
            _isDying = true;

            Debug.Log($"[BossController] {_bossData.BossName} has been defeated!");
            Debug.Log("[BossController] <color=green>★★★ VICTORY! ★★★</color>");
    
            // STEP 1: Stop all coroutines IMMEDIATELY (prevents delayed projectile spawns)
            StopAllCoroutines();
    
            // STEP 2: Disable this component to stop Update/FixedUpdate
            enabled = false;
    
            // STEP 3: Optional - play death animation
            if (_animator != null)
            {
                _animator.SetTrigger("Death"); // If you have a death animation
            }
    
            // STEP 4: Raise victory event (GameManager will handle artifacts and UI)
            OnBossVictory?.Invoke();
    
            Debug.Log("[BossController] Death sequence complete - boss safely stopped");
        }

        /// <summary>
        /// Safe artifact drop that can't freeze or cause issues
        /// </summary>
        private void DropArtifactsSafe()
        {
            if (_bossData == null || _bossData.PossibleDrops == null)
            {
                Debug.Log("[BossController] No artifacts to drop");
                return;
            }
    
            Debug.Log("[BossController] Rolling for artifact drops...");
            
            foreach (var drop in _bossData.PossibleDrops)
            {
                if (drop.ArtifactData == null) continue;
        
                float roll = Random.Range(0f, 1f);
                if (roll <= drop.DropChance)
                {
                    Debug.Log($"[BossController] <color=yellow>★ ARTIFACT DROPPED!</color> {drop.ArtifactData.name} (roll: {roll:F2})");
                    
                    // TODO: Add to player inventory or spawn pickup
                    // PlayerInventory.Instance.AddArtifact(drop.ArtifactData);
                    // OR
                    // Instantiate(artifactPickupPrefab, transform.position, Quaternion.identity);
                }
                else
                {
                    Debug.Log($"[BossController] No drop for {drop.ArtifactData.name}. Roll: {roll:F2}, Chance: {drop.DropChance:F2}");
                }
            }
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnThreadBreakThreshold.RemoveListener(TriggerThreadBreak);
                _health.OnBossDefeated.RemoveListener(HandleBossDeath);
            }
        }

        private void OnDrawGizmos()
        {
            if (!_showDebugGizmos) return;

            if (_projectileSpawnPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_projectileSpawnPoint.position, 0.2f);
                Gizmos.DrawLine(_projectileSpawnPoint.position, _projectileSpawnPoint.position + Vector3.down);
            }

            if (_ceilingAnchor != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_ceilingAnchor.position, 0.3f);
            }
            
            // Draw execution range
            if (Application.isPlaying && _threadSystem != null && _threadSystem.AllThreadsBroken)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, _executionRange);
            }
        }

        [ContextMenu("Debug: Take 25% Damage")]
        private void DebugTakeDamage()
        {
            OnDamaged(_health.MaxHealth * 0.25f);
        }
    }
}