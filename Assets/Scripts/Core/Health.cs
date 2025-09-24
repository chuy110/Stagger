using UnityEngine;
using System;

public class Health : MonoBehaviour {
    public float max = 100f;
    public float current = 100f;
    public event Action<float> onDamage;
    public event Action onDeath;

    void Awake() { current = max; }

    public void Take(float amt) {
        if (current <= 0) return;
        current = Mathf.Max(0, current - amt);
        onDamage?.Invoke(amt);
        if (current <= 0) onDeath?.Invoke();
    }
    public void Heal(float amt) { current = Mathf.Min(max, current + amt); }
}