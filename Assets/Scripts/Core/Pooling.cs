using System.Collections.Generic;
using Stagger.Core.Managers;
using UnityEngine;

namespace Stagger.Core.Pooling
{
    /// <summary>
    /// Generic Object Pool implementing the Flyweight pattern.
    /// Reuses objects to reduce instantiation overhead and memory allocation.
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private T _prefab;
        private Queue<T> _availableObjects;
        private HashSet<T> _activeObjects;
        private Transform _parent;
        private int _maxSize;

        public int AvailableCount => _availableObjects.Count;
        public int ActiveCount => _activeObjects.Count;
        public int TotalCount => AvailableCount + ActiveCount;

        /// <summary>
        /// Create a new object pool.
        /// </summary>
        /// <param name="prefab">The prefab to pool</param>
        /// <param name="initialSize">Number of objects to pre-instantiate</param>
        /// <param name="maxSize">Maximum pool size (0 = unlimited)</param>
        /// <param name="parent">Parent transform for organization</param>
        public ObjectPool(T prefab, int initialSize, int maxSize = 0, Transform parent = null)
        {
            _prefab = prefab;
            _availableObjects = new Queue<T>();
            _activeObjects = new HashSet<T>();
            _parent = parent;
            _maxSize = maxSize;

            // Pre-instantiate initial pool
            for (int i = 0; i < initialSize; i++)
            {
                T obj = CreateNewObject();
                obj.gameObject.SetActive(false);
                _availableObjects.Enqueue(obj);
            }

            Debug.Log($"[ObjectPool] Created pool for {typeof(T).Name} with {initialSize} objects");
        }

        /// <summary>
        /// Get an object from the pool. Creates a new one if pool is empty.
        /// </summary>
        public T Get()
        {
            T obj;

            if (_availableObjects.Count > 0)
            {
                obj = _availableObjects.Dequeue();
            }
            else
            {
                // Pool exhausted - create new object if under max size
                if (_maxSize > 0 && TotalCount >= _maxSize)
                {
                    Debug.LogWarning($"[ObjectPool] Pool for {typeof(T).Name} at max capacity ({_maxSize})");
                    return null;
                }

                obj = CreateNewObject();
                Debug.Log($"[ObjectPool] Pool expanded for {typeof(T).Name}. New size: {TotalCount}");
            }

            _activeObjects.Add(obj);
            obj.gameObject.SetActive(true);

            // Notify pooled object it was retrieved
            if (obj is IPoolable poolable)
            {
                poolable.OnSpawnFromPool();
            }

            return obj;
        }

        /// <summary>
        /// Get an object at a specific position and rotation.
        /// </summary>
        public T Get(Vector3 position, Quaternion rotation)
        {
            T obj = Get();
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
            }
            return obj;
        }

        /// <summary>
        /// Return an object to the pool.
        /// </summary>
        public void Return(T obj)
        {
            if (obj == null)
            {
                Debug.LogWarning($"[ObjectPool] Attempted to return null object to pool");
                return;
            }

            if (!_activeObjects.Contains(obj))
            {
                Debug.LogWarning($"[ObjectPool] Attempted to return object not from this pool: {obj.name}");
                return;
            }

            _activeObjects.Remove(obj);
            obj.gameObject.SetActive(false);
            _availableObjects.Enqueue(obj);

            // Notify pooled object it was returned
            if (obj is IPoolable poolable)
            {
                poolable.OnReturnToPool();
            }
        }

        /// <summary>
        /// Return all active objects to the pool.
        /// </summary>
        public void ReturnAll()
        {
            // Create a copy since we're modifying the collection
            var activeObjectsCopy = new List<T>(_activeObjects);
            foreach (T obj in activeObjectsCopy)
            {
                Return(obj);
            }
        }

        /// <summary>
        /// Destroy all objects in the pool.
        /// </summary>
        public void Clear()
        {
            ReturnAll();

            while (_availableObjects.Count > 0)
            {
                T obj = _availableObjects.Dequeue();
                if (obj != null)
                {
                    Object.Destroy(obj.gameObject);
                }
            }

            _availableObjects.Clear();
            _activeObjects.Clear();
        }

        private T CreateNewObject()
        {
            T obj = Object.Instantiate(_prefab, _parent);
            obj.name = $"{_prefab.name} (Pooled)";
            return obj;
        }
    }

    /// <summary>
    /// Interface for objects that need to be notified when spawned/returned from pool.
    /// Implement this on pooled objects that need reset logic.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called when the object is retrieved from the pool.
        /// </summary>
        void OnSpawnFromPool();

        /// <summary>
        /// Called when the object is returned to the pool.
        /// </summary>
        void OnReturnToPool();
    }

    /// <summary>
    /// Component that can be attached to pooled objects for automatic return after lifetime.
    /// </summary>
    public class PooledObject : MonoBehaviour, IPoolable
    {
        [SerializeField] private float _lifetime = 3f;
        [SerializeField] private bool _autoReturnOnLifetime = true;

        private float _spawnTime;
        private bool _isActive;

        public float Lifetime
        {
            get => _lifetime;
            set => _lifetime = value;
        }

        private void Update()
        {
            if (_isActive && _autoReturnOnLifetime)
            {
                if (Time.time >= _spawnTime + _lifetime)
                {
                    ReturnToPool();
                }
            }
        }

        public void OnSpawnFromPool()
        {
            _spawnTime = Time.time;
            _isActive = true;
        }

        public void OnReturnToPool()
        {
            _isActive = false;
        }

        /// <summary>
        /// Manually return this object to its pool.
        /// </summary>
        public void ReturnToPool()
        {
            // Let PoolManager handle the return
            PoolManager.Instance.ReturnToPool(gameObject);
        }
    }
}