using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public enum GameState { Boot, Hub, Arena, Result }

public class GameLoop : MonoBehaviour {
    public static GameLoop I { get; private set; }
    public GameState State { get; private set; } = GameState.Boot;

    [Header("Scene Names")]
    [SerializeField] string hubScene = "Hub";
    [SerializeField] string arenaScene = "Arena_01";

    [Header("Refs (assigned at runtime)")]
    public PlayerController2D Player;
    public BossDummyAI Boss;

    void Awake() {
        if (I != null) { Destroy(gameObject); return; }
        I = this; DontDestroyOnLoad(gameObject);
    }

    IEnumerator Start() {
        yield return LoadSingleAsync(SceneManager.GetActiveScene().name); // ensure Boot is active
        yield return LoadAdditiveAsync(hubScene);
        State = GameState.Hub;
    }

    public void EnterArena() => StartCoroutine(EnterArenaCR());
    IEnumerator EnterArenaCR() {
        yield return UnloadIfLoaded(arenaScene);
        yield return LoadAdditiveAsync(arenaScene);
        State = GameState.Arena;

        // find refs in freshly loaded scene
        Player = FindObjectOfType<PlayerController2D>();
        Boss   = FindObjectOfType<BossDummyAI>();

        // hook win/lose
        Player.GetComponent<Health>().onDeath += OnPlayerDeath;
        Boss.GetComponent<Health>().onDeath   += OnBossDeath;

        UIHud.I?.Bind(Player?.GetComponent<Health>(), Boss?.GetComponent<Health>());
        ResultUI.I?.Hide();
    }

    public void ReturnToHub() => StartCoroutine(ReturnHubCR());
    IEnumerator ReturnHubCR() {
        yield return UnloadIfLoaded(arenaScene);
        State = GameState.Hub;
        ResultUI.I?.Hide();
    }

    void OnPlayerDeath()  => ResultUI.I?.Show(false);
    void OnBossDeath()    => ResultUI.I?.Show(true);

    public void Retry()   => StartCoroutine(EnterArenaCR());

    // --- helpers ---
    IEnumerator LoadAdditiveAsync(string n) {
        if (!IsLoaded(n)) {
            var op = SceneManager.LoadSceneAsync(n, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;
        }
    }
    IEnumerator LoadSingleAsync(string n) {
        var op = SceneManager.LoadSceneAsync(n, LoadSceneMode.Single);
        while (!op.isDone) yield return null;
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
}
