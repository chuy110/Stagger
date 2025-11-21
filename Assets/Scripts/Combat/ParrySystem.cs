using UnityEngine;
using Stagger.Core.Events;

// FIXED: Removed references to non-existent ProjectileData properties
// Parry system handles detection of projectiles during parry window and reflection.
// Attach to Player GameObject
[RequireComponent(typeof(PlayerController))]
public class ParrySystem : MonoBehaviour
{
    [Header("Parry Detection")]
    [SerializeField] private float _parryRadius = 1.5f;
    [SerializeField] private LayerMask _projectileLayer;
    
    [Header("Reflection")]
    [SerializeField] private bool _reflectTowardBoss = true;
    [SerializeField] private Vector2 _fallbackReflectionDirection = Vector2.up;
    [SerializeField] private float _reflectionSpeedMultiplier = 1.5f;
    
    [Header("Events")]
    [SerializeField] private GameEvent _onParrySuccess;
    [SerializeField] private GameEvent _onParryFailed;
    [SerializeField] private GameEvent _onPerfectParry;
    
    [Header("Debug")]
    [SerializeField] private bool _showDebugGizmos = true;

    private PlayerController _player;
    private int _parriedThisWindow = 0;
    private GameObject _bossCache;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
    }

    private void Start()
    {
        _bossCache = GameObject.FindGameObjectWithTag("Boss");
        if (_bossCache == null)
        {
            Debug.LogWarning("[ParrySystem] Boss not found! Projectiles will reflect upward as fallback.");
        }
    }

    private void Update()
    {
        if (_player.StateMachine.CurrentState is PlayerParryingState parryState)
        {
            CheckForProjectiles(parryState);
        }
    }
    
    private void CheckForProjectiles(PlayerParryingState parryState)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _parryRadius, _projectileLayer);

        foreach (Collider2D hit in hits)
        {
            Stagger.Boss.Projectile projectile = hit.GetComponent<Stagger.Boss.Projectile>();
            if (projectile != null && projectile.gameObject.activeInHierarchy)
            {
                TryParryProjectile(projectile, parryState);
            }
        }
    }
    
    private void TryParryProjectile(Stagger.Boss.Projectile projectile, PlayerParryingState parryState)
    {
        Stagger.Boss.ProjectileData data = projectile.Data;
        if (data == null)
        {
            Debug.LogWarning("[ParrySystem] Projectile has no data!");
            return;
        }

        // REMOVED: CanBeParried check (property doesn't exist)
        // Assume all projectiles can be parried unless you add this property to ProjectileData

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
    
    private void ParryProjectile(Stagger.Boss.Projectile projectile, bool isPerfect)
    {
        _parriedThisWindow++;

        Vector2 reflectionDir;
        
        if (_reflectTowardBoss && _bossCache != null)
        {
            Vector3 projectilePos = projectile.transform.position;
            Vector3 bossPos = _bossCache.transform.position;
            reflectionDir = (bossPos - projectilePos).normalized;
            Debug.Log($"[ParrySystem] Reflecting toward boss: {reflectionDir}");
        }
        else
        {
            reflectionDir = _fallbackReflectionDirection.normalized;
            Debug.Log($"[ParrySystem] Using fallback reflection direction: {reflectionDir}");
        }

        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Stagger.Boss.ProjectileData data = projectile.Data;
            float speed = data != null ? data.Speed : 10f;
            rb.linearVelocity = reflectionDir * speed * _reflectionSpeedMultiplier;
            Debug.Log($"[ParrySystem] Projectile reflected at speed: {rb.linearVelocity.magnitude}");
        }

        if (isPerfect)
        {
            Debug.Log($"[ParrySystem] Perfect parry bonus applied!");
            // TODO: Apply perfect parry bonuses (e.g., extra damage, slow-mo, etc.)
        }

        _player.OnParrySuccess();
        
        // Change layer to prevent re-parrying
        projectile.gameObject.layer = LayerMask.NameToLayer("Default");
    }
    
    public bool CanParryNow()
    {
        return _player.StateMachine.CurrentState is PlayerParryingState parryState && 
               parryState.IsParryActive();
    }
    
    public void ResetParryCount()
    {
        if (_parriedThisWindow > 0)
        {
            Debug.Log($"[ParrySystem] Parried {_parriedThisWindow} projectile(s) this window");
        }
        _parriedThisWindow = 0;
    }

    private void OnDrawGizmos()
    {
        if (!_showDebugGizmos) return;

        Color gizmoColor = Color.green;
        
        if (_player != null && _player.StateMachine.CurrentState is PlayerParryingState parryState)
        {
            float quality = parryState.GetTimingQuality();
            if (quality > 0.8f)
                gizmoColor = Color.yellow;
            else if (quality > 0f)
                gizmoColor = Color.green;
            else
                gizmoColor = Color.red;
        }
        else
        {
            gizmoColor = Color.gray;
        }

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, _parryRadius);
        
        if (_bossCache != null && _player != null && _player.StateMachine.CurrentState is PlayerParryingState)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, _bossCache.transform.position);
        }
    }
}