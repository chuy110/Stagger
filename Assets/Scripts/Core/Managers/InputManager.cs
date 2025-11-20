using UnityEngine;
using UnityEngine.InputSystem;
using Stagger.Core.Commands;
using Stagger.Core.Managers;  // ← ADD THIS for GameManager
using Stagger.UI;              // ← ADD THIS for UIManager
using Stagger.Boss;            // ← ADD THIS for BossData

namespace Stagger.Core.Managers
{
    /// <summary>
    /// Input Manager singleton using new Input System.
    /// Converts input to Command objects for execution via Command pattern.
    /// Provides input buffering for frame-perfect timing.
    /// </summary>
    public class InputManager : Singleton<InputManager>
    {
        [Header("Input Settings")]
        [SerializeField] private float _inputBufferTime = 0.2f; // Time to buffer inputs
        [SerializeField] private bool _enableInputBuffering = true;
        [SerializeField] private KeyCode _pauseKey = KeyCode.Escape;

        [Header("Debug")]
        [SerializeField] private bool _logInputs = false;

        private PlayerInputActions _playerInput;
        private CommandInvoker _commandInvoker;
        private PlayerController _playerController;

        private bool _inputEnabled = true;

        public bool InputEnabled
        {
            get => _inputEnabled;
            set
            {
                _inputEnabled = value;
                if (!value)
                {
                    _commandInvoker?.ClearBuffer();
                }
            }
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            _playerInput = new PlayerInputActions();
            _commandInvoker = new CommandInvoker(
                maxBufferSize: 10,
                maxHistorySize: 100
            );

            BindInputActions();
        }

        private void OnEnable()
        {
            _playerInput?.Enable();
        }

        private void OnDisable()
        {
            _playerInput?.Disable();
        }

        private void Update()
        {
            if (_inputEnabled && _enableInputBuffering)
            {
                // Execute buffered commands each frame
                HandlePauseInput();
                _commandInvoker.ExecuteAll();
            }
        }

        /// <summary>
        /// Set the player controller that will receive commands.
        /// </summary>
        public void SetPlayerController(PlayerController player)
        {
            _playerController = player;
            Debug.Log($"[InputManager] Player controller set: {player.name}");
        }

        /// <summary>
        /// Bind input actions to command creation.
        /// </summary>
        private void BindInputActions()
        {
            // Movement
            _playerInput.Player.Move.performed += OnMovePerformed;
            _playerInput.Player.Move.canceled += OnMoveCanceled;

            // Parry
            _playerInput.Player.Parry.performed += OnParryPerformed;

            // Dodge
            _playerInput.Player.Dodge.performed += OnDodgePerformed;

            // Execute
            _playerInput.Player.Execute.performed += OnExecutePerformed;

            // Pause - removed binding, handled in Update()
        }

        // Input Callbacks

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            if (!_inputEnabled || _playerController == null)
                return;

            // Read as Vector2 for axis composite, or float for single axis
            float direction = 0f;
            try
            {
                direction = context.ReadValue<float>();
            }
            catch
            {
                // If reading as float fails, try Vector2 (for 2D vector composites)
                Vector2 vec = context.ReadValue<Vector2>();
                direction = vec.x;
            }

            if (_logInputs)
                Debug.Log($"[InputManager] Move: {direction}");

            ICommand moveCommand = new MoveCommand(_playerController, direction);

            if (_enableInputBuffering)
                _commandInvoker.AddCommand(moveCommand);
            else
                moveCommand.Execute();
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            if (!_inputEnabled || _playerController == null)
                return;

            if (_logInputs)
                Debug.Log("[InputManager] Move Canceled");

            ICommand stopCommand = new MoveCommand(_playerController, 0f);

            if (_enableInputBuffering)
                _commandInvoker.AddCommand(stopCommand);
            else
                stopCommand.Execute();
        }

        private void OnParryPerformed(InputAction.CallbackContext context)
        {
            if (!_inputEnabled || _playerController == null)
                return;

            if (_logInputs)
                Debug.Log("[InputManager] Parry");

            ICommand parryCommand = new ParryCommand(_playerController);

            if (_enableInputBuffering)
                _commandInvoker.AddCommand(parryCommand);
            else
                parryCommand.Execute();
        }

