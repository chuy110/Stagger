using UnityEngine;

/// <summary>
/// ScriptableObject defining projectile properties (Prototype pattern).
/// Create instances via Assets > Create > Combat > Projectile Data
/// </summary>
[CreateAssetMenu(fileName = "New Projectile", menuName = "Stagger/Combat/Projectile Data")]
public class ProjectileData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Name of this projectile type")]
    public string ProjectileName = "Projectile";

    [Header("Visuals")]
    [Tooltip("Sprite to display")]
    public Sprite ProjectileSprite;
    
    [Tooltip("Color tint")]
    public Color ProjectileColor = Color.white;
    
    [Tooltip("Scale multiplier")]
    public float Scale = 1f;

    [Header("Movement")]
    [Tooltip("Movement speed (units per second)")]
    public float Speed = 5f;
    
    [Tooltip("Rotation speed (degrees per second, 0 = no rotation)")]
    public float RotationSpeed = 0f;
    
    [Tooltip("Movement pattern curve (optional)")]
    public AnimationCurve MovementCurve;

    [Header("Combat")]
    [Tooltip("Damage dealt to player on hit")]
    public float Damage = 10f;
    
    [Tooltip("Damage dealt to boss when reflected")]
    public float ReflectedDamage = 15f;
    
    [Tooltip("Can this projectile be parried?")]
    public bool CanBeParried = true;
    
    [Tooltip("Can this projectile be dodged through?")]
    public bool CanBeDodged = true;

    [Header("Timing")]
    [Tooltip("How long projectile stays active before auto-despawning")]
    public float Lifetime = 10f;
    
    [Tooltip("If true, requires rhythm timing (Flow Thread)")]
    public bool IsFlowThread = false;
    
    [Tooltip("Time window for rhythm dodge (Flow Threads only)")]
    public float RhythmWindow = 0.2f;

    [Header("Effects")]
    [Tooltip("Particle effect on spawn (optional)")]
    public GameObject SpawnVFX;
    
    [Tooltip("Particle effect on hit (optional)")]
    public GameObject HitVFX;
    
    [Tooltip("Particle effect on parry (optional)")]
    public GameObject ParryVFX;
    
    [Tooltip("Trail renderer prefab (optional)")]
    public TrailRenderer TrailPrefab;

    [Header("Audio")]
    [Tooltip("Sound when projectile is fired")]
    public AudioClip FireSound;
    
    [Tooltip("Sound when projectile hits")]
    public AudioClip HitSound;
    
    [Tooltip("Sound when projectile is parried")]
    public AudioClip ParrySound;

    /// <summary>
    /// Clone this projectile data (Prototype pattern).
    /// </summary>
    public ProjectileData Clone()
    {
        ProjectileData clone = CreateInstance<ProjectileData>();
        
        // Copy all fields
        clone.ProjectileName = ProjectileName;
        clone.ProjectileSprite = ProjectileSprite;
        clone.ProjectileColor = ProjectileColor;
        clone.Scale = Scale;
        clone.Speed = Speed;
        clone.RotationSpeed = RotationSpeed;
        clone.MovementCurve = MovementCurve;
        clone.Damage = Damage;
        clone.ReflectedDamage = ReflectedDamage;
        clone.CanBeParried = CanBeParried;
        clone.CanBeDodged = CanBeDodged;
        clone.Lifetime = Lifetime;
        clone.IsFlowThread = IsFlowThread;
        clone.RhythmWindow = RhythmWindow;
        clone.SpawnVFX = SpawnVFX;
        clone.HitVFX = HitVFX;
        clone.ParryVFX = ParryVFX;
        clone.TrailPrefab = TrailPrefab;
        clone.FireSound = FireSound;
        clone.HitSound = HitSound;
        clone.ParrySound = ParrySound;
        
        return clone;
    }
}