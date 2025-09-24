using UnityEngine;
using UnityEngine.UI;

public class UIHud : MonoBehaviour {
    public static UIHud I { get; private set; }
    [SerializeField] Slider playerHp, bossHp;
    [SerializeField] Slider combo;

    Health p, b;
    ComboMeter cmb;

    void Awake() { I = this; }

    public void Bind(Health player, Health boss) {
        p = player; b = boss;
        cmb = player ? player.GetComponent<ComboMeter>() : null;
    }

    void Update() {
        if (p) playerHp.value = p.current / p.max;
        if (b) bossHp.value   = b.current / b.max;
        if (cmb) combo.value  = cmb.value / cmb.max;
    }
}