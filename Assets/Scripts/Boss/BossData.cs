using System.Collections.Generic;
using UnityEngine;

namespace Stagger.Boss
{
    /// <summary>
    /// Boss configuration data - Prototype pattern via ScriptableObject.
    /// Create instances via: Right-click > Create > Stagger > Boss Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewBoss", menuName = "Stagger/Boss Data")]
    public class BossData : ScriptableObject
    {
        [Header("Basic Info")]
        public string BossName = "Boss Name";
        [TextArea(3, 5)]
        public string Description = "Boss description";
        public GameObject BossPrefab; // Prefab with BossController
        
        [Header("Visual")]
        public Sprite BossSprite;
        public RuntimeAnimatorController AnimatorController;
        public float Scale = 1f;
        
        [Header("Stats")]
        public float MaxHealth = 1000f;
        public float EnrageThreshold = 30f; // Boss enrages at 30% HP
        
        [Header("Combat")]
        public float AttackInterval = 2f; // Time between attacks (normal)
        public float EnragedAttackInterval = 1f; // Time between attacks (enraged)
        public float StunDuration = 0.5f; // How long boss is stunned after taking damage
        
        [Header("Thread System")]
        public int ThreadCount = 3;
        public List<float> ThreadBreakThresholds = new List<float> { 75f, 50f, 25f }; // HP % when threads break
        public List<ThreadData> Threads = new List<ThreadData>();
        public AudioClip ThreadBreakSound;
        
        [Header("Attack Patterns")]
        public List<AttackPattern> AttackPatterns = new List<AttackPattern>();
        
        [Header("Loot")]
        public List<ArtifactDrop> PossibleDrops = new List<ArtifactDrop>();

        [ContextMenu("Validate Data")]
        private void ValidateData()
        {
            Debug.Log($"=== {BossName} Configuration ===");
            Debug.Log($"Max HP: {MaxHealth}");
            Debug.Log($"Threads: {ThreadCount}");
            Debug.Log($"Thresholds: {string.Join(", ", ThreadBreakThresholds)}");
            Debug.Log($"Attack Patterns: {AttackPatterns.Count}");
            Debug.Log($"Possible Drops: {PossibleDrops.Count}");
            
            // Validation
            if (BossPrefab == null)
                Debug.LogError("❌ Boss Prefab is missing!");
            
            if (ThreadBreakThresholds.Count != ThreadCount)
                Debug.LogWarning($"⚠️ Thread count ({ThreadCount}) doesn't match thresholds ({ThreadBreakThresholds.Count})");
            
            if (AttackPatterns.Count == 0)
                Debug.LogWarning("⚠️ No attack patterns configured!");
                
            Debug.Log("============================");
        }
    }

    /// <summary>
    /// Thread configuration for visual puppet strings.
    /// </summary>
    [System.Serializable]
    public class ThreadData
    {
        public string LimbName = "Limb"; // Which limb/attack this controls
        public Vector2 AttachmentPoint = Vector2.zero; // Offset from boss center where thread attaches
        public List<AttackPattern> LinkedAttacks = new List<AttackPattern>(); // Attacks disabled when thread breaks
    }

    /// <summary>
    /// Attack pattern configuration.
    /// </summary>
    [System.Serializable]
    public class AttackPattern
    {
        [Header("Identity")]
        public string PatternName = "Attack";
        public string AttackAnimation = ""; // Animation trigger name
        
        [Header("Projectile")]
        public ProjectileData ProjectileData;
        public int ProjectileCount = 1;
        
        [Header("Targeting")]
        public bool AimAtPlayer = true;
        public Vector2 FixedDirection = Vector2.down; // Used if not aiming at player
        public float SpreadAngle = 0f; // Spread for multi-shot (degrees)
        
        [Header("Timing")]
        public float ProjectileDelay = 0f; // Delay between each projectile in multi-shot
    }

    /// <summary>
    /// Projectile configuration data - could also be a ScriptableObject.
    /// </summary>
    [System.Serializable]
    public class ProjectileData
    {
        [Header("Visual")]
        public Sprite ProjectileSprite;
        public Color ProjectileColor = Color.white;
        public float Size = 1f;
        
        [Header("Movement")]
        public float Speed = 10f;
        public float Lifetime = 5f;
        
        [Header("Behavior")]
        public bool CanBeReflected = true;
        public float Damage = 10f;
        
        [Header("Effects")]
        public GameObject HitEffectPrefab;
        public AudioClip FireSound;
        public AudioClip HitSound;
    }

    /// <summary>
    /// Artifact drop configuration with drop chance.
    /// </summary>
    [System.Serializable]
    public class ArtifactDrop
    {
        [Tooltip("The artifact that can drop")]
        public Stagger.UI.ArtifactData ArtifactData;
        
        [Tooltip("Chance to drop (0.0 = never, 1.0 = always)")]
        [Range(0f, 1f)]
        public float DropChance = 0.5f;
    }

    /// <summary>
    /// Example boss creation workflow in code.
    /// This shows how to programmatically create boss data.
    /// </summary>
#if UNITY_EDITOR
    public static class BossDataCreator
    {
        [UnityEditor.MenuItem("Stagger/Create Example Boss Data")]
        public static void CreateExampleBoss()
        {
            // Create boss data asset
            BossData boss = ScriptableObject.CreateInstance<BossData>();
            
            // Configure basic info
            boss.BossName = "Puppet Master";
            boss.Description = "The first puppet boss. Simple attack patterns.";
            boss.MaxHealth = 1000f;
            boss.ThreadCount = 3;
            
            // Configure thresholds
            boss.ThreadBreakThresholds = new List<float> { 75f, 50f, 25f };
            
            // Configure threads
            boss.Threads = new List<ThreadData>
            {
                new ThreadData 
                { 
                    LimbName = "Left Arm",
                    AttachmentPoint = new Vector2(-1f, 0.5f)
                },
                new ThreadData 
                { 
                    LimbName = "Right Arm",
                    AttachmentPoint = new Vector2(1f, 0.5f)
                },
                new ThreadData 
                { 
                    LimbName = "Head",
                    AttachmentPoint = new Vector2(0f, 1f)
                }
            };
            
            // Configure a simple attack pattern
            boss.AttackPatterns = new List<AttackPattern>
            {
                new AttackPattern
                {
                    PatternName = "Single Shot",
                    ProjectileCount = 1,
                    AimAtPlayer = true,
                    ProjectileData = new ProjectileData
                    {
                        Speed = 8f,
                        Damage = 10f,
                        Size = 0.5f,
                        Lifetime = 5f,
                        CanBeReflected = true
                    }
                },
                new AttackPattern
                {
                    PatternName = "Triple Spread",
                    ProjectileCount = 3,
                    AimAtPlayer = true,
                    SpreadAngle = 30f,
                    ProjectileData = new ProjectileData
                    {
                        Speed = 10f,
                        Damage = 8f,
                        Size = 0.4f,
                        Lifetime = 5f,
                        CanBeReflected = true
                    }
                }
            };
            
            // Save asset
            string path = "Assets/Data/Bosses/PuppetMaster.asset";
            UnityEditor.AssetDatabase.CreateAsset(boss, path);
            UnityEditor.AssetDatabase.SaveAssets();
            
            Debug.Log($"✓ Created example boss data at: {path}");
            UnityEditor.Selection.activeObject = boss;
        }
    }
#endif
}