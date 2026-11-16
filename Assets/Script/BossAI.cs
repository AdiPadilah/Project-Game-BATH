using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// BossAI — King Demon Slime
///
/// SKILL 1 : Lava Dash      — Boss melesat kencang ke arah player + jejak lava di lantai
/// SKILL 2 : Twin Slash    — 2x ayunan tebasan cakar/pedang + letusan lava di tanah bawah player
/// SKILL 3 : Cyclone Sweep — Seperti Skill 2 BALMOND (Mobile Legends):
///            - Muter pedang KE SAMPING (horizontal, bukan ke atas!)
///            - Badan bos IKUT BERGERAK mengejar player sambil muter
///            - Berputar selama 3.5 detik dengan efek lava berputar horizontal
///            - HANYA aktif saat darah bos di bawah 50% (HP <= 50%)
/// </summary>
public class BossAI : Entity
{
    // ─── Movement ─────────────────────────────────────────────────────────────
    [Header("Movement")]
    public float moveSpeed    = 6f;
    public float stopDistance = 1.8f;

    // ─── Visual ───────────────────────────────────────────────────────────────
    [Header("Visual & Size")]
    public float bossScale = 1.8f;

    // ─── Skill 1: Lava Dash ───────────────────────────────────────────────────
    [Header("Skill 1 - Lava Dash")]
    public float skill1Cooldown  = 5.0f;
    public int   skill1Damage    = 22;
    public float skill1DashSpeed = 20f;
    public float skill1DashDur   = 0.28f;

    // ─── Skill 2: Twin Slash + Lava Pool ──────────────────────────────────────
    [Header("Skill 2 - Twin Slash + Lava Pool")]
    public float skill2Cooldown  = 6.0f;
    public int   skill2SlashDmg  = 20;
    public int   skill2LavaDmg   = 25;
    public float skill2Range     = 3.2f;

    // ─── Skill 3: Balmond Cyclone Sweep (HP <= 50% only) ──────────────────────
    [Header("Skill 3 - Balmond Cyclone Sweep [HP <= 50% ONLY]")]
    public float skill3Cooldown   = 12.0f;
    public int   skill3SpinDmg    = 14;     // Damage per tick saat memutar
    public float skill3Duration   = 3.5f;   // Durasi putaran
    public float skill3Radius     = 3.8f;   // Jangkauan pedang memutar
    public float skill3ChaseSpeed = 7.0f;   // Kecepatan bos berjalan sambil muter (Balmond style)
    private bool skill3Unlocked   = false;

    // ─── Layer ────────────────────────────────────────────────────────────────
    public LayerMask playerLayer;

    // ─── UI ───────────────────────────────────────────────────────────────────
    [Header("UI")]
    public Slider bossHealthBar;

    // ─── Hit Effect ───────────────────────────────────────────────────────────
    [Header("Hit Effect")]
    public Color hitColor    = Color.red;
    public float hitDuration = 0.1f;

    [Header("Knockback")]
    public float knockbackTime = 0.2f;

    // ─── Audio ────────────────────────────────────────────────────────────────
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   attackSound;
    public AudioClip   hitSound;
    public AudioClip   deathSound;
    public AudioClip   roarSound;

    // ─── FX Sprites ───────────────────────────────────────────────────────────
    private Sprite fxSlashArc;
    private Sprite fxSlashCross;
    private Sprite fxWhirlwind;
    private Sprite fxLavaPool;
    private Sprite fxLavaSplash;
    private Sprite fxSpinRing;
    private Sprite fxHorizontalCyclone;
    private Sprite fxHorizontalSlash;

    // ─── State ────────────────────────────────────────────────────────────────
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

