using UnityEngine;
using Stagger.Core.States;
using Stagger.Core.Events;
using Stagger.Core.Managers;

    /// <summary>
    /// Main player controller implementing lateral movement, parrying, dodging, and execution.
    /// Uses State pattern for behavior management.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _moveBounds = 4f; // How far left/right player can move

        [Header("Combat Settings")]
        [SerializeField] private float _parryWindow = 0.3f;
        [SerializeField] private float _perfectParryWindow = 0.1f;
        [SerializeField] private float _parryCooldown = 0.5f;
        [SerializeField] private float _dodgeDuration = 0.5f;
        [SerializeField] private float _dodgeCooldown = 1f;
        [SerializeField] private float _invincibilityFrames = 0.3f;

        [Header("Animation Settings")]
        [SerializeField] private string _idleAnimation = "Player_Idle";
        [SerializeField] private string _moveAnimation = "Player_Move";
        [SerializeField] private string _parryAnimation = "Player_Parry";
        [SerializeField] private string _dodgeAnimation = "Player_Dodge";
        [SerializeField] private string _executeAnimation = "Player_Execute";

        [Header("Events")]
        [SerializeField] private GameEvent _onParrySuccess;
        [SerializeField] private GameEvent _onParryFailed;
        [SerializeField] private FloatGameEvent _onPlayerMoved;

        [Header("Debug")]
        [SerializeField] private bool _showDebugGizmos = true;
        [SerializeField] private bool _logStateChanges = false;

        // Components
        private Rigidbody2D _rigidbody;
        private Animator _animator;
        private StateMachine _stateMachine;

        // State instances
        private PlayerIdleState _idleState;
        private PlayerMovingState _movingState;
        private PlayerParryingState _parryingState;
        private PlayerDodgingState _dodgingState;
        private PlayerExecutingState _executingState;

        // State tracking
        private float _currentMoveDirection;
        private bool _isInvincible;
        private float _lastParryTime;
        private float _lastDodgeTime;

        // Public properties
        public StateMachine StateMachine => _stateMachine;
        public Rigidbody2D Rigidbody => _rigidbody;
        public Animator Animator => _animator;
        public float MoveSpeed => _moveSpeed;
        public float MoveBounds => _moveBounds;
        public float CurrentMoveDirection => _currentMoveDirection;
        public bool IsInvincible => _isInvincible;

        // State properties
        public PlayerIdleState IdleState => _idleState;
        public PlayerMovingState MovingState => _movingState;
        public PlayerParryingState ParryingState => _parryingState;
        public PlayerDodgingState DodgingState => _dodgingState;
        public PlayerExecutingState ExecutingState => _executingState;

        // Combat properties
        public float ParryWindow => _parryWindow;
        public float PerfectParryWindow => _perfectParryWindow;
        public float ParryCooldown => _parryCooldown;
        public float DodgeDuration => _dodgeDuration;
        public float DodgeCooldown => _dodgeCooldown;
        public float InvincibilityFrames => _invincibilityFrames;

        private void Awake()
        {
            // Get components
            _rigidbody = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();

            // Configure Rigidbody2D
            _rigidbody.gravityScale = 0f; // No gravity for 2D side-view
            _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;

            // Initialize state machine
            _stateMachine = new StateMachine();

            // Create state instances
            _idleState = new PlayerIdleState(this);
            _movingState = new PlayerMovingState(this);
            _parryingState = new PlayerParryingState(this);
            _dodgingState = new PlayerDodgingState(this);
            _executingState = new PlayerExecutingState(this);

            // Initialize to idle state
            _stateMachine.Initialize(_idleState);

            Debug.Log("[PlayerController] Initialized with state machine");
        }

        private void Start()
        {
            // Register with InputManager
            if (InputManager.Instance != null)
            {
                InputManager.Instance.SetPlayerController(this);
                Debug.Log("[PlayerController] Registered with InputManager");
            }
            else
            {
                Debug.LogError("[PlayerController] InputManager not found!");
            }
        }

        private void Update()
        {
            _stateMachine.Update();
        }

        private void FixedUpdate()
        {
            _stateMachine.FixedUpdate();
        }

        // === MOVEMENT ===

        /// <summary>
        /// Move the player laterally. Called by MoveCommand.
        /// </summary>
        public void Move(float direction)
        {
            _currentMoveDirection = direction;

            // Transition to moving state if not already
            if (Mathf.Abs(direction) > 0.01f && _stateMachine.CurrentState == _idleState)
            {
                _stateMachine.ChangeState(_movingState);
            }
            else if (Mathf.Abs(direction) < 0.01f && _stateMachine.CurrentState == _movingState)
            {
                _stateMachine.ChangeState(_idleState);
            }
        }

        /// <summary>
        /// Apply movement physics. Called by states.
        /// </summary>
        public void ApplyMovement(float direction)
        {
            Vector2 movement = new Vector2(direction * _moveSpeed, 0f);
            _rigidbody.linearVelocity = movement;

            // Clamp position to bounds
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, -_moveBounds, _moveBounds);
            transform.position = pos;

            // Raise movement event
            _onPlayerMoved?.Raise(pos.x);

            // Flip sprite based on direction
            if (Mathf.Abs(direction) > 0.01f)
            {
                Vector3 scale = transform.localScale;
                scale.x = direction > 0 ? 1f : -1f;
                transform.localScale = scale;
            }
        }

        /// <summary>
        /// Stop all movement.
        /// </summary>
        public void StopMovement()
        {
            _rigidbody.linearVelocity = Vector2.zero;
            _currentMoveDirection = 0f;
        }

        // === COMBAT ACTIONS ===

        /// <summary>
        /// Perform a parry action. Called by ParryCommand.
        /// </summary>
        public void PerformParry()
        {
            // Check cooldown
            if (Time.time < _lastParryTime + _parryCooldown)
            {
                Debug.Log($"[PlayerController] Parry on cooldown ({Time.time - _lastParryTime:F2}s / {_parryCooldown}s)");
                return;
            }

            // Only allow parry from idle or moving state
            if (_stateMachine.CurrentState == _idleState || _stateMachine.CurrentState == _movingState)
            {
                _lastParryTime = Time.time;
                _stateMachine.ChangeState(_parryingState);
                Debug.Log("[PlayerController] Parry initiated");
            }
            else
            {
                Debug.Log($"[PlayerController] Cannot parry from state: {_stateMachine.CurrentState.GetType().Name}");
            }
        }

        /// <summary>
        /// Perform a dodge action. Called by DodgeCommand.
        /// </summary>
        public void PerformDodge(float direction)
        {
            // Check cooldown
            if (Time.time < _lastDodgeTime + _dodgeCooldown)
            {
                Debug.Log($"[PlayerController] Dodge on cooldown ({Time.time - _lastDodgeTime:F2}s / {_dodgeCooldown}s)");
                return;
            }

            // Only allow dodge from idle or moving state
            if (_stateMachine.CurrentState == _idleState || _stateMachine.CurrentState == _movingState)
            {
                _lastDodgeTime = Time.time;
                _stateMachine.ChangeState(_dodgingState);
                Debug.Log($"[PlayerController] Dodge initiated (direction: {direction})");
            }
            else
            {
                Debug.Log($"[PlayerController] Cannot dodge from state: {_stateMachine.CurrentState.GetType().Name}");
            }
        }

        /// <summary>
        /// Perform an execution finisher. Called by ExecuteCommand.
        /// </summary>
        public void PerformExecution()
        {
            // TODO: Check if execution is available (all threads cut)
            // For now, allow from any state for testing
            _stateMachine.ChangeState(_executingState);
            Debug.Log("[PlayerController] Execution initiated");
        }

        // === PARRY SYSTEM ===

        /// <summary>
        /// Check if a parry was successful based on timing.
        /// </summary>
        public bool CheckParryTiming(float projectileArrivalTime)
        {
            float timeSinceParry = projectileArrivalTime - _lastParryTime;
            
            if (timeSinceParry <= _perfectParryWindow)
            {
                Debug.Log("[PlayerController] PERFECT PARRY!");
                _onParrySuccess?.Raise();
                return true;
            }
            else if (timeSinceParry <= _parryWindow)
            {
                Debug.Log("[PlayerController] Parry success");
                _onParrySuccess?.Raise();
                return true;
            }
            else
            {
                Debug.Log("[PlayerController] Parry failed (bad timing)");
                _onParryFailed?.Raise();
                return false;
            }
        }

        /// <summary>
        /// Called when a parry is successful.
        /// </summary>
        public void OnParrySuccess()
        {
            _onParrySuccess?.Raise();
        }

        /// <summary>
        /// Called when a parry fails.
        /// </summary>
        public void OnParryFailed()
        {
            _onParryFailed?.Raise();
        }

        // === INVINCIBILITY ===

        /// <summary>
        /// Set invincibility state (for dodge i-frames).
        /// </summary>
        public void SetInvincible(bool invincible)
        {
            _isInvincible = invincible;
            
            if (_showDebugGizmos)
            {
                Debug.Log($"[PlayerController] Invincibility: {invincible}");
            }
        }

        // === ANIMATION ===

        /// <summary>
        /// Play an animation by name.
        /// </summary>
        public void PlayAnimation(string animationName)
        {
            if (_animator != null)
            {
                _animator.Play(animationName);
            }
        }

        /// <summary>
        /// Play idle animation.
        /// </summary>
        public void PlayIdleAnimation()
        {
            PlayAnimation(_idleAnimation);
        }

        /// <summary>
        /// Play move animation.
        /// </summary>
        public void PlayMoveAnimation()
        {
            PlayAnimation(_moveAnimation);
        }

        /// <summary>
        /// Play parry animation.
        /// </summary>
        public void PlayParryAnimation()
        {
            PlayAnimation(_parryAnimation);
        }

        /// <summary>
        /// Play dodge animation.
        /// </summary>
        public void PlayDodgeAnimation()
        {
            PlayAnimation(_dodgeAnimation);
        }

        /// <summary>
        /// Play execution animation.
        /// </summary>
        public void PlayExecuteAnimation()
        {
            PlayAnimation(_executeAnimation);
        }

        // === DEBUG ===

        private void OnDrawGizmos()
        {
            if (!_showDebugGizmos) return;

            // Draw movement bounds
            Gizmos.color = Color.yellow;
            Vector3 leftBound = transform.position;
            leftBound.x = -_moveBounds;
            Vector3 rightBound = transform.position;
            rightBound.x = _moveBounds;

            Gizmos.DrawLine(leftBound + Vector3.up * 2f, leftBound - Vector3.up * 2f);
            Gizmos.DrawLine(rightBound + Vector3.up * 2f, rightBound - Vector3.up * 2f);

            // Draw invincibility indicator
            if (_isInvincible)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, 0.6f);
            }

            // Draw current state
            if (_stateMachine != null && _stateMachine.CurrentState != null)
            {
                string stateName = _stateMachine.CurrentState.GetType().Name;
                // Unity doesn't support text in Gizmos, but we can use handles in editor
#if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, stateName);
#endif
            }
        }

        [ContextMenu("Debug: Log Current State")]
        private void DebugLogCurrentState()
        {
            Debug.Log($"=== Player Controller Debug ===");
            Debug.Log($"Current State: {_stateMachine.CurrentState?.GetType().Name}");
            Debug.Log($"Position: {transform.position}");
            Debug.Log($"Move Direction: {_currentMoveDirection}");
            Debug.Log($"Invincible: {_isInvincible}");
            Debug.Log($"Last Parry: {_lastParryTime}");
            Debug.Log($"Last Dodge: {_lastDodgeTime}");
        }
    }