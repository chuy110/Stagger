using UnityEngine;
using UnityEngine.Events;

namespace Stagger.Boss
{
    /// <summary>
    /// Manages boss health, damage, and death.
    /// Triggers thread break thresholds and defeat events.
    /// Implements Observer pattern through UnityEvents.
    /// </summary>
    [RequireComponent(typeof(BossController))]
    public class BossHealth : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BossData _bossData;
        
        [Header("Events - Observer Pattern")]
        [Tooltip("Raised when health value changes")]
        public UnityEvent<float> OnHealthChanged;
        
        [Tooltip("Raised when health percentage changes")]
        public UnityEvent<float> OnHealthPercentChanged;
        
        [Tooltip("Raised when boss takes damage")]
        public UnityEvent OnBossDamaged;
        
        [Tooltip("Raised when boss is defeated")]
        public UnityEvent OnBossDefeated;
        
        [Tooltip("Raised when thread break threshold is crossed")]
        public UnityEvent<int> OnThreadBreakThreshold;
        
        [Header("Debug")]
        [SerializeField] private bool _isInvulnerable = false;

        private float _currentHealth;
        private float _maxHealth;
        private int _nextThresholdIndex = 0;
        private bool _isDead = false;

        // Properties
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public float HealthPercent => _maxHealth > 0 ? (_currentHealth / _maxHealth) * 100f : 0f;
        public bool IsDead => _isDead;
        public bool IsInvulnerable => _isInvulnerable;

        public void Initialize(BossData bossData)
        {
            _bossData = bossData;
            _maxHealth = bossData.MaxHealth;
            _currentHealth = _maxHealth;
            _nextThresholdIndex = 0;
            _isDead = false;
            
            Debug.Log($"[BossHealth] Initialized: {bossData.BossName}, HP: {_currentHealth}/{_maxHealth}");
            
            // Raise initial health events
            OnHealthChanged?.Invoke(_currentHealth);
            OnHealthPercentChanged?.Invoke(HealthPercent);
        }

        /// <summary>
        /// Deal damage to the boss.
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (_isDead || _isInvulnerable)
            {
                Debug.Log($"[BossHealth] Damage blocked (Dead: {_isDead}, Invulnerable: {_isInvulnerable})");
                return;
            }

            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            
            Debug.Log($"[BossHealth] Took {damage} damage! HP: {_currentHealth}/{_maxHealth} ({HealthPercent:F1}%)");

            // Raise events (Observer pattern)
            OnHealthChanged?.Invoke(_currentHealth);
            OnHealthPercentChanged?.Invoke(HealthPercent);
            OnBossDamaged?.Invoke();

            // Check for thread break thresholds
            CheckThresholds(previousHealth);

            // Check for death
            if (_currentHealth <= 0f)
            {
                Die();
            }
        }

        /// <summary>
        /// Heal the boss (for testing or mechanics).
        /// </summary>
        public void Heal(float amount)
        {
            if (_isDead) return;

            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            
            Debug.Log($"[BossHealth] Healed {amount}! HP: {_currentHealth}/{_maxHealth}");

            OnHealthChanged?.Invoke(_currentHealth);
            OnHealthPercentChanged?.Invoke(HealthPercent);
        }

        /// <summary>
        /// Check if any thread break thresholds have been crossed.
        /// </summary>
        private void CheckThresholds(float previousHealth)
        {
            if (_bossData == null || _bossData.ThreadBreakThresholds == null) return;

            float previousPercent = (previousHealth / _maxHealth) * 100f;
            float currentPercent = HealthPercent;

            // Check if we crossed a threshold
            while (_nextThresholdIndex < _bossData.ThreadBreakThresholds.Count)
            {
                float threshold = _bossData.ThreadBreakThresholds[_nextThresholdIndex];
                
                if (previousPercent >= threshold && currentPercent < threshold)
                {
                    Debug.Log($"[BossHealth] <color=yellow>THREAD BREAK THRESHOLD!</color> {threshold}% HP reached");
                    
                    // Raise thread break event with threshold index (Observer pattern)
                    OnThreadBreakThreshold?.Invoke(_nextThresholdIndex);
                    
                    _nextThresholdIndex++;
                    
                    // Only trigger one threshold per damage instance
                    break;
                }
                else if (currentPercent >= threshold)
                {
                    // Already below this threshold, skip it
                    _nextThresholdIndex++;
                }
                else
                {
                    // Haven't reached this threshold yet
                    break;
                }
            }
        }

        /// <summary>
        /// Kill the boss.
        /// </summary>
        private void Die()
        {
            if (_isDead) return;

            _isDead = true;
            _currentHealth = 0f;

            Debug.Log($"[BossHealth] <color=red>{_bossData?.BossName ?? "Boss"} DEFEATED!</color>");

            // Raise defeat event (Observer pattern)
            OnBossDefeated?.Invoke();

            // Play defeat sound
            if (_bossData != null && _bossData.DefeatSound != null)
            {
                AudioSource.PlayClipAtPoint(_bossData.DefeatSound, transform.position);
            }
        }

        /// <summary>
        /// Set invulnerability (for cutscenes, thread breaks, etc.)
        /// </summary>
        public void SetInvulnerable(bool invulnerable)
        {
            _isInvulnerable = invulnerable;
            Debug.Log($"[BossHealth] Invulnerability: {invulnerable}");
        }

        /// <summary>
        /// Reset health to full (for testing).
        /// </summary>
        [ContextMenu("Debug: Reset Health")]
        public void ResetHealth()
        {
            if (_bossData != null)
            {
                Initialize(_bossData);
            }
        }

        /// <summary>
        /// Take damage equal to 25% of max health (for testing thresholds).
        /// </summary>
        [ContextMenu("Debug: Take 25% Damage")]
        public void DebugTakeDamage()
        {
            TakeDamage(_maxHealth * 0.25f);
        }

        // Debug visualization
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Draw health bar above boss
            Vector3 healthBarPos = transform.position + Vector3.up * 2f;
            float barWidth = 2f;
            float barHeight = 0.2f;

            // Background (gray)
            Gizmos.color = Color.gray;
            Gizmos.DrawCube(healthBarPos, new Vector3(barWidth, barHeight, 0f));

            // Foreground (health - green/yellow/red based on %)
            float healthWidth = barWidth * (HealthPercent / 100f);
            Color healthColor = HealthPercent > 50f ? Color.green : HealthPercent > 25f ? Color.yellow : Color.red;
            Gizmos.color = healthColor;
            Vector3 healthPos = healthBarPos - new Vector3((barWidth - healthWidth) * 0.5f, 0f, 0f);
            Gizmos.DrawCube(healthPos, new Vector3(healthWidth, barHeight, 0f));

#if UNITY_EDITOR
            // Draw HP text
            UnityEditor.Handles.Label(healthBarPos + Vector3.up * 0.3f, 
                $"{_currentHealth:F0}/{_maxHealth:F0} ({HealthPercent:F0}%)");
#endif
        }
    }
}