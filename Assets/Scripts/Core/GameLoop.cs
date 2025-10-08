using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public enum GameState { Boot, Hub, Arena }

public class GameLoop : MonoBehaviour {
    public static GameLoop I { get; private set; }

    [Header("Scene Names")]
    [SerializeField] string hubScene = "Hub";
    [SerializeField] string defaultArena = "Arena_001";

    public GameState State { get; private set; } = GameState.Boot;

    void Awake() {
        if (I != null) { Destroy(gameObject); return; }
        I = this; DontDestroyOnLoad(gameObject);
    }

    // === Public API used by UI ===
    public void EnterArena()                => StartCoroutine(EnterArenaCR(defaultArena));
    public void EnterArena(string sceneName)=> StartCoroutine(EnterArenaCR(sceneName));
    public void ReturnToHub()               => StartCoroutine(ReturnHubCR());
    public void Retry()                     => StartCoroutine(DoRetryCR());

    // === Internals ===
    IEnumerator DoRetryCR() {
        // Reload current arena (or default if none)
        var target = State == GameState.Arena ? GetActiveArenaName() : defaultArena;
        // Unload and load fresh
        yield return UnloadIfLoaded(target);
        yield return LoadAdditiveAsync(target);
        RebindRefs();
        ResultUI.I?.Hide();
    }

    IEnumerator EnterArenaCR(string sceneName) {
        // Ensure hub is loaded if going to hub, otherwise just load target
        if (sceneName == hubScene) {
            if (!IsLoaded(hubScene)) yield return LoadAdditiveAsync(hubScene);
            State = GameState.Hub;
            RebindRefs();
            ResultUI.I?.Hide();
            yield break;
        }
        // Load arena additively
        if (IsLoaded(sceneName)) yield return UnloadIfLoaded(sceneName);
        yield return LoadAdditiveAsync(sceneName);
        State = GameState.Arena;
        RebindRefs();
        ResultUI.I?.Hide();
    }

    IEnumerator ReturnHubCR() {
        // Unload all non-hub additive scenes
        for (int i = 0; i < SceneManager.sceneCount; i++) {
            var s = SceneManager.GetSceneAt(i);
            if (s.name != hubScene && s.name != gameObject.scene.name) {
                yield return SceneManager.UnloadSceneAsync(s);
            }
        }
        if (!IsLoaded(hubScene)) yield return LoadAdditiveAsync(hubScene);
        State = GameState.Hub;
        RebindRefs();
        ResultUI.I?.Hide();
    }

    void RebindRefs() {
        var player = Object.FindFirstObjectByType<PlayerController2D>();
        var boss   = Object.FindFirstObjectByType<BossDummyAI>();

        if (player) player.GetComponent<Health>().onDeath += OnPlayerDeath;
        if (boss)   boss.GetComponent<Health>().onDeath   += OnBossDeath;

        UIHud.I?.Bind(player ? player.GetComponent<Health>() : null,
                      boss   ? boss.GetComponent<Health>()   : null);
    }

    void OnPlayerDeath() => ResultUI.I?.Show(false);
    void OnBossDeath()   => ResultUI.I?.Show(true);

    // Helpers
    IEnumerator LoadAdditiveAsync(string n) {
        if (!IsLoaded(n)) {
            var op = SceneManager.LoadSceneAsync(n, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;
        }
    }
    IEnumerator UnloadIfLoaded(string n) {
        if (IsLoaded(n)) {
            var op = SceneManager.UnloadSceneAsync(n);
            while (op != null && !op.isDone) yield return null;
        }
    }
    bool IsLoaded(string n) {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).name == n) return true;
        return false;
    }
    string GetActiveArenaName() {
        // naive: return the last loaded non-hub scene
        string name = defaultArena;
        for (int i = 0; i < SceneManager.sceneCount; i++) {
            var s = SceneManager.GetSceneAt(i);
            if (s.name != hubScene && s.name != gameObject.scene.name) name = s.name;
        }
        return name;
    }
}
