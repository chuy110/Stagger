using Stagger.Core.Managers;
using UnityEngine;

/// <summary>
/// Simple debug script to verify Player GameObject is active and receiving updates.
/// Attach this to the Player GameObject temporarily for testing.
/// </summary>
public class PlayerDebug : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log($"[PlayerDebug] Awake called on GameObject: {gameObject.name}");
    }

    private void Start()
    {
        Debug.Log($"[PlayerDebug] Start called on GameObject: {gameObject.name}");
        
        // Check for PlayerController
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            Debug.Log("[PlayerDebug] PlayerController found!");
        }
        else
        {
            Debug.LogError("[PlayerDebug] PlayerController NOT found!");
        }
        
        // Check for InputManager
        if (InputManager.Instance != null)
        {
            Debug.Log("[PlayerDebug] InputManager found!");
        }
        else
        {
            Debug.LogError("[PlayerDebug] InputManager NOT found!");
        }
    }

    private void Update()
    {
        // Test direct input
        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("[PlayerDebug] A key pressed (old input system)");
        }
        
        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("[PlayerDebug] D key pressed (old input system)");
        }
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("[PlayerDebug] Space pressed (old input system)");
        }
    }
}