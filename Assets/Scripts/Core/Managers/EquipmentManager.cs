using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Stagger.UI
{
    /// <summary>
    /// Manages player's artifact inventory and equipped artifacts.
    /// Implements Singleton pattern for global access.
    /// </summary>
    public class EquipmentManager : MonoBehaviour
    {
        private static EquipmentManager _instance;
        public static EquipmentManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<EquipmentManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("EquipmentManager");
                        _instance = go.AddComponent<EquipmentManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Equipment Slots")]
        [SerializeField] private int _maxEquippedArtifacts = 5;

        [Header("Current Equipment")]
        [SerializeField] private List<ArtifactData> _equippedArtifacts = new List<ArtifactData>();
        [SerializeField] private List<ArtifactData> _inventoryArtifacts = new List<ArtifactData>();

        // Properties
        public List<ArtifactData> EquippedArtifacts => _equippedArtifacts;
        public List<ArtifactData> InventoryArtifacts => _inventoryArtifacts;
        public int EquippedCount => _equippedArtifacts.Count;
        public int InventoryCount => _inventoryArtifacts.Count;
        public bool HasEquipmentSpace => _equippedArtifacts.Count < _maxEquippedArtifacts;

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

        /// <summary>
        /// Add a new artifact to inventory.
        /// </summary>
        public void AddArtifact(ArtifactData artifact)
        {
            if (artifact == null)
            {
                Debug.LogWarning("[EquipmentManager] Cannot add null artifact");
                return;
            }

            _inventoryArtifacts.Add(artifact);
            Debug.Log($"[EquipmentManager] Added artifact to inventory: {artifact.ArtifactName}");

            // Auto-equip if there's space
            if (HasEquipmentSpace)
            {
                EquipArtifact(artifact);
            }
        }

        /// <summary>
        /// Equip an artifact from inventory.
        /// </summary>
        public bool EquipArtifact(ArtifactData artifact)
        {
            if (artifact == null)
            {
                Debug.LogWarning("[EquipmentManager] Cannot equip null artifact");
                return false;
            }

            if (!_inventoryArtifacts.Contains(artifact))
            {
                Debug.LogWarning($"[EquipmentManager] Artifact not in inventory: {artifact.ArtifactName}");
                return false;
            }

            if (_equippedArtifacts.Count >= _maxEquippedArtifacts)
            {
                Debug.LogWarning($"[EquipmentManager] Equipment slots full ({_maxEquippedArtifacts})");
                return false;
            }

            _inventoryArtifacts.Remove(artifact);
            _equippedArtifacts.Add(artifact);
            
            Debug.Log($"[EquipmentManager] Equipped artifact: {artifact.ArtifactName}");
            return true;
        }

        /// <summary>
        /// Unequip an artifact back to inventory.
        /// </summary>
        public bool UnequipArtifact(ArtifactData artifact)
        {
            if (artifact == null)
            {
                Debug.LogWarning("[EquipmentManager] Cannot unequip null artifact");
                return false;
            }

            if (!_equippedArtifacts.Contains(artifact))
            {
                Debug.LogWarning($"[EquipmentManager] Artifact not equipped: {artifact.ArtifactName}");
                return false;
            }

            _equippedArtifacts.Remove(artifact);
            _inventoryArtifacts.Add(artifact);
            
            Debug.Log($"[EquipmentManager] Unequipped artifact: {artifact.ArtifactName}");
            return true;
        }

        /// <summary>
        /// Get total stat bonuses from equipped artifacts.
        /// </summary>
        public Dictionary<string, float> GetEquippedStats()
        {
            Dictionary<string, float> stats = new Dictionary<string, float>();

            foreach (var artifact in _equippedArtifacts)
            {
                foreach (var stat in artifact.StatBonuses)
                {
                    if (stats.ContainsKey(stat.StatName))
                    {
                        stats[stat.StatName] += stat.Value;
                    }
                    else
                    {
                        stats[stat.StatName] = stat.Value;
                    }
                }
            }

            return stats;
        }

        /// <summary>
        /// Get equipped stats as a formatted string for UI display.
        /// </summary>
        public string GetEquippedStatsString()
        {
            var stats = GetEquippedStats();
            
            if (stats.Count == 0)
            {
                return "No artifacts equipped";
            }

            string result = "Current Bonuses:\n";
            foreach (var stat in stats)
            {
                string sign = stat.Value >= 0 ? "+" : "";
                result += $"{stat.Key}: {sign}{stat.Value}\n";
            }

            return result;
        }

        /// <summary>
        /// Clear all equipment (for new game).
        /// </summary>
        public void ClearAllEquipment()
        {
            _equippedArtifacts.Clear();
            _inventoryArtifacts.Clear();
            Debug.Log("[EquipmentManager] Cleared all equipment");
        }

        /// <summary>
        /// Get artifact by name.
        /// </summary>
        public ArtifactData GetArtifactByName(string name)
        {
            var artifact = _equippedArtifacts.FirstOrDefault(a => a.ArtifactName == name);
            if (artifact != null) return artifact;

            artifact = _inventoryArtifacts.FirstOrDefault(a => a.ArtifactName == name);
            return artifact;
        }

        #region Debug

        [ContextMenu("Debug: Log Equipment")]
        private void DebugLogEquipment()
        {
            Debug.Log("=== EQUIPMENT MANAGER ===");
            Debug.Log($"Equipped ({_equippedArtifacts.Count}/{_maxEquippedArtifacts}):");
            foreach (var artifact in _equippedArtifacts)
            {
                Debug.Log($"  - {artifact.ArtifactName} ({artifact.Rarity})");
            }
            Debug.Log($"Inventory ({_inventoryArtifacts.Count}):");
            foreach (var artifact in _inventoryArtifacts)
            {
                Debug.Log($"  - {artifact.ArtifactName} ({artifact.Rarity})");
            }
            Debug.Log("========================");
        }

        [ContextMenu("Debug: Clear All")]
        private void DebugClearAll()
        {
            ClearAllEquipment();
        }

        #endregion
    }

    /// <summary>
    /// Artifact data - using ScriptableObject for Prototype pattern.
    /// </summary>
    [CreateAssetMenu(fileName = "NewArtifact", menuName = "Stagger/Artifact Data")]
    public class ArtifactData : ScriptableObject
    {
        [Header("Basic Info")]
        public string ArtifactName = "New Artifact";
        [TextArea(3, 5)]
        public string Description = "Artifact description";
        public ArtifactRarity Rarity = ArtifactRarity.Common;
        public Sprite Icon;

        [Header("Stats")]
        public List<StatBonus> StatBonuses = new List<StatBonus>();

        [Header("Special Effects")]
        [TextArea(2, 4)]
        public string SpecialEffect = "No special effect";
    }

    [System.Serializable]
    public class StatBonus
    {
        public string StatName = "Stat";
        public float Value = 0f;
    }

    public enum ArtifactRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}