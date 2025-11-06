using UnityEngine;
using Stagger.Core.States;

/// <summary>
/// Player parrying state - active parry window with timing detection.
/// </summary>
public class PlayerParryingState : IState
    {
        private PlayerController _player;
        private float _parryStartTime;
        private float _parryDuration;
        private bool _parryActive;

        public PlayerParryingState(PlayerController player)
        {
            _player = player;
            _parryDuration = player.ParryWindow;
        }

        public void Enter()
        {
            Debug.Log("[PlayerParryingState] Entered - Parry window OPEN");
            
            // Record when parry started
            _parryStartTime = Time.time;
            _parryActive = true;
            
            // Stop movement during parry
            _player.StopMovement();
            
            // Play parry animation
            _player.PlayParryAnimation();
        }

        public void Update()
        {
            // Check if parry window has expired
            float timeSinceParry = Time.time - _parryStartTime;
            
            if (timeSinceParry >= _parryDuration)
            {
                // Parry window closed
                _parryActive = false;
                Debug.Log("[PlayerParryingState] Parry window CLOSED");
                
                // Return to idle state
                _player.StateMachine.ChangeState(_player.IdleState);
            }
            else
            {
                // Show debug timing
                if (timeSinceParry <= _player.PerfectParryWindow)
                {
                    // In perfect parry window
                    Debug.Log($"[PlayerParryingState] Perfect window: {timeSinceParry:F3}s / {_player.PerfectParryWindow:F3}s");
                }
                else
                {
                    // In normal parry window
                    Debug.Log($"[PlayerParryingState] Normal window: {timeSinceParry:F3}s / {_parryDuration:F3}s");
                }
            }
        }

        public void FixedUpdate()
        {
            // Keep player stationary during parry
            _player.StopMovement();
        }

        public void Exit()
        {
            Debug.Log("[PlayerParryingState] Exited");
            _parryActive = false;
        }

        /// <summary>
        /// Check if parry is currently active.
        /// </summary>
        public bool IsParryActive()
        {
            return _parryActive && (Time.time - _parryStartTime) <= _parryDuration;
        }

        /// <summary>
        /// Get the current timing quality (for perfect parry detection).
        /// </summary>
        public float GetTimingQuality()
        {
            float timeSinceParry = Time.time - _parryStartTime;
            
            if (timeSinceParry <= _player.PerfectParryWindow)
            {
                return 1.0f; // Perfect
            }
            else if (timeSinceParry <= _parryDuration)
            {
                // Normalized quality from perfect to normal window
                return 1.0f - (timeSinceParry - _player.PerfectParryWindow) / (_parryDuration - _player.PerfectParryWindow);
            }
            else
            {
                return 0f; // Missed
            }
        }
    }