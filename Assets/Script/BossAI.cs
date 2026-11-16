using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// BossAI - King Demon Slime dengan 2 Fase & Ultimate Brutal
///
/// FASE 1 (HP > 50%): Skill 1 (Cleave) + Skill 2 (Leap Slam) | Cooldown normal
/// FASE 2 (HP <= 50%): RAGE MODE — lebih cepat, skill lebih ganas, Ultimate tersedia (CD 40 detik)
/// ULTIMATE: Brutal Rampage — lompat ke player + shockwave + area burst damage dahsyat
/// </summary>
public class BossAI : Entity
{
    // ─── Movement ────────────────────────────────────────────────────────────
    [Header("Movement")]
    public float moveSpeed      = 8f;
    public float rageMoveSpeed  = 13f;   // Kecepatan di fase rage
    public float stopDistance   = 1.5f;

    // ─── Visual ──────────────────────────────────────────────────────────────
    [Header("Visual & Size")]
    public float bossScale = 1.8f;

    // ─── Attack Fase 1 ───────────────────────────────────────────────────────
    [Header("Skill 1 - Cleave [FASE 1 & 2]")]
    public float attackRange      = 3f;
    public float skill1Cooldown   = 2.0f;   // Cooldown normal
    public float skill1CdRage     = 1.2f;   // Cooldown rage (lebih cepat)
    public int   skill1Damage     = 15;
    public int   skill1DamageRage = 25;     // Damage di fase rage

    [Header("Skill 2 - Leap Slam [FASE 1 & 2]")]
    public float skill2Cooldown   = 5.0f;
    public float skill2CdRage     = 3.0f;
    public int   skill2Damage     = 25;
    public int   skill2DamageRage = 45;
    public float skill2LeapForce  = 18f;    // Kekuatan lompatan

    [Header("Ultimate - Brutal Rampage [FASE 2 ONLY]")]
    public float ultimateCooldown = 40f;
    public int   ultimateDamage   = 80;     // Damage burst utama
    public float ultimateRadius   = 7f;     // Radius ledakan

    // ─── Layer ───────────────────────────────────────────────────────────────
    public LayerMask playerLayer;

    // ─── UI ──────────────────────────────────────────────────────────────────
    [Header("UI")]
    public Slider bossHealthBar;

    // ─── Hit Effect ──────────────────────────────────────────────────────────
    [Header("Hit Effect")]
    public Color hitColor     = Color.red;
    public float hitDuration  = 0.1f;

    // ─── Knockback ───────────────────────────────────────────────────────────
    [Header("Knockback")]
    public float knockbackTime = 0.2f;

    // ─── Audio ───────────────────────────────────────────────────────────────
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   attackSound;
    public AudioClip   hitSound;
    public AudioClip   deathSound;
    public AudioClip   roarSound;
    public AudioClip   leapSound;
    public AudioClip   ultimateSound;

    // ─── State ───────────────────────────────────────────────────────────────
    private Transform player;
    private bool  isAttacking        = false;
    private bool  isKnocked          = false;
    private bool  isDead             = false;
    public  bool  bossActive         = false;
    private bool  isTakingDamage     = false;
    private bool  frozenOnGameOver   = false;
    private bool  isRageMode         = false;
    private bool  rageTriggered      = false;   // Agar rage hanya trigger sekali

    private float skill1Timer   = 0f;
    private float skill2Timer   = 0f;
    private float ultimateTimer = 0f;

    private Color originalColor;
    private Coroutine attackTimeoutCoroutine;

    // ─── Fase Rage threshold ─────────────────────────────────────────────────
    private float rageThreshold => maxHealth * 0.5f;   // 50% HP

    // ─────────────────────────────────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();
        currentHealth = maxHealth;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        if (sr != null) originalColor = sr.color;

        if (Mathf.Abs(transform.localScale.y) > 0.1f)
            bossScale = Mathf.Abs(transform.localScale.y);
        else
            bossScale = 1.8f;

        bossScale = Mathf.Clamp(bossScale, 1.5f, 2.0f);

