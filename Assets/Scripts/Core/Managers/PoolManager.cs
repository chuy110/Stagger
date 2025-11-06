using System.Collections.Generic;
using UnityEngine;
using Stagger.Core.Pooling;

namespace Stagger.Core.Managers
{
    /// <summary>
    /// Singleton manager for all object pools in the game (Flyweight pattern).
    /// Provides centralized access to pooled objects.
    /// </summary>
    public class PoolManager : Singleton<PoolManager>
    {
        [Header("Pool Configuration")]
        [SerializeField] private Transform _poolParent;
        [SerializeField] private List<PoolConfiguration> _poolConfigurations;

        private Dictionary<string, object> _pools = new Dictionary<string, object>();

        [System.Serializable]
        public class PoolConfiguration
        {
            public string PoolKey;
            public GameObject Prefab;
            public int InitialSize = 10;
            public int MaxSize = 100;
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            if (_poolParent == null)
            {
                _poolParent = new GameObject("Pool Container").transform;
                _poolParent.SetParent(transform);
            }

            InitializePools();
        }

        /// <summary>
        /// Initialize all pre-configured pools.
        /// </summary>
        private void InitializePools()
        {
            foreach (var config in _poolConfigurations)
            {
                if (config.Prefab == null)
                {
                    Debug.LogWarning($"[PoolManager] Pool configuration has null prefab for key: {config.PoolKey}");
                    continue;
                }

                Component component = config.Prefab.GetComponent<Component>();
                if (component == null)
                {
                    Debug.LogWarning($"[PoolManager] Prefab for key '{config.PoolKey}' has no components");
                    continue;
                }

                CreatePoolInternal(config.PoolKey, component, config.InitialSize, config.MaxSize);
            }
        }

        /// <summary>
        /// Create a new pool at runtime.
        /// </summary>
        public void CreatePool<T>(string key, T prefab, int initialSize, int maxSize = 0) where T : Component
        {
            CreatePoolInternal(key, prefab, initialSize, maxSize);
        }

        private void CreatePoolInternal<T>(string key, T prefab, int initialSize, int maxSize) where T : Component
        {
            if (_pools.ContainsKey(key))
            {
                Debug.LogWarning($"[PoolManager] Pool with key '{key}' already exists");
                return;
            }

            Transform poolContainer = new GameObject($"Pool_{key}").transform;
            poolContainer.SetParent(_poolParent);

            ObjectPool<T> pool = new ObjectPool<T>(prefab, initialSize, maxSize, poolContainer);
            _pools[key] = pool;

            Debug.Log($"[PoolManager] Created pool: {key} with {initialSize} objects");
        }

        /// <summary>
        /// Get an object from a pool.
        /// </summary>
        public T Spawn<T>(string key) where T : Component
        {
            if (!_pools.ContainsKey(key))
            {
                Debug.LogError($"[PoolManager] No pool found with key: {key}");
                return null;
            }

            ObjectPool<T> pool = _pools[key] as ObjectPool<T>;
            if (pool == null)
            {
                Debug.LogError($"[PoolManager] Pool '{key}' is not of type {typeof(T).Name}");
                return null;
            }

            return pool.Get();
        }

        /// <summary>
        /// Get an object from a pool at a specific position and rotation.
        /// </summary>
        public T Spawn<T>(string key, Vector3 position, Quaternion rotation) where T : Component
        {
            if (!_pools.ContainsKey(key))
            {
                Debug.LogError($"[PoolManager] No pool found with key: {key}");
                return null;
            }

            ObjectPool<T> pool = _pools[key] as ObjectPool<T>;
            if (pool == null)
            {
                Debug.LogError($"[PoolManager] Pool '{key}' is not of type {typeof(T).Name}");
                return null;
            }

            return pool.Get(position, rotation);
        }

        /// <summary>
        /// Return an object to its pool.
        /// </summary>
        public void Despawn<T>(string key, T obj) where T : Component
        {
            if (!_pools.ContainsKey(key))
            {
                Debug.LogError($"[PoolManager] No pool found with key: {key}");
                return;
            }

            ObjectPool<T> pool = _pools[key] as ObjectPool<T>;
            if (pool == null)
            {
                Debug.LogError($"[PoolManager] Pool '{key}' is not of type {typeof(T).Name}");
                return;
            }

            pool.Return(obj);
        }

        /// <summary>
        /// Return a GameObject to its pool by searching for the appropriate component.
        /// </summary>
        public void ReturnToPool(GameObject obj)
        {
            // Try to find a PooledObject component
            PooledObject pooledObj = obj.GetComponent<PooledObject>();
            if (pooledObj == null)
            {
                Debug.LogWarning($"[PoolManager] GameObject {obj.name} does not have PooledObject component");
                return;
            }

            // Search through pools to find which one this object belongs to
            foreach (var kvp in _pools)
            {
                var poolType = kvp.Value.GetType();
                if (poolType.IsGenericType && poolType.GetGenericTypeDefinition() == typeof(ObjectPool<>))
                {
                    var method = poolType.GetMethod("Return");
                    if (method != null)
                    {
                        try
                        {
                            method.Invoke(kvp.Value, new object[] { obj.GetComponent(poolType.GetGenericArguments()[0]) });
                            return;
                        }
                        catch
                        {
                            // This pool doesn't contain this object, continue searching
                        }
                    }
                }
            }

            Debug.LogWarning($"[PoolManager] Could not find pool for GameObject: {obj.name}");
        }

        /// <summary>
        /// Return all objects in a pool.
        /// </summary>
        public void DespawnAll<T>(string key) where T : Component
        {
            if (!_pools.ContainsKey(key))
            {
                Debug.LogError($"[PoolManager] No pool found with key: {key}");
                return;
            }

            ObjectPool<T> pool = _pools[key] as ObjectPool<T>;
            pool?.ReturnAll();
        }

        /// <summary>
        /// Clear a specific pool.
        /// </summary>
        public void ClearPool(string key)
        {
            if (!_pools.ContainsKey(key))
            {
                Debug.LogError($"[PoolManager] No pool found with key: {key}");
                return;
            }

            var pool = _pools[key];
            var clearMethod = pool.GetType().GetMethod("Clear");
            clearMethod?.Invoke(pool, null);
            _pools.Remove(key);

            Debug.Log($"[PoolManager] Cleared pool: {key}");
        }

        /// <summary>
        /// Clear all pools.
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var kvp in _pools)
            {
                var pool = kvp.Value;
                var clearMethod = pool.GetType().GetMethod("Clear");
                clearMethod?.Invoke(pool, null);
            }

            _pools.Clear();
            Debug.Log("[PoolManager] Cleared all pools");
        }

        /// <summary>
        /// Get pool statistics for debugging.
        /// </summary>
        public void LogPoolStats()
        {
            Debug.Log("=== Pool Manager Statistics ===");
            foreach (var kvp in _pools)
            {
                var pool = kvp.Value;
                var availableCount = pool.GetType().GetProperty("AvailableCount")?.GetValue(pool);
                var activeCount = pool.GetType().GetProperty("ActiveCount")?.GetValue(pool);
                var totalCount = pool.GetType().GetProperty("TotalCount")?.GetValue(pool);

                Debug.Log($"Pool '{kvp.Key}': Total={totalCount}, Active={activeCount}, Available={availableCount}");
            }
            Debug.Log("===============================");
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            ClearAllPools();
        }
    }
}