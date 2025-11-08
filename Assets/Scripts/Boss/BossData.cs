using System.Collections.Generic;
using UnityEngine;

// NOTE: ProjectileData must be in the global namespace (no namespace wrapper)
// or you need to add: using YourProjectileNamespace; if it has one

namespace Stagger.Boss
{
    /// <summary>
    /// Configuration for a single thread attached to a boss limb.
    /// </summary>
    [System.Serializable]
    public class ThreadData
    {
        [Tooltip("Name of the limb this thread is attached to")]
        public string LimbName = "Right Arm";
        
        [Tooltip("Local position offset from boss center")]
        public Vector2 AttachmentPoint = Vector2.right;
        
        [Tooltip("Is this thread currently intact?")]
        public bool IsIntact = true;
        
        [Tooltip("Which attack patterns require this thread to be intact")]
        public List<int> LinkedAttackIndices = new List<int>();
    }

    /// <summary>
    /// Defines an attack pattern for the boss.
    /// </summary>
    [System.Serializable]
    public class AttackPattern
    {
        [Tooltip("Name of this attack pattern")]
        public string PatternName = "Basic Attack";
        
        [Tooltip("Projectile data to use - ProjectileData must be accessible (global namespace or with using statement)")]
        public ProjectileData ProjectileData;
        
        [Tooltip("Number of projectiles in this pattern")]
        public int ProjectileCount = 1;
        
        [Tooltip("Spread angle for multiple projectiles")]
        public float SpreadAngle = 0f;
        
        [Tooltip("Delay between projectiles in the pattern")]
        public float ProjectileDelay = 0.1f;
        
        [Tooltip("Target to aim at (usually player)")]
        public bool AimAtPlayer = true;
        
        [Tooltip("Fixed direction if not aiming at player")]
        public Vector2 FixedDirection = Vector2.down;
        
        [Tooltip("Animation to play when using this attack")]
        public string AttackAnimation = "Boss_Attack";
    }

    /// <summary>
    /// Artifact drop configuration.
    /// </summary>
    [System.Serializable]
    public class ArtifactDrop
    {
        [Tooltip("Artifact data (will be implemented in Phase 8)")]
        public ScriptableObject ArtifactData; // Placeholder for now
        
        [Tooltip("Drop chance (0-1)")]
        [Range(0f, 1f)]
        public float DropChance = 0.5f;
    }

    /// <summary>
    /// ScriptableObject defining boss configuration (Prototype pattern).
    /// Create instances via Assets > Create > Stagger > Boss > Boss Data
    /// </summary>
    [CreateAssetMenu(fileName = "New Boss", menuName = "Stagger/Boss/Boss Data")]
    public class BossData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Name of this boss")]
        public string BossName = "Puppet Boss";
        
        [Tooltip("Description or lore")]
        [TextArea(3, 5)]
        public string Description;

        [Header("Visuals")]
        [Tooltip("Boss sprite/visual representation")]
        public Sprite BossSprite;
        
        [Tooltip("Animator controller for boss animations")]
        public RuntimeAnimatorController AnimatorController;
        
        [Tooltip("Scale of the boss")]
        public float Scale = 2f;

        [Header("Stats")]
        [Tooltip("Maximum health points")]
        public float MaxHealth = 100f;
        
        [Tooltip("Time between attacks (seconds)")]
        public float AttackInterval = 2f;
        
        [Tooltip("Time boss is stunned after taking damage")]
        public float StunDuration = 0.5f;

        [Header("Threads")]
        [Tooltip("Number of threads attached to boss limbs")]
        public int ThreadCount = 5;
        
        [Tooltip("Thread configuration for each limb")]
        public List<ThreadData> Threads = new List<ThreadData>();
        
        [Tooltip("HP percentages that trigger thread break QTE (e.g., 75%, 50%, 25%)")]
        public List<float> ThreadBreakThresholds = new List<float> { 75f, 50f, 25f };

        [Header("Attack Patterns")]
        [Tooltip("List of attack patterns this boss can use")]
        public List<AttackPattern> AttackPatterns = new List<AttackPattern>();
        
        [Tooltip("Boss becomes more aggressive when below this HP%")]
        public float EnrageThreshold = 30f;
        
        [Tooltip("Attack interval when enraged")]
        public float EnragedAttackInterval = 1f;

        [Header("Drops")]
        [Tooltip("Artifacts this boss can drop on defeat")]
        public List<ArtifactDrop> PossibleDrops = new List<ArtifactDrop>();

        [Header("Audio")]
        [Tooltip("Music that plays during this boss fight")]
        public AudioClip BossMusic;
        
        [Tooltip("Sound when boss takes damage")]
        public AudioClip HitSound;
        
        [Tooltip("Sound when thread breaks")]
        public AudioClip ThreadBreakSound;
        
        [Tooltip("Sound when boss is defeated")]
        public AudioClip DefeatSound;

        /// <summary>
        /// Clone this boss data (Prototype pattern).
        /// </summary>
        public BossData Clone()
        {
            BossData clone = CreateInstance<BossData>();
            
            clone.BossName = BossName;
            clone.Description = Description;
            clone.BossSprite = BossSprite;
            clone.AnimatorController = AnimatorController;
            clone.Scale = Scale;
            clone.MaxHealth = MaxHealth;
            clone.AttackInterval = AttackInterval;
            clone.StunDuration = StunDuration;
            clone.ThreadCount = ThreadCount;
            clone.Threads = new List<ThreadData>(Threads);
            clone.ThreadBreakThresholds = new List<float>(ThreadBreakThresholds);
            clone.AttackPatterns = new List<AttackPattern>(AttackPatterns);
            clone.EnrageThreshold = EnrageThreshold;
            clone.EnragedAttackInterval = EnragedAttackInterval;
            clone.PossibleDrops = new List<ArtifactDrop>(PossibleDrops);
            clone.BossMusic = BossMusic;
            clone.HitSound = HitSound;
            clone.ThreadBreakSound = ThreadBreakSound;
            clone.DefeatSound = DefeatSound;
            
            return clone;
        }
    }
}