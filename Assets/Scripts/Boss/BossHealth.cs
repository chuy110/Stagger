using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace Stagger.Boss
{
    /// <summary>
    /// Manages boss health, damage, and death.
    /// CRASH FIX: Uses deferred event invocation to prevent Unity crashes during physics callbacks
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
        private bool _allThreadsBroken = false;
        private bool _deathTriggered = false;

        // Properties
        public BossData Data => _bossData;
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
            _allThreadsBroken = false;
            _deathTriggered = false;
            _isInvulnerable = false;
            
            // Re-enable component for respawn scenarios
            enabled = true;
            
            Debug.Log($"[BossHealth] Initialized: {bossData.BossName}, HP: {_currentHealth}/{_maxHealth}");
            
            OnHealthChanged?.Invoke(_currentHealth);
            OnHealthPercentChanged?.Invoke(HealthPercent);
        }

        public void TakeDamage(float damage)
        {
            if (_isDead)
            {
                Debug.Log("[BossHealth] Already dead, ignoring damage");
                return;
            }

            if (_isInvulnerable && damage < _maxHealth)
            {
                Debug.Log($"[BossHealth] Damage blocked - Boss is invulnerable");
                return;
            }

            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            
            Debug.Log($"[BossHealth] Took {damage} damage! HP: {_currentHealth}/{_maxHealth} ({HealthPercent:F1}%)");

            OnHealthChanged?.Invoke(_currentHealth);
            OnHealthPercentChanged?.Invoke(HealthPercent);
            OnBossDamaged?.Invoke();

            if (!_allThreadsBroken)
            {
                CheckThresholds(previousHealth);
            }

            if (_currentHealth <= 0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (_isDead) return;

            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            
            Debug.Log($"[BossHealth] Healed {amount}! HP: {_currentHealth}/{_maxHealth}");

            OnHealthChanged?.Invoke(_currentHealth);
            OnHealthPercentChanged?.Invoke(HealthPercent);
        }

        private void CheckThresholds(float previousHealth)
        {
            if (_bossData == null || _bossData.ThreadBreakThresholds == null) 
            {
                Debug.LogWarning("[BossHealth] No thresholds configured!");
                return;
            }

            float previousPercent = (previousHealth / _maxHealth) * 100f;
            float currentPercent = HealthPercent;

            if (_nextThresholdIndex >= _bossData.ThreadBreakThresholds.Count)
            {
                return;
            }

            float threshold = _bossData.ThreadBreakThresholds[_nextThresholdIndex];
            
            if (previousPercent >= threshold && currentPercent < threshold)
            {
                Debug.Log($"[BossHealth] <color=yellow>★★★ THREAD BREAK THRESHOLD! ★★★</color> {threshold}% HP reached");
                
                SetInvulnerable(true);
                Debug.Log("[BossHealth] Boss is now INVULNERABLE until thread breaks!");
                
                if (OnThreadBreakThreshold != null)
                {
                    OnThreadBreakThreshold.Invoke(_nextThresholdIndex);
                }
                
                _nextThresholdIndex++;
            }
        }

        /// <summary>
        /// Kill the boss.
        /// CRASH FIX: Defers event invocation to avoid physics callback issues
        /// </summary>
        private void Die()
        {
            Debug.Log("═══════════════════════════════════════════");
            Debug.Log($"[BossHealth] Die() called - Death triggered: {_deathTriggered}");
            
            if (_deathTriggered)
            {
                Debug.LogWarning("[BossHealth] ⚠ Death already triggered - BLOCKING duplicate call");
                return;
            }
            
            _deathTriggered = true;
            _isDead = true;
            _currentHealth = 0f;
            _isInvulnerable = false;

            Debug.Log($"[BossHealth] ✓ Boss marked as defeated");
            
            // CRASH FIX: Start coroutine BEFORE disabling component
            Debug.Log($"[BossHealth] Starting deferred death invocation coroutine...");
            StartCoroutine(DeferredDeathEvent());
            
            Debug.Log("═══════════════════════════════════════════");
        }

        /// <summary>
        /// CRASH FIX: Deferred death event invocation.
        /// Invokes OnBossDefeated on next frame to avoid physics callback issues.
        /// </summary>
        private IEnumerator DeferredDeathEvent()
        {
            Debug.Log("[BossHealth] Waiting for end of frame...");
            
            yield return new WaitForEndOfFrame();
            
            Debug.Log($"[BossHealth] End of frame reached, invoking OnBossDefeated event...");
            
            if (OnBossDefeated != null)
            {
                int listenerCount = OnBossDefeated.GetPersistentEventCount();
                Debug.Log($"[BossHealth] OnBossDefeated has {listenerCount} persistent listeners");
                
                try
                {
                    OnBossDefeated.Invoke();
                    Debug.Log($"[BossHealth] ✓ OnBossDefeated invoked successfully");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[BossHealth] ✗ Error invoking OnBossDefeated: {e.Message}");
                    Debug.LogError($"[BossHealth] Stack trace: {e.StackTrace}");
                }
            }
            else
            {
                Debug.LogWarning($"[BossHealth] ⚠ OnBossDefeated is NULL - no listeners");
            }
            
            enabled = false;
            Debug.Log($"[BossHealth] ✓ Component disabled");
            
            Debug.Log("[BossHealth] ✓✓✓ Deferred death event COMPLETE ✓✓✓");
        }

        public void SetInvulnerable(bool invulnerable)
        {
            if (_isDead && invulnerable)
            {
                Debug.Log("[BossHealth] Cannot make dead boss invulnerable");
                return;
            }
            
            _isInvulnerable = invulnerable;
            Debug.Log($"[BossHealth] Invulnerability: {invulnerable}");
        }

        public void OnAllThreadsBroken()
        {
            _allThreadsBroken = true;
            SetInvulnerable(true);
            Debug.Log("[BossHealth] All threads broken - boss is now invulnerable until execution!");
        }

        public void ExecutionKill()
        {
            Debug.Log("[BossHealth] EXECUTION KILL!");
            _isInvulnerable = false;
            TakeDamage(_maxHealth * 10f);
        }

        [ContextMenu("Debug: Reset Health")]
        public void ResetHealth()
        {
            if (_bossData != null)
            {
                Initialize(_bossData);
            }
        }

        [ContextMenu("Debug: Take 25% Damage")]
        public void DebugTakeDamage()
        {
            TakeDamage(_maxHealth * 0.25f);
        }

        [ContextMenu("Debug: Check Invulnerability")]
        private void DebugCheckInvulnerability()
        {
            Debug.Log($"[BossHealth] Invulnerable: {_isInvulnerable}, Dead: {_isDead}, HP: {_currentHealth}/{_maxHealth}");
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            Vector3 healthBarPos = transform.position + Vector3.up * 2f;
            float barWidth = 2f;
            float barHeight = 0.2f;

            Gizmos.color = Color.gray;
            Gizmos.DrawCube(healthBarPos, new Vector3(barWidth, barHeight, 0f));

            float healthWidth = barWidth * (HealthPercent / 100f);
            Color healthColor = HealthPercent > 50f ? Color.green : HealthPercent > 25f ? Color.yellow : Color.red;
            Gizmos.color = healthColor;
            Vector3 healthPos = healthBarPos - new Vector3((barWidth - healthWidth) * 0.5f, 0f, 0f);
            Gizmos.DrawCube(healthPos, new Vector3(healthWidth, barHeight, 0f));

#if UNITY_EDITOR
            UnityEditor.Handles.Label(healthBarPos + Vector3.up * 0.3f, 
                $"{_currentHealth:F0}/{_maxHealth:F0} ({HealthPercent:F0}%)");
#endif
        }
    }
}