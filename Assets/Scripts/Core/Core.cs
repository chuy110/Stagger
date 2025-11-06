using UnityEngine;

namespace Stagger.Core
{
    /// <summary>
    /// Generic Singleton pattern implementation for MonoBehaviour classes.
    /// Ensures only one instance exists and provides global access point.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance of {typeof(T)} already destroyed. Returning null.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindFirstObjectByType<T>();

                        if (_instance == null)
                        {
                            GameObject singletonObject = new GameObject($"{typeof(T).Name} (Singleton)");
                            _instance = singletonObject.AddComponent<T>();
                            DontDestroyOnLoad(singletonObject);
                            Debug.Log($"[Singleton] Created new instance of {typeof(T)}");
                        }
                    }

                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
                OnAwake();
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[Singleton] Duplicate instance of {typeof(T)} detected. Destroying duplicate.");
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _applicationIsQuitting = true;
                OnDestroyed();
            }
        }

        /// <summary>
        /// Called when the singleton is first created. Override for initialization.
        /// </summary>
        protected virtual void OnAwake() { }

        /// <summary>
        /// Called when the singleton is destroyed. Override for cleanup.
        /// </summary>
        protected virtual void OnDestroyed() { }
    }
}