using UnityEngine;
using System.Collections.Generic;
using Stagger.Boss;
using Stagger.UI;

namespace Stagger.Core.Managers
{
    /// <summary>
    /// FIXED VERSION - Proper state management and boss lifecycle handling
    /// Addresses: Boss continuing to run during results/equipment screens
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

        private List<ArtifactData> _currentBossDrops = new List<ArtifactData>();
        private List<ArtifactData> _lastDroppedArtifacts = new List<ArtifactData>();

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
            
            Debug.Log("[GameManager] Initialized");
        }

        private void Start()
        {
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

        public void StartNewGame()
        {
            Debug.Log("[GameManager] ═══════════════════════════");
            Debug.Log("[GameManager] Starting new game");
            Debug.Log("[GameManager] ═══════════════════════════");

            _currentBossIndex = 0;
            _bossesDefeated = 0;
            _totalScore = 0;
            _totalTimePlayed = 0f;

            // Clear equipment
            if (EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.ClearAllEquipment();
            }

            // Clean up any existing boss
            CleanupCurrentBoss();

            TransitionToState(GameState.Combat);
            LoadBoss(_currentBossIndex);
        }

        public void LoadNextBoss()
        {
            if (!HasMoreBosses)
            {
                Debug.Log("[GameManager] No more bosses! Game complete!");
                GameComplete();
                return;
            }

            _currentBossIndex++;
            
            // CRITICAL FIX: Clean up previous boss before loading next
            CleanupCurrentBoss();
            
            TransitionToState(GameState.Combat);
            LoadBoss(_currentBossIndex);
        }

        public void LoadPreviousBoss()
        {
            if (!HasPreviousBoss)
            {
                Debug.LogWarning("[GameManager] No previous boss to load");
                return;
            }

            _currentBossIndex--;
            
            // CRITICAL FIX: Clean up current boss before loading previous
            CleanupCurrentBoss();
            
            TransitionToState(GameState.Combat);
            LoadBoss(_currentBossIndex);
        }

        /// <summary>
        /// CRITICAL FIX: Properly clean up boss before loading new one
        /// </summary>
        private void CleanupCurrentBoss()
        {
            if (_currentBossController != null)
            {
                Debug.Log("[GameManager] Cleaning up current boss...");
                
                // Unsubscribe from events
                _currentBossController.OnBossVictory.RemoveListener(OnBossDefeated);
                
                // Despawn all projectiles from this boss
                if (PoolManager.Instance != null)
                {
                    try
                    {
                        PoolManager.Instance.DespawnAll<Projectile>("BossProjectile");
                        Debug.Log("[GameManager] ✓ Despawned all projectiles");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[GameManager] Error despawning projectiles: {e.Message}");
                    }
                }
                
                // Destroy boss GameObject
                Destroy(_currentBossController.gameObject);
                _currentBossController = null;
                
                Debug.Log("[GameManager] ✓ Boss cleaned up");
            }
        }

        private void LoadBoss(int index)
        {
            if (index < 0 || index >= _bossProgression.Count)
            {
                Debug.LogError($"[GameManager] Invalid boss index: {index}");
                return;
            }

            BossData bossData = _bossProgression[index];
            Debug.Log($"[GameManager] ════════════════════════════════");
            Debug.Log($"[GameManager] Loading boss: {bossData.BossName} (Index: {index})");
            Debug.Log($"[GameManager] ════════════════════════════════");

            // Spawn new boss
            SpawnBoss(bossData);

            // Show HUD
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowHUD(bossData.BossName);
            }
        }

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

            // Initialize boss - CRITICAL: This resets all flags
            _currentBossController.Initialize(bossData);

            // Subscribe to boss events (Observer pattern)
            _currentBossController.OnBossVictory.AddListener(OnBossDefeated);
            Debug.Log("[GameManager] ✓ Subscribed to OnBossVictory");
            
            // Subscribe to health updates for UI
            BossHealth health = _currentBossController.Health;
            if (health != null && UIManager.Instance != null)
            {
                health.OnHealthPercentChanged.AddListener((percent) => 
                {
                    UIManager.Instance.UpdateBossHealth(health.CurrentHealth, health.MaxHealth);
                });
                
                // Initialize health bar
                UIManager.Instance.UpdateBossHealth(health.CurrentHealth, health.MaxHealth);
            }

            Debug.Log($"[GameManager] ✓ Boss spawned: {bossData.BossName}");
        }

        private void OnBossDefeated()
        {
            Debug.Log($"[GameManager] ════════════════════════════════");
            Debug.Log($"[GameManager] Boss defeated callback received!");
            Debug.Log($"[GameManager] ════════════════════════════════");

            _bossesDefeated++;
            _totalScore += 100;

            // Get battle time
            float battleTime = UIManager.Instance != null ? UIManager.Instance.GetBattleTime() : 0f;

            // Get dropped artifacts
            _currentBossDrops = GetBossDrops();
            _lastDroppedArtifacts = new List<ArtifactData>(_currentBossDrops);

            // Add artifacts to inventory
            if (EquipmentManager.Instance != null)
            {
                foreach (var artifact in _currentBossDrops)
                {
                    EquipmentManager.Instance.AddArtifact(artifact);
                }
            }

            // CRITICAL FIX: Transition to result state BEFORE showing screen
            TransitionToState(GameState.Result);
            
            // Show result screen
            if (UIManager.Instance != null)
            {
                string bossName = _currentBossController != null && _currentBossController.Data != null 
                    ? _currentBossController.Data.BossName 
                    : "Boss";
                    
                UIManager.Instance.ShowResultScreen(bossName, battleTime, _currentBossDrops);
            }
            
            Debug.Log("[GameManager] ✓ Result screen shown");
        }

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

        private void GameComplete()
        {
            Debug.Log("[GameManager] ★★★ ALL BOSSES DEFEATED! GAME COMPLETE! ★★★");
            ReturnToMenu();
        }

        public void ReturnToMenu()
        {
            Debug.Log("[GameManager] Returning to menu");

            // Clean up current boss
            CleanupCurrentBoss();

            TransitionToState(GameState.Menu);
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowStartScreen();
            }
        }

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

        public BossData GetCurrentBossData()
        {
            if (_currentBossIndex >= 0 && _currentBossIndex < _bossProgression.Count)
            {
                return _bossProgression[_currentBossIndex];
            }

            Debug.LogWarning($"[GameManager] Invalid boss index: {_currentBossIndex}");
            return null;
        }

        public BossController GetCurrentBossController()
        {
            return _currentBossController;
        }

        public void SetLastDroppedArtifacts(List<ArtifactData> artifacts)
        {
            _lastDroppedArtifacts = artifacts;
            Debug.Log($"[GameManager] Tracked {artifacts.Count} dropped artifacts");
        }

        public List<ArtifactData> GetLastDroppedArtifacts()
        {
            return _lastDroppedArtifacts;
        }

        #endregion

        #region State Management

        private void TransitionToState(GameState newState)
        {
            if (_currentState == newState)
                return;

            GameState previousState = _currentState;
            _currentState = newState;

            Debug.Log($"[GameManager] ═══ State: {previousState} → {newState} ═══");

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
                    // CRITICAL FIX: Don't pause time, but boss will check state
                    Time.timeScale = 1f;
                    Debug.Log("[GameManager] Result state - boss updates will be paused");
                    break;

                case GameState.Equipment:
                    Time.timeScale = 1f;
                    Debug.Log("[GameManager] Equipment state - boss updates will be paused");
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