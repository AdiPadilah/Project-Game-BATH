using System.Collections;
using UnityEngine;

public class Entity : MonoBehaviour
{
    protected Animator anim;
    protected Rigidbody2D rb;
    protected Collider2D col;
    protected SpriteRenderer sr;

    [Header("Health")]
    [SerializeField] protected int maxHealth = 100;
    // PENTING: Harus 'protected' agar BossAI, Player, dan Enemy bisa membaca 'currentHealth'
    [SerializeField] protected int currentHealth;

    [Header("Visual Feedback")]
    // PENTING: Harus 'protected' agar error CS0103 di Console hilang
    [SerializeField] protected Material damagedMaterial;
    [SerializeField] protected float damageFeedbackDuration = .2f;

    [Header("Extra Combat")]
    [SerializeField] protected float invincibleTime = 0.4f;

    [Header("Knockback")]
    [SerializeField] protected float knockbackForce = 5f;
    [SerializeField] protected float knockbackUpForce = 3f;

    protected bool isInvincible;
    private Coroutine damageCoroutine;
    protected Material defaultMaterial;

    [Header("Attack")]
    [SerializeField] protected int attackDamage = 10;
    [SerializeField] protected float attackRadius = 1f;
    [SerializeField] protected Transform entityAttackPoint;
    [SerializeField] protected LayerMask whatIsTarget;

    [Header("Ground Check")]
    [SerializeField] protected float groundCheckDistance = 0.2f;
    [SerializeField] protected LayerMask whatIsGround;

    protected bool isGrounded;
    public bool canMove = true;

    public bool facingRight = true;
    protected int facingDir = 1;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponentInChildren<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();

        if (sr != null)
        {
            defaultMaterial = sr.material;
        }

        // Logika agar darah penuh saat game pertama kali dijalankan
        if (currentHealth <= 0)
        {
            currentHealth = maxHealth;
        }
    }

    protected virtual void Update()
    {
        if (UI.instance != null && UI.instance.isGameOver)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            canMove = false;
            return;
        }

        HandleCollision();
        HandleMovement();
        HandleAttack();
        HandleAnimations();
        HandleFlip();
    }

    public virtual void TakeDamage(int damage = 1)
    {
        if (UI.instance != null && UI.instance.isGameOver) return;
        if (isInvincible) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        PlayDamageFeedback();
        ApplyKnockback();
        StartCoroutine(InvincibleCoroutine());

        OnHealthChanged();

        if (currentHealth <= 0) Die();
    }

    protected virtual void OnHealthChanged() { }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;

    protected virtual void Die()
    {
        canMove = false;
        if (anim != null) anim.SetTrigger("Die");
        if (col != null) col.enabled = false;

        if (rb != null)
        {
            rb.velocity = new Vector2(0, 8f);
            rb.gravityScale = 5f;
        }

        Destroy(gameObject, 2f);
    }

    private IEnumerator InvincibleCoroutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibleTime);
        isInvincible = false;
    }

    private void ApplyKnockback()
    {
        if (rb == null) return;
        float direction = facingRight ? -1 : 1;
        rb.velocity = new Vector2(direction * knockbackForce, knockbackUpForce);
    }

    private void PlayDamageFeedback()
    {
        if (sr == null || damagedMaterial == null) return;
        if (damageCoroutine != null) StopCoroutine(damageCoroutine);
        damageCoroutine = StartCoroutine(DamageFlash());
    }

    private IEnumerator DamageFlash()
    {
        if (defaultMaterial == null && sr != null)
            defaultMaterial = sr.material;

        sr.material = damagedMaterial;
        yield return new WaitForSeconds(damageFeedbackDuration);
        if (sr != null && defaultMaterial != null)
            sr.material = defaultMaterial;
    }

    public void EnableMovement(bool enable) => canMove = enable;
    protected virtual void HandleMovement() { }
    protected virtual void HandleAttack() { }

    protected virtual void HandleCollision()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, whatIsGround);
    }

    protected virtual void HandleFlip()
    {
        if (rb == null) return;
        if (rb.velocity.x > 0 && !facingRight) Flip();
        else if (rb.velocity.x < 0 && facingRight) Flip();
    }

    public void Flip()
    {
        facingRight = !facingRight;
        facingDir *= -1;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    protected virtual void HandleAnimations()
    {
        if (anim == null || rb == null) return;
        anim.SetFloat("xVelocity", Mathf.Abs(rb.velocity.x));
        anim.SetFloat("yVelocity", rb.velocity.y);
        anim.SetBool("isGrounded", isGrounded);
    }

    public virtual void DamageTargets()
    {
        if (UI.instance != null && UI.instance.isGameOver) return;
        if (entityAttackPoint == null) return;

        Collider2D[] targets = Physics2D.OverlapCircleAll(entityAttackPoint.position, attackRadius, whatIsTarget);
        foreach (Collider2D target in targets)
        {
            Entity e = target.GetComponent<Entity>();
            if (e != null && e != this)
            {
                e.TakeDamage(attackDamage);
                break;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (entityAttackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(entityAttackPoint.position, attackRadius);
    }
}