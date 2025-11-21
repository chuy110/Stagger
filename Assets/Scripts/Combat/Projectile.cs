using UnityEngine;
using Stagger.Core.Managers;

namespace Stagger.Boss
{
    /// <summary>
    /// FIXED VERSION - Addresses projectile visibility issues
    /// Ensures projectiles are visible in Game view, not just Scene view
    /// Adds proper pause handling
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class Projectile : MonoBehaviour
    {
        private ProjectileData _data;
        private Vector2 _direction;
        private float _spawnTime;
        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private CircleCollider2D _collider;
        private string _projectilePoolKey = "BossProjectile";
        private bool _isInitialized = false;

        // Public property to access projectile data
        public ProjectileData Data => _data;

        private void Awake()
        {
            // Ensure components exist
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null)
            {
                _rb = gameObject.AddComponent<Rigidbody2D>();
            }
            
            // Configure Rigidbody2D
            _rb.gravityScale = 0f;
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
            
            // CRITICAL FIX: Ensure sprite renderer is properly configured
            _spriteRenderer.sortingLayerName = "Default"; // Or your projectile layer
            _spriteRenderer.sortingOrder = 10; // Above most other sprites
            
            _collider = GetComponent<CircleCollider2D>();
            if (_collider == null)
            {
                _collider = gameObject.AddComponent<CircleCollider2D>();
            }
            _collider.isTrigger = true;
            _collider.radius = 0.2f; // Adjust as needed
            
            Debug.Log($"[Projectile] Awake - Components initialized");
        }

        /// <summary>
        /// Initialize the projectile with data and direction.
        /// </summary>
        public void Initialize(ProjectileData projectileData, Vector2 direction)
        {
            _data = projectileData;
            _direction = direction.normalized;
            _spawnTime = Time.time;
            _isInitialized = true;
            
            // CRITICAL FIX: Apply visuals immediately and ensure they're visible
            if (_spriteRenderer != null && projectileData.ProjectileSprite != null)
            {
                _spriteRenderer.sprite = projectileData.ProjectileSprite;
                _spriteRenderer.color = projectileData.ProjectileColor;
                _spriteRenderer.enabled = true; // Ensure it's enabled
                
                Debug.Log($"[Projectile] Sprite set: {projectileData.ProjectileSprite.name}, Color: {projectileData.ProjectileColor}");
            }
            else
            {
                Debug.LogWarning($"[Projectile] Missing sprite renderer or sprite data!");
                
                // Fallback: Create a simple colored sprite
                if (_spriteRenderer != null && projectileData.ProjectileSprite == null)
                {
                    // Create a simple white square as fallback
                    _spriteRenderer.color = projectileData.ProjectileColor;
                    Debug.LogWarning("[Projectile] Using color-only rendering (no sprite assigned)");
                }
            }
            
            // Apply scale
            transform.localScale = Vector3.one * projectileData.Size;
            
            // CRITICAL FIX: Ensure collider size matches sprite
            if (_collider != null)
            {
                _collider.radius = 0.2f * projectileData.Size;
            }
            
            // Apply velocity
            if (_rb != null)
            {
                _rb.linearVelocity = _direction * projectileData.Speed;
                Debug.Log($"[Projectile] Velocity set: {_rb.linearVelocity}, Speed: {projectileData.Speed}");
            }
            
            // Set layer for proper collision
            gameObject.layer = LayerMask.NameToLayer("Default"); // Or your projectile layer
            
            Debug.Log($"[Projectile] Initialized at {transform.position} moving {direction} at speed {projectileData.Speed}");
        }

        public void OnSpawnFromPool()
        {
            _spawnTime = Time.time;
            _isInitialized = false;
            
            // Reset visibility
            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = true;
            }
            
            // Reset rigidbody
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.simulated = true;
            }
            
            Debug.Log($"[Projectile] Spawned from pool at {transform.position}");
        }

        public void OnReturnToPool()
        {
            _isInitialized = false;
            
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.simulated = false;
            }
            
            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = false;
            }
            
            Debug.Log($"[Projectile] Returned to pool");
        }

        private void Update()
        {
            if (!_isInitialized) return;
            
            // Check if game is paused
            if (GameManager.Instance != null && 
                GameManager.Instance.CurrentState != GameManager.GameState.Combat)
            {
                // Freeze projectile during non-combat states
                if (_rb != null)
                {
                    _rb.linearVelocity = Vector2.zero;
                }
                return;
            }
            
            // Check lifetime
            if (_data != null && Time.time - _spawnTime >= _data.Lifetime)
            {
                Debug.Log($"[Projectile] Lifetime expired ({_data.Lifetime}s), returning to pool");
                ReturnToPool();
            }
            
            // Optional: Rotate projectile to face direction
            if (_direction != Vector2.zero)
            {
                float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log($"[Projectile] Collision with {other.gameObject.name} (tag: {other.tag})");
            
            // Handle collisions
            if (other.CompareTag("Player"))
            {
                Debug.Log("[Projectile] Hit player!");
                // TODO: Check if player is parrying
                // If not, damage player
                // If yes, reflect projectile
                
                // For now, just return to pool on hit
                ReturnToPool();
            }
            else if (other.CompareTag("Boss"))
            {
                Debug.Log("[Projectile] Hit boss (reflected)!");
                
                // Apply damage to boss
                var bossController = other.GetComponent<BossController>();
                if (bossController != null && _data != null)
                {
                    bossController.OnDamaged(_data.ReflectedDamage);
                }
                
                ReturnToPool();
            }
            else if (other.CompareTag("Wall") || other.CompareTag("Boundary"))
            {
                Debug.Log("[Projectile] Hit wall/boundary!");
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            if (PoolManager.Instance != null)
            {
                Debug.Log($"[Projectile] Returning to pool: {_projectilePoolKey}");
                PoolManager.Instance.Despawn(_projectilePoolKey, this);
            }
            else
            {
                // Fallback if PoolManager is destroyed
                Debug.LogWarning("[Projectile] PoolManager not found, deactivating");
                gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Set which pool this projectile belongs to (for returning).
        /// </summary>
        public void SetPoolKey(string key)
        {
            _projectilePoolKey = key;
        }
        
        private void OnDrawGizmos()
        {
            // Debug visualization
            if (_isInitialized && _data != null)
            {
                Gizmos.color = _data.ProjectileColor;
                Gizmos.DrawWireSphere(transform.position, 0.2f * _data.Size);
                
                // Draw velocity direction
                Gizmos.color = Color.yellow;
                if (_rb != null)
                {
                    Gizmos.DrawLine(transform.position, transform.position + (Vector3)_rb.linearVelocity.normalized * 0.5f);
                }
            }
        }
    }
}