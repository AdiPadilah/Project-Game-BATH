using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// BossAI — King Demon Slime
///
/// SKILL 1 : Lava Dash  — Boss ngedash ke arah player (selalu aktif)
/// SKILL 2 : Twin Slash + Lava Burst — 2x tebasan + lava keluar dari tanah (selalu aktif)
/// SKILL 3 : Lava Gasing (Spin) — berputar seperti gasing dengan efek lava
///            HANYA aktif saat HP <= 50%
/// </summary>
public class BossAI : Entity
{
    // ─── Movement ────────────────────────────────────────────────────────────
    [Header("Movement")]
    public float moveSpeed   = 8f;
    public float stopDistance = 1.5f;

    // ─── Visual ──────────────────────────────────────────────────────────────
    [Header("Visual & Size")]
    public float bossScale = 1.8f;

    // ─── Skill 1: Lava Dash ──────────────────────────────────────────────────
    [Header("Skill 1 - Lava Dash")]
    public float skill1Cooldown = 4.0f;
    public int   skill1Damage   = 20;
    public float skill1DashSpeed = 24f;
    public float skill1DashDuration = 0.30f;

    // ─── Skill 2: Twin Slash + Lava Burst ────────────────────────────────────
    [Header("Skill 2 - Twin Slash + Lava Burst")]
    public float skill2Cooldown   = 6.0f;
    public int   skill2SlashDamage = 18;   // per tebasan (x2)
    public int   skill2LavaDamage  = 30;   // lava yang keluar
    public float skill2AttackRange = 3.5f;

    // ─── Skill 3: Lava Gasing (HP <= 50% only) ───────────────────────────────
    [Header("Skill 3 - Lava Gasing [HP < 50% ONLY]")]
    public float skill3Cooldown    = 10.0f;
    public int   skill3SpinDamage  = 15;   // damage per tick saat spin
    public float skill3SpinDuration = 3.0f;
    public float skill3SpinRadius  = 4.5f;
    private bool skill3Unlocked    = false; // unlock saat HP <= 50%

    // ─── Layer ───────────────────────────────────────────────────────────────
    public LayerMask playerLayer;

    // ─── UI ──────────────────────────────────────────────────────────────────
    [Header("UI")]
    public Slider bossHealthBar;

    // ─── Hit Effect ──────────────────────────────────────────────────────────
    [Header("Hit Effect")]
    public Color hitColor    = Color.red;
    public float hitDuration = 0.1f;

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

    // ─── FX Sprites (di-load dari Resources) ─────────────────────────────────
    private Sprite fxSlashArc;
    private Sprite fxSlashCross;
    private Sprite fxWhirlwind;
    private Sprite fxLavaPool;
    private Sprite fxLavaSplash;
    private Sprite fxSpinRing;

    // ─── State ───────────────────────────────────────────────────────────────
    private Transform player;
    private bool isAttacking      = false;
    private bool isKnocked        = false;
    private bool isDead           = false;
    public  bool bossActive       = false;
    private bool isTakingDamage   = false;
    private bool frozenOnGameOver = false;

    private float skill1Timer = 0f;
    private float skill2Timer = 0f;
    private float skill3Timer = 0f;

    private Color originalColor;
    private Coroutine attackTimeoutCoroutine;

    // ─────────────────────────────────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();
        currentHealth = maxHealth;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        if (sr != null) originalColor = sr.color;

        bossScale = Mathf.Abs(transform.localScale.y) > 0.1f
            ? Mathf.Abs(transform.localScale.y) : 1.8f;
        bossScale = Mathf.Clamp(bossScale, 1.5f, 2.0f);

        LoadFXSprites();

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

    private void LoadFXSprites()
    {
        fxSlashArc   = Resources.Load<Sprite>("FX/fx_slash_arc_gold");
        fxSlashCross = Resources.Load<Sprite>("FX/fx_slash_cross");
        fxWhirlwind  = Resources.Load<Sprite>("FX/fx_whirlwind_ring");
        fxLavaPool   = Resources.Load<Sprite>("FX/fx_lava_pool");
        fxLavaSplash = Resources.Load<Sprite>("FX/fx_lava_splash");
        fxSpinRing   = Resources.Load<Sprite>("FX/fx_spin_ring");
    }

