using UnityEngine;
using Stagger.Core.States;

    /// Player executing state - performing final execution move on boss.
    /// Locks player in place during cinematic finisher.
    /// </summary>
    public class PlayerExecutingState : IState
    {
        private PlayerController _player;
        private float _executionStartTime;
        private float _executionDuration = 3f; // Duration of execution animation
        private bool _executionTriggered;

        public PlayerExecutingState(PlayerController player)
        {
            _player = player;
        }

        public void Enter()
        {
            Debug.Log("[PlayerExecutingState] Entered - EXECUTION!");
            
            _executionStartTime = Time.time;
            _executionTriggered = false;
            
            // Stop all movement
            _player.StopMovement();
            
            // Play execution animation
            _player.PlayExecuteAnimation();
            
            // TODO: Trigger slow-motion effect
            // TODO: Trigger camera zoom/shake
            // TODO: Trigger execution VFX
        }

        public void Update()
        {
            float elapsedTime = Time.time - _executionStartTime;
            
            // Trigger execution hit at midpoint of animation
            if (!_executionTriggered && elapsedTime >= _executionDuration * 0.5f)
            {
                _executionTriggered = true;
                TriggerExecutionHit();
            }
            
            // Check if execution animation is complete
            if (elapsedTime >= _executionDuration)
            {
                Debug.Log("[PlayerExecutingState] Execution complete");
                
                // TODO: Trigger boss defeat
                // TODO: Transition to victory/reward screen
                
                // For now, return to idle
                _player.StateMachine.ChangeState(_player.IdleState);
            }
        }

        public void FixedUpdate()
        {
            // Keep player locked in position during execution
            _player.StopMovement();
        }

        public void Exit()
        {
            Debug.Log("[PlayerExecutingState] Exited");
            
            // TODO: Reset time scale if slow-mo was active
            // TODO: Reset camera
        }

        /// <summary>
        /// Trigger the actual execution damage/effect.
        /// </summary>
        private void TriggerExecutionHit()
        {
            Debug.Log("[PlayerExecutingState] EXECUTION HIT!");
            
            // TODO: Deal massive damage to boss
            // TODO: Play execution VFX
            // TODO: Play execution SFX
            // TODO: Trigger screen shake
            // TODO: Raise execution event
        }
    }