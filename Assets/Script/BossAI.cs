using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BossAI : Entity
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float stopDistance = 1.5f;

    [Header("Visual & Size")]
    public float bossScale = 2.5f;

    [Header("Attack")]
    public float attackRange = 3f;
    public float attackCooldown = 2f;
    public int damage = 10;
    public LayerMask playerLayer;

    [Header("UI")]
    public Slider bossHealthBar;

    [Header("Hit Effect")]
    public Color hitColor = Color.red;
    public float hitDuration = 0.1f;

    [Header("Knockback")]
    public float knockbackTime = 0.2f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip attackSound;
    public AudioClip hitSound;
    public AudioClip deathSound;

    private Transform player;
    private float attackTimer;
    private bool isAttacking = false;
    private bool isKnocked = false;
    private bool isDead = false;
    public bool bossActive = false;
    private bool isTakingDamage = false;
    private bool frozenOnGameOver = false;
    private Color originalColor;

    protected override void Awake()
    {
        // Menjalankan Awake dari Entity.cs
        base.Awake();

        // --- PERBAIKAN: PAKSA DARAH FULL SAAT AWAL ---
        currentHealth = maxHealth;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        if (sr != null) originalColor = sr.color;

        if (bossHealthBar != null)
        {
            bossHealthBar.maxValue = maxHealth;
            bossHealthBar.value = maxHealth; // Pastikan Slider langsung penuh
        }
    }

    protected override void Update()
    {
        if (UI.instance != null && UI.instance.isGameOver)
        {
            FreezeBoss();
            return;
        }

        if (!bossActive || player == null || isDead) return;

        if (isKnocked)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        attackTimer += Time.deltaTime;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        float dir = player.position.x - transform.position.x;

        // Flip logic menggunakan bossScale
        if (dir > 0.1f)
            transform.localScale = new Vector3(-bossScale, bossScale, 1f);
        else if (dir < -0.1f)
            transform.localScale = new Vector3(bossScale, bossScale, 1f);

        if (isAttacking)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (entityAttackPoint == null) return;

        Collider2D hitPlayer = Physics2D.OverlapCircle(
            entityAttackPoint.position,
            attackRange,
            playerLayer
        );

        if (hitPlayer != null)
        {
            Stop();
            if (attackTimer >= attackCooldown)
                Attack();
        }
        else if (distanceToPlayer > stopDistance)
        {
            MoveToPlayer(dir);
        }
        else
        {
            Stop();
        }
    }

    private void FreezeBoss()
    {
        if (frozenOnGameOver) return;
        frozenOnGameOver = true;

        StopAllCoroutines();
        isAttacking = false;
        isKnocked = false;
        canMove = false;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }

        if (anim != null)
            anim.enabled = false;

        enabled = false;
    }

    void Attack()
    {
        if (!isActiveAndEnabled) return;
        if (UI.instance != null && UI.instance.isGameOver) return;

        if (anim != null && !isAttacking)
        {
            if (audioSource != null && attackSound != null)
                audioSource.PlayOneShot(attackSound);

            isAttacking = true;
            attackTimer = 0f;
            rb.velocity = Vector2.zero;

            anim.SetBool("isMoving", false);
            anim.SetTrigger("Cleave");

            StopAllCoroutines();
            StartCoroutine(AttackTimeout(2.5f));
        }
    }

    IEnumerator AttackTimeout(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        isAttacking = false;
    }

    public void EndAttack()
    {
        isAttacking = false;
        attackTimer = 0f;
    }

    public void DealDamage()
    {
        if (!isActiveAndEnabled || (UI.instance != null && UI.instance.isGameOver)) return;
        if (entityAttackPoint == null) return;

        Collider2D hitPlayer = Physics2D.OverlapCircle(
            entityAttackPoint.position,
            attackRange,
            playerLayer
        );

        if (hitPlayer != null)
        {
            Entity p = hitPlayer.GetComponent<Entity>();
            if (p != null) p.TakeDamage(damage);
        }
    }

    public override void TakeDamage(int dmg)
    {
        if (isDead || isTakingDamage) return;
        if (UI.instance != null && UI.instance.isGameOver) return;

        if (audioSource != null && hitSound != null)
            audioSource.PlayOneShot(hitSound);

        isTakingDamage = true;
        currentHealth -= dmg;

        // Update UI Slider saat kena damage
        if (bossHealthBar != null)
            bossHealthBar.value = currentHealth;

        if (anim != null)
            anim.SetTrigger("Hit");

        StartCoroutine(HitRoutine());

        if (currentHealth <= 0)
            Die();
    }

    IEnumerator HitRoutine()
    {
        if (sr != null) sr.color = hitColor;
        isKnocked = true;
        yield return new WaitForSeconds(hitDuration);
        if (sr != null) sr.color = originalColor;
        yield return new WaitForSeconds(knockbackTime);
        isKnocked = false;
        isTakingDamage = false;
    }

    protected override void Die()
    {
        if (isDead) return;
        isDead = true;

        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound);

        if (anim != null)
            anim.SetTrigger("Die");

        rb.velocity = Vector2.zero;
        rb.simulated = false;

        StartCoroutine(MunculkanVictoryPanel());
        Destroy(gameObject, 5f);
    }

    IEnumerator MunculkanVictoryPanel()
    {
        yield return new WaitForSecondsRealtime(2f);
        VictoryManager vm = FindObjectOfType<VictoryManager>(true);
        Player playerScript = FindObjectOfType<Player>();
        if (vm != null)
        {
            float waktu = playerScript != null ? playerScript.waktuMain : 999f;
            vm.MunculkanMenang(waktu);
        }
    }

    public void ActivateBoss()
    {
        bossActive = true;
        // Memastikan saat boss aktif, darahnya sinkron ke UI
        if (bossHealthBar != null) bossHealthBar.value = currentHealth;
    }

    void MoveToPlayer(float dir)
    {
        float move = Mathf.Sign(dir);
        rb.velocity = new Vector2(move * moveSpeed, rb.velocity.y);
        if (anim != null) anim.SetBool("isMoving", true);
    }

    void Stop()
    {
        rb.velocity = Vector2.zero;
        if (anim != null) anim.SetBool("isMoving", false);
    }

    void OnDrawGizmosSelected()
    {
        if (entityAttackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(entityAttackPoint.position, attackRange);
        }
    }
}