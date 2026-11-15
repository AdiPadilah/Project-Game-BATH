using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BossAI : Entity
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float stopDistance = 1.5f;

    [Header("Visual & Size")]
    public float bossScale = 3.2f;
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
    public AudioClip roarSound;

    private Transform player;
    private float attackTimer;
    private bool isAttacking = false;
    private bool isKnocked = false;
    private bool isDead = false;
    public bool bossActive = false;
    private bool isTakingDamage = false;
    private bool frozenOnGameOver = false;
    private Color originalColor;
    private Coroutine attackTimeoutCoroutine; // Track attack coroutine terpisah agar tidak cancel HitRoutine

    protected override void Awake()
    {
        // Menjalankan Awake dari Entity.cs
        base.Awake();

        // --- PERBAIKAN: PAKSA DARAH FULL SAAT AWAL ---
        currentHealth = maxHealth;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        if (sr != null) originalColor = sr.color;

        // Pertahankan skala boss dari scene agar tidak berubah saat runtime
        if (Mathf.Abs(transform.localScale.y) > 0.1f)
        {
            bossScale = Mathf.Abs(transform.localScale.y);
        }
        else
        {
            bossScale = 3.2f;
        }

        if (roarSound == null)
        {
#if UNITY_EDITOR
            roarSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sound/MONSTER DEATH SCREAMS - FREE SOUND EFFECT [HD] - VKY Sounds (youtube).wav");
#endif
        }

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

        float hDistance = Mathf.Abs(player.position.x - transform.position.x);
        float vDistance = Mathf.Abs(player.position.y - transform.position.y);
        float dir = player.position.x - transform.position.x;

        // Flip logic menggunakan bossScale (hanya saat tidak sedang menyerang)
        if (!isAttacking)
        {
            if (dir > 0.1f)
                transform.localScale = new Vector3(-bossScale, bossScale, 1f);
            else if (dir < -0.1f)
                transform.localScale = new Vector3(bossScale, bossScale, 1f);
        }

        if (isAttacking)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Collider2D hitPlayer = null;
        if (entityAttackPoint != null)
        {
            hitPlayer = Physics2D.OverlapCircle(
                entityAttackPoint.position,
                attackRange,
                playerLayer
            );
        }

        // Cek jangkauan serang secara horizontal & vertikal agar saat player berada di bawah boss langsung diserang (bukan joging)
        bool inRangeToAttack = (hitPlayer != null) || (hDistance <= attackRange && vDistance <= 4.0f);

        if (inRangeToAttack)
        {
            Stop();
            if (attackTimer >= attackCooldown)
                Attack();
        }
        else
        {
            MoveToPlayer(dir);
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

            // Animasi Cleave berdurasi ~1.1 detik, beri timeout 1.1 detik agar boss tidak macet/freeze
            if (attackTimeoutCoroutine != null)
                StopCoroutine(attackTimeoutCoroutine);
            attackTimeoutCoroutine = StartCoroutine(AttackTimeout(1.1f));
        }
    }

    IEnumerator AttackTimeout(float duration)
    {
        yield return new WaitForSeconds(duration);
        isAttacking = false;
        attackTimer = 0f;
    }

    public void EndAttack()
    {
        isAttacking = false;
        attackTimer = 0f;
        if (attackTimeoutCoroutine != null)
            StopCoroutine(attackTimeoutCoroutine);
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
        else if (player != null && Vector2.Distance(transform.position, player.position) <= attackRange + 1.5f)
        {
            Entity p = player.GetComponent<Entity>();
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
        isAttacking = false; // Batalkan status attack agar boss tidak stuck setelah kena hit
        if (attackTimeoutCoroutine != null)
            StopCoroutine(attackTimeoutCoroutine);

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

        // Hentikan semua coroutine aktif (HitRoutine, AttackTimeout) agar tidak konflik
        StopAllCoroutines();
        isAttacking = false;
        isKnocked = false;
        isTakingDamage = false;

        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound);

        if (anim != null)
            anim.SetTrigger("Die");

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }

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

    public void PlayRoar()
    {
        // Pastikan skala boss sesuai setting scene (minimal 3.2)
        bossScale = Mathf.Max(bossScale, 3.2f);

        if (anim != null)
        {
            anim.ResetTrigger("Roar");
            anim.SetTrigger("Roar");
        }

        if (audioSource != null && roarSound != null)
        {
            audioSource.PlayOneShot(roarSound, 1.3f);
        }

        // Hadapkan ke arah player saat mengaum
        if (player != null)
        {
            float dir = player.position.x - transform.position.x;
            if (dir > 0.1f)
                transform.localScale = new Vector3(-bossScale, bossScale, 1f);
            else if (dir < -0.1f)
                transform.localScale = new Vector3(bossScale, bossScale, 1f);
        }

        StartCoroutine(RoarEffectRoutine());
    }

    private IEnumerator RoarEffectRoutine()
    {
        // Efek visual dramatis kobaran api merah/oranye saat mengaum
        if (sr != null)
        {
            float elapsed = 0f;
            Color fieryColor = new Color(1.3f, 0.6f, 0.3f, 1f);
            while (elapsed < 1.8f)
            {
                elapsed += Time.deltaTime;
                float pulse = (Mathf.Sin(elapsed * 12f) + 1f) * 0.5f;
                sr.color = Color.Lerp(originalColor, fieryColor, 0.5f + pulse * 0.5f);
                yield return null;
            }
            sr.color = originalColor;
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
        if (Mathf.Abs(dir) < 0.1f)
        {
            Stop();
            return;
        }

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