using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class PlayerController2D : MonoBehaviour {
    [Header("Move")]
    public float moveSpeed = 8f;
    public float jumpForce = 13f;
    public Transform feet;
    public LayerMask groundMask;
    public float feetRadius = 0.1f;

    [Header("Combat")]
    public DamageDealer attackHitbox; // child with collider set to "PlayerHitbox"
    public float attackCooldown = 0.35f;

    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashTime = 0.12f;
    public float dashCooldown = 0.5f;

    [Header("Parry")]
    public ParryWindow parry; // child component enabling parry frames

    Rigidbody2D rb;
    Animator anim;
    Vector2 moveInput;
    bool canAttack = true, canDash = true, isDashing = false;
    float facing = 1f;

    void Awake() {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        attackHitbox.enabled = false;
    }

    void Update() {
        // Horizontal
        if (!isDashing) rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        // Flip by velocity or input
        if (moveInput.x != 0) {
            facing = Mathf.Sign(moveInput.x);
            transform.localScale = new Vector3(facing, 1, 1);
        }

        // Anim
        anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        anim.SetBool("Grounded", IsGrounded());
    }

    bool IsGrounded() => Physics2D.OverlapCircle(feet.position, feetRadius, groundMask);

    // --- Input System callbacks (via PlayerInput component) ---
    public void OnMove(InputValue v) => moveInput = v.Get<Vector2>();

    public void OnJump(InputValue v) {
        if (v.isPressed && IsGrounded()) {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            anim.SetTrigger("Jump");
        }
    }

    public void OnAttack(InputValue v) {
        if (v.isPressed && canAttack) StartCoroutine(AttackCR());
    }

    public void OnParry(InputValue v) {
        if (v.isPressed) parry.TryParry();
    }

    public void OnDash(InputValue v) {
        if (v.isPressed && canDash) StartCoroutine(DashCR());
    }

    System.Collections.IEnumerator AttackCR() {
        canAttack = false;
        anim.SetTrigger("Attack");
        attackHitbox.enabled = true;
        yield return new WaitForSeconds(0.1f);
        attackHitbox.enabled = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    System.Collections.IEnumerator DashCR() {
        canDash = false; isDashing = true;
        float t = 0f; float origGrav = rb.gravityScale;
        rb.gravityScale = 0f;
        while (t < dashTime) {
            rb.linearVelocity = new Vector2(facing * dashSpeed, 0f);
            t += Time.deltaTime;
            yield return null;
        }
        rb.gravityScale = origGrav;
        isDashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}
