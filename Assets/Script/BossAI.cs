using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BossAI : Entity
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float stopDistance = 1.5f;

    [Header("Visual & Size")]
    public float bossScale = 1.8f;
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

        // Pertahankan skala boss dari scene (proporsional 1.8)
        if (Mathf.Abs(transform.localScale.y) > 0.1f)
        {
            bossScale = Mathf.Abs(transform.localScale.y);
        }
        else
        {
            bossScale = 1.8f;
        }

        bossScale = Mathf.Clamp(bossScale, 1.5f, 2.0f);

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
            SetupBossNameText();
        }
    }

    private void SetupBossNameText()
    {
        if (bossHealthBar == null) return;
        Text t = bossHealthBar.GetComponentInChildren<Text>();
        if (t == null)
        {
            GameObject txtGo = new GameObject("BossNameLabel");
            txtGo.transform.SetParent(bossHealthBar.transform, false);
            t = txtGo.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = "King Demon Slime";
            t.fontSize = 22;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(1f, 0.85f, 0.45f, 1f); // Golden amber
            RectTransform rt = txtGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 4f);
            rt.sizeDelta = new Vector2(0f, 26f);
            Outline outline = txtGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.15f, 0.02f, 0.02f, 1f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }
        else
        {
            t.text = "King Demon Slime";
        }
    }

    protected virtual void Start()
    {
        SnapToGround();
    }

    private void SnapToGround()
    {
        // Pastikan boss sudah menapak di tanah sejak awal agar tidak jatuh dari atas
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 30f, whatIsGround);
        if (hit.collider != null)
        {
            Collider2D bossCol = GetComponent<Collider2D>();
            float bottomOffset = bossCol != null ? bossCol.bounds.extents.y : 1.4f;
            transform.position = new Vector3(transform.position.x, hit.point.y + bottomOffset, transform.position.z);
            if (rb != null) rb.velocity = Vector2.zero;
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
            float s = Mathf.Clamp(bossScale, 1.5f, 2.0f);
            if (dir > 0.1f)
                transform.localScale = new Vector3(-s, s, 1f);
            else if (dir < -0.1f)
                transform.localScale = new Vector3(s, s, 1f);
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

    public override void TakeDamage(int dmg, bool applyStun = false)
    {
        if (isDead || isTakingDamage) return;
        if (UI.instance != null && UI.instance.isGameOver) return;

        if (audioSource != null && hitSound != null)
            audioSource.PlayOneShot(hitSound);

        currentHealth -= dmg;

        // Update UI Slider saat kena damage
        if (bossHealthBar != null)
            bossHealthBar.value = currentHealth;

        // EFEK STUN / HIT FLINCH: HANYA jika serangan berupa kombo finisher!
        if (applyStun)
        {
            isTakingDamage = true;
            isAttacking = false; // Batalkan status attack saat terkena kombo stun
            if (attackTimeoutCoroutine != null)
                StopCoroutine(attackTimeoutCoroutine);

            if (anim != null)
                anim.SetTrigger("Hit");

            StartCoroutine(HitRoutine());
        }
        else
        {
            // Basic attack biasa: berikan damage dan feedback flash visual singkat, tanpa menghentikan/men-stun bos
            StartCoroutine(DamageFlashOnly());
        }

        if (currentHealth <= 0)
            Die();
    }

    private IEnumerator DamageFlashOnly()
    {
        if (sr != null) sr.color = hitColor;
        yield return new WaitForSeconds(hitDuration);
        if (sr != null) sr.color = originalColor;
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
        bossScale = Mathf.Clamp(bossScale, 1.5f, 2.0f);

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
            float s = Mathf.Clamp(bossScale, 1.5f, 2.0f);
            if (dir > 0.1f)
                transform.localScale = new Vector3(-s, s, 1f);
            else if (dir < -0.1f)
                transform.localScale = new Vector3(s, s, 1f);
        }

        StartCoroutine(RoarEffectRoutine());
    }

    private IEnumerator RoarEffectRoutine()
    {
        // Efek visual intimidasi merah membara saat mengaum
        if (sr != null)
        {
            float elapsed = 0f;
            Color intimidatingColor = new Color(1.8f, 0.25f, 0.15f, 1f);
            while (elapsed < 1.8f)
            {
                elapsed += Time.deltaTime;
                float pulse = (Mathf.Sin(elapsed * 14f) + 1f) * 0.5f;
                sr.color = Color.Lerp(originalColor, intimidatingColor, 0.35f + pulse * 0.65f);
                yield return null;
            }
            sr.color = originalColor;
        }

        if (bossHealthBar != null)
        {
            StartCoroutine(AnimateHealthBarFill());
        }
    }

    private IEnumerator AnimateHealthBarFill()
    {
        if (bossHealthBar == null) yield break;
        bossHealthBar.maxValue = maxHealth;
        bossHealthBar.value = 0;
        float fillDuration = 0.6f;
        float t = 0f;
        while (t < fillDuration)
        {
            t += Time.deltaTime;
            bossHealthBar.value = Mathf.Lerp(0, maxHealth, t / fillDuration);
            yield return null;
        }
        bossHealthBar.value = maxHealth;
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