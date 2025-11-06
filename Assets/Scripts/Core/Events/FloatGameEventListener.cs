using UnityEngine;
using UnityEngine.Events;
using Stagger.Core.Events;

/// <summary>
/// Listener for Float Game Events.
/// </summary>
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

[System.Serializable]
public class FloatUnityEvent : UnityEvent<float> { }