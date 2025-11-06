using UnityEngine;
using Stagger.Core.Managers;
using Stagger.Core.Events;

public class TestSetup : MonoBehaviour
{
    [Header("Test Events")]
    [SerializeField] private GameEvent testEvent;

    private void Start()
    {
        Debug.Log("=== Stagger Test Setup ===");
        
        // Test Singleton Access
        if (GameManager.Instance != null)
            Debug.Log("✓ GameManager initialized");
        else
            Debug.LogError("✗ GameManager missing");

        if (InputManager.Instance != null)
            Debug.Log("✓ InputManager initialized");
        else
            Debug.LogError("✗ InputManager missing");

        if (PoolManager.Instance != null)
            Debug.Log("✓ PoolManager initialized");
        else
            Debug.LogError("✗ PoolManager missing");
    }

    [ContextMenu("Test Event System")]
    public void TestEventSystem()
    {
        if (testEvent != null)
        {
            Debug.Log("Raising test event...");
            testEvent.Raise();
        }
        else
        {
            Debug.LogWarning("Test event not assigned");
        }
    }
}