        if (roarSound == null)
        {
#if UNITY_EDITOR
            roarSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Sound/MONSTER DEATH SCREAMS - FREE SOUND EFFECT [HD] - VKY Sounds (youtube).wav");
#endif
        }

        if (bossHealthBar != null)
        {
            bossHealthBar.maxValue = maxHealth;
            bossHealthBar.value    = maxHealth;
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
            t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text      = "King Demon Slime";
            t.fontSize  = 22;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color     = new Color(1f, 0.85f, 0.45f, 1f);
            RectTransform rt = txtGo.GetComponent<RectTransform>();
            rt.anchorMin      = new Vector2(0f, 1f);
            rt.anchorMax      = new Vector2(1f, 1f);
            rt.pivot          = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 4f);
            rt.sizeDelta      = new Vector2(0f, 26f);
            Outline outline   = txtGo.AddComponent<Outline>();
            outline.effectColor    = new Color(0.15f, 0.02f, 0.02f, 1f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }
        else { t.text = "King Demon Slime"; }
    }

    protected virtual void Start() => SnapToGround();

    private void SnapToGround()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 30f, whatIsGround);
        if (hit.collider != null)
        {
            Collider2D bossCol = GetComponent<Collider2D>();
            float bottomOffset = bossCol != null ? bossCol.bounds.extents.y : 1.4f;
            transform.position = new Vector3(transform.position.x, hit.point.y + bottomOffset, transform.position.z);
            if (rb != null) rb.velocity = Vector2.zero;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UPDATE LOOP
    // ─────────────────────────────────────────────────────────────────────────
    protected override void Update()
    {
        if (UI.instance != null && UI.instance.isGameOver) { FreezeBoss(); return; }
        if (!bossActive || player == null || isDead) return;
        if (isKnocked) { rb.velocity = Vector2.zero; return; }
        if (isAttacking) { rb.velocity = Vector2.zero; return; }

        // Tick cooldown timer
        if (skill1Timer  > 0f) skill1Timer  -= Time.deltaTime;
        if (skill2Timer  > 0f) skill2Timer  -= Time.deltaTime;
        if (ultimateTimer > 0f) ultimateTimer -= Time.deltaTime;

        // ── Cek Rage Mode ─────────────────────────────────────────────────────
        if (!rageTriggered && currentHealth <= rageThreshold)
        {
            rageTriggered = true;
            StartCoroutine(EnterRageMode());
            return;
        }

        if (isRageMode)
            UpdateRageMode();
        else
            UpdateNormalMode();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FASE 1 — HP > 50%: Cleave + Leap Slam, gerakan normal
    // ─────────────────────────────────────────────────────────────────────────
    private void UpdateNormalMode()
    {
        FlipToPlayer();

        float hDist = Mathf.Abs(player.position.x - transform.position.x);
        float vDist = Mathf.Abs(player.position.y - transform.position.y);
        bool  close = hDist <= attackRange && vDist <= 4f;

        // Skill 2 (Leap Slam) diprioritaskan saat player agak jauh
        if (skill2Timer <= 0f && hDist > attackRange * 1.5f && hDist < 12f)
        {
            StartCoroutine(LeapSlamRoutine(false));
            return;
        }

        // Skill 1 (Cleave) saat player dekat
        if (close && skill1Timer <= 0f)
        {
            DoSkill1(false);
            return;
        }

        // Jika tidak ada skill, dekati player
        if (!close) MoveToPlayer(player.position.x - transform.position.x, moveSpeed);
        else         Stop();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FASE 2 — HP <= 50%: RAGE MODE — lebih ganas, plus Ultimate
    // ─────────────────────────────────────────────────────────────────────────
    private void UpdateRageMode()
    {
        FlipToPlayer();

        float hDist = Mathf.Abs(player.position.x - transform.position.x);
        float vDist = Mathf.Abs(player.position.y - transform.position.y);
        bool  close = hDist <= attackRange && vDist <= 4f;

        // ── Ultimate (prioritas tertinggi di rage mode) ───────────────────────
        if (ultimateTimer <= 0f && hDist < 15f)
        {
            StartCoroutine(BrutalRampageUltimate());
            return;
        }

        // ── Skill 2 Rage (Leap lebih jauh & cepat) ───────────────────────────
        if (skill2Timer <= 0f && hDist > attackRange && hDist < 14f)
        {
            StartCoroutine(LeapSlamRoutine(true));
            return;
        }

        // ── Skill 1 Rage (Cleave lebih cepat & sakit) ────────────────────────
        if (close && skill1Timer <= 0f)
        {
            DoSkill1(true);
            return;
        }

        // Kejar player lebih cepat di rage mode
        if (!close) MoveToPlayer(player.position.x - transform.position.x, rageMoveSpeed);
        else         Stop();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SKILL 1 — Cleave (Tebasan Langsung)
    // ─────────────────────────────────────────────────────────────────────────
    private void DoSkill1(bool rage)
    {
        if (!isActiveAndEnabled) return;
        if (isAttacking) return;

        isAttacking = true;
        skill1Timer = rage ? skill1CdRage : skill1Cooldown;

        if (audioSource != null && attackSound != null)
            audioSource.PlayOneShot(attackSound);

        if (anim != null)
        {
            anim.SetBool("isMoving", false);
            anim.SetTrigger("Cleave");
        }

        rb.velocity = Vector2.zero;

        // Damage langsung (dipanggil juga dari AnimationEvent jika ada)
        StartCoroutine(Skill1DamageDelay(rage ? skill1DamageRage : skill1Damage));

        // Timeout agar boss tidak macet
        if (attackTimeoutCoroutine != null) StopCoroutine(attackTimeoutCoroutine);
        attackTimeoutCoroutine = StartCoroutine(AttackTimeout(rage ? 0.9f : 1.1f));
    }

    private IEnumerator Skill1DamageDelay(int dmg)
    {
        yield return new WaitForSeconds(0.45f);   // Delay agar sync dengan animasi tebas

        if (player == null || isDead) yield break;

        // Damage dalam radius tebasan
        Collider2D hit = Physics2D.OverlapCircle(
            entityAttackPoint != null ? entityAttackPoint.position : transform.position + Vector3.right * facingDir * 1.2f,
            attackRange, playerLayer);

        if (hit != null)
        {
            Entity p = hit.GetComponent<Entity>();
            if (p != null) p.TakeDamage(dmg, false);
        }

        // Flash merah di rage mode
        if (isRageMode && sr != null)
            StartCoroutine(QuickFlash(new Color(1.5f, 0.1f, 0.1f, 1f), 0.08f));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SKILL 2 — Leap Slam (Lompat ke Player + Shockwave)
    // ─────────────────────────────────────────────────────────────────────────
    private IEnumerator LeapSlamRoutine(bool rage)
    {
        isAttacking = true;
        skill2Timer = rage ? skill2CdRage : skill2Cooldown;

        if (anim != null) anim.SetBool("isMoving", false);

        // Jeda singkat "kuda-kuda" sebelum lompat
        float windupDur = rage ? 0.25f : 0.4f;
        if (sr != null)
        {
            // Efek warna kuning (bersiap lompat)
            float elapsed = 0f;
            while (elapsed < windupDur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / windupDur;
                if (sr != null) sr.color = Color.Lerp(originalColor, new Color(1.2f, 0.9f, 0.1f, 1f), t);
                yield return null;
            }
        }
        else { yield return new WaitForSeconds(windupDur); }

        if (sr != null) sr.color = originalColor;
        if (player == null || isDead) { isAttacking = false; yield break; }

        // Lompat ke arah player
        if (leapSound != null && audioSource != null)
            audioSource.PlayOneShot(leapSound, 1.0f);

        float dirX = Mathf.Sign(player.position.x - transform.position.x);
        float leapH = rage ? skill2LeapForce * 1.3f : skill2LeapForce;
        rb.velocity = new Vector2(dirX * 10f, leapH);

        if (anim != null) anim.SetTrigger("Cleave"); // pakai Cleave sebagai anim lompat

        // Tunggu sampai mendarat (atau timeout)
        float airTime = 0f;
        while (!isGrounded && airTime < 1.5f)
        {
            airTime += Time.deltaTime;
            yield return null;
        }

        // SLAM saat mendarat
        yield return new WaitForSeconds(0.05f);

        int slamDmg = rage ? skill2DamageRage : skill2Damage;
        if (CameraFollow.instance != null)
            CameraFollow.instance.TriggerScreenShake(rage ? 0.5f : 0.3f, rage ? 0.45f : 0.28f);

        // Damage area shockwave saat mendarat
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, rage ? 4.5f : 3.5f, playerLayer);
        foreach (var h in hits)
        {
            Entity ent = h.GetComponent<Entity>();
            if (ent != null) ent.TakeDamage(slamDmg, true); // applyStun = true (knockback)
        }

        // Flash oranye saat slam
        if (sr != null) StartCoroutine(QuickFlash(new Color(1.3f, 0.5f, 0.0f, 1f), 0.12f));

        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(0.3f);

        isAttacking = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ULTIMATE — Brutal Rampage (CD 40 detik, hanya di Fase 2 RAGE)
    // Lompat ke player → Shockwave raksasa → Burst damage brutal → Stun semua
    // ─────────────────────────────────────────────────────────────────────────
    private IEnumerator BrutalRampageUltimate()
    {
        isAttacking   = true;
        ultimateTimer = ultimateCooldown; // 40 detik CD

        // ── FASE PEMBUKA: Raungan menggelegar ─────────────────────────────────
        if (anim != null)
        {
            anim.SetBool("isMoving", false);
            anim.SetTrigger("Roar");
        }

        if (audioSource != null && roarSound != null)
            audioSource.PlayOneShot(roarSound, 1.5f);

        // Efek visual: boss berpendar merah brutal selama 1.2 detik
        yield return StartCoroutine(BrutalChargeEffect(1.2f));

        // ── FASE 1 ULTIMATE: Time Slow + Lompat ke Player ────────────────────
        if (player == null || isDead) { isAttacking = false; yield break; }

        Time.timeScale = 0.15f;    // Slow motion dramatis

        // Suara ultimate
        if (ultimateSound != null && audioSource != null)
            audioSource.PlayOneShot(ultimateSound, 1.4f);
        else if (attackSound != null && audioSource != null)
            audioSource.PlayOneShot(attackSound, 1.6f);

        yield return new WaitForSecondsRealtime(0.3f);  // Jeda dramatis

        Time.timeScale = 1.0f;

        // LOMPATAN BRUTAL ke player
        float dirX = Mathf.Sign(player.position.x - transform.position.x);
        rb.velocity = new Vector2(dirX * 16f, 22f);    // Lompatan lebih tinggi & cepat

        // ── FASE 2 ULTIMATE: Melayang + efek aura gelap ──────────────────────
        float airTimer = 0f;
        while (!isGrounded && airTimer < 1.8f)
        {
            airTimer += Time.deltaTime;
            // Pulsasi merah-hitam di udara
            if (sr != null)
            {
                float pulse = (Mathf.Sin(airTimer * 20f) + 1f) * 0.5f;
                sr.color = Color.Lerp(new Color(0.8f, 0.0f, 0.0f, 1f), new Color(0.15f, 0.0f, 0.0f, 1f), pulse);
            }
            yield return null;
        }

        if (sr != null) sr.color = originalColor;

        // ── FASE 3 ULTIMATE: BRUTAL SLAM + SHOCKWAVE RAKSASA ─────────────────
        rb.velocity = Vector2.zero;

        // Screen shake DAHSYAT
        if (CameraFollow.instance != null)
            CameraFollow.instance.TriggerScreenShake(0.9f, 0.75f);

        // Flash putih di seluruh layar (seperti ledakan)
        yield return StartCoroutine(ScreenFlashWhite(0.08f, 0.6f));

        // Damage burst raksasa di area luas
        Collider2D[] victims = Physics2D.OverlapCircleAll(transform.position, ultimateRadius, playerLayer);
        foreach (var v in victims)
        {
            Entity ent = v.GetComponent<Entity>();
            if (ent != null)
            {
                ent.TakeDamage(ultimateDamage, true);   // Knockback brutal!
                // Delay kecil antar hit agar terasa brutal
                yield return new WaitForSeconds(0.02f);
            }
        }

        // ── FASE 4 ULTIMATE: Aftershock (serangan susulan kecil) ─────────────
        yield return new WaitForSeconds(0.25f);

        if (CameraFollow.instance != null)
            CameraFollow.instance.TriggerScreenShake(0.4f, 0.35f);

        // Aftershock damage (setengah dari utama)
        victims = Physics2D.OverlapCircleAll(transform.position, ultimateRadius * 0.7f, playerLayer);
        foreach (var v in victims)
        {
            Entity ent = v.GetComponent<Entity>();
            if (ent != null) ent.TakeDamage(ultimateDamage / 3, false);
        }

        // Flash merah
        if (sr != null) StartCoroutine(QuickFlash(new Color(2f, 0.0f, 0.0f, 1f), 0.2f));

        yield return new WaitForSeconds(0.6f);

        isAttacking = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ENTER RAGE MODE (Transisi dari Fase 1 ke Fase 2)
    // ─────────────────────────────────────────────────────────────────────────
    private IEnumerator EnterRageMode()
    {
        isAttacking = true;
        canMove     = false;

        // Stop semua gerakan
        if (rb != null) rb.velocity = Vector2.zero;

        // ── Roar (sama seperti sebelumnya, tanda masuk rage) ─────────────────
        PlayRoar();

        // Efek warna berubah dramatis (merah membara 2 detik)
        float elapsed = 0f;
        while (elapsed < 2.0f)
        {
            elapsed += Time.deltaTime;
            float pulse = (Mathf.Sin(elapsed * 12f) + 1f) * 0.5f;
            if (sr != null)
                sr.color = Color.Lerp(originalColor, new Color(2f, 0.1f, 0.05f, 1f), 0.4f + pulse * 0.6f);
            yield return null;
        }
        if (sr != null) sr.color = originalColor;

        // Screen shake saat rage masuk
        if (CameraFollow.instance != null)
            CameraFollow.instance.TriggerScreenShake(0.6f, 0.55f);

        // Update nama bos di health bar menjadi "RAGE MODE"
        UpdateBossNameRage();

        // Health bar berkedip merah
        yield return StartCoroutine(FlashHealthBarRed());

        // Aktifkan rage
        isRageMode  = true;
        isAttacking = false;
        canMove     = true;

        // Langsung ultimate saat masuk rage (CD mulai dari 5 detik, bukan 40)
        ultimateTimer = 5f;
    }

    private void UpdateBossNameRage()
    {
        if (bossHealthBar == null) return;
        Text t = bossHealthBar.GetComponentInChildren<Text>();
        if (t != null)
        {
            t.text  = "King Demon Slime  [RAGE]";
            t.color = new Color(1f, 0.3f, 0.1f, 1f);
        }
    }

    private IEnumerator FlashHealthBarRed()
    {
        if (bossHealthBar == null) yield break;
        Image fill = bossHealthBar.fillRect != null ? bossHealthBar.fillRect.GetComponent<Image>() : null;
        if (fill == null) yield break;

        Color origFill = fill.color;
        for (int i = 0; i < 5; i++)
        {
            fill.color = Color.red;
            yield return new WaitForSeconds(0.12f);
            fill.color = origFill;
            yield return new WaitForSeconds(0.10f);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPER EFFECTS
    // ─────────────────────────────────────────────────────────────────────────
    private IEnumerator BrutalChargeEffect(float duration)
    {
        // Pulsasi warna ungu-merah + scaleup sedikit (efek ngeri)
        Vector3 origScale = transform.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float pulse = (Mathf.Sin(elapsed * 18f) + 1f) * 0.5f;
            if (sr != null)
                sr.color = Color.Lerp(new Color(0.6f, 0.0f, 0.8f, 1f), new Color(1.5f, 0.05f, 0.05f, 1f), pulse);
            // Scale sedikit lebih besar saat charge
            transform.localScale = Vector3.Lerp(origScale, origScale * 1.15f, t * 0.5f);
            yield return null;
        }
        transform.localScale = origScale;
        if (sr != null) sr.color = originalColor;
    }

    private IEnumerator ScreenFlashWhite(float holdDur, float fadeDur)
    {
        // Buat canvas overlay putih sesaat
        GameObject go = new GameObject("BossUlt_Flash");
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode  = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 85;
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(go.transform, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        Image img = panel.AddComponent<Image>();
        img.color = new Color(1f, 0.85f, 0.7f, 0.85f);
        img.raycastTarget = false;

        yield return new WaitForSecondsRealtime(holdDur);

        float elapsed = 0f;
        while (elapsed < fadeDur)
        {
            elapsed += Time.unscaledDeltaTime;
            img.color = new Color(1f, 0.85f, 0.7f, Mathf.Lerp(0.85f, 0f, elapsed / fadeDur));
            yield return null;
        }
        Destroy(go);
    }

    private IEnumerator QuickFlash(Color flashCol, float dur)
    {
        if (sr == null) yield break;
        sr.color = flashCol;
        yield return new WaitForSeconds(dur);
        if (sr != null) sr.color = originalColor;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TAKE DAMAGE (Override dari Entity)
    // ─────────────────────────────────────────────────────────────────────────
    public override void TakeDamage(int dmg, bool applyStun = false)
    {
        if (isDead || isTakingDamage) return;
        if (UI.instance != null && UI.instance.isGameOver) return;

        if (audioSource != null && hitSound != null)
            audioSource.PlayOneShot(hitSound);

        currentHealth -= dmg;

        if (bossHealthBar != null)
            bossHealthBar.value = currentHealth;

        if (applyStun)
        {
            isTakingDamage = true;
            isAttacking    = false;
            if (attackTimeoutCoroutine != null) StopCoroutine(attackTimeoutCoroutine);
            if (anim != null) anim.SetTrigger("Hit");
            StartCoroutine(HitRoutine());
        }
        else
        {
            StartCoroutine(DamageFlashOnly());
        }

        if (currentHealth <= 0) Die();
    }

    private IEnumerator DamageFlashOnly()
    {
        if (sr != null) sr.color = hitColor;
        yield return new WaitForSeconds(hitDuration);
        if (sr != null) sr.color = isRageMode ? new Color(1f, 0.4f, 0.4f, 1f) : originalColor;
    }

    private IEnumerator HitRoutine()
    {
        if (sr != null) sr.color = hitColor;
        isKnocked = true;
        yield return new WaitForSeconds(hitDuration);
        if (sr != null) sr.color = isRageMode ? new Color(1f, 0.4f, 0.4f, 1f) : originalColor;
        yield return new WaitForSeconds(knockbackTime);
        isKnocked      = false;
        isTakingDamage = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DIE
    // ─────────────────────────────────────────────────────────────────────────
    protected override void Die()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();
        isAttacking    = false;
        isKnocked      = false;
        isTakingDamage = false;

        Time.timeScale = 1.0f; // Pastikan timeScale normal jika mati saat ult

        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound);

        if (anim != null) anim.SetTrigger("Die");

        if (rb != null) { rb.velocity = Vector2.zero; rb.simulated = false; }

        if (bossHealthBar != null) bossHealthBar.gameObject.SetActive(false);

        StartCoroutine(MunculkanVictoryPanel());
        Destroy(gameObject, 5f);
    }

    private IEnumerator MunculkanVictoryPanel()
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

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC API (dipanggil dari BossTrigger / AnimationEvent)
    // ─────────────────────────────────────────────────────────────────────────
    public void ActivateBoss()
    {
        bossActive = true;
        if (bossHealthBar != null) bossHealthBar.value = currentHealth;
    }

    public void EndAttack()
    {
        isAttacking = false;
        if (attackTimeoutCoroutine != null) StopCoroutine(attackTimeoutCoroutine);
    }

    public void DealDamage()
    {
        if (!isActiveAndEnabled || (UI.instance != null && UI.instance.isGameOver)) return;
        if (entityAttackPoint == null) return;

        int dmg = isRageMode ? skill1DamageRage : skill1Damage;
        Collider2D hitPlayer = Physics2D.OverlapCircle(entityAttackPoint.position, attackRange, playerLayer);
        if (hitPlayer != null)
        {
            Entity p = hitPlayer.GetComponent<Entity>();
            if (p != null) p.TakeDamage(dmg);
        }
        else if (player != null && Vector2.Distance(transform.position, player.position) <= attackRange + 1.5f)
        {
            Entity p = player.GetComponent<Entity>();
            if (p != null) p.TakeDamage(dmg);
        }
    }

    public void PlayRoar()
    {
        bossScale = Mathf.Clamp(bossScale, 1.5f, 2.0f);
        if (anim != null) { anim.ResetTrigger("Roar"); anim.SetTrigger("Roar"); }
        if (audioSource != null && roarSound != null)
            audioSource.PlayOneShot(roarSound, 1.3f);
        if (player != null)
        {
            float d = player.position.x - transform.position.x;
            float s = Mathf.Clamp(bossScale, 1.5f, 2.0f);
            if (d > 0.1f) transform.localScale = new Vector3(-s, s, 1f);
            else if (d < -0.1f) transform.localScale = new Vector3(s, s, 1f);
        }
        StartCoroutine(RoarEffectRoutine());
    }

    private IEnumerator RoarEffectRoutine()
    {
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
        if (bossHealthBar != null) StartCoroutine(AnimateHealthBarFill());
    }

    private IEnumerator AnimateHealthBarFill()
    {
        if (bossHealthBar == null) yield break;
        bossHealthBar.maxValue = maxHealth;
        bossHealthBar.value    = 0;
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            bossHealthBar.value = Mathf.Lerp(0, maxHealth, t / 0.6f);
            yield return null;
        }
        bossHealthBar.value = maxHealth;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UTILITY
    // ─────────────────────────────────────────────────────────────────────────
    private void FreezeBoss()
    {
        if (frozenOnGameOver) return;
        frozenOnGameOver = true;
        StopAllCoroutines();
        Time.timeScale = 1.0f;
        isAttacking = false;
        isKnocked   = false;
        canMove     = false;
        if (rb != null) { rb.velocity = Vector2.zero; rb.simulated = false; }
        if (anim != null) anim.enabled = false;
        enabled = false;
    }

    private void FlipToPlayer()
    {
        if (isAttacking || player == null) return;
        float d = player.position.x - transform.position.x;
        float s = Mathf.Clamp(bossScale, 1.5f, 2.0f);
        if (d > 0.1f)       transform.localScale = new Vector3(-s, s, 1f);
        else if (d < -0.1f) transform.localScale = new Vector3(s, s, 1f);
    }

    private void MoveToPlayer(float dir, float speed)
    {
        if (Mathf.Abs(dir) < 0.1f) { Stop(); return; }
        rb.velocity = new Vector2(Mathf.Sign(dir) * speed, rb.velocity.y);
        if (anim != null) anim.SetBool("isMoving", true);
    }

    private void Stop()
    {
        rb.velocity = Vector2.zero;
        if (anim != null) anim.SetBool("isMoving", false);
    }

    private IEnumerator AttackTimeout(float duration)
    {
        yield return new WaitForSeconds(duration);
        isAttacking = false;
    }

    void OnDrawGizmosSelected()
    {
        if (entityAttackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(entityAttackPoint.position, attackRange);
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, ultimateRadius);
        }
    }
}
