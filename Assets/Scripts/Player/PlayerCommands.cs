using UnityEngine;
using Stagger.Core.Commands;

namespace Stagger.Player
{
    // Forward declaration - full implementation will be created later
    public class PlayerController : MonoBehaviour
    {
        public virtual void Move(float direction) { }
        public virtual void PerformParry() { }
        public virtual void PerformDodge(float direction) { }
        public virtual void PerformExecution() { }
    }

    /// <summary>
    /// Command to move the player laterally.
    /// </summary>
    public class MoveCommand : Command
    {
        private PlayerController _player;
        private float _direction;
        private Vector3 _previousPosition;

        public MoveCommand(PlayerController player, float direction) : base()
        {
            _player = player;
            _direction = direction;
        }

        public override void Execute()
        {
            if (_player == null)
            {
                Debug.LogWarning("[MoveCommand] Player is null");
                return;
            }

            _previousPosition = _player.transform.position;
            _player.Move(_direction);
        }

        public override void Undo()
        {
            if (_player != null)
            {
                _player.transform.position = _previousPosition;
            }
        }
    }

    /// <summary>
    /// Command to perform a parry action.
    /// </summary>
    public class ParryCommand : Command
    {
        private PlayerController _player;

        public ParryCommand(PlayerController player) : base()
        {
            _player = player;
        }

        public override void Execute()
        {
            if (_player == null)
            {
                Debug.LogWarning("[ParryCommand] Player is null");
                return;
            }

            _player.PerformParry();
        }

        public override void Undo()
        {
            // Parry cannot be undone
        }
    }

    /// <summary>
    /// Command to perform a dodge action.
    /// </summary>
    public class DodgeCommand : Command
    {
        private PlayerController _player;
        private float _direction;

        public DodgeCommand(PlayerController player, float direction) : base()
        {
            _player = player;
            _direction = direction;
        }

        public override void Execute()
        {
            if (_player == null)
            {
                Debug.LogWarning("[DodgeCommand] Player is null");
                return;
            }

            _player.PerformDodge(_direction);
        }

        public override void Undo()
        {
            // Dodge cannot be undone
        }
    }

    /// <summary>
    /// Command to perform an execution finisher.
    /// </summary>
    public class ExecuteCommand : Command
    {
        private PlayerController _player;

        public ExecuteCommand(PlayerController player) : base()
        {
            _player = player;
        }

        public override void Execute()
        {
            if (_player == null)
            {
                Debug.LogWarning("[ExecuteCommand] Player is null");
                return;
            }

            _player.PerformExecution();
        }

        public override void Undo()
        {
            // Execution cannot be undone
        }
    }
}