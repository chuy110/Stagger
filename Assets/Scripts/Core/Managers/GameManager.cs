using UnityEngine;
using UnityEngine.SceneManagement;
using Stagger.Core.Events;

namespace Stagger.Core.Managers
{
    /// <summary>
    /// Main Game Manager singleton controlling game flow and state.
    /// Manages scene transitions, boss progression, and overall game state.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [Header("Game State")]
        [SerializeField] private GameState _currentState = GameState.Menu;

        [Header("Boss Progression")]
        [SerializeField] private BossData[] _bossProgression;
        [SerializeField] private int _currentBossIndex = 0;

        [Header("Game Events")]
        [SerializeField] private GameEvent _onGameStart;
        [SerializeField] private GameEvent _onGameOver;
        [SerializeField] private GameEvent _onBossDefeated;
        [SerializeField] private GameEvent _onAllBossesDefeated;
        [SerializeField] private GameEvent _onReturnToMenu;

        [Header("Scene Names")]
        [SerializeField] private string _menuSceneName = "StartScreen";
        [SerializeField] private string _arenaSceneName = "Arena01";
        [SerializeField] private string _artifactSceneName = "ArtifactSelection";

        [Header("Stats")]
        [SerializeField] private int _totalScore = 0;
        [SerializeField] private int _bossesDefeated = 0;
        [SerializeField] private float _totalTimePlayed = 0f;

        public enum GameState
        {
            Menu,
            Combat,
            ArtifactSelection,
            GameOver,
            Paused
        }

        public GameState CurrentState => _currentState;
        public int CurrentBossIndex => _currentBossIndex;
        public int BossesDefeated => _bossesDefeated;
        public int TotalScore => _totalScore;
        public float TotalTimePlayed => _totalTimePlayed;
        public bool HasMoreBosses => _currentBossIndex < _bossProgression.Length;

        private bool _isGameActive;

        protected override void OnAwake()
        {
            base.OnAwake();
            _isGameActive = false;
        }

        private void Update()
        {
            if (_isGameActive)
            {
                _totalTimePlayed += Time.deltaTime;
            }
        }

        /// <summary>
        /// Start a new game from the beginning.
        /// </summary>
        public void StartNewGame()
        {
            Debug.Log("[GameManager] Starting new game");

            _currentBossIndex = 0;
            _bossesDefeated = 0;
            _totalScore = 0;
            _totalTimePlayed = 0f;
            _isGameActive = true;

            TransitionToState(GameState.Combat);
            _onGameStart?.Raise();

            LoadArenaScene();
        }

        /// <summary>
        /// Continue game after artifact selection.
        /// </summary>
        public void ContinueGame()
        {
            Debug.Log("[GameManager] Continuing game");

            if (HasMoreBosses)
            {
                TransitionToState(GameState.Combat);
                LoadArenaScene();
            }
            else
            {
                GameComplete();
            }
        }

        /// <summary>
        /// Called when a boss is defeated.
        /// </summary>
        public void OnBossDefeated(int scoreEarned)
        {
            Debug.Log($"[GameManager] Boss defeated! Score: {scoreEarned}");

            _bossesDefeated++;
            _totalScore += scoreEarned;
            _currentBossIndex++;

            _onBossDefeated?.Raise();

            if (HasMoreBosses)
            {
                TransitionToState(GameState.ArtifactSelection);
                LoadArtifactSelectionScene();
            }
            else
            {
                GameComplete();
            }
        }

        /// <summary>
        /// Called when the player fails (if HP mode is used).
        /// </summary>
        public void OnPlayerDefeated()
        {
            Debug.Log("[GameManager] Player defeated - Game Over");

            _isGameActive = false;
            TransitionToState(GameState.GameOver);
            _onGameOver?.Raise();
        }

