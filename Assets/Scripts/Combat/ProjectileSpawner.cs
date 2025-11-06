using UnityEngine;
using Stagger.Core.Managers;

/// <summary>
/// Simple projectile spawner for testing combat.
/// Attach to an empty GameObject in the scene.
/// </summary>
public class ProjectileSpawner : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private ProjectileData _projectileData;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _target; // Usually the player
    [SerializeField] private float _spawnInterval = 2f;
    [SerializeField] private bool _autoSpawn = true;
    
    [Header("Spawn Pattern")]
    [SerializeField] private bool _aimAtTarget = true;
    [SerializeField] private Vector2 _fixedDirection = Vector2.down;
    [SerializeField] private float _spreadAngle = 0f; // Random spread in degrees
    
    [Header("Pool Settings")]
    [SerializeField] private string _poolKey = "Projectile_Default";
    [SerializeField] private int _initialPoolSize = 20;

    private float _lastSpawnTime;
    private bool _poolInitialized;

    private void Start()
    {
        // Initialize pool
        if (PoolManager.Instance != null && _projectilePrefab != null)
        {
            Projectile projComponent = _projectilePrefab.GetComponent<Projectile>();
            if (projComponent != null)
            {
                PoolManager.Instance.CreatePool(_poolKey, projComponent, _initialPoolSize);
                _poolInitialized = true;
                Debug.Log($"[ProjectileSpawner] Initialized pool: {_poolKey}");
            }
            else
            {
                Debug.LogError($"[ProjectileSpawner] Prefab does not have Projectile component!");
            }
        }

        // Find player if target not set
        if (_target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _target = player.transform;
                Debug.Log($"[ProjectileSpawner] Found player target");
            }
        }
    }

    private void Update()
    {
        if (_autoSpawn && _poolInitialized)
        {
            if (Time.time >= _lastSpawnTime + _spawnInterval)
            {
                SpawnProjectile();
                _lastSpawnTime = Time.time;
            }
        }
    }

    /// <summary>
    /// Spawn a projectile from the pool.
    /// </summary>
    [ContextMenu("Spawn Projectile")]
    public void SpawnProjectile()
    {
        if (!_poolInitialized || _projectileData == null)
        {
            Debug.LogWarning($"[ProjectileSpawner] Cannot spawn - pool not initialized or data missing");
            return;
        }

        // Get projectile from pool
        Projectile projectile = PoolManager.Instance.Spawn<Projectile>(_poolKey);
        if (projectile == null)
        {
            Debug.LogError($"[ProjectileSpawner] Failed to spawn projectile from pool");
            return;
        }

        // Position at spawner
        projectile.transform.position = transform.position;

        // Calculate direction
        Vector2 direction;
        if (_aimAtTarget && _target != null)
        {
            direction = (_target.position - transform.position).normalized;
        }
        else
        {
            direction = _fixedDirection.normalized;
        }

        // Apply spread
        if (_spreadAngle > 0f)
        {
            float randomAngle = Random.Range(-_spreadAngle, _spreadAngle);
            direction = Quaternion.Euler(0, 0, randomAngle) * direction;
        }

        // Initialize projectile
        projectile.Initialize(_projectileData, direction);

        Debug.Log($"[ProjectileSpawner] Spawned projectile toward {direction}");
    }

    /// <summary>
    /// Spawn multiple projectiles in a spread pattern.
    /// </summary>
    public void SpawnSpread(int count, float spreadAngle)
    {
        float angleStep = spreadAngle / (count - 1);
        float startAngle = -spreadAngle / 2f;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + (angleStep * i);
            
            // Get projectile from pool
            Projectile projectile = PoolManager.Instance.Spawn<Projectile>(_poolKey);
            if (projectile == null) continue;

            // Position at spawner
            projectile.transform.position = transform.position;

            // Calculate direction with angle
            Vector2 baseDirection = _aimAtTarget && _target != null 
                ? (_target.position - transform.position).normalized 
                : _fixedDirection.normalized;
            
            Vector2 direction = Quaternion.Euler(0, 0, angle) * baseDirection;

            // Initialize projectile
            projectile.Initialize(_projectileData, direction);
        }

        Debug.Log($"[ProjectileSpawner] Spawned {count} projectiles in spread pattern");
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        // Draw spawn point
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        // Draw direction
        Vector2 direction = _aimAtTarget && _target != null 
            ? (_target.position - transform.position).normalized 
            : _fixedDirection.normalized;

        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(direction * 2f));

        // Draw spread cone
        if (_spreadAngle > 0f)
        {
            Gizmos.color = Color.yellow;
            Vector2 leftDir = Quaternion.Euler(0, 0, -_spreadAngle) * direction;
            Vector2 rightDir = Quaternion.Euler(0, 0, _spreadAngle) * direction;
            
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(leftDir * 1.5f));
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(rightDir * 1.5f));
        }
    }

    [ContextMenu("Test: Spawn 5-Spread")]
    private void TestSpawnSpread()
    {
        SpawnSpread(5, 45f);
    }
}