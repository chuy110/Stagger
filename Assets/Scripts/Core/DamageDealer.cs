using UnityEngine;
using System.Collections;

public class DamageDealer : MonoBehaviour {
    public float baseDamage = 10f;
    public LayerMask targets; // e.g., Enemy
    static float globalMult = 1f;
    static float multTimer = 0f;

    void Update() {
        if (multTimer > 0f) multTimer -= Time.deltaTime;
        else globalMult = 1f;
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (((1 << other.gameObject.layer) & targets) == 0) return;
        var hp = other.GetComponentInParent<Health>();
        if (!hp) return;

        float comboBonus = 1f;
        var cmb = GetComponentInParent<ComboMeter>();
        if (cmb) comboBonus = cmb.DamageBonus();

        hp.Take(baseDamage * globalMult * comboBonus);
    }

    public static void SetGlobalDamageMultiplier(float mult, float sec) {
        globalMult = mult; multTimer = sec;
    }
}