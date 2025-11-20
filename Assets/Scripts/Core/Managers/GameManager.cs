using UnityEngine;
using System.Collections.Generic;
using Stagger.Boss;
using Stagger.UI;

namespace Stagger.Core.Managers
{
    /// <summary>
    /// Main Game Manager singleton controlling game flow and state.
    /// Manages scene transitions, boss progression, and overall game state.
    /// Uses State pattern for game states.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Game State")]
        [SerializeField] private GameState _currentState = GameState.Menu;

        [Header("Boss Progression")]
        [SerializeField] private List<BossData> _bossProgression = new List<BossData>();
        [SerializeField] private int _currentBossIndex = 0;

        [Header("Boss References")]
        [SerializeField] private BossController _currentBossController;
        [SerializeField] private Transform _bossSpawnPoint;

        [Header("Stats")]
        [SerializeField] private int _totalScore = 0;
        [SerializeField] private int _bossesDefeated = 0;
        [SerializeField] private float _totalTimePlayed = 0f;

        // Dropped artifacts from current boss
        private List<ArtifactData> _currentBossDrops = new List<ArtifactData>();

        public enum GameState
        {
            Menu,
            Combat,
            Result,
            Equipment,
            Paused
        }

        public GameState CurrentState => _currentState;
        public int CurrentBossIndex => _currentBossIndex;
        public int BossesDefeated => _bossesDefeated;
        public int TotalScore => _totalScore;
        public float TotalTimePlayed => _totalTimePlayed;
        public bool HasMoreBosses => _currentBossIndex < _bossProgression.Count;
        public bool HasPreviousBoss => _currentBossIndex > 0;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Start at menu
            TransitionToState(GameState.Menu);
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowStartScreen();
            }
        }

        private void Update()
        {
            if (_currentState == GameState.Combat)
            {
                _totalTimePlayed += Time.deltaTime;
            }
        }

        #region Game Flow

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

            // Clear equipment
            if (EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.ClearAllEquipment();
            }

            TransitionToState(GameState.Combat);
            LoadBoss(_currentBossIndex);
        }

        /// <summary>
        /// Load the next boss.
        /// </summary>
        public void LoadNextBoss()
        {
            if (!HasMoreBosses)
            {
                Debug.Log("[GameManager] No more bosses! Game complete!");
                GameComplete();
                return;
            }

            _currentBossIndex++;
            TransitionToState(GameState.Combat);
            LoadBoss(_currentBossIndex);
        }

        /// <summary>
        /// Load the previous boss (if player wants to retry/grind).
        /// </summary>
        public void LoadPreviousBoss()
        {
            if (!HasPreviousBoss)
            {
                Debug.LogWarning("[GameManager] No previous boss to load");
                return;
            }

            _currentBossIndex--;
            TransitionToState(GameState.Combat);
            LoadBoss(_currentBossIndex);
        }

        /// <summary>
        /// Load a specific boss by index.
        /// </summary>
        private void LoadBoss(int index)
        {
            if (index < 0 || index >= _bossProgression.Count)
            {
                Debug.LogError($"[GameManager] Invalid boss index: {index}");
                return;
            }

            BossData bossData = _bossProgression[index];
            Debug.Log($"[GameManager] Loading boss: {bossData.BossName} (Index: {index})");

            // Clear previous boss
            if (_currentBossController != null)
            {
                Destroy(_currentBossController.gameObject);
            }

            // Spawn new boss
            SpawnBoss(bossData);

            // Show HUD
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowHUD(bossData.BossName);
            }
        }

        /// <summary>
        /// Spawn a boss from data.
        /// </summary>
        private void SpawnBoss(BossData bossData)
        {
            if (bossData.BossPrefab == null)
            {
                Debug.LogError($"[GameManager] No prefab for boss: {bossData.BossName}");
                return;
            }

            Vector3 spawnPos = _bossSpawnPoint != null ? _bossSpawnPoint.position : Vector3.zero;
            GameObject bossObj = Instantiate(bossData.BossPrefab, spawnPos, Quaternion.identity);
            
            _currentBossController = bossObj.GetComponent<BossController>();
            if (_currentBossController == null)
            {
                Debug.LogError($"[GameManager] Boss prefab missing BossController: {bossData.BossName}");
                Destroy(bossObj);
                return;
            }

            // Initialize boss
            _currentBossController.Initialize(bossData);

            // Subscribe to boss events (Observer pattern)
            _currentBossController.OnBossVictory.AddListener(OnBossDefeated);
            
            // Subscribe to health updates for UI
            BossHealth health = _currentBossController.Health;
            if (health != null && UIManager.Instance != null)
            {
                health.OnHealthPercentChanged.AddListener((percent) => 
                {
                    UIManager.Instance.UpdateBossHealth(health.CurrentHealth, health.MaxHealth);
                });
            }

            Debug.Log($"[GameManager] Boss spawned: {bossData.BossName}");
        }

        /// <summary>
        /// Called when current boss is defeated.
        /// </summary>
        private void OnBossDefeated()
        {
            Debug.Log($"[GameManager] Boss defeated!");

            _bossesDefeated++;
            _totalScore += 100; // Base score, can be modified

            // Get battle time
            float battleTime = UIManager.Instance != null ? UIManager.Instance.GetBattleTime() : 0f;

            // Get dropped artifacts
            _currentBossDrops = GetBossDrops();

            // Add artifacts to inventory
            if (EquipmentManager.Instance != null)
            {
                foreach (var artifact in _currentBossDrops)
                {
                    EquipmentManager.Instance.AddArtifact(artifact);
                }
            }

            // Show result screen
            TransitionToState(GameState.Result);
            
            if (UIManager.Instance != null)
            {
                string bossName = _currentBossController != null && _currentBossController.Data != null 
                    ? _currentBossController.Data.BossName 
                    : "Boss";
                    
                UIManager.Instance.ShowResultScreen(bossName, battleTime, _currentBossDrops);
            }
        }

        /// <summary>
        /// Get artifacts dropped by current boss.
        /// </summary>
        private List<ArtifactData> GetBossDrops()
        {
            List<ArtifactData> drops = new List<ArtifactData>();

            if (_currentBossController == null || _currentBossController.Data == null)
            {
                Debug.LogWarning("[GameManager] No boss controller for drops");
                return drops;
            }

            BossData bossData = _currentBossController.Data;
            
            if (bossData.PossibleDrops == null || bossData.PossibleDrops.Count == 0)
            {
                Debug.Log("[GameManager] No possible drops configured for boss");
                return drops;
            }

            // Roll for each possible drop
            foreach (var drop in bossData.PossibleDrops)
            {
                if (drop.ArtifactData == null) continue;

                float roll = Random.Range(0f, 1f);
                
                if (roll <= drop.DropChance)
                {
                    drops.Add(drop.ArtifactData);
                    Debug.Log($"[GameManager] ★ ARTIFACT DROPPED: {drop.ArtifactData.ArtifactName} (roll: {roll:F2})");
                }
                else
                {
                    Debug.Log($"[GameManager] No drop: {drop.ArtifactData.name} (roll: {roll:F2} > {drop.DropChance:F2})");
                }
            }

            return drops;
        }

        /// <summary>
        /// Called when all bosses are defeated.
        /// </summary>
        private void GameComplete()
        {
            Debug.Log("[GameManager] ★★★ ALL BOSSES DEFEATED! GAME COMPLETE! ★★★");
            
            // TODO: Show victory screen or credits
            ReturnToMenu();
        }

        /// <summary>
        /// Return to main menu.
        /// </summary>
        public void ReturnToMenu()
        {
            Debug.Log("[GameManager] Returning to menu");

            // Clean up current boss
            if (_currentBossController != null)
            {
                Destroy(_currentBossController.gameObject);
                _currentBossController = null;
            }

            TransitionToState(GameState.Menu);
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowStartScreen();
            }
        }

        /// <summary>
        /// Pause the game.
        /// </summary>
        public void PauseGame()
        {
            if (_currentState == GameState.Combat)
            {
                TransitionToState(GameState.Paused);
                
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowPauseScreen();
                }
            }
        }

        /// <summary>
        /// Get the current boss data.
        /// </summary>
        public BossData GetCurrentBossData()
        {
            if (_currentBossIndex >= 0 && _currentBossIndex < _bossProgression.Count)
            {
                return _bossProgression[_currentBossIndex];
            }

            Debug.LogWarning($"[GameManager] Invalid boss index: {_currentBossIndex}");
            return null;
        }

        /// <summary>
        /// Get the current boss controller instance.
        /// </summary>
        public BossController GetCurrentBossController()
        {
            return _currentBossController;
        }

        #endregion

        #region State Management

        private void TransitionToState(GameState newState)
        {
            if (_currentState == newState)
                return;

            GameState previousState = _currentState;
            _currentState = newState;

            Debug.Log($"[GameManager] State: {previousState} → {newState}");

            OnStateChanged(previousState, newState);
        }

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

                case GameState.Result:
                    Time.timeScale = 1f;
                    break;

                case GameState.Equipment:
                    Time.timeScale = 1f;
                    break;

                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
            }
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Log Stats")]
        private void DebugLogStats()
        {
            Debug.Log("=== GAME MANAGER STATS ===");
            Debug.Log($"Current State: {_currentState}");
            Debug.Log($"Current Boss: {_currentBossIndex}/{_bossProgression.Count}");
            Debug.Log($"Bosses Defeated: {_bossesDefeated}");
            Debug.Log($"Total Score: {_totalScore}");
            Debug.Log($"Time Played: {_totalTimePlayed:F2}s");
            Debug.Log("==========================");
        }

        [ContextMenu("Debug: Skip to Next Boss")]
        private void DebugSkipBoss()
        {
            LoadNextBoss();
        }

        #endregion
    }
}