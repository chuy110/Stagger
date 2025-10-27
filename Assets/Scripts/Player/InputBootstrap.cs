using UnityEngine;
using UnityEngine.InputSystem;

public class InputBootstrap : MonoBehaviour
{
    [Tooltip("Name of the gameplay action map inside your Actions asset.")]
    public string gameplayMapName = "Player"; // <-- this must match the Action Map name

    void Awake()
    {
        // Always unpause when entering a scene directly
        Time.timeScale = 1f;

        // Make sure we have a PlayerInput and it's enabled
        var pi = GetComponent<PlayerInput>() ?? FindFirstObjectByType<PlayerInput>();
        if (!pi) { Debug.LogWarning("InputBootstrap: No PlayerInput found."); return; }

        if (!pi.enabled) pi.enabled = true;

        // If no current map or wrong map, switch to gameplay
        if (pi.currentActionMap == null || pi.currentActionMap.name != gameplayMapName)
            pi.SwitchCurrentActionMap(gameplayMapName);

        // Optional: make sure the device pairing is active
        pi.ActivateInput();

        // Optional comfort
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
