using UnityEngine;

namespace Stagger.Boss
{
    /// <summary>
    /// State interface for State pattern.
    /// </summary>
    public interface IState
    {
        void Enter();
        void Update();
        void FixedUpdate();
        void Exit();
    }
    
    /// <summary>
    /// Simple state machine implementation (State pattern).
    /// </summary>
    public class StateMachine
    {
        private IState _currentState;

        public IState CurrentState => _currentState;

        public void Initialize(IState startingState)
        {
            _currentState = startingState;
            _currentState?.Enter();
        }

        public void ChangeState(IState newState)
        {
            if (newState == _currentState) return;

            _currentState?.Exit();
            _currentState = newState;
            _currentState?.Enter();
        }

        public void Update()
        {
            _currentState?.Update();
        }

        public void FixedUpdate()
        {
            _currentState?.FixedUpdate();
        }
    }
    
    /// <summary>
    /// Boss idle state - waiting between attacks.
    /// </summary>
    public class BossIdleState : IState
    {
        private BossController _boss;
        private float _idleTime;

        public BossIdleState(BossController boss)
        {
            _boss = boss;
        }

        public void Enter()
        {
            Debug.Log("[BossIdleState] Entered");
            _idleTime = Random.Range(0.5f, 1.5f);
        }

        public void Update()
        {
            // Don't transition if boss is dead
            if (_boss.Health.IsDead)
            {
                return;
            }
            
            // Don't attack if all threads are broken (waiting for execution)
            if (_boss.ThreadSystem.AllThreadsBroken)
            {
                return;
            }
    
            _idleTime -= Time.deltaTime;
    
            if (_idleTime <= 0f)
            {
                // Transition to attacking
                _boss.StateMachine.ChangeState(_boss.AttackingState);
            }
        }

        public void FixedUpdate() { }

        public void Exit()
        {
            Debug.Log("[BossIdleState] Exited");
        }
    }
    
    /// <summary>
    /// Boss attacking state - firing projectiles.
    /// </summary>
    public class BossAttackingState : IState
    {
        private BossController _boss;
        private AttackPattern _currentAttack;
        private bool _attackExecuted;

        public BossAttackingState(BossController boss)
        {
            _boss = boss;
        }

        public void Enter()
        {
            Debug.Log("[BossAttackingState] Entered");
            
            // Select attack pattern
            _currentAttack = _boss.SelectRandomAttack();
            _attackExecuted = false;
            
            // Play attack animation if configured
            if (_currentAttack != null && !string.IsNullOrEmpty(_currentAttack.AttackAnimation))
            {
                // TODO: Play animation
            }
        }

        public void Update()
        {
            // Don't attack if boss is dead
            if (_boss.Health.IsDead)
            {
                return;
            }
            
            if (!_attackExecuted && _currentAttack != null)
            {
                // Fire projectile
                _boss.FireProjectile(_currentAttack);
                _attackExecuted = true;
            }
            
            // Return to idle after attack
            if (_attackExecuted)
            {
                _boss.StateMachine.ChangeState(_boss.IdleState);
            }
        }

        public void FixedUpdate() { }

        public void Exit()
        {
            Debug.Log("[BossAttackingState] Exited");
        }
    }
    
    /// <summary>
    /// Boss stunned state - brief pause after taking damage.
    /// </summary>
    public class BossStunnedState : IState
    {
        private BossController _boss;
        private float _stunnedTime;
        private float _stunDuration;

        public BossStunnedState(BossController boss)
        {
            _boss = boss;
            _stunDuration = boss.Data.StunDuration;
        }

        public void Enter()
        {
            Debug.Log("[BossStunnedState] Entered - Boss stunned!");
            _stunnedTime = 0f;
            
            // Visual feedback
            // TODO: Play stun animation or effect
        }

        public void Update()
        {
            // Don't transition if boss is dead
            if (_boss.Health.IsDead)
            {
                return;
            }
            
            _stunnedTime += Time.deltaTime;
            
            if (_stunnedTime >= _stunDuration)
            {
                // Return to idle
                _boss.StateMachine.ChangeState(_boss.IdleState);
            }
        }

