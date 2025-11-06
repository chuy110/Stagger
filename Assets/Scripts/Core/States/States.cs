using UnityEngine;

namespace Stagger.Core.States
{
    /// <summary>
    /// Interface for all state classes in the State pattern.
    /// States control behavior and can transition to other states.
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Called once when entering this state.
        /// </summary>
        void Enter();

        /// <summary>
        /// Called every frame while in this state.
        /// </summary>
        void Update();

        /// <summary>
        /// Called every fixed frame while in this state.
        /// </summary>
        void FixedUpdate();

        /// <summary>
        /// Called once when exiting this state.
        /// </summary>
        void Exit();
    }

    /// <summary>
    /// Generic State Machine implementation using the State pattern.
    /// Manages state transitions and updates the current state.
    /// </summary>
    public class StateMachine
    {
        private IState _currentState;
        private IState _previousState;

        public IState CurrentState => _currentState;
        public IState PreviousState => _previousState;

        /// <summary>
        /// Initialize the state machine with a starting state.
        /// </summary>
        public void Initialize(IState startingState)
        {
            _currentState = startingState;
            _currentState?.Enter();
        }

        /// <summary>
        /// Transition to a new state, calling Exit on current and Enter on new.
        /// </summary>
        public void ChangeState(IState newState)
        {
            if (_currentState == newState)
            {
                Debug.LogWarning($"[StateMachine] Attempted to change to same state: {newState?.GetType().Name}");
                return;
            }

            _currentState?.Exit();
            _previousState = _currentState;
            _currentState = newState;
            _currentState?.Enter();

            Debug.Log($"[StateMachine] State changed: {_previousState?.GetType().Name} → {_currentState?.GetType().Name}");
        }

        /// <summary>
        /// Return to the previous state.
        /// </summary>
        public void RevertToPreviousState()
        {
            if (_previousState != null)
            {
                ChangeState(_previousState);
            }
            else
            {
                Debug.LogWarning("[StateMachine] No previous state to revert to.");
            }
        }

        /// <summary>
        /// Update the current state. Call this in MonoBehaviour Update().
        /// </summary>
        public void Update()
        {
            _currentState?.Update();
        }

        /// <summary>
        /// Fixed update the current state. Call this in MonoBehaviour FixedUpdate().
        /// </summary>
        public void FixedUpdate()
        {
            _currentState?.FixedUpdate();
        }
    }
}