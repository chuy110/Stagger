using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Projectile MonoBehaviour that can be pooled (Flyweight pattern).
/// Handles movement, collision, and parrying.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private ProjectileData _data;
    
    [Header("Components")]
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private Collider2D _collider;
    
    [Header("Events - Observer Pattern")]
    [Tooltip("Raised when projectile is parried")]
    public UnityEvent OnProjectileParried;
    
    [Tooltip("Raised when projectile hits a target")]
    public UnityEvent<float> OnProjectileHit;

    // Runtime state
    private Vector2 _direction;
    private float _spawnTime;
    private bool _isReflected;
    private bool _isActive;
    private TrailRenderer _trail;

    // Properties
    public ProjectileData Data => _data;
    public bool IsReflected => _isReflected;
    public bool IsActive => _isActive;

    private void Awake()
    {
        // Get components if not assigned
        if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody2D>();
        if (_collider == null) _collider = GetComponent<Collider2D>();
        
        // Configure Rigidbody2D
        _rigidbody.gravityScale = 0f;
        _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    /// <summary>
    /// Initialize projectile with data and direction.
    /// </summary>
    public void Initialize(ProjectileData data, Vector2 direction, bool isReflected = false)
    {
        _data = data;
        _direction = direction.normalized;
        _isReflected = isReflected;
        _isActive = true;
        _spawnTime = Time.time;

        // Apply visuals
        if (_data.ProjectileSprite != null)
            _spriteRenderer.sprite = _data.ProjectileSprite;
        
        _spriteRenderer.color = _data.ProjectileColor;
        transform.localScale = Vector3.one * _data.Scale;

        // Set velocity
        _rigidbody.linearVelocity = _direction * _data.Speed;

        // Face direction of movement
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Add trail if configured
        if (_data.TrailPrefab != null && _trail == null)
        {
            _trail = Instantiate(_data.TrailPrefab, transform);
        }

        // Spawn VFX
        if (_data.SpawnVFX != null && !isReflected)
        {
            Instantiate(_data.SpawnVFX, transform.position, Quaternion.identity);
        }

        Debug.Log($"[Projectile] Initialized: {_data.ProjectileName}, Direction: {_direction}, Reflected: {_isReflected}");
    }

    private void Update()
    {
        if (!_isActive) return;

        // Check lifetime
        if (Time.time >= _spawnTime + _data.Lifetime)
        {
            Debug.Log($"[Projectile] Lifetime expired: {_data.ProjectileName}");
            ReturnToPool();
            return;
        }

        // Apply rotation
        if (_data.RotationSpeed != 0f)
        {
            transform.Rotate(0, 0, _data.RotationSpeed * Time.deltaTime);
        }

        // Apply movement curve if configured
        if (_data.MovementCurve != null && _data.MovementCurve.length > 0)
        {
            float t = (Time.time - _spawnTime) / _data.Lifetime;
            float speedMultiplier = _data.MovementCurve.Evaluate(t);
            _rigidbody.linearVelocity = _direction * _data.Speed * speedMultiplier;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isActive) return;

        // Check what we hit
        if (_isReflected)
        {
            // Reflected projectile hits boss
            if (other.CompareTag("Boss"))
            {
                OnHitBoss(other.gameObject);
            }
        }
        else
        {
            // Normal projectile hits player
            if (other.CompareTag("Player"))
            {
                OnHitPlayer(other.gameObject);
            }
        }

        // Projectiles can also hit walls/boundaries
        if (other.CompareTag("Boundary"))
        {
            Debug.Log($"[Projectile] Hit boundary, despawning");
            ReturnToPool();
        }
    }

    /// <summary>
    /// Called when projectile hits the player.
    /// </summary>
    private void OnHitPlayer(GameObject player)
    {
        // Check if player is invincible (dodging)
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null && playerController.IsInvincible)
        {
            Debug.Log($"[Projectile] Player is invincible, projectile passed through");
            return; // Projectile passes through
        }

        Debug.Log($"[Projectile] Hit player! Damage: {_data.Damage}");

        // Deal damage (Observer pattern)
        OnProjectileHit?.Invoke(_data.Damage);

        // Spawn hit VFX
        if (_data.HitVFX != null)
        {
            Instantiate(_data.HitVFX, transform.position, Quaternion.identity);
        }

        // Play hit sound
        if (_data.HitSound != null)
        {
            AudioSource.PlayClipAtPoint(_data.HitSound, transform.position);
        }

        ReturnToPool();
    }

    /// <summary>
    /// Called when reflected projectile hits the boss.
    /// </summary>
    private void OnHitBoss(GameObject boss)
    {
        Debug.Log($"[Projectile] Hit boss! Reflected damage: {_data.ReflectedDamage}");

        // Deal damage to boss - using the Boss namespace
        Stagger.Boss.BossController bossController = boss.GetComponent<Stagger.Boss.BossController>();
        if (bossController != null)
        {
            bossController.OnDamaged(_data.ReflectedDamage);
        }

        // Raise event (Observer pattern)
        OnProjectileHit?.Invoke(_data.ReflectedDamage);

        // Spawn hit VFX
        if (_data.HitVFX != null)
        {
            Instantiate(_data.HitVFX, transform.position, Quaternion.identity);
        }

        // Play hit sound
        if (_data.HitSound != null)
        {
            AudioSource.PlayClipAtPoint(_data.HitSound, transform.position);
        }

        ReturnToPool();
    }

    /// <summary>
    /// Reflect this projectile (called by ParrySystem).
    /// </summary>
    public void Reflect(Vector2 newDirection)
    {
        if (!_data.CanBeParried)
        {
            Debug.LogWarning($"[Projectile] {_data.ProjectileName} cannot be parried!");
            return;
        }

        _isReflected = true;
        _direction = newDirection.normalized;
        _rigidbody.linearVelocity = _direction * _data.Speed * 1.5f; // Reflected projectiles slightly faster

        // Face new direction
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Change color to indicate reflection
        _spriteRenderer.color = Color.yellow;

        // Spawn parry VFX
        if (_data.ParryVFX != null)
        {
            Instantiate(_data.ParryVFX, transform.position, Quaternion.identity);
        }

        // Play parry sound
        if (_data.ParrySound != null)
        {
            AudioSource.PlayClipAtPoint(_data.ParrySound, transform.position);
        }

        // Raise event (Observer pattern)
        OnProjectileParried?.Invoke();

        Debug.Log($"[Projectile] Reflected! New direction: {_direction}");
    }

    /// <summary>
    /// Destroy this projectile immediately.
    /// </summary>
    public void DestroyProjectile()
    {
        ReturnToPool();
    }

    /// <summary>
    /// Called when spawned from pool.
    /// </summary>
    public void OnSpawnFromPool()
    {
        _isActive = true;
        _isReflected = false;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Called when returned to pool.
    /// </summary>
    public void OnReturnToPool()
    {
        _isActive = false;
        _rigidbody.linearVelocity = Vector2.zero;
        
        // Destroy trail if exists
        if (_trail != null)
        {
            Destroy(_trail.gameObject);
            _trail = null;
        }
        
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Return this projectile to the pool.
    /// </summary>
    private void ReturnToPool()
    {
        if (!_isActive) return;
        
        _isActive = false;
        
        // Return via SimplePoolManager (Singleton pattern)
        if (Stagger.Boss.SimplePoolManager.Instance != null)
        {
            Stagger.Boss.SimplePoolManager.Instance.Despawn("BossProjectile", this);
        }
        else
        {
            // Fallback: just disable if pool manager doesn't exist
            gameObject.SetActive(false);
        }
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        if (!_isActive) return;

        // Draw velocity vector
        Gizmos.color = _isReflected ? Color.yellow : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(_direction * 2f));
    }
}