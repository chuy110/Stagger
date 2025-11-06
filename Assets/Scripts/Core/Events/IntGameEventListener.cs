using UnityEngine;
using UnityEngine.Events;
using Stagger.Core.Events;

/// <summary>
/// Listener for Int Game Events.
/// </summary>
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

[System.Serializable]
public class IntUnityEvent : UnityEvent<int> { }