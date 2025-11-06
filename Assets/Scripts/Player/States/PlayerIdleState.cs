using UnityEngine;
using Stagger.Core.States;

/// <summary>
/// Player idle state - waiting for input.
/// </summary>
public class PlayerIdleState : IState
{
    private PlayerController _player;

    public PlayerIdleState(PlayerController player)
    {
        _player = player;
    }

    public void Enter()
    {
        Debug.Log("[PlayerIdleState] Entered");
        
        // Stop all movement
        _player.StopMovement();
        
        // Play idle animation
        _player.PlayIdleAnimation();
    }

    public void Update()
    {
        // Idle state just waits for commands
        // State transitions handled by PlayerController based on commands
    }

    public void FixedUpdate()
    {
        // Ensure player stays stopped
        _player.StopMovement();
    }

    public void Exit()
    {
        Debug.Log("[PlayerIdleState] Exited");
    }
}