        private void OnDodgePerformed(InputAction.CallbackContext context)
        {
            if (!_inputEnabled || _playerController == null)
                return;

            // Get current movement direction
            float direction = 0f;
            try
            {
                direction = _playerInput.Player.Move.ReadValue<float>();
            }
            catch
            {
                // If reading as float fails, try Vector2
                Vector2 vec = _playerInput.Player.Move.ReadValue<Vector2>();
                direction = vec.x;
            }

            // If no movement input, default to facing direction (right = 1)
            if (Mathf.Abs(direction) < 0.01f)
            {
                direction = 1f;
            }

            if (_logInputs)
                Debug.Log($"[InputManager] Dodge (direction: {direction})");

            ICommand dodgeCommand = new DodgeCommand(_playerController, direction);

            if (_enableInputBuffering)
                _commandInvoker.AddCommand(dodgeCommand);
            else
                dodgeCommand.Execute();
        }

        private void OnExecutePerformed(InputAction.CallbackContext context)
        {
            if (!_inputEnabled || _playerController == null)
                return;

            if (_logInputs)
                Debug.Log("[InputManager] Execute");

            ICommand executeCommand = new ExecuteCommand(_playerController);

            if (_enableInputBuffering)
                _commandInvoker.AddCommand(executeCommand);
            else
                executeCommand.Execute();
        }

        private void HandlePauseInput()
        {
            if (!Input.GetKeyDown(_pauseKey)) return;

            // Safety checks
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[InputManager] GameManager not found!");
                return;
            }

            if (UIManager.Instance == null)
            {
                Debug.LogWarning("[InputManager] UIManager not found!");
                return;
            }

            // Get current game state
            GameManager.GameState currentState = GameManager.Instance.CurrentState;

            switch (currentState)
            {
                case GameManager.GameState.Combat:
                    // Pause the game
                    PauseGame();
                    break;

                case GameManager.GameState.Paused:
                    // Resume the game
                    ResumeGame();
                    break;

                default:
                    // Don't allow pause in other states (menu, results, equipment)
                    Debug.Log($"[InputManager] Cannot pause in state: {currentState}");
                    break;
            }
        }
        
        private void PauseGame()
        {
            Debug.Log("[InputManager] Pausing game");
            UIManager.Instance.ShowPauseScreen();
        }
        
        private void ResumeGame()
        {
            Debug.Log("[InputManager] Resuming game");
            
            // Get current boss name to pass to HUD
            string bossName = GetCurrentBossName();
            
            // Show HUD (will also unpause via UIManager)
            UIManager.Instance.ShowHUD(bossName);
        }

        /// <summary>
        /// Get the current boss name for HUD display.
        /// </summary>
        private string GetCurrentBossName()
        {
            // Try to get from GameManager
            if (GameManager.Instance != null)
            {
                BossData currentBoss = GameManager.Instance.GetCurrentBossData();
                if (currentBoss != null)
                {
                    return currentBoss.BossName;
                }
            }

            // Fallback
            return "Boss";
        }

        // Public API

        /// <summary>
        /// Enable or disable specific input actions.
        /// </summary>
        public void SetInputActionEnabled(string actionName, bool enabled)
        {
            switch (actionName.ToLower())
            {
                case "move":
                    if (enabled) _playerInput.Player.Move.Enable();
                    else _playerInput.Player.Move.Disable();
                    break;
                case "parry":
                    if (enabled) _playerInput.Player.Parry.Enable();
                    else _playerInput.Player.Parry.Disable();
                    break;
                case "dodge":
                    if (enabled) _playerInput.Player.Dodge.Enable();
                    else _playerInput.Player.Dodge.Disable();
                    break;
                case "execute":
                    if (enabled) _playerInput.Player.Execute.Enable();
                    else _playerInput.Player.Execute.Disable();
                    break;
            }
        }

        /// <summary>
        /// Clear all buffered commands.
        /// </summary>
        public void ClearBuffer()
        {
            _commandInvoker.ClearBuffer();
        }

        /// <summary>
        /// Get current movement input value.
        /// </summary>
        public float GetMovementInput()
        {
            try
            {
                return _playerInput.Player.Move.ReadValue<float>();
            }
            catch
            {
                Vector2 vec = _playerInput.Player.Move.ReadValue<Vector2>();
                return vec.x;
            }
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            if (_playerInput != null)
            {
                _playerInput.Player.Move.performed -= OnMovePerformed;
                _playerInput.Player.Move.canceled -= OnMoveCanceled;
                _playerInput.Player.Parry.performed -= OnParryPerformed;
                _playerInput.Player.Dodge.performed -= OnDodgePerformed;
                _playerInput.Player.Execute.performed -= OnExecutePerformed;

                _playerInput.Dispose();
            }
        }
    }

    // Note: PlayerInputActions class will be auto-generated by Unity's Input System
    // You need to create an InputActions asset in Unity to generate this class
}