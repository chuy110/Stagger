using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Stagger.Core.Events
{
    /// <summary>
    /// ScriptableObject-based Game Event for decoupled communication (Observer pattern).
    /// Create instances via Assets/Create/Events/Game Event
    /// </summary>
    [CreateAssetMenu(fileName = "New Game Event", menuName = "Stagger/Events/Game Event")]
    public class GameEvent : ScriptableObject
    {
        private List<GameEventListener> _listeners = new List<GameEventListener>();

        /// <summary>
        /// Raise the event, notifying all registered listeners.
        /// </summary>
        public void Raise()
        {
            // Iterate backwards in case listeners unregister during the event
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i].OnEventRaised();
            }
        }

        /// <summary>
        /// Register a listener to this event.
        /// </summary>
        public void RegisterListener(GameEventListener listener)
        {
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        /// <summary>
        /// Unregister a listener from this event.
        /// </summary>
        public void UnregisterListener(GameEventListener listener)
        {
            if (_listeners.Contains(listener))
            {
                _listeners.Remove(listener);
            }
        }

        /// <summary>
        /// Clear all listeners (useful for scene transitions).
        /// </summary>
        public void ClearAllListeners()
        {
            _listeners.Clear();
        }
    }

    /// <summary>
    /// MonoBehaviour component that listens to a GameEvent and invokes a UnityEvent response.
    /// Attach this to GameObjects that need to respond to events.
    /// </summary>
    public class GameEventListener : MonoBehaviour
    {
        [Tooltip("The Game Event to listen to")]
        public GameEvent Event;

        [Tooltip("Response to invoke when the event is raised")]
        public UnityEvent Response;

        private void OnEnable()
        {
            if (Event != null)
            {
                Event.RegisterListener(this);
            }
        }

        private void OnDisable()
        {
            if (Event != null)
            {
                Event.UnregisterListener(this);
            }
        }

        public void OnEventRaised()
        {
            Response?.Invoke();
        }
    }

    /// <summary>
    /// Generic typed Game Event that passes a value to listeners.
    /// </summary>
    public abstract class GenericGameEvent<T> : ScriptableObject
    {
        private List<GenericGameEventListener<T>> _listeners = new List<GenericGameEventListener<T>>();

        public void Raise(T value)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i].OnEventRaised(value);
            }
        }

        public void RegisterListener(GenericGameEventListener<T> listener)
        {
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        public void UnregisterListener(GenericGameEventListener<T> listener)
        {
            if (_listeners.Contains(listener))
            {
                _listeners.Remove(listener);
            }
        }

        public void ClearAllListeners()
        {
            _listeners.Clear();
        }
    }

    /// <summary>
    /// Generic typed listener component.
    /// </summary>
    public abstract class GenericGameEventListener<T> : MonoBehaviour
    {
        public abstract GenericGameEvent<T> Event { get; set; }
        public abstract UnityEvent<T> Response { get; set; }

        private void OnEnable()
        {
            if (Event != null)
            {
                Event.RegisterListener(this);
            }
        }

        private void OnDisable()
        {
            if (Event != null)
            {
                Event.UnregisterListener(this);
            }
        }

        public void OnEventRaised(T value)
        {
            Response?.Invoke(value);
        }
    }

    // ===== Specific Typed Events =====

    /// <summary>
    /// Float Game Event for numeric values (health, damage, etc.)
    /// </summary>
    [CreateAssetMenu(fileName = "New Float Event", menuName = "Stagger/Events/Float Event")]
    public class FloatGameEvent : GenericGameEvent<float> { }

    [System.Serializable]
    public class FloatUnityEvent : UnityEvent<float> { }

    public class FloatGameEventListener : GenericGameEventListener<float>
    {
        [SerializeField] private FloatGameEvent _event;
        [SerializeField] private FloatUnityEvent _response;

        public override GenericGameEvent<float> Event
        {
            get => _event;
            set => _event = value as FloatGameEvent;
        }

        public override UnityEvent<float> Response
        {
            get => _response;
            set => _response = value as FloatUnityEvent;
        }
    }

    /// <summary>
    /// Int Game Event for integer values (thread count, score, etc.)
    /// </summary>
    [CreateAssetMenu(fileName = "New Int Event", menuName = "Stagger/Events/Int Event")]
    public class IntGameEvent : GenericGameEvent<int> { }

    [System.Serializable]
    public class IntUnityEvent : UnityEvent<int> { }

    public class IntGameEventListener : GenericGameEventListener<int>
    {
        [SerializeField] private IntGameEvent _event;
        [SerializeField] private IntUnityEvent _response;

        public override GenericGameEvent<int> Event
        {
            get => _event;
            set => _event = value as IntGameEvent;
        }

        public override UnityEvent<int> Response
        {
            get => _response;
            set => _response = value as IntUnityEvent;
        }
    }

    /// <summary>
    /// String Game Event for text messages
    /// </summary>
    [CreateAssetMenu(fileName = "New String Event", menuName = "Stagger/Events/String Event")]
    public class StringGameEvent : GenericGameEvent<string> { }

    [System.Serializable]
    public class StringUnityEvent : UnityEvent<string> { }

    public class StringGameEventListener : GenericGameEventListener<string>
    {
        [SerializeField] private StringGameEvent _event;
        [SerializeField] private StringUnityEvent _response;

        public override GenericGameEvent<string> Event
        {
            get => _event;
            set => _event = value as StringGameEvent;
        }

        public override UnityEvent<string> Response
        {
            get => _response;
            set => _response = value as StringUnityEvent;
        }
    }

    /// <summary>
    /// Bool Game Event for toggle states
    /// </summary>
    [CreateAssetMenu(fileName = "New Bool Event", menuName = "Stagger/Events/Bool Event")]
    public class BoolGameEvent : GenericGameEvent<bool> { }

    [System.Serializable]
    public class BoolUnityEvent : UnityEvent<bool> { }

    public class BoolGameEventListener : GenericGameEventListener<bool>
    {
        [SerializeField] private BoolGameEvent _event;
        [SerializeField] private BoolUnityEvent _response;

        public override GenericGameEvent<bool> Event
        {
            get => _event;
            set => _event = value as BoolGameEvent;
        }

        public override UnityEvent<bool> Response
        {
            get => _response;
            set => _response = value as BoolUnityEvent;
        }
    }
}