using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Stagger.Core.Managers;

namespace Stagger.UI
{
    /// <summary>
    /// Manages all UI screens and transitions between them.
    /// Implements State pattern for screen management and Singleton pattern.
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

        // Current screen state
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
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Wire up button listeners (Observer pattern)
            SetupButtonListeners();
        }

        private void Start()
        {
            ShowStartScreen();
        }

        private void Update()
        {
            // Update timer if in battle
            if (_currentScreen == UIScreen.HUD)
            {
                UpdateBattleTimer();
            }

            // Pause button
            if (Input.GetKeyDown(KeyCode.Escape) && _currentScreen == UIScreen.HUD)
            {
                ShowPauseScreen();
            }
        }

        private void SetupButtonListeners()
        {
            // Start screen
            if (_startButton != null)
                _startButton.onClick.AddListener(OnStartButtonClicked);
            if (_quitButton != null)
                _quitButton.onClick.AddListener(OnQuitButtonClicked);

            // Pause screen
            if (_resumeButton != null)
                _resumeButton.onClick.AddListener(OnResumeButtonClicked);
            if (_mainMenuButton != null)
                _mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);

            // Result screen
            if (_continueToEquipmentButton != null)
                _continueToEquipmentButton.onClick.AddListener(OnContinueToEquipmentClicked);

            // Equipment screen
            if (_nextBossButton != null)
                _nextBossButton.onClick.AddListener(OnNextBossButtonClicked);
            if (_previousBossButton != null)
                _previousBossButton.onClick.AddListener(OnPreviousBossButtonClicked);
            if (_backToMenuButton != null)
                _backToMenuButton.onClick.AddListener(OnBackToMenuButtonClicked);
        }

        #region Screen Transitions

        public void ShowStartScreen()
        {
            HideAllScreens();
            _currentScreen = UIScreen.Start;
            if (_startScreen != null) _startScreen.SetActive(true);
            
            if (_titleText != null)
                _titleText.text = "PUPPET BOSS RUSH";

            Time.timeScale = 1f;
            Debug.Log("[UIManager] Showing Start Screen");
        }

        public void ShowPauseScreen()
        {
            HideAllScreens();
            _currentScreen = UIScreen.Pause;
            if (_pauseScreen != null) _pauseScreen.SetActive(true);
            if (_hudScreen != null) _hudScreen.SetActive(true); // Keep HUD visible

            Time.timeScale = 0f;
            Debug.Log("[UIManager] Showing Pause Screen");
        }

        public void ShowResultScreen(string bossName, float battleTime, List<ArtifactData> droppedArtifacts)
        {
            HideAllScreens();
            _currentScreen = UIScreen.Result;
            if (_resultScreen != null) _resultScreen.SetActive(true);

            // Display boss name
            if (_bossNameText != null)
                _bossNameText.text = $"{bossName} DEFEATED!";

            // Display time
            if (_timeText != null)
            {
                int minutes = Mathf.FloorToInt(battleTime / 60f);
                int seconds = Mathf.FloorToInt(battleTime % 60f);
                _timeText.text = $"Time: {minutes:00}:{seconds:00}";
            }

            // Display artifacts
            if (_artifactsDroppedText != null)
                _artifactsDroppedText.text = $"Artifacts Dropped: {droppedArtifacts.Count}";

            DisplayDroppedArtifacts(droppedArtifacts);

            Time.timeScale = 1f;
            Debug.Log("[UIManager] Showing Result Screen");
        }

        public void ShowEquipmentScreen()
        {
            HideAllScreens();
            _currentScreen = UIScreen.Equipment;
            if (_equipmentScreen != null) _equipmentScreen.SetActive(true);

            RefreshEquipmentScreen();

            Time.timeScale = 1f;
            Debug.Log("[UIManager] Showing Equipment Screen");
        }

        public void ShowHUD(string bossName)
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

        private void HideAllScreens()
        {
            if (_startScreen != null) _startScreen.SetActive(false);
            if (_pauseScreen != null) _pauseScreen.SetActive(false);
            if (_resultScreen != null) _resultScreen.SetActive(false);
            if (_equipmentScreen != null) _equipmentScreen.SetActive(false);
            if (_hudScreen != null) _hudScreen.SetActive(false);
        }

        #endregion

        #region Button Callbacks

        private void OnStartButtonClicked()
        {
            Debug.Log("[UIManager] Start button clicked");
            
            // Start the game
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartNewGame();
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
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMenu();
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
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadNextBoss();
            }
        }

        private void OnPreviousBossButtonClicked()
        {
            Debug.Log("[UIManager] Previous boss button clicked");
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadPreviousBoss();
            }
        }

        private void OnBackToMenuButtonClicked()
        {
            Debug.Log("[UIManager] Back to menu clicked");
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMenu();
            }
        }

        #endregion

        #region HUD Updates

        public void UpdateBossHealth(float currentHealth, float maxHealth)
        {
            if (_bossHealthBar != null)
            {
                _bossHealthBar.fillAmount = maxHealth > 0 ? currentHealth / maxHealth : 0f;
            }
        }

        private void UpdateBattleTimer()
        {
            if (_timerText != null)
            {
                float elapsed = Time.time - _battleStartTime;
                int minutes = Mathf.FloorToInt(elapsed / 60f);
                int seconds = Mathf.FloorToInt(elapsed % 60f);
                _timerText.text = $"{minutes:00}:{seconds:00}";
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
            if (_artifactDisplayContainer == null) return;

            // Clear previous artifacts
            foreach (Transform child in _artifactDisplayContainer)
            {
                Destroy(child.gameObject);
            }

            // Create artifact icons
            foreach (var artifact in artifacts)
            {
                if (_artifactIconPrefab != null)
                {
                    GameObject icon = Instantiate(_artifactIconPrefab, _artifactDisplayContainer);
                    
                    // Set artifact visual
                    Image img = icon.GetComponent<Image>();
                    if (img != null && artifact.Icon != null)
                    {
                        img.sprite = artifact.Icon;
                        img.color = GetRarityColor(artifact.Rarity);
                    }

                    // Set artifact name
                    TextMeshProUGUI nameText = icon.GetComponentInChildren<TextMeshProUGUI>();
                    if (nameText != null)
                    {
                        nameText.text = artifact.ArtifactName;
                    }
                }
            }
        }

        private void RefreshEquipmentScreen()
        {
            if (EquipmentManager.Instance == null) return;

            // Display equipped artifacts
            if (_equippedArtifactsContainer != null)
            {
                // Clear
                foreach (Transform child in _equippedArtifactsContainer)
                {
                    Destroy(child.gameObject);
                }

                // Add equipped
                foreach (var artifact in EquipmentManager.Instance.EquippedArtifacts)
                {
                    CreateArtifactSlot(artifact, _equippedArtifactsContainer, true);
                }
            }

            // Display inventory artifacts
            if (_inventoryArtifactsContainer != null)
            {
                // Clear
                foreach (Transform child in _inventoryArtifactsContainer)
                {
                    Destroy(child.gameObject);
                }

                // Add inventory
                foreach (var artifact in EquipmentManager.Instance.InventoryArtifacts)
                {
                    CreateArtifactSlot(artifact, _inventoryArtifactsContainer, false);
                }
            }

            // Update stats display
            if (_equippedStatsText != null)
            {
                _equippedStatsText.text = EquipmentManager.Instance.GetEquippedStatsString();
            }
        }

        private void CreateArtifactSlot(ArtifactData artifact, Transform parent, bool isEquipped)
        {
            if (_artifactIconPrefab == null) return;

            GameObject slot = Instantiate(_artifactIconPrefab, parent);
            
            // Set visual
            Image img = slot.GetComponent<Image>();
            if (img != null)
            {
                if (artifact.Icon != null)
                {
                    img.sprite = artifact.Icon;
                }
                img.color = GetRarityColor(artifact.Rarity);
            }

            // Set name
            TextMeshProUGUI nameText = slot.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = artifact.ArtifactName;
            }

            // Add click listener to equip/unequip
            Button btn = slot.GetComponent<Button>();
            if (btn == null) btn = slot.AddComponent<Button>();
            
            btn.onClick.AddListener(() => OnArtifactClicked(artifact, isEquipped));
        }

        private void OnArtifactClicked(ArtifactData artifact, bool isCurrentlyEquipped)
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
                    return new Color(0.5f, 0f, 1f); // Purple
                case ArtifactRarity.Legendary:
                    return new Color(1f, 0.5f, 0f); // Orange
                default:
                    return Color.white;
            }
        }

        #endregion
    }
}