        /// <summary>
        /// Called when all bosses are defeated.
        /// </summary>
        private void GameComplete()
        {
            Debug.Log("[GameManager] All bosses defeated - Victory!");

            _isGameActive = false;
            TransitionToState(GameState.GameOver);
            _onAllBossesDefeated?.Raise();
        }

        /// <summary>
        /// Return to main menu.
        /// </summary>
        public void ReturnToMenu()
        {
            Debug.Log("[GameManager] Returning to menu");

            _isGameActive = false;
            TransitionToState(GameState.Menu);
            _onReturnToMenu?.Raise();

            LoadMenuScene();
        }

        /// <summary>
        /// Pause the game.
        /// </summary>
        public void PauseGame()
        {
            if (_currentState == GameState.Combat)
            {
                Time.timeScale = 0f;
                TransitionToState(GameState.Paused);
                Debug.Log("[GameManager] Game paused");
            }
        }

        /// <summary>
        /// Resume the game.
        /// </summary>
        public void ResumeGame()
        {
            if (_currentState == GameState.Paused)
            {
                Time.timeScale = 1f;
                TransitionToState(GameState.Combat);
                Debug.Log("[GameManager] Game resumed");
            }
        }

        /// <summary>
        /// Quit the game.
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[GameManager] Quitting game");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Get the current boss data.
        /// </summary>
        public BossData GetCurrentBossData()
        {
            if (_currentBossIndex >= 0 && _currentBossIndex < _bossProgression.Length)
            {
                return _bossProgression[_currentBossIndex];
            }

            Debug.LogWarning($"[GameManager] Invalid boss index: {_currentBossIndex}");
            return null;
        }

        /// <summary>
        /// Transition to a new game state.
        /// </summary>
        private void TransitionToState(GameState newState)
        {
            if (_currentState == newState)
                return;

            GameState previousState = _currentState;
            _currentState = newState;

            Debug.Log($"[GameManager] State changed: {previousState} → {newState}");

            OnStateChanged(previousState, newState);
        }

        /// <summary>
        /// Called when state changes. Override for custom behavior.
        /// </summary>
        private void OnStateChanged(GameState previousState, GameState newState)
        {
            switch (newState)
            {
                case GameState.Menu:
                    Time.timeScale = 1f;
                    break;

                case GameState.Combat:
                    Time.timeScale = 1f;
                    break;

                case GameState.ArtifactSelection:
                    Time.timeScale = 1f;
                    break;

                case GameState.GameOver:
                    Time.timeScale = 1f;
                    break;

                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
            }
        }

        // Scene Loading Methods

        private void LoadMenuScene()
        {
            SceneManager.LoadScene(_menuSceneName);
        }

        private void LoadArenaScene()
        {
            SceneManager.LoadScene(_arenaSceneName);
        }

        private void LoadArtifactSelectionScene()
        {
            SceneManager.LoadScene(_artifactSceneName);
        }

        // Debug Methods

        [ContextMenu("Debug: Skip to Next Boss")]
        private void DebugSkipToNextBoss()
        {
            if (HasMoreBosses)
            {
                _currentBossIndex++;
                Debug.Log($"[GameManager] DEBUG: Skipped to boss {_currentBossIndex}");
            }
        }

        [ContextMenu("Debug: Log Stats")]
        private void DebugLogStats()
        {
            Debug.Log("=== Game Manager Stats ===");
            Debug.Log($"Current State: {_currentState}");
            Debug.Log($"Current Boss Index: {_currentBossIndex}/{_bossProgression.Length}");
            Debug.Log($"Bosses Defeated: {_bossesDefeated}");
            Debug.Log($"Total Score: {_totalScore}");
            Debug.Log($"Time Played: {_totalTimePlayed:F2}s");
            Debug.Log("=========================");
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            Time.timeScale = 1f; // Reset time scale on destroy
        }
    }

    // Forward declaration - will be defined in Boss system
    [System.Serializable]
    public class BossData : ScriptableObject
    {
        // This is a placeholder - full implementation will be in Boss scripts
    }
}