        public void FixedUpdate() { }

        public void Exit()
        {
            Debug.Log("[BossStunnedState] Exited");
        }
    }
    
    /// <summary>
    /// Boss thread break state - waiting for player QTE.
    /// </summary>
    public class BossThreadBreakState : IState
    {
        private BossController _boss;

        public BossThreadBreakState(BossController boss)
        {
            _boss = boss;
        }

        public void Enter()
        {
            Debug.Log("[BossThreadBreakState] Entered - Thread Break QTE!");
            
            // Boss is invulnerable during QTE (set by BossHealth.CheckThresholds)
            // No need to set it again here
            
            // TODO: Play thread break animation
            // TODO: Slow-motion effect?
        }

        public void Update()
        {
            // Wait for QTE to complete
            if (!_boss.ThreadSystem.IsQTEActive)
            {
                // QTE finished (success or fail)
                _boss.StateMachine.ChangeState(_boss.IdleState);
            }
        }

        public void FixedUpdate() { }

        public void Exit()
        {
            Debug.Log("[BossThreadBreakState] Exited");
            
            // Check if all threads are broken
            if (_boss.ThreadSystem.AllThreadsBroken)
            {
                _boss.OnAllThreadsBroken();
            }
        }
    }
    
    /// <summary>
    /// Boss execution state - being executed by player.
    /// </summary>
    public class BossExecutionState : IState
    {
        private BossController _boss;
        private float _executionTime;
        private float _executionDuration = 3f;
        private bool _killExecuted = false;

        public BossExecutionState(BossController boss)
        {
            _boss = boss;
        }

        public void Enter()
        {
            Debug.Log("[BossExecutionState] Entered - EXECUTION!");
            _executionTime = 0f;
            _killExecuted = false;
            
            // TODO: Play execution animation
            // TODO: Slow-motion effect
            // TODO: Camera effects
        }

        public void Update()
        {
            _executionTime += Time.deltaTime;
            
            // Kill boss at midpoint of execution
            if (_executionTime >= _executionDuration * 0.5f && !_killExecuted)
            {
                Debug.Log("[BossExecutionState] Executing kill...");
                
                // Use special execution kill method that bypasses invulnerability
                if (_boss.Health != null)
                {
                    _boss.Health.ExecutionKill();
                }
                
                _killExecuted = true;
            }
            
            // Execution should automatically transition to death state
            // when Health.Die() is called and OnBossDefeated event fires
        }

        public void FixedUpdate() { }

        public void Exit()
        {
            Debug.Log("[BossExecutionState] Exited");
        }
    }
    
    /// <summary>
    /// Boss death state - boss is defeated and inactive.
    /// </summary>
    public class BossDeathState : IState
    {
        private BossController _boss;
        private float _deathTime;
        private bool _victoryHandled;

        public BossDeathState(BossController boss)
        {
            _boss = boss;
        }

        public void Enter()
        {
            Debug.Log("[BossDeathState] Entered - Boss is defeated!");
            _deathTime = 0f;
            _victoryHandled = false;
            
            // Stop all ongoing coroutines safely
            try
            {
                _boss.StopAllCoroutines();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BossDeathState] Error stopping coroutines: {e.Message}");
            }
            
            // Disable boss AI but keep GameObject active for death animation
            // Don't disable the entire GameObject yet!
            
            // TODO: Play death animation
        }

        public void Update()
        {
            _deathTime += Time.deltaTime;
            
            // Wait for death animation to complete (2 seconds)
            if (_deathTime >= 2f && !_victoryHandled)
            {
                _victoryHandled = true;
                
                Debug.Log("[BossDeathState] Death sequence complete");
                
                // Optionally hide boss after a delay
                // You might want to keep it visible for the player to see
                // _boss.gameObject.SetActive(false);
            }
        }

        public void FixedUpdate() { }

        public void Exit()
        {
            Debug.Log("[BossDeathState] Exited");
        }
    }
}