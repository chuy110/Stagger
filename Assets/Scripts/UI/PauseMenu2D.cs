using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu2D : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("The panel that shows the pause menu contents (keep the root Canvas enabled).")]
    public GameObject pausePanel;   // e.g., Canvas/PauseRoot panel

    [Header("Refs (optional; auto-found if left empty)")]
    public PlayerController2D player;
    public PlayerInput playerInput; // from the new Input System

    bool isPaused;

    void Awake()
    {
        if (!player)      player      = FindFirstObjectByType<PlayerController2D>();
        if (!playerInput && player) playerInput = player.GetComponent<PlayerInput>();
        if (pausePanel) pausePanel.SetActive(false); // start hidden
        Cursor.visible = false;                      // optional for 2D
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Works even if you haven't added a "Pause" action yet.
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();
    }

    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        // If your map overlay is open, close it first so pause takes priority.
        if (MapHubUI.I != null) {
            // Expose a tiny property in MapHubUI: public bool IsOpen => _open;
            // If you didn't add that yet, this call is still safe:
            MapHubUI.I.SetOpen(false, immediate: true);
        }

        if (pausePanel) pausePanel.SetActive(true);
        Time.timeScale = 0f;               // freeze physics/animators
        isPaused = true;

        if (playerInput) playerInput.enabled = false;  // disable controls
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Resume()
    {
        if (pausePanel) pausePanel.SetActive(false);
        Time.timeScale = 1f;               // unfreeze
        isPaused = false;

        if (playerInput) playerInput.enabled = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        var pi = FindFirstObjectByType<UnityEngine.InputSystem.PlayerInput>();
        if (pi) pi.enabled = true;
    }

    // Optional button hook
    public void BackToHub()
    {
        Time.timeScale = 1f;
        isPaused = false;
        if (playerInput) playerInput.enabled = true;
        GameLoop.I?.ReturnToHub();
    }

    public void QuitGame(string screenName)
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(screenName))
            SceneManager.LoadScene(screenName);
        else 
            Debug.LogWarning("No Screen Name");
    }
}
