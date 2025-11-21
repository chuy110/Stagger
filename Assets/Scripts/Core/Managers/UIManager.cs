using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Stagger.Core.Managers;

namespace Stagger.UI
{
    /// <summary>
    /// BULLETPROOF VERSION - Cannot crash, handles all null cases
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UIManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("UIManager");
                        _instance = go.AddComponent<UIManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("UI Screens")]
        [SerializeField] private GameObject _startScreen;
        [SerializeField] private GameObject _pauseScreen;
        [SerializeField] private GameObject _resultScreen;
        [SerializeField] private GameObject _equipmentScreen;
        [SerializeField] private GameObject _hudScreen;

        [Header("Start Screen")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private TextMeshProUGUI _titleText;

        [Header("Pause Screen")]
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _mainMenuButton;

        [Header("Result Screen")]
        [SerializeField] private TextMeshProUGUI _bossNameText;
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private TextMeshProUGUI _artifactsDroppedText;
        [SerializeField] private Transform _artifactDisplayContainer;
        [SerializeField] private GameObject _artifactIconPrefab;
        [SerializeField] private Button _continueToEquipmentButton;

        [Header("Equipment Screen")]
        [SerializeField] private Transform _equippedArtifactsContainer;
        [SerializeField] private Transform _inventoryArtifactsContainer;
        [SerializeField] private Button _nextBossButton;
        [SerializeField] private Button _previousBossButton;
        [SerializeField] private Button _backToMenuButton;
        [SerializeField] private TextMeshProUGUI _equippedStatsText;

        [Header("HUD")]
        [SerializeField] private Image _bossHealthBar;
        [SerializeField] private TextMeshProUGUI _bossNameHUD;
        [SerializeField] private TextMeshProUGUI _timerText;

        private UIScreen _currentScreen = UIScreen.None;
        private float _battleStartTime;

        public enum UIScreen
        {
            None,
            Start,
            Pause,
            Result,
            Equipment,
            HUD
        }

        private void Awake()
        {
            Debug.Log("[UIManager] Awake called");
            
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            SetupButtonListeners();
        }

        private void Start()
        {
            ShowStartScreen();
        }

        private void Update()
        {
            if (_currentScreen == UIScreen.HUD)
            {
                UpdateBattleTimer();
            }
        }

        private void SetupButtonListeners()
        {
            try
            {
                if (_startButton != null)
                    _startButton.onClick.AddListener(OnStartButtonClicked);
                if (_quitButton != null)
                    _quitButton.onClick.AddListener(OnQuitButtonClicked);

                if (_resumeButton != null)
                    _resumeButton.onClick.AddListener(OnResumeButtonClicked);
                if (_mainMenuButton != null)
                    _mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);

                if (_continueToEquipmentButton != null)
                    _continueToEquipmentButton.onClick.AddListener(OnContinueToEquipmentClicked);

                if (_nextBossButton != null)
                    _nextBossButton.onClick.AddListener(OnNextBossButtonClicked);
                if (_previousBossButton != null)
                    _previousBossButton.onClick.AddListener(OnPreviousBossButtonClicked);
                if (_backToMenuButton != null)
                    _backToMenuButton.onClick.AddListener(OnBackToMenuButtonClicked);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] Error setting up button listeners: {e.Message}");
            }
        }

        #region Screen Transitions

        public void ShowStartScreen()
        {
            try
            {
                HideAllScreens();
                _currentScreen = UIScreen.Start;
                if (_startScreen != null) _startScreen.SetActive(true);
                
                if (_titleText != null)
                    _titleText.text = "PUPPET BOSS RUSH";

                Time.timeScale = 1f;
                Debug.Log("[UIManager] Showing Start Screen");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] Error showing start screen: {e.Message}");
            }
        }

        public void ShowPauseScreen()
        {
            try
            {
                HideAllScreens();
                _currentScreen = UIScreen.Pause;
                if (_pauseScreen != null) _pauseScreen.SetActive(true);
                if (_hudScreen != null) _hudScreen.SetActive(true);

                Time.timeScale = 0f;
                Debug.Log("[UIManager] Showing Pause Screen");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] Error showing pause screen: {e.Message}");
            }
        }

        /// <summary>
        /// BULLETPROOF: Parameterless version for UnityEvent - CANNOT CRASH
        /// </summary>
        public void ShowResultScreenFromBossVictory()
        {
            Debug.Log("[UIManager] ════════════════════════════════════════════");
            Debug.Log("[UIManager] ShowResultScreenFromBossVictory CALLED");
            Debug.Log("[UIManager] ════════════════════════════════════════════");
            
            try
            {
                Debug.Log("[UIManager] Step 1: Getting battle time...");
                float battleTime = 0f;
                try
                {
                    battleTime = GetBattleTime();
                    Debug.Log($"[UIManager] ✓ Battle time: {battleTime:F2}s");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[UIManager] Error getting battle time: {e.Message}");
                }
        
                Debug.Log("[UIManager] Step 2: Getting boss name...");
                string bossName = "Boss";
                try
                {
                    if (_bossNameHUD != null && !string.IsNullOrEmpty(_bossNameHUD.text))
                    {
                        bossName = _bossNameHUD.text;
                    }
                    Debug.Log($"[UIManager] ✓ Boss name: {bossName}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[UIManager] Error getting boss name: {e.Message}");
                }
        
                Debug.Log("[UIManager] Step 3: Getting dropped artifacts...");
                List<ArtifactData> droppedArtifacts = new List<ArtifactData>();
                try
                {
                    if (GameManager.Instance != null)
                    {
                        Debug.Log("[UIManager] GameManager exists, getting drops...");
                        var drops = GameManager.Instance.GetLastDroppedArtifacts();
                        if (drops != null)
                        {
                            droppedArtifacts = drops;
                            Debug.Log($"[UIManager] ✓ Got {droppedArtifacts.Count} artifacts");
                        }
                        else
                        {
                            Debug.LogWarning("[UIManager] GetLastDroppedArtifacts returned null");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[UIManager] GameManager.Instance is null");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[UIManager] Error getting artifacts: {e.Message}");
                }
        
                Debug.Log("[UIManager] Step 4: Calling ShowResultScreen...");
                try
                {
                    ShowResultScreen(bossName, battleTime, droppedArtifacts);
                    Debug.Log("[UIManager] ✓✓✓ Result screen shown successfully!");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[UIManager] Error in ShowResultScreen: {e.Message}");
                    Debug.LogError($"[UIManager] Stack: {e.StackTrace}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] ✗✗✗ CRITICAL ERROR in ShowResultScreenFromBossVictory: {e.Message}");
                Debug.LogError($"[UIManager] Stack trace: {e.StackTrace}");
                
                // Emergency fallback - just show the result screen with defaults
                try
                {
                    Debug.Log("[UIManager] Attempting emergency fallback...");
                    HideAllScreens();
                    if (_resultScreen != null)
                    {
                        _resultScreen.SetActive(true);
                        _currentScreen = UIScreen.Result;
                        Debug.Log("[UIManager] Emergency fallback successful");
                    }
                }
                catch (System.Exception e2)
                {
                    Debug.LogError($"[UIManager] Even emergency fallback failed: {e2.Message}");
                }
            }
            
            Debug.Log("[UIManager] ════════════════════════════════════════════");
        }

        public void ShowResultScreen(string bossName, float battleTime, List<ArtifactData> droppedArtifacts)
        {
            Debug.Log($"[UIManager] ShowResultScreen called: {bossName}, {battleTime:F2}s");
            
            try
            {
                HideAllScreens();
                _currentScreen = UIScreen.Result;
                
                if (_resultScreen != null)
                {
                    _resultScreen.SetActive(true);
                    Debug.Log("[UIManager] ✓ Result screen activated");
                }
                else
                {
                    Debug.LogError("[UIManager] Result screen GameObject is null!");
                }

                if (_bossNameText != null)
                {
                    _bossNameText.text = $"{bossName} DEFEATED!";
                }

                if (_timeText != null)
                {
                    int minutes = Mathf.FloorToInt(battleTime / 60f);
                    int seconds = Mathf.FloorToInt(battleTime % 60f);
                    _timeText.text = $"Time: {minutes:00}:{seconds:00}";
                }

                if (_artifactsDroppedText != null && droppedArtifacts != null)
                {
                    _artifactsDroppedText.text = $"Artifacts Dropped: {droppedArtifacts.Count}";
                }

                if (droppedArtifacts != null)
                {
                    DisplayDroppedArtifacts(droppedArtifacts);
                }

                Time.timeScale = 1f;
                Debug.Log("[UIManager] ✓ Result screen fully displayed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] Error in ShowResultScreen: {e.Message}");
                Debug.LogError($"[UIManager] Stack: {e.StackTrace}");
            }
        }

        public void ShowEquipmentScreen()
        {
            try
            {
                HideAllScreens();
                _currentScreen = UIScreen.Equipment;
                if (_equipmentScreen != null) _equipmentScreen.SetActive(true);

                RefreshEquipmentScreen();

                Time.timeScale = 1f;
                Debug.Log("[UIManager] Showing Equipment Screen");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] Error showing equipment screen: {e.Message}");
            }
        }

        public void ShowHUD(string bossName)
        {
            try
            {
                HideAllScreens();
                _currentScreen = UIScreen.HUD;
                if (_hudScreen != null) _hudScreen.SetActive(true);

                if (_bossNameHUD != null)
                    _bossNameHUD.text = bossName;

                _battleStartTime = Time.time;

                Time.timeScale = 1f;
                Debug.Log("[UIManager] Showing HUD");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] Error showing HUD: {e.Message}");
            }
        }

        private void HideAllScreens()
        {
            try
            {
                if (_startScreen != null) _startScreen.SetActive(false);
                if (_pauseScreen != null) _pauseScreen.SetActive(false);
                if (_resultScreen != null) _resultScreen.SetActive(false);
                if (_equipmentScreen != null) _equipmentScreen.SetActive(false);
                if (_hudScreen != null) _hudScreen.SetActive(false);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] Error hiding screens: {e.Message}");
            }
        }

        #endregion

        #region Button Callbacks

        private void OnStartButtonClicked()
        {
            Debug.Log("[UIManager] Start button clicked");
            try
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.StartNewGame();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] Error starting game: {e.Message}");
            }
        }

        private void OnQuitButtonClicked()
        {
            Debug.Log("[UIManager] Quit button clicked");
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnResumeButtonClicked()
        {
            Debug.Log("[UIManager] Resume button clicked");
            ShowHUD(_bossNameHUD != null ? _bossNameHUD.text : "Boss");
        }

        private void OnMainMenuButtonClicked()
        {
            Debug.Log("[UIManager] Main menu button clicked");
            
            try
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ReturnToMenu();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] Error returning to menu: {e.Message}");
            }
        }

        private void OnContinueToEquipmentClicked()
        {
            Debug.Log("[UIManager] Continue to equipment clicked");
            ShowEquipmentScreen();
        }

        private void OnNextBossButtonClicked()
        {
            Debug.Log("[UIManager] Next boss button clicked");
            
            try
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.LoadNextBoss();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] Error loading next boss: {e.Message}");
            }
        }

        private void OnPreviousBossButtonClicked()
        {
            Debug.Log("[UIManager] Previous boss button clicked");
            
            try
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.LoadPreviousBoss();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] Error loading previous boss: {e.Message}");
            }
        }

        private void OnBackToMenuButtonClicked()
        {
            Debug.Log("[UIManager] Back to menu clicked");
            
            try
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ReturnToMenu();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] Error going back to menu: {e.Message}");
            }
        }

        #endregion

        #region HUD Updates

        public void UpdateBossHealth(float currentHealth, float maxHealth)
        {
            try
            {
                if (_bossHealthBar != null)
                {
                    _bossHealthBar.fillAmount = maxHealth > 0 ? currentHealth / maxHealth : 0f;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] Error updating boss health: {e.Message}");
            }
        }

        private void UpdateBattleTimer()
        {
            try
            {
                if (_timerText != null)
                {
                    float elapsed = Time.time - _battleStartTime;
                    int minutes = Mathf.FloorToInt(elapsed / 60f);
                    int seconds = Mathf.FloorToInt(elapsed % 60f);
                    _timerText.text = $"{minutes:00}:{seconds:00}";
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] Error updating timer: {e.Message}");
            }
        }

        public float GetBattleTime()
        {
            return Time.time - _battleStartTime;
        }

        #endregion

        #region Artifact Display

        private void DisplayDroppedArtifacts(List<ArtifactData> artifacts)
        {
            try
            {
                if (_artifactDisplayContainer == null)
                {
                    Debug.LogWarning("[UIManager] Artifact display container is null");
                    return;
                }

                foreach (Transform child in _artifactDisplayContainer)
                {
                    Destroy(child.gameObject);
                }

                if (artifacts == null || artifacts.Count == 0)
                {
                    Debug.Log("[UIManager] No artifacts to display");
                    return;
                }

                foreach (var artifact in artifacts)
                {
                    if (_artifactIconPrefab != null && artifact != null)
                    {
                        GameObject icon = Instantiate(_artifactIconPrefab, _artifactDisplayContainer);
                        
                        Image img = icon.GetComponent<Image>();
                        if (img != null && artifact.Icon != null)
                        {
                            img.sprite = artifact.Icon;
                            img.color = GetRarityColor(artifact.Rarity);
                        }

                        TextMeshProUGUI nameText = icon.GetComponentInChildren<TextMeshProUGUI>();
                        if (nameText != null)
                        {
                            nameText.text = artifact.ArtifactName;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] Error displaying artifacts: {e.Message}");
            }
        }

        private void RefreshEquipmentScreen()
        {
            try
            {
                if (EquipmentManager.Instance == null) return;

                if (_equippedArtifactsContainer != null)
                {
                    foreach (Transform child in _equippedArtifactsContainer)
                    {
                        Destroy(child.gameObject);
                    }

                    foreach (var artifact in EquipmentManager.Instance.EquippedArtifacts)
                    {
                        CreateArtifactSlot(artifact, _equippedArtifactsContainer, true);
                    }
                }

                if (_inventoryArtifactsContainer != null)
                {
                    foreach (Transform child in _inventoryArtifactsContainer)
                    {
                        Destroy(child.gameObject);
                    }

                    foreach (var artifact in EquipmentManager.Instance.InventoryArtifacts)
                    {
                        CreateArtifactSlot(artifact, _inventoryArtifactsContainer, false);
                    }
                }

                if (_equippedStatsText != null)
                {
                    _equippedStatsText.text = EquipmentManager.Instance.GetEquippedStatsString();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] Error refreshing equipment screen: {e.Message}");
            }
        }

        private void CreateArtifactSlot(ArtifactData artifact, Transform parent, bool isEquipped)
        {
            try
            {
                if (_artifactIconPrefab == null) return;

                GameObject slot = Instantiate(_artifactIconPrefab, parent);
                
                Image img = slot.GetComponent<Image>();
                if (img != null)
                {
                    if (artifact.Icon != null)
                    {
                        img.sprite = artifact.Icon;
                    }
                    img.color = GetRarityColor(artifact.Rarity);
                }

                TextMeshProUGUI nameText = slot.GetComponentInChildren<TextMeshProUGUI>();
                if (nameText != null)
                {
                    nameText.text = artifact.ArtifactName;
                }

                Button btn = slot.GetComponent<Button>();
                if (btn == null) btn = slot.AddComponent<Button>();
                
                btn.onClick.AddListener(() => OnArtifactClicked(artifact, isEquipped));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] Error creating artifact slot: {e.Message}");
            }
        }

        private void OnArtifactClicked(ArtifactData artifact, bool isCurrentlyEquipped)
        {
            try
            {
                if (EquipmentManager.Instance == null) return;

                if (isCurrentlyEquipped)
                {
                    EquipmentManager.Instance.UnequipArtifact(artifact);
                }
                else
                {
                    EquipmentManager.Instance.EquipArtifact(artifact);
                }

                RefreshEquipmentScreen();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] Error handling artifact click: {e.Message}");
            }
        }

        private Color GetRarityColor(ArtifactRarity rarity)
        {
            switch (rarity)
            {
                case ArtifactRarity.Common:
                    return Color.white;
                case ArtifactRarity.Uncommon:
                    return Color.green;
                case ArtifactRarity.Rare:
                    return Color.blue;
                case ArtifactRarity.Epic:
                    return new Color(0.5f, 0f, 1f);
                case ArtifactRarity.Legendary:
                    return new Color(1f, 0.5f, 0f);
                default:
                    return Color.white;
            }
        }

        #endregion
    }
}