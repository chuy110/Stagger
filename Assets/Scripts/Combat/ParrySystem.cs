using UnityEngine;
using Stagger.Core.Events;

// Parry system handles detection of projectiles during parry window and reflection.
// Attach to Player GameObject
[RequireComponent(typeof(PlayerController))]
public class ParrySystem : MonoBehaviour
{
    [Header("Parry Detection")]
    [SerializeField] private float _parryRadius = 1.5f;
    [SerializeField] private LayerMask _projectileLayer;
    
    [Header("Reflection")]
    [SerializeField] private bool _reflectTowardBoss = true; // Reflect toward boss (recommended)
    [SerializeField] private Vector2 _fallbackReflectionDirection = Vector2.up; // Fallback if boss not found
    
    [Header("Events")]
    [SerializeField] private GameEvent _onParrySuccess;
    [SerializeField] private GameEvent _onParryFailed;
    [SerializeField] private GameEvent _onPerfectParry;
    
    [Header("Debug")]
    [SerializeField] private bool _showDebugGizmos = true;

    private PlayerController _player;
    private int _parriedThisWindow = 0;
    private GameObject _bossCache; // Cache boss reference for performance

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
    }

    private void Start()
    {
        // Cache boss reference
        _bossCache = GameObject.FindGameObjectWithTag("Boss");
        if (_bossCache == null)
        {
            Debug.LogWarning("[ParrySystem] Boss not found! Projectiles will reflect upward as fallback.");
        }
    }

    private void Update()
    {
        // Only check for parries when in parrying state
        if (_player.StateMachine.CurrentState is PlayerParryingState parryState)
        {
            CheckForProjectiles(parryState);
        }
    }
    
    // Check for nearby projectiles to parry
    private void CheckForProjectiles(PlayerParryingState parryState)
    {
        // Find all colliders in parry radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _parryRadius, _projectileLayer);

        foreach (Collider2D hit in hits)
        {
            // Check if it's a projectile
            Projectile projectile = hit.GetComponent<Projectile>();
            if (projectile != null && projectile.IsActive && !projectile.IsReflected)
            {
                // Attempt to parry this projectile
                TryParryProjectile(projectile, parryState);
            }
        }
    }
    
    // Attempt to parry a projectile based on timing
    private void TryParryProjectile(Projectile projectile, PlayerParryingState parryState)
    {
        if (!projectile.Data.CanBeParried)
        {
            Debug.Log($"[ParrySystem] Projectile {projectile.Data.ProjectileName} cannot be parried!");
            return;
        }

        // Check timing quality
        float timingQuality = parryState.GetTimingQuality();

        if (timingQuality > 0.8f)
        {
            // Perfect parry!
            Debug.Log($"[ParrySystem] <color=yellow>PERFECT PARRY!</color> Quality: {timingQuality:F2}");
            ParryProjectile(projectile, true);
            _onPerfectParry?.Raise();
        }
        else if (timingQuality > 0f)
        {
            // Normal parry
            Debug.Log($"[ParrySystem] Parry success! Quality: {timingQuality:F2}");
            ParryProjectile(projectile, false);
            _onParrySuccess?.Raise();
        }
        else
        {
            // Failed parry (too late)
            Debug.Log($"[ParrySystem] Parry failed - too late!");
            _onParryFailed?.Raise();
        }
    }
    
    // Parry (reflect) a projectile
    private void ParryProjectile(Projectile projectile, bool isPerfect)
    {
        _parriedThisWindow++;

        // Calculate reflection direction
        Vector2 reflectionDir;
        
        if (_reflectTowardBoss && _bossCache != null)
        {
            // Calculate direction from projectile to boss
            Vector3 projectilePos = projectile.transform.position;
            Vector3 bossPos = _bossCache.transform.position;
            
            reflectionDir = (bossPos - projectilePos).normalized;
            
            Debug.Log($"[ParrySystem] Reflecting toward boss: {reflectionDir}");
        }
        else
        {
            // Use fallback direction (straight up)
            reflectionDir = _fallbackReflectionDirection.normalized;
            
            Debug.Log($"[ParrySystem] Using fallback reflection direction: {reflectionDir}");
        }

        // Reflect the projectile
        projectile.Reflect(reflectionDir);

        // Perfect parry bonus (could add damage multiplier, etc.)
        if (isPerfect)
        {
            Debug.Log($"[ParrySystem] Perfect parry bonus applied!");
            // TODO: Apply perfect parry bonuses (e.g., extra damage, slow-mo, etc.)
        }

        // Call player's parry success callback
        _player.OnParrySuccess();
    }
    
    // Public method to check if a projectile can be parried right now
    public bool CanParryNow()
    {
        return _player.StateMachine.CurrentState is PlayerParryingState parryState && 
               parryState.IsParryActive();
    }
    
    // Reset parry counter (called when parry state ends)
    public void ResetParryCount()
    {
        if (_parriedThisWindow > 0)
        {
            Debug.Log($"[ParrySystem] Parried {_parriedThisWindow} projectile(s) this window");
        }
        _parriedThisWindow = 0;
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        if (!_showDebugGizmos) return;

        // Draw parry detection radius
        Color gizmoColor = Color.green;
        
        if (_player != null && _player.StateMachine.CurrentState is PlayerParryingState parryState)
        {
            // Change color based on timing
            float quality = parryState.GetTimingQuality();
            if (quality > 0.8f)
                gizmoColor = Color.yellow; // Perfect window
            else if (quality > 0f)
                gizmoColor = Color.green;  // Normal window
            else
                gizmoColor = Color.red;    // Missed window
        }
        else
        {
            gizmoColor = Color.gray; // Not in parry state
        }

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, _parryRadius);
        
        // Draw line to boss (if in parry state and boss exists)
        if (_bossCache != null && _player != null && _player.StateMachine.CurrentState is PlayerParryingState)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, _bossCache.transform.position);
        }
    }
}