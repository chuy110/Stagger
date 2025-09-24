using UnityEngine;

public class ParryWindow : MonoBehaviour {
    public Collider2D parryCollider;   // small arc in front; layer to collide with EnemyHitbox
    public float activeTime = 0.08f;   // perfect window
    public float impactBuffSec = 2f;   // bonus damage window
    public float impactBuffMult = 1.5f;

    bool active = false;
    PlayerController2D player;
    ComboMeter combo;
    Health playerHealth;

    void Awake() {
        player = GetComponentInParent<PlayerController2D>();
        combo  = GetComponentInParent<ComboMeter>();
        playerHealth = GetComponentInParent<Health>();
        parryCollider.enabled = false;
    }

    public void TryParry() {
        if (active) return;
        StartCoroutine(ParryCR());
    }

    System.Collections.IEnumerator ParryCR() {
        active = true;
        parryCollider.enabled = true;
        yield return new WaitForSeconds(activeTime);
        parryCollider.enabled = false;
        active = false;
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (!active) return;
        if (other.gameObject.layer == LayerMask.NameToLayer("EnemyHitbox")) {
            // Successful parry: cancel enemy attack & grant buff
            var bossAtk = other.GetComponentInParent<BossAttackMarker>();
            bossAtk?.OnParried();

            // Impact buff -> increase player's damage temporarily
            DamageDealer.SetGlobalDamageMultiplier(impactBuffMult, impactBuffSec);

            // Optional: dash micro-boost on perfect timing
            // (simple: nudges velocity; fancy: set a “dash bonus” flag)
            var rb = player.GetComponent<Rigidbody2D>();
            rb.linearVelocity = new Vector2(Mathf.Sign(player.transform.localScale.x) * (player.dashSpeed * 1.25f), rb.linearVelocity.y);

            combo?.OnParrySuccess();
        }
    }
}