        LoadFX();

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
            SetupNameText();
        }
    }

    private void LoadFX()
    {
        fxSlashArc          = Resources.Load<Sprite>("FX/fx_slash_arc_gold");
        fxSlashCross        = Resources.Load<Sprite>("FX/fx_slash_cross");
        fxWhirlwind         = Resources.Load<Sprite>("FX/fx_whirlwind_ring");
        fxLavaPool          = Resources.Load<Sprite>("FX/fx_lava_pool");
        fxLavaSplash        = Resources.Load<Sprite>("FX/fx_lava_splash");
        fxSpinRing          = Resources.Load<Sprite>("FX/fx_spin_ring");
        fxHorizontalCyclone = Resources.Load<Sprite>("FX/fx_horizontal_cyclone");
        fxHorizontalSlash   = Resources.Load<Sprite>("FX/fx_horizontal_slash");
    }

    private void SetupNameText()
    {
        if (bossHealthBar == null) return;
        Text t = bossHealthBar.GetComponentInChildren<Text>();
        if (t == null)
        {
            GameObject go = new GameObject("BossName");
            go.transform.SetParent(bossHealthBar.transform, false);
            t           = go.AddComponent<Text>();
            t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize  = 22;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color     = new Color(1f, 0.85f, 0.45f, 1f);
            RectTransform rt    = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 4f);
            rt.sizeDelta        = new Vector2(0f, 26f);
            Outline o           = go.AddComponent<Outline>();
            o.effectColor    = new Color(0.15f, 0.02f, 0.02f, 1f);
            o.effectDistance = new Vector2(1.5f, -1.5f);
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
            float off     = bc != null ? bc.bounds.extents.y : 1.4f;
            transform.position = new Vector3(transform.position.x, hit.point.y + off, 0f);
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
        if (isAttacking) return; // Note: isAttacking handled inside coroutines

        if (skill1Timer > 0f) skill1Timer -= Time.deltaTime;
        if (skill2Timer > 0f) skill2Timer -= Time.deltaTime;
        if (skill3Timer > 0f) skill3Timer -= Time.deltaTime;

        // Unlock Skill 3 saat HP turun ke 50%
        if (!skill3Unlocked && currentHealth <= maxHealth * 0.5f)
        {
            skill3Unlocked = true;
            skill3Timer    = 1.0f; // Segera siap digunakan
            StartCoroutine(Skill3UnlockWarning());
        }

        FlipToPlayer();

        float hDist = Mathf.Abs(player.position.x - transform.position.x);
        float vDist = Mathf.Abs(player.position.y - transform.position.y);
        bool  close = hDist <= skill2Range && vDist <= 3.5f;

        // Prioritas Skill:
        // 1. Skill 3 (Balmond Cyclone) jika unlock & siap
        if (skill3Unlocked && skill3Timer <= 0f && hDist <= 12f)
        {
            StartCoroutine(BalmondCycloneRoutine());
            return;
        }

        // 2. Skill 1 (Lava Dash) jika jarak menengah
        if (skill1Timer <= 0f && hDist > skill2Range + 0.5f && hDist < 14f)
        {
            StartCoroutine(LavaDashRoutine());
            return;
        }

        // 3. Skill 2 (Twin Slash + Lava) jika jarak dekat
        if (close && skill2Timer <= 0f)
        {
            StartCoroutine(TwinSlashRoutine());
            return;
        }

        // Gerak biasa jika tidak ada skill ready
        if (!close) MoveToPlayer(player.position.x - transform.position.x);
        else         Stop();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SKILL 1 — LAVA DASH
    // Boss ngedash lurus ke arah player, meninggalkan jejak lava di lantai
    // ═════════════════════════════════════════════════════════════════════════
    private IEnumerator LavaDashRoutine()
    {
        isAttacking = true;
        skill1Timer = skill1Cooldown;
        rb.velocity = Vector2.zero;

        if (anim != null) anim.SetBool("isMoving", false);

        // Windup: ancang-ancang menyala oranye
        yield return StartCoroutine(ColorPulse(new Color(1.3f, 0.55f, 0.0f, 1f), 0.22f));

        if (player == null || isDead) { isAttacking = false; yield break; }

        if (attackSound != null && audioSource != null)
            audioSource.PlayOneShot(attackSound, 1.1f);

        float dir     = Mathf.Sign(player.position.x - transform.position.x);
        float elapsed = 0f;
        HashSet<Entity> hitSet = new HashSet<Entity>();

        while (elapsed < skill1DashDur)
        {
            elapsed += Time.deltaTime;
            rb.velocity = new Vector2(dir * skill1DashSpeed, rb.velocity.y);

            SpawnAfterimage(new Color(0.9f, 0.3f, 0.0f, 0.55f));

            // Jejak lava di tanah di bawah kaki
            if (Random.value < 0.35f)
            {
                Vector3 groundPos = new Vector3(
                    transform.position.x - dir * Random.Range(0.2f, 0.8f),
                    transform.position.y - 0.55f,
                    0f);
                SpawnFX(groundPos, fxLavaPool,
                        new Vector3(Random.Range(0.5f, 0.9f), 0.5f, 1f),
                        0f, 0.6f, new Color(1f, 0.3f, 0.0f, 0.75f), true, 1.05f);
            }

            // Tabrak player
            Collider2D hit = Physics2D.OverlapCircle(transform.position, 1.5f, playerLayer);
            if (hit != null)
            {
                Entity ent = hit.GetComponent<Entity>();
                if (ent != null && !hitSet.Contains(ent))
                {
                    hitSet.Add(ent);
                    ent.TakeDamage(skill1Damage, true);
                    SpawnFX(hit.transform.position, fxSlashCross,
                            Vector3.one * 0.9f, Random.Range(-20f, 20f),
                            0.22f, new Color(1f, 0.6f, 0.1f, 0.95f));
                }
            }

            yield return null;
        }

        rb.velocity = Vector2.zero;

        if (CameraFollow.instance != null)
            CameraFollow.instance.TriggerScreenShake(0.20f, 0.18f);

        // Pool lava tersisa di tanah
        Vector3 poolGround = new Vector3(transform.position.x, transform.position.y - 0.55f, 0f);
        SpawnFX(poolGround, fxLavaPool,
                new Vector3(2.2f, 0.9f, 1f), 0f, 1.5f,
                new Color(1f, 0.28f, 0.0f, 0.80f), true, 1.08f);

        yield return new WaitForSeconds(0.25f);
        isAttacking = false;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SKILL 2 — TWIN SLASH + LAVA POOL
    // 2x tebasan langsung + semburan lava dari tanah bawah kaki player
    // ═════════════════════════════════════════════════════════════════════════
    private IEnumerator TwinSlashRoutine()
    {
        isAttacking = true;
        skill2Timer = skill2Cooldown;
        rb.velocity = Vector2.zero;

        if (anim != null) { anim.SetBool("isMoving", false); anim.SetTrigger("Cleave"); }
        if (attackSound != null && audioSource != null)
            audioSource.PlayOneShot(attackSound, 1.0f);

        // ── AYUNAN 1 ─────────────────────────────────────────────────────────
        yield return new WaitForSeconds(0.28f);

        float faceDir = transform.localScale.x < 0 ? 1f : -1f;
        Vector3 swingOffset1 = new Vector3(faceDir * 1.3f, 0.2f, 0f);

        SpawnFX(transform.position + swingOffset1, fxSlashArc,
                new Vector3(faceDir * 1.3f, 1.3f, 1f),
                faceDir > 0 ? 25f : -25f, 0.22f,
                new Color(1f, 0.7f, 0.2f, 1f));
        SpawnFX(transform.position + swingOffset1 * 0.7f, fxSlashCross,
                Vector3.one * 0.9f, 0f, 0.20f, Color.white);

        HitMelee(skill2SlashDmg, false);

        if (CameraFollow.instance != null)
            CameraFollow.instance.TriggerScreenShake(0.18f, 0.15f);

        yield return new WaitForSeconds(0.30f);

        // ── AYUNAN 2 (lebih kuat) ────────────────────────────────────────────
        if (anim != null) anim.SetTrigger("Cleave");
        if (attackSound != null && audioSource != null)
            audioSource.PlayOneShot(attackSound, 1.15f);

        Vector3 swingOffset2 = new Vector3(faceDir * 1.5f, -0.1f, 0f);

        SpawnFX(transform.position + swingOffset2, fxSlashArc,
                new Vector3(faceDir * 1.5f, 1.5f, 1f),
                faceDir > 0 ? -20f : 20f, 0.25f,
                new Color(1f, 0.85f, 0.3f, 1f));
        SpawnFX(transform.position + swingOffset2 * 0.6f, fxSlashCross,
                Vector3.one * 1.1f, faceDir > 0 ? 15f : -15f, 0.22f,
                new Color(1f, 0.65f, 0.1f, 0.9f));

        HitMelee(skill2SlashDmg, true);

        if (CameraFollow.instance != null)
            CameraFollow.instance.TriggerScreenShake(0.28f, 0.22f);

        yield return new WaitForSeconds(0.25f);

        // ── LAVA BURST DI BAWAH KAKI PLAYER ──────────────────────────────────
        if (player != null)
        {
            for (int i = 0; i < 3; i++)
            {
                float offsetX = (i - 1) * 1.4f;
                Vector3 lavaGroundPos = new Vector3(
                    player.position.x + offsetX,
                    player.position.y - 0.4f,
                    0f);

                SpawnFX(lavaGroundPos, fxLavaSplash,
                        new Vector3(0.9f, 1.2f, 1f), 0f, 0.5f,
                        new Color(1f, 0.35f, 0.0f, 0.90f), true, 1.15f);

                SpawnFX(lavaGroundPos + Vector3.down * 0.1f, fxLavaPool,
                        new Vector3(1.4f, 0.7f, 1f), 0f, 1.2f,
                        new Color(1f, 0.22f, 0.0f, 0.78f), true, 1.06f);

                Collider2D[] nearPlayer = Physics2D.OverlapCircleAll(lavaGroundPos, 1.5f, playerLayer);
                foreach (var h in nearPlayer)
                {
                    Entity ent = h.GetComponent<Entity>();
                    if (ent != null) ent.TakeDamage(skill2LavaDmg / 3, false);
                }

                yield return new WaitForSeconds(0.15f);
            }

            if (CameraFollow.instance != null)
                CameraFollow.instance.TriggerScreenShake(0.32f, 0.28f);
        }

        yield return new WaitForSeconds(0.25f);
        isAttacking = false;
    }

    private void HitMelee(int dmg, bool knockback)
    {
        if (player == null) return;
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            entityAttackPoint != null
                ? entityAttackPoint.position
                : transform.position + new Vector3(transform.localScale.x < 0 ? 1.5f : -1.5f, 0f, 0f),
            skill2Range, playerLayer);
        foreach (var h in hits)
        {
            Entity ent = h.GetComponent<Entity>();
            if (ent != null) ent.TakeDamage(dmg, knockback);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SKILL 3 — BALMOND-STYLE CYCLONE SWEEP (HANYA SAAT HP <= 50%)
    // - Boss memutar pedang KE SAMPING (horizontal, elips mendatar di pinggang)
    // - Badan boss IKUT BERGERAK mengejar player sambil muter (Balmond Skill 2)
    // - Berlangsung selama 3.5 detik dengan damage bertubi-tubi + efek lava
    // ═════════════════════════════════════════════════════════════════════════
    private IEnumerator BalmondCycloneRoutine()
    {
        isAttacking = true;
        skill3Timer = skill3Cooldown;
        rb.velocity = Vector2.zero;

        // ── Ancang-ancang singkat: Roar + Cahaya Merah Membara ───────────────
        if (roarSound != null && audioSource != null)
            audioSource.PlayOneShot(roarSound, 1.2f);

        if (anim != null) anim.SetTrigger("Roar");

        // Warna merah menyala 0.35s
        yield return StartCoroutine(ColorPulse(new Color(1.6f, 0.2f, 0.0f, 1f), 0.35f));

        if (player == null || isDead) { isAttacking = false; yield break; }

        // ── Buat Objek Cyclone Horizontal (Muter ke Samping di Pinggang Boss) ──
        GameObject cycloneGO = new GameObject("Boss_Horizontal_Cyclone");
        cycloneGO.transform.position = transform.position + new Vector3(0f, 0.2f, 0f);
        SpriteRenderer cycloneSR = cycloneGO.AddComponent<SpriteRenderer>();
        cycloneSR.sprite       = fxHorizontalCyclone != null ? fxHorizontalCyclone : fxWhirlwind;
        cycloneSR.color        = new Color(1f, 0.5f, 0.1f, 0.95f);
        cycloneSR.sortingOrder = 55; // Di atas karakter

        Material cycloneMat = new Material(Shader.Find("Sprites/Default"));
        cycloneMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        cycloneMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive Glow!
        cycloneSR.material = cycloneMat;

        // Skala horizontal: LEBAR ke samping (5.0f), TIPIK ke atas (1.4f) -> Balmond horizontal spin!
        Vector3 cycloneBaseScale = new Vector3(5.0f, 1.4f, 1f);
        cycloneGO.transform.localScale = cycloneBaseScale;

        // ── Putaran Balmond: Berlangsung 3.5 Detik ────────────────────────────
        float elapsed         = 0f;
        float damageTickTimer = 0f;
        float damageTickCd    = 0.28f; // Damage tiap 0.28 detik
        float slashFxTimer    = 0f;
        float slashFxCd       = 0.14f; // Ayunan tebasan horizontal tiap 0.14s
        float afterimageTimer = 0f;
        bool  slashFlip       = false;

        if (anim != null) anim.SetBool("isMoving", true); // Kaki berjalan saat muter

        while (elapsed < skill3Duration)
        {
            if (isDead) break;

            elapsed         += Time.deltaTime;
            damageTickTimer += Time.deltaTime;
            slashFxTimer    += Time.deltaTime;
            afterimageTimer += Time.deltaTime;

            // 1. BADAN BOS IKUT GERAK MENGEJAR PLAYER (Balmond skill 2 style!)
            if (player != null)
            {
                float dirX = Mathf.Sign(player.position.x - transform.position.x);
                float hDistance = Mathf.Abs(player.position.x - transform.position.x);

                // Bergerak maju mengejar player
                if (hDistance > 0.8f)
                    rb.velocity = new Vector2(dirX * skill3ChaseSpeed, rb.velocity.y);
                else
                    rb.velocity = new Vector2(dirX * 2.0f, rb.velocity.y);

                // Hadapkan badan ke arah player
                float s = Mathf.Clamp(bossScale, 1.5f, 2.0f);
                if (dirX > 0.1f)       transform.localScale = new Vector3(-s, s, 1f);
                else if (dirX < -0.1f) transform.localScale = new Vector3(s,  s, 1f);
            }

            // Cyclone horizontal selalu mengikuti posisi pinggang bos
            if (cycloneGO != null)
            {
                cycloneGO.transform.position = transform.position + new Vector3(0f, 0.25f, 0f);
                // Efek denyut membesar-mengecil sedikit saat memutar
                float pulse = 1f + Mathf.Sin(elapsed * 16f) * 0.08f;
                cycloneGO.transform.localScale = new Vector3(cycloneBaseScale.x * pulse, cycloneBaseScale.y * pulse, 1f);
            }

            // 2. MUTER KE SAMPING: Tebasan pedang horizontal bergantian kiri-kanan
            if (slashFxTimer >= slashFxCd)
            {
                slashFxTimer = 0f;
                slashFlip    = !slashFlip;

                float faceDir = transform.localScale.x < 0 ? 1f : -1f;
                float slashSide = slashFlip ? 1f : -1f;

                // Ayunan tebasan horizontal membentang ke samping
                Vector3 slashPos = transform.position + new Vector3(slashSide * 0.6f, 0.2f, 0f);
                SpawnFX(slashPos,
                        fxHorizontalSlash != null ? fxHorizontalSlash : fxSlashArc,
                        new Vector3(slashSide * 4.4f, 1.3f, 1f), // Mendatar ke samping!
                        Random.Range(-8f, 8f),                  // Nyaris datar horizontal
                        0.20f,
                        new Color(1f, 0.65f, 0.15f, 0.95f));

                // Suara tebasan berputar cepat
                if (attackSound != null && audioSource != null && Random.value < 0.65f)
                    audioSource.PlayOneShot(attackSound, 0.75f);

                // Percikan lava terlempar ke samping kiri & kanan
                Vector3 sparkPos = transform.position + new Vector3(slashSide * 2.2f, -0.3f, 0f);
                SpawnFX(sparkPos, fxLavaSplash,
                        new Vector3(Random.Range(0.6f, 0.9f), Random.Range(0.7f, 1.0f), 1f),
                        slashSide * 30f, 0.35f,
                        new Color(1f, 0.35f, 0.0f, 0.85f));
            }

            // Afterimage api di belakang badan bos
            if (afterimageTimer >= 0.10f)
            {
                afterimageTimer = 0f;
                SpawnAfterimage(new Color(1f, 0.3f, 0.0f, 0.6f));
            }

            // 3. MULTI-HIT DAMAGE: Setiap 0.28 detik, siapa pun dalam jangkauan kena tebasan!
            if (damageTickTimer >= damageTickCd)
            {
                damageTickTimer = 0f;

                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, skill3Radius, playerLayer);
                foreach (var h in hits)
                {
                    Entity ent = h.GetComponent<Entity>();
                    if (ent != null)
                    {
                        ent.TakeDamage(skill3SpinDmg, false);

                        // Efek benturan pedang di badan player
                        SpawnFX(ent.transform.position, fxSlashCross,
                                Vector3.one * 1.0f, Random.Range(-30f, 30f),
                                0.20f, Color.white);
                    }
                }

                // Screen shake halus per hit
                if (CameraFollow.instance != null && hits.Length > 0)
                    CameraFollow.instance.TriggerScreenShake(0.14f, 0.15f);
            }

            yield return null;
        }

        // Hancurkan efek cyclone horizontal setelah selesai
        if (cycloneGO != null) Destroy(cycloneGO);

        rb.velocity = Vector2.zero;
        if (anim != null) anim.SetBool("isMoving", false);

        // ── 4. FINISHER PUTARAN: Ayunan Terakhir Menyapu Bersih ────────────────
        if (!isDead)
        {
            if (attackSound != null && audioSource != null)
                audioSource.PlayOneShot(attackSound, 1.3f);

            if (CameraFollow.instance != null)
                CameraFollow.instance.TriggerScreenShake(0.45f, 0.38f);

            // Tebasan horizontal besar terakhir ke kedua sisi
            SpawnFX(transform.position + new Vector3(0f, 0.2f, 0f),
                    fxHorizontalSlash != null ? fxHorizontalSlash : fxSlashArc,
                    new Vector3(5.5f, 1.6f, 1f), 0f, 0.35f,
                    new Color(1f, 0.85f, 0.3f, 1f));
            SpawnFX(transform.position + new Vector3(0f, 0.2f, 0f),
                    fxHorizontalSlash != null ? fxHorizontalSlash : fxSlashArc,
                    new Vector3(-5.5f, 1.6f, 1f), 0f, 0.35f,
                    new Color(1f, 0.85f, 0.3f, 1f));

            // Kolam lava di tanah di bawah bos
            Vector3 groundPos = new Vector3(transform.position.x, transform.position.y - 0.55f, 0f);
            SpawnFX(groundPos, fxLavaPool, new Vector3(3.0f, 1.1f, 1f), 0f, 1.8f,
                    new Color(1f, 0.25f, 0.0f, 0.85f), true, 1.1f);

            // Damage akhir + knockback terpental
            Collider2D[] finalHits = Physics2D.OverlapCircleAll(transform.position, skill3Radius + 0.5f, playerLayer);
            foreach (var h in finalHits)
            {
                Entity ent = h.GetComponent<Entity>();
                if (ent != null) ent.TakeDamage(skill3SpinDmg * 2, true);
            }
        }

        yield return new WaitForSeconds(0.35f);
        isAttacking = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UNLOCK SKILL 3 WARNING (Flash merah saat HP <= 50%)
    // ─────────────────────────────────────────────────────────────────────────
    private IEnumerator Skill3UnlockWarning()
    {
        for (int i = 0; i < 4; i++)
        {
            if (sr != null) sr.color = new Color(1.6f, 0.2f, 0.0f, 1f);
            yield return new WaitForSeconds(0.10f);
            if (sr != null) sr.color = originalColor;
            yield return new WaitForSeconds(0.09f);
        }
        if (roarSound != null && audioSource != null)
            audioSource.PlayOneShot(roarSound, 1.0f);
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
        Time.timeScale  = 1.0f;
        isAttacking     = false;
        isKnocked       = false;
        isTakingDamage  = false;

        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound);
        if (anim != null) anim.SetTrigger("Die");
        if (rb   != null) { rb.velocity = Vector2.zero; rb.simulated = false; }
        if (bossHealthBar != null) bossHealthBar.gameObject.SetActive(false);

        StartCoroutine(MunculkanVictory());
        Destroy(gameObject, 5f);
    }

    private IEnumerator MunculkanVictory()
    {
        yield return new WaitForSecondsRealtime(2f);
        VictoryManager vm = FindObjectOfType<VictoryManager>(true);
        Player ps         = FindObjectOfType<Player>();
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

    public void EndAttack() => isAttacking = false;

    public void DealDamage()
    {
        if (!isActiveAndEnabled || (UI.instance != null && UI.instance.isGameOver)) return;
        HitMelee(skill2SlashDmg, false);
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
            if (d >  0.1f) transform.localScale = new Vector3(-s, s, 1f);
            if (d < -0.1f) transform.localScale = new Vector3(s,  s, 1f);
        }
        StartCoroutine(RoarEffect());
    }

    private IEnumerator RoarEffect()
    {
        if (sr != null)
        {
            float e = 0f;
            while (e < 1.8f)
            {
                e += Time.deltaTime;
                float p = (Mathf.Sin(e * 14f) + 1f) * 0.5f;
                sr.color = Color.Lerp(originalColor, new Color(1.8f, 0.25f, 0.15f, 1f), 0.35f + p * 0.65f);
                yield return null;
            }
            sr.color = originalColor;
        }
        if (bossHealthBar != null) StartCoroutine(AnimateBar());
    }

    private IEnumerator AnimateBar()
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
        Time.timeScale = 1.0f;
        isAttacking = false;
        isKnocked   = false;
        canMove     = false;
        if (rb   != null) { rb.velocity = Vector2.zero; rb.simulated = false; }
        if (anim != null) anim.enabled = false;
        enabled = false;
    }

    private void FlipToPlayer()
    {
        if (isAttacking || player == null) return;
        float d = player.position.x - transform.position.x;
        float s = Mathf.Clamp(bossScale, 1.5f, 2.0f);
        if (d >  0.1f) transform.localScale = new Vector3(-s, s, 1f);
        if (d < -0.1f) transform.localScale = new Vector3(s,  s, 1f);
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

    private IEnumerator ColorPulse(Color targetColor, float duration)
    {
        if (sr == null) { yield return new WaitForSeconds(duration); yield break; }
        float e = 0f;
        while (e < duration * 0.5f)
        {
            e += Time.deltaTime;
            sr.color = Color.Lerp(originalColor, targetColor, e / (duration * 0.5f));
            yield return null;
        }
        e = 0f;
        while (e < duration * 0.5f)
        {
            e += Time.deltaTime;
            sr.color = Color.Lerp(targetColor, originalColor, e / (duration * 0.5f));
            yield return null;
        }
        sr.color = originalColor;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FX SPAWNER
    // ─────────────────────────────────────────────────────────────────────────
    private void SpawnFX(Vector3 pos, Sprite sprite, Vector3 scale, float rotZ,
                         float duration, Color color, bool expand = false, float endMul = 1.2f)
    {
        if (sprite == null) return;

        GameObject go = new GameObject("BossFX");
        go.transform.position   = pos;
        go.transform.rotation   = Quaternion.Euler(0, 0, rotZ);
        go.transform.localScale = scale;

        SpriteRenderer s2 = go.AddComponent<SpriteRenderer>();
        s2.sprite       = sprite;
        s2.color        = color;
        s2.sortingOrder = 50;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive Glow
        s2.material = mat;

        StartCoroutine(FadeFX(go, s2, expand ? scale * endMul : scale * 1.08f, duration));
    }

    private IEnumerator FadeFX(GameObject go, SpriteRenderer s2, Vector3 targetScale, float dur)
    {
        if (go == null) yield break;
        Vector3 s0  = go.transform.localScale;
        Color   c0  = s2.color;
        float   el  = 0f;
        while (el < dur)
        {
            if (go == null) yield break;
            el += Time.deltaTime;
            float t = el / dur;
            go.transform.localScale = Vector3.Lerp(s0, targetScale, t);
            s2.color = new Color(c0.r, c0.g, c0.b, Mathf.Lerp(c0.a, 0f, t));
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
        SpriteRenderer g = ghost.AddComponent<SpriteRenderer>();
        g.sprite       = sr.sprite;
        g.flipX        = sr.flipX;
        g.color        = tint;
        g.sortingOrder = sr.sortingOrder - 1;
        StartCoroutine(FadeFX(ghost, g, ghost.transform.localScale, 0.22f));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (entityAttackPoint != null)
            Gizmos.DrawWireSphere(entityAttackPoint.position, skill2Range);
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, skill3Radius);
    }
}
