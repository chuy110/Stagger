using UnityEngine;
using Stagger.Core.States;
    /// Player dodging state - quick dash with invincibility frames.
    /// </summary>
    public class PlayerDodgingState : IState
    {
        private PlayerController _player;
        private float _dodgeStartTime;
        private float _dodgeDuration;
        private float _dodgeDirection;
        private Vector3 _dodgeStartPosition;
        private float _dodgeDistance = 2f; // How far to dodge

        public PlayerDodgingState(PlayerController player)
        {
            _player = player;
            _dodgeDuration = player.DodgeDuration;
        }

        public void Enter()
        {
            Debug.Log("[PlayerDodgingState] Entered - INVINCIBLE");
            
            // Record dodge start
            _dodgeStartTime = Time.time;
            _dodgeStartPosition = _player.transform.position;
            
            // Use current movement direction, or default to right if no input
            _dodgeDirection = _player.CurrentMoveDirection;
            if (Mathf.Abs(_dodgeDirection) < 0.01f)
            {
                _dodgeDirection = 1f; // Default right
            }
            
            // Activate invincibility frames
            _player.SetInvincible(true);
            
            // Play dodge animation
            _player.PlayDodgeAnimation();
        }

        public void Update()
        {
            float elapsedTime = Time.time - _dodgeStartTime;
            
            // Check if invincibility should end
            if (elapsedTime >= _player.InvincibilityFrames && _player.IsInvincible)
            {
                _player.SetInvincible(false);
                Debug.Log("[PlayerDodgingState] I-frames ended");
            }
            
            // Check if dodge is complete
            if (elapsedTime >= _dodgeDuration)
            {
                // End dodge
                Debug.Log("[PlayerDodgingState] Dodge complete");
                _player.StateMachine.ChangeState(_player.IdleState);
            }
        }

        public void FixedUpdate()
        {
            // Calculate dodge progress (0 to 1)
            float elapsedTime = Time.time - _dodgeStartTime;
            float progress = Mathf.Clamp01(elapsedTime / _dodgeDuration);
            
            // Use an ease-out curve for smooth dodge movement
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            
            // Calculate target position
            float targetX = _dodgeStartPosition.x + (_dodgeDirection * _dodgeDistance * easedProgress);
            
            // Clamp to movement bounds
            targetX = Mathf.Clamp(targetX, -_player.MoveBounds, _player.MoveBounds);
            
            // Apply movement
            Vector3 newPosition = _player.transform.position;
            newPosition.x = Mathf.Lerp(_dodgeStartPosition.x, targetX, easedProgress);
            _player.transform.position = newPosition;
        }

        public void Exit()
        {
            Debug.Log("[PlayerDodgingState] Exited");
            
            // Ensure invincibility is removed
            _player.SetInvincible(false);
            
            // Stop all momentum
            _player.StopMovement();
        }
    }