using UnityEngine;
using UnityEngine.Events;
using Stagger.Core.Events;

/// <summary>
/// MonoBehaviour component that listens to a GameEvent and invokes a UnityEvent response.
/// Attach this to GameObjects that need to respond to events.
/// This is a standalone version for easier Unity Inspector usage.
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