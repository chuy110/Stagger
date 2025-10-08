using UnityEngine;
public class ResultUI : MonoBehaviour {
    public static ResultUI I { get; private set; }
    [SerializeField] GameObject winRoot;
    [SerializeField] GameObject loseRoot;

    void Awake() { I = this; Hide(); }

    public void Show(bool win) {
        gameObject.SetActive(true);
        winRoot.SetActive(win);
        loseRoot.SetActive(!win);
    }
    public void Hide() {
        winRoot?.SetActive(false);
        loseRoot?.SetActive(false);
        gameObject.SetActive(true); // keep root active so Awake runs
    }

    // Button hooks
    public void OnRetry()      => GameLoop.I?.Retry();
    public void OnBackToHub()  => GameLoop.I?.ReturnToHub();
}