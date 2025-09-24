using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class BossDummyAI : MonoBehaviour {
    public Transform player;
    public float moveSpeed = 3.5f;
    public float lightRange = 2.3f;
    public float heavyRange = 3.6f;

    public DamageDealer lightHitbox; // Collider on "EnemyHitbox"
    public DamageDealer heavyHitbox;
    public BossAttackMarker lightMarker;
    public BossAttackMarker heavyMarker;

    Animator anim;
    Rigidbody2D rb;
    bool busy;

    void Awake() {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        lightHitbox.enabled = false; heavyHitbox.enabled = false;

        lightMarker.onParried += () => StopAllCoroutines();
        heavyMarker.onParried += () => StopAllCoroutines();
    }

    void Update() {
        if (!player || busy) return;

        float d = Vector2.Distance(transform.position, player.position);
        float dir = Mathf.Sign(player.position.x - transform.position.x);
        transform.localScale = new Vector3(dir, 1, 1);

        if (d > heavyRange) {
            rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
            anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        } else if (d > lightRange) {
            StartCoroutine(HeavyCR());
        } else {
            StartCoroutine(LightCR());
        }
    }

    IEnumerator LightCR() {
        busy = true; rb.linearVelocity = Vector2.zero;
        anim.SetTrigger("Light");
        lightHitbox.enabled = true;
        yield return new WaitForSeconds(0.15f);
        lightHitbox.enabled = false;
        yield return new WaitForSeconds(0.5f);
        busy = false;
    }

    IEnumerator HeavyCR() {
        busy = true; rb.linearVelocity = Vector2.zero;
        anim.SetTrigger("Heavy");
        yield return new WaitForSeconds(0.25f);
        heavyHitbox.enabled = true;
        yield return new WaitForSeconds(0.25f);
        heavyHitbox.enabled = false;
        yield return new WaitForSeconds(1.0f);
        busy = false;
    }
}
