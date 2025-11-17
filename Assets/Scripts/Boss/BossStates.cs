using UnityEngine;

namespace Stagger.Boss
{
    // State interface for State pattern
    public interface IState
    {
        void Enter();
        void Update();
        void FixedUpdate();
        void Exit();
    }
    
    // Simple state machine implementation (State pattern)
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
    
    // Boss idle state, waiting between attacks
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
    
    // Boss attacking state: firing projectiles
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
    
    // Boss stunned state: brief pause after taking damage
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
    
    // Boss thread break state: waiting for player QTE
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
            
            // Boss is invulnerable during QTE
            _boss.Health.SetInvulnerable(true);
            
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
            
            // Remove invulnerability
            _boss.Health.SetInvulnerable(false);
            
            // Check if all threads are broken
            if (_boss.ThreadSystem.AllThreadsBroken)
            {
                _boss.OnAllThreadsBroken();
            }
        }
    }
    
    // Boss execution state: being executed by player
    public class BossExecutionState : IState
    {
        private BossController _boss;
        private float _executionTime;
        private float _executionDuration = 3f;
        private bool _damageDealt = false;

        public BossExecutionState(BossController boss)
        {
            _boss = boss;
        }

        public void Enter()
        {
            Debug.Log("[BossExecutionState] Entered - EXECUTION!");
            _executionTime = 0f;
            _damageDealt = false;
            
            // Boss is completely frozen
            _boss.Health.SetInvulnerable(true);
            
            // TODO: Play execution animation
            // TODO: Slow-motion effect
            // TODO: Camera effects
        }

        public void Update()
        {
            _executionTime += Time.deltaTime;
            
            // Deal massive damage at midpoint
            if (_executionTime >= _executionDuration * 0.5f && !_damageDealt)
            {
                _boss.Health.SetInvulnerable(false);
                _boss.Health.TakeDamage(_boss.Health.MaxHealth); // Instant kill
                _damageDealt = true;
            }
            
            // Stay in this state until boss is dead
            if (_executionTime >= _executionDuration)
            {
                // Boss should be dead now
                Debug.Log("[BossExecutionState] Execution complete");
            }
        }

        public void FixedUpdate() { }

        public void Exit()
        {
            Debug.Log("[BossExecutionState] Exited");
        }
    }
    
    // Boss death state: boss is defeated and inactive
    public class BossDeathState : IState
    {
        private BossController _boss;
        private float _deathTime;
        private bool _deathHandled;

        public BossDeathState(BossController boss)
        {
            _boss = boss;
        }

        public void Enter()
        {
            Debug.Log("[BossDeathState] Entered - Boss is defeated!");
            _deathTime = 0f;
            _deathHandled = false;
        
            // Play death animation
            // TODO: Trigger death animation
        
            // Stop all projectile spawning
            // Boss should no longer attack
        }

        public void Update()
        {
            _deathTime += Time.deltaTime;
        
            // After death animation completes (e.g., 2 seconds)
            if (_deathTime >= 2f && !_deathHandled)
            {
                _deathHandled = true;
            
                // Handle death consequences
                // - Drop artifacts
                // - Show victory screen
                // - Disable boss GameObject
            
                Debug.Log("[BossDeathState] Death sequence complete");
            
                // Optionally disable the boss
                _boss.gameObject.SetActive(false);
            }
        }

        public void FixedUpdate() { }

        public void Exit()
        {
            Debug.Log("[BossDeathState] Exited");
        }
    }
}