    private void SetupBossNameText()
    {
        if (bossHealthBar == null) return;
        Text t = bossHealthBar.GetComponentInChildren<Text>();
        if (t == null)
        {
            GameObject go = new GameObject("BossNameLabel");
            go.transform.SetParent(bossHealthBar.transform, false);
            t = go.AddComponent<Text>();
            t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize  = 22;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color     = new Color(1f, 0.85f, 0.45f, 1f);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 4f);
            rt.sizeDelta = new Vector2(0f, 26f);
            Outline outline = go.AddComponent<Outline>();
            outline.effectColor    = new Color(0.15f, 0.02f, 0.02f, 1f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }
        t.text = "King Demon Slime";
    }

    protected virtual void Start() => SnapToGround();

    private void SnapToGround()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 30f, whatIsGround);
        if (hit.collider != null)
        {
            Collider2D bc = GetComponent<Collider2D>();
            float offset  = bc != null ? bc.bounds.extents.y : 1.4f;
            transform.position = new Vector3(transform.position.x, hit.point.y + offset, 0f);
            if (rb != null) rb.velocity = Vector2.zero;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────────────────────────────────
    protected override void Update()
    {
        if (UI.instance != null && UI.instance.isGameOver) { FreezeBoss(); return; }
        if (!bossActive || player == null || isDead) return;
        if (isKnocked)   { rb.velocity = Vector2.zero; return; }
        if (isAttacking) { rb.velocity = Vector2.zero; return; }

        // Tick timers
        if (skill1Timer > 0f) skill1Timer -= Time.deltaTime;
        if (skill2Timer > 0f) skill2Timer -= Time.deltaTime;
        if (skill3Timer > 0f) skill3Timer -= Time.deltaTime;

        // Cek unlock skill 3 (HP <= 50%)
        if (!skill3Unlocked && currentHealth <= maxHealth * 0.5f)
        {
            skill3Unlocked = true;
            skill3Timer    = 2.0f;  // Langsung siap dalam 2 detik setelah unlock
            // Flash peringatan merah
            StartCoroutine(UnlockSkill3Flash());
        }

        FlipToPlayer();

        float hDist = Mathf.Abs(player.position.x - transform.position.x);
        float vDist = Mathf.Abs(player.position.y - transform.position.y);
        bool  close = hDist <= skill2AttackRange && vDist <= 4f;

        // ── Prioritas skill ───────────────────────────────────────────────────
        // Skill 3: Gasing lava (paling kuat, prioritas tertinggi jika unlock)
        if (skill3Unlocked && skill3Timer <= 0f && hDist < skill3SpinRadius + 2f)
        {
            StartCoroutine(LavaGasingRoutine());
            return;
        }

        // Skill 1: Dash (jika player agak jauh)
        if (skill1Timer <= 0f && hDist > skill2AttackRange && hDist < 15f)
        {
            StartCoroutine(LavaDashRoutine());
            return;
        }

        // Skill 2: Twin Slash + Lava (jika player dekat)
        if (close && skill2Timer <= 0f)
        {
            StartCoroutine(TwinSlashLavaBurstRoutine());
            return;
        }

        // Jika tidak ada skill ready: dekati player biasa
        if (!close) MoveToPlayer(player.position.x - transform.position.x);
        else         Stop();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SKILL 1 — LAVA DASH
    // Boss melesat dengan jejak lava membakar ke arah player
    // ═════════════════════════════════════════════════════════════════════════
    private IEnumerator LavaDashRoutine()
    {
        isAttacking = true;
        skill1Timer = skill1Cooldown;

        if (anim != null) anim.SetBool("isMoving", false);

        // Windup singkat: warna oranye membara
        float wu = 0.22f, we = 0f;
        while (we < wu)
        {
            we += Time.deltaTime;
            if (sr != null) sr.color = Color.Lerp(originalColor, new Color(1.4f, 0.5f, 0.0f, 1f), we / wu);
            yield return null;
        }
        if (sr != null) sr.color = originalColor;

        if (player == null || isDead) { isAttacking = false; yield break; }

        float dir = Mathf.Sign(player.position.x - transform.position.x);
        rb.velocity = new Vector2(dir * skill1DashSpeed, rb.velocity.y);

        if (attackSound != null && audioSource != null)
            audioSource.PlayOneShot(attackSound, 1.1f);

        HashSet<Entity> hitSet  = new HashSet<Entity>();
        List<Vector3>   lavaPos = new List<Vector3>();
        float elapsed = 0f;

        while (elapsed < skill1DashDuration)
        {
            elapsed += Time.deltaTime;

            // Jejak lava saat dash
            if (Random.value < 0.4f)
            {
                Vector3 lp = transform.position + new Vector3(-dir * Random.Range(0.3f, 1.2f), -0.5f, 0f);
                lavaPos.Add(lp);
                SpawnFX(lp, fxLavaSplash, new Vector3(dir * 0.7f, 0.7f, 1f), 0f, 0.4f,
                        new Color(1f, 0.4f, 0.0f, 0.9f), false);
            }

            // Afterimage merah-oranye
            SpawnAfterimage(new Color(1f, 0.35f, 0.0f, 0.65f));

            // Damage saat menyentuh player
            Collider2D hit = Physics2D.OverlapCircle(transform.position, 1.4f, playerLayer);
            if (hit != null)
            {
                Entity ent = hit.GetComponent<Entity>();
                if (ent != null && !hitSet.Contains(ent))
                {
                    hitSet.Add(ent);
                    ent.TakeDamage(skill1Damage, true);
                }
            }

            yield return null;
        }

        rb.velocity = Vector2.zero;

        // Tinggalkan pool lava di tanah setelah dash
        Vector3 poolPos = transform.position + new Vector3(0f, -0.4f, 0f);
        SpawnFX(poolPos, fxLavaPool, new Vector3(2.0f, 1.2f, 1f), 0f, 1.8f,
                new Color(1f, 0.3f, 0.0f, 0.85f), true, 1.1f);

        // Screen shake kecil
        if (CameraFollow.instance != null)
            CameraFollow.instance.TriggerScreenShake(0.22f, 0.20f);

        yield return new WaitForSeconds(0.25f);
        isAttacking = false;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SKILL 2 — TWIN SLASH + LAVA BURST
    // 2 kali tebasan, lalu lava muncrat dari tanah di bawah player
    // ═════════════════════════════════════════════════════════════════════════
    private IEnumerator TwinSlashLavaBurstRoutine()
    {
        isAttacking = true;
        skill2Timer = skill2Cooldown;

        rb.velocity = Vector2.zero;
        if (anim != null) { anim.SetBool("isMoving", false); anim.SetTrigger("Cleave"); }

        if (attackSound != null && audioSource != null)
            audioSource.PlayOneShot(attackSound, 1.0f);

        // ── TEBASAN 1 ─────────────────────────────────────────────────────────
        yield return new WaitForSeconds(0.30f);

        Vector3 slash1Pos = transform.position + new Vector3((facingRight ? 1f : -1f) * 1.5f, 0.3f, 0f);
        SpawnFX(slash1Pos, fxSlashArc,  Vector3.one * 1.4f, facingRight ? 30f : -30f, 0.25f, Color.white);
        SpawnFX(slash1Pos, fxSlashCross, Vector3.one * 1.1f, 0f, 0.22f, new Color(1f, 0.7f, 0.2f, 0.9f));

        DealSlashDamage(skill2SlashDamage, false);

        if (CameraFollow.instance != null)
            CameraFollow.instance.TriggerScreenShake(0.20f, 0.18f);

        yield return new WaitForSeconds(0.28f);

        // ── TEBASAN 2 (lebih kuat) ────────────────────────────────────────────
        if (anim != null) anim.SetTrigger("Cleave");
        if (attackSound != null && audioSource != null)
            audioSource.PlayOneShot(attackSound, 1.2f);

        Vector3 slash2Pos = transform.position + new Vector3((facingRight ? 1f : -1f) * 1.8f, 0.0f, 0f);
        SpawnFX(slash2Pos, fxSlashCross, Vector3.one * 1.6f, facingRight ? -20f : 20f, 0.28f,
                new Color(1f, 0.85f, 0.3f, 1f));
        SpawnFX(slash2Pos, fxSlashArc,   new Vector3(facingRight ? 1.5f : -1.5f, 1.5f, 1f),
                0f, 0.25f, Color.white);

        DealSlashDamage(skill2SlashDamage, true);  // Tebasan 2 pakai knockback

        if (CameraFollow.instance != null)
            CameraFollow.instance.TriggerScreenShake(0.30f, 0.25f);

        yield return new WaitForSeconds(0.30f);

        // ── LAVA BURST: lava muncrat dari tanah ───────────────────────────────
        // Targetnya posisi player saat ini + sedikit offset random
        if (player != null)
        {
            int lavaBurstCount = 3;
            for (int i = 0; i < lavaBurstCount; i++)
            {
                float offsetX = Random.Range(-2.0f, 2.0f);
                Vector3 burstPos = player.position + new Vector3(offsetX, 0f, 0f);

                // Lava splash ke atas
                SpawnFX(burstPos, fxLavaSplash, new Vector3(Random.Range(0.8f, 1.3f), Random.Range(1.0f, 1.6f), 1f),
                        Random.Range(-15f, 15f), 0.55f, new Color(1f, 0.35f, 0.0f, 0.95f), true, 1.2f);

                // Pool lava di tanah
                SpawnFX(burstPos + Vector3.down * 0.3f, fxLavaPool,
                        new Vector3(1.5f + Random.value, 0.9f, 1f), 0f, 1.4f,
                        new Color(1f, 0.25f, 0.0f, 0.80f), true, 1.05f);

                // Damage area lava
                Collider2D[] hits = Physics2D.OverlapCircleAll(burstPos, 1.8f, playerLayer);
                foreach (var h in hits)
                {
                    Entity ent = h.GetComponent<Entity>();
                    if (ent != null) ent.TakeDamage(skill2LavaDamage / lavaBurstCount, false);
                }

                yield return new WaitForSeconds(0.14f);  // Jeda antar burst
            }

            if (CameraFollow.instance != null)
                CameraFollow.instance.TriggerScreenShake(0.38f, 0.32f);
        }

        yield return new WaitForSeconds(0.30f);
        isAttacking = false;
    }

    private void DealSlashDamage(int dmg, bool knockback)
    {
        if (player == null) return;
        float range = skill2AttackRange + 0.5f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            entityAttackPoint != null ? entityAttackPoint.position : transform.position,
            range, playerLayer);
        foreach (var h in hits)
        {
            Entity ent = h.GetComponent<Entity>();
            if (ent != null) ent.TakeDamage(dmg, knockback);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SKILL 3 — LAVA GASING (HP <= 50% ONLY)
    // Boss berputar seperti gasing, mengeluarkan lava ke segala arah
    // ═════════════════════════════════════════════════════════════════════════
    private IEnumerator LavaGasingRoutine()
    {
        isAttacking = true;
        skill3Timer = skill3Cooldown;

        rb.velocity = Vector2.zero;

        // ── WINDUP: Ancang-ancang sebelum gasing ─────────────────────────────
        if (roarSound != null && audioSource != null)
            audioSource.PlayOneShot(roarSound, 1.2f);

        if (anim != null) anim.SetTrigger("Roar");

        float windupDur = 0.6f, we = 0f;
        while (we < windupDur)
        {
            we += Time.deltaTime;
            float t = we / windupDur;
            if (sr != null) sr.color = Color.Lerp(originalColor, new Color(1.5f, 0.3f, 0.0f, 1f), t);
            yield return null;
        }

        if (CameraFollow.instance != null)
            CameraFollow.instance.TriggerScreenShake(0.3f, 0.25f);

        // ── SPIN: Berputar selama skill3SpinDuration detik ────────────────────
        float spinElapsed   = 0f;
        float spinAngle     = 0f;
        float damageTickCd  = 0.35f;
        float damageTickTimer = 0f;
        float lavaSpawnCd   = 0.18f;
        float lavaTimer     = 0f;

        HashSet<Entity> spinHitSet = new HashSet<Entity>();

        while (spinElapsed < skill3SpinDuration)
        {
            spinElapsed   += Time.deltaTime;
            spinAngle     += Time.deltaTime * 720f;      // 2 putaran per detik
            damageTickTimer += Time.deltaTime;
            lavaTimer       += Time.deltaTime;

            // Visual spin: rotasi dan warna berkedip
            transform.rotation = Quaternion.Euler(0f, 0f, spinAngle);
            if (sr != null)
            {
                float pulse = (Mathf.Sin(spinElapsed * 25f) + 1f) * 0.5f;
                sr.color = Color.Lerp(new Color(1.5f, 0.4f, 0.0f, 1f), new Color(1f, 0.1f, 0.0f, 1f), pulse);
            }

            // Cincin spin berputar di sekitar boss
            if (lavaTimer >= lavaSpawnCd)
            {
                lavaTimer = 0f;
                float a = spinAngle * Mathf.Deg2Rad;
                for (int i = 0; i < 4; i++)
                {
                    float ang = a + i * Mathf.PI * 0.5f;
                    Vector3 lp = transform.position + new Vector3(
                        Mathf.Cos(ang) * skill3SpinRadius * 0.7f,
                        Mathf.Sin(ang) * skill3SpinRadius * 0.4f, 0f);

                    SpawnFX(lp, fxLavaSplash, Vector3.one * Random.Range(0.6f, 1.0f),
                            Random.Range(0f, 360f), 0.30f, new Color(1f, 0.3f, 0.0f, 0.85f));
                }

                // Cincin besar berputar
                SpawnFX(transform.position, fxSpinRing, Vector3.one * (1.5f + Mathf.Sin(spinElapsed * 4f) * 0.3f),
                        spinAngle, 0.22f, new Color(1f, 0.45f, 0.0f, 0.8f), true, 1.2f);

                // Pool lava di bawah boss
                SpawnFX(transform.position + Vector3.down * 0.5f, fxLavaPool,
                        new Vector3(1.8f, 0.9f, 1f), 0f, 0.4f,
                        new Color(1f, 0.2f, 0.0f, 0.7f), true, 1.1f);
            }

            // Damage tick tiap 0.35 detik ke player dalam radius
            if (damageTickTimer >= damageTickCd)
            {
                damageTickTimer = 0f;
                spinHitSet.Clear(); // Reset set tiap tick agar bisa kena lagi

                Collider2D[] nearTargets = Physics2D.OverlapCircleAll(transform.position, skill3SpinRadius, playerLayer);
                foreach (var h in nearTargets)
                {
                    Entity ent = h.GetComponent<Entity>();
                    if (ent != null && !spinHitSet.Contains(ent))
                    {
                        spinHitSet.Add(ent);
                        ent.TakeDamage(skill3SpinDamage, false);
                    }
                }

                // Screen shake kecil per tick
                if (CameraFollow.instance != null)
                    CameraFollow.instance.TriggerScreenShake(0.15f, 0.12f);
            }

            yield return null;
        }

        // ── BERHENTI: Ledakan lava akhir ──────────────────────────────────────
        transform.rotation = Quaternion.identity;
        if (sr != null) sr.color = originalColor;

        // Ledakan lava besar di akhir spin
        if (CameraFollow.instance != null)
            CameraFollow.instance.TriggerScreenShake(0.55f, 0.45f);

        for (int i = 0; i < 8; i++)
        {
            float ang = i * Mathf.PI * 0.25f;
            Vector3 bpos = transform.position + new Vector3(
                Mathf.Cos(ang) * 2.5f, Mathf.Sin(ang) * 1.5f, 0f);
            SpawnFX(bpos, fxLavaSplash, Vector3.one * Random.Range(1.0f, 1.5f),
                    ang * Mathf.Rad2Deg, 0.6f, new Color(1f, 0.35f, 0.0f, 1f), true, 1.3f);
        }
        SpawnFX(transform.position, fxLavaPool, new Vector3(3.5f, 2.0f, 1f),
                0f, 1.5f, new Color(1f, 0.25f, 0.0f, 0.9f), true, 1.1f);

        // Damage ledakan akhir
        Collider2D[] finalHits = Physics2D.OverlapCircleAll(transform.position, skill3SpinRadius + 1f, playerLayer);
        foreach (var h in finalHits)
        {
            Entity ent = h.GetComponent<Entity>();
            if (ent != null) ent.TakeDamage(skill3SpinDamage * 3, true);
        }

        yield return new WaitForSeconds(0.4f);
        isAttacking = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UNLOCK SKILL 3 FLASH (efek visual peringatan)
    // ─────────────────────────────────────────────────────────────────────────
    private IEnumerator UnlockSkill3Flash()
    {
        // 3x kilat merah-oranye sebagai tanda bahaya
        for (int i = 0; i < 3; i++)
        {
            if (sr != null) sr.color = new Color(1.8f, 0.3f, 0.0f, 1f);
            yield return new WaitForSeconds(0.12f);
            if (sr != null) sr.color = originalColor;
            yield return new WaitForSeconds(0.10f);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TAKE DAMAGE
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
        if (sr != null) sr.color = originalColor;
    }

    private IEnumerator HitRoutine()
    {
        if (sr != null) sr.color = hitColor;
        isKnocked = true;
        yield return new WaitForSeconds(hitDuration);
        if (sr != null) sr.color = originalColor;
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
        transform.rotation = Quaternion.identity;
        Time.timeScale     = 1.0f;
        isAttacking    = false;
        isKnocked      = false;
        isTakingDamage = false;

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
        Player ps = FindObjectOfType<Player>();
        if (vm != null) vm.MunculkanMenang(ps != null ? ps.waktuMain : 999f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC API
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
        // Dipanggil dari Animation Event (Cleave)
        if (!isActiveAndEnabled || (UI.instance != null && UI.instance.isGameOver)) return;
        DealSlashDamage(skill2SlashDamage, false);
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
            if (d > 0.1f)       transform.localScale = new Vector3(-s, s, 1f);
            else if (d < -0.1f) transform.localScale = new Vector3(s, s, 1f);
        }
        StartCoroutine(RoarEffectRoutine());
    }

    private IEnumerator RoarEffectRoutine()
    {
        if (sr != null)
        {
            float e = 0f;
            while (e < 1.8f)
            {
                e += Time.deltaTime;
                float pulse = (Mathf.Sin(e * 14f) + 1f) * 0.5f;
                sr.color = Color.Lerp(originalColor, new Color(1.8f, 0.25f, 0.15f, 1f), 0.35f + pulse * 0.65f);
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
        transform.rotation = Quaternion.identity;
        Time.timeScale  = 1.0f;
        isAttacking     = false;
        isKnocked       = false;
        canMove         = false;
        if (rb  != null) { rb.velocity = Vector2.zero; rb.simulated = false; }
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

    private void MoveToPlayer(float dir)
    {
        if (Mathf.Abs(dir) < 0.1f) { Stop(); return; }
        rb.velocity = new Vector2(Mathf.Sign(dir) * moveSpeed, rb.velocity.y);
        if (anim != null) anim.SetBool("isMoving", true);
    }

    private void Stop()
    {
        rb.velocity = Vector2.zero;
        if (anim != null) anim.SetBool("isMoving", false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // VISUAL FX SPAWNER
    // ─────────────────────────────────────────────────────────────────────────
    private void SpawnFX(Vector3 pos, Sprite sprite, Vector3 scale, float rotZ,
                         float duration, Color color, bool expand = false, float endMul = 1.3f)
    {
        if (sprite == null) return;

        GameObject go = new GameObject("BossFX");
        go.transform.position   = pos;
        go.transform.rotation   = Quaternion.Euler(0, 0, rotZ);
        go.transform.localScale = scale;

        SpriteRenderer sr2 = go.AddComponent<SpriteRenderer>();
        sr2.sprite       = sprite;
        sr2.color        = color;
        sr2.sortingOrder = 50;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive
        sr2.material = mat;

        Vector3 targetScale = expand ? scale * endMul : scale * 1.1f;
        StartCoroutine(FadeFX(go, sr2, targetScale, duration));
    }

    private IEnumerator FadeFX(GameObject go, SpriteRenderer sr2, Vector3 targetScale, float dur)
    {
        if (go == null) yield break;
        Vector3 startScale = go.transform.localScale;
        Color   startCol   = sr2.color;
        float   elapsed    = 0f;
        while (elapsed < dur)
        {
            if (go == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            go.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            sr2.color = new Color(startCol.r, startCol.g, startCol.b,
                                  Mathf.Lerp(startCol.a, 0f, t));
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    private void SpawnAfterimage(Color tint)
    {
        if (sr == null || sr.sprite == null) return;
        GameObject ghost = new GameObject("BossAfterimage");
        ghost.transform.position   = sr.transform.position;
        ghost.transform.rotation   = sr.transform.rotation;
        ghost.transform.localScale = sr.transform.lossyScale;
        SpriteRenderer gsr = ghost.AddComponent<SpriteRenderer>();
        gsr.sprite       = sr.sprite;
        gsr.flipX        = sr.flipX;
        gsr.color        = tint;
        gsr.sortingOrder = sr.sortingOrder - 1;
        StartCoroutine(FadeFX(ghost, gsr, ghost.transform.localScale, 0.25f));
    }

    void OnDrawGizmosSelected()
    {
        if (entityAttackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(entityAttackPoint.position, skill2AttackRange);
        }
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, skill3SpinRadius);
    }
}
