using UnityEngine;

public class ResultUI : MonoBehaviour {
    public static ResultUI I { get; private set; }
    [SerializeField] GameObject winRoot;
    [SerializeField] GameObject loseRoot;
    void Awake() { I = this; Hide(); }

    public void Show(bool win) {
        winRoot.SetActive(win);  loseRoot.SetActive(!win);
        gameObject.SetActive(true);
    }
    public void Hide() { gameObject.SetActive(false); }
    public void OnRetry() => GameLoop.I.Retry();
    public void OnBackToHub() => GameLoop.I.ReturnToHub();
}