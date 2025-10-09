using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreen : MonoBehaviour
{
    public void ScreenLoading(string screenName)
    {
        if (!string.IsNullOrEmpty(screenName))
            SceneManager.LoadScene(screenName);
        else 
            Debug.LogWarning("No Screen Name");
    }
    
    public void QuitGame()
    {
            Debug.Log("Quitting game...");
            Time.timeScale = 1f;
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
    }
}
