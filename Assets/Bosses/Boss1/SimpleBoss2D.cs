using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class SimpleBoss2D : MonoBehaviour {
    public Transform player;
    public float moveSpeed = 2.5f;
    public float attackRange = 1.7f;
    public float windup = 0.25f;
    public float swing = 0.15f;
    public float cooldown = 0.8f;

    public Collider2D enemyHitbox; // assign child trigger
    Rigidbody2D rb;
    bool busy;

    void Awake() {
        rb = GetComponent<Rigidbody2D>();
        if (!player) player = FindFirstObjectByType<PlayerController2D>()?.transform;
        if (enemyHitbox) enemyHitbox.enabled = false;
    }

    void Update() {
        if (!player || busy) return;
        float d = Vector2.Distance(transform.position, player.position);
        float dir = Mathf.Sign(player.position.x - transform.position.x);
        transform.localScale = new Vector3(dir, 1, 1);

        if (d > attackRange) {
            rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
        } else {
            StartCoroutine(AttackCR());
        }
    }

    IEnumerator AttackCR() {
        busy = true; rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(windup);
        if (enemyHitbox) enemyHitbox.enabled = true;
        yield return new WaitForSeconds(swing);
        if (enemyHitbox) enemyHitbox.enabled = false;
        yield return new WaitForSeconds(cooldown);
        busy = false;
    }
}
