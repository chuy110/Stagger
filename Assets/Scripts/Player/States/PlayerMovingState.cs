using UnityEngine;
using Stagger.Core.States;

/// <summary>
/// Player moving state - lateral movement left/right.
/// </summary>
public class PlayerMovingState : IState
{
    private PlayerController _player;

    public PlayerMovingState(PlayerController player)
    {
        _player = player;
    }

    public void Enter()
    {
        Debug.Log("[PlayerMovingState] Entered");
        
        // Play move animation
        _player.PlayMoveAnimation();
    }

    public void Update()
    {
        // Movement handled by FixedUpdate for physics
    }

    public void FixedUpdate()
    {
        // Apply movement based on current direction
        _player.ApplyMovement(_player.CurrentMoveDirection);
        
        // If direction becomes zero, transition back to idle
        // (This is also handled in PlayerController.Move(), but double-check here)
        if (Mathf.Abs(_player.CurrentMoveDirection) < 0.01f)
        {
            _player.StateMachine.ChangeState(_player.IdleState);
        }
    }

    public void Exit()
    {
        Debug.Log("[PlayerMovingState] Exited");
    }
}