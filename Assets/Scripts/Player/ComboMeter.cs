using UnityEngine;

public class ComboMeter : MonoBehaviour {
    public float value { get; private set; }
    public float max = 100f;
    public float gainPerSecond = 8f;
    public float decayPerSecond = 5f;

    bool recentlyHit;

    void OnEnable() {
        var hp = GetComponent<Health>();
        hp.onDamage += _ => { value = 0f; recentlyHit = true; Invoke(nameof(ClearRecentlyHit), 1.0f); };
    }
    void ClearRecentlyHit() => recentlyHit = false;

    public void OnParrySuccess() => value = Mathf.Min(max, value + 20f);

    void Update() {
        if (recentlyHit) return;
        value = Mathf.Clamp(value + gainPerSecond * Time.deltaTime - decayPerSecond * Time.deltaTime, 0, max);
    }

    public float DamageBonus() => 1f + (value / max) * 0.25f; // up to +25%
}