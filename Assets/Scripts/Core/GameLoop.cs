using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public enum GameState { Boot, StartMenu, Hub, Arena }

public class GameLoop : MonoBehaviour
{
    public static GameLoop I { get; private set; }

    [Header("Scene Names")]
    [SerializeField] string startMenuScene = "StartMenu";
    [SerializeField] string hubScene       = "Hub";
    [SerializeField] string defaultArena   = "Stagger"; // or "Arena_001"

    public GameState State { get; private set; } = GameState.Boot;

    void Awake() {
        if (I != null) { Destroy(gameObject); return; }
        I = this; DontDestroyOnLoad(gameObject);
    }

    // === Public API (call these from UI) ===
    public void GoToStartMenu()           => StartCoroutine(LoadSingleCR(startMenuScene, GameState.StartMenu));
    public void ReturnToHub()             => StartCoroutine(LoadAdditiveCR(hubScene, GameState.Hub, unloadNonPersistent:true));
    public void EnterArena()              => StartCoroutine(LoadAdditiveCR(defaultArena, GameState.Arena, unloadNonPersistent:true));
    public void EnterArena(string name)   => StartCoroutine(LoadAdditiveCR(name, GameState.Arena, unloadNonPersistent:true));
    public void Retry()                   => StartCoroutine(RetryCR());

    // === Internals ===
    IEnumerator LoadSingleCR(string scene, GameState state) {
        // Load a single scene (menus)
        var op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
        while (!op.isDone) yield return null;
        State = state;
        RebindHUD();
    }

    IEnumerator LoadAdditiveCR(string scene, GameState state, bool unloadNonPersistent) {
        if (unloadNonPersistent) {
            // Unload everything except the persistent GameLoop scene
            for (int i = 0; i < SceneManager.sceneCount; i++) {
                var s = SceneManager.GetSceneAt(i);
                if (s.name != gameObject.scene.name)
                    yield return SceneManager.UnloadSceneAsync(s);
            }
        }
        if (!IsLoaded(scene)) {
            var op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;
        }
        State = state;
        RebindHUD();
        ResultUI.I?.Hide();
    }

    IEnumerator RetryCR() {
        // Reload the current arena (fallback to defaultArena)
        string arena = CurrentArenaName() ?? defaultArena;
        // Unload & load fresh
        if (IsLoaded(arena)) {
            var u = SceneManager.UnloadSceneAsync(arena);
            while (!u.isDone) yield return null;
        }
        var l = SceneManager.LoadSceneAsync(arena, LoadSceneMode.Additive);
        while (!l.isDone) yield return null;
        State = GameState.Arena;
        RebindHUD();
        ResultUI.I?.Hide();
    }

    void RebindHUD() {
        var player = Object.FindFirstObjectByType<PlayerController2D>();
        var boss   = Object.FindFirstObjectByType<BossDummyAI>();
        if (player) player.GetComponent<Health>().onDeath += OnPlayerDeath;
        if (boss)   boss.GetComponent<Health>().onDeath   += OnBossDeath;
        UIHud.I?.Bind(player ? player.GetComponent<Health>() : null,
                      boss   ? boss.GetComponent<Health>()   : null);
    }

    void OnPlayerDeath() => ResultUI.I?.Show(false);
    void OnBossDeath()   => ResultUI.I?.Show(true);

    bool IsLoaded(string n) {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).name == n) return true;
        return false;
    }
    string CurrentArenaName() {
        string name = null;
        for (int i = 0; i < SceneManager.sceneCount; i++) {
            var s = SceneManager.GetSceneAt(i);
            if (s.name != gameObject.scene.name && s.name != startMenuScene && s.name != hubScene)
                name = s.name;
        }
        return name;
    }
}
