using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// BossAI — King Demon Slime
///
/// SKILL 1 : Lava Dash        — Boss melesat kencang ke arah player + jejak lava di lantai
/// SKILL 2 : Twin Slash      — 2x ayunan tebasan cakar/pedang + letusan lava di tanah bawah player
/// SKILL 3 : Magma Cataclysm — (HP <= 50% ONLY):
///            FASE 1: Bola Api Lava — Bos mengayunkan pedang raksasa 3x berturut-turut (animasi Cleave penuh),
///                    setiap sabetan melontarkan bola api magma tebal melengkung ke posisi player.
///            FASE 2: Semburan Lahar Vulkanik — Bos meraung dahsyat (animasi Roar penuh), tubuh bergetar hebat,
///                    menyemburkan arus lahar TEBAL, PADAT & MENYALA langsung ke arah lantai dan tubuh player!
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
    public float skill2Range     = 3.0f;

    // ─── Skill 3: Magma Cataclysm (HP <= 50% only) ────────────────────────────
    [Header("Skill 3 - Magma Cataclysm [HP <= 50% ONLY]")]
    public float skill3Cooldown   = 12.0f;
    public int   skill3BallDamage = 20;     // Damage bola api lava
    public int   skill3BreathDmg  = 9;      // Damage per tick semburan lahar tebal (multi-hit)
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
    private Sprite fxLavaBall;
    private Sprite fxLavaPillar;
    private Sprite fxLavaStreamBody;
    private Sprite fxThickFireball;
    private Sprite[] roarAnimSprites;

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

        // Skala Damage Skill Bos berdasarkan Tingkat Kesulitan
        float dmgMult = DifficultyManager.GetEnemyDamageMultiplier();
        skill1Damage     = Mathf.Max(1, Mathf.RoundToInt(skill1Damage * dmgMult));
        skill3BallDamage = Mathf.Max(1, Mathf.RoundToInt(skill3BallDamage * dmgMult));
        skill3BreathDmg  = Mathf.Max(1, Mathf.RoundToInt(skill3BreathDmg * dmgMult));

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
        fxLavaBall          = Resources.Load<Sprite>("FX/fx_lava_ball");
        fxLavaPillar        = Resources.Load<Sprite>("FX/fx_lava_pillar");
        fxLavaStreamBody    = Resources.Load<Sprite>("FX/fx_lava_stream_body");
        fxThickFireball     = Resources.Load<Sprite>("FX/fx_thick_fireball");

        roarAnimSprites = new Sprite[6];
        for (int i = 0; i < 6; i++)
        {
            roarAnimSprites[i] = Resources.Load<Sprite>($"FX/BosRoar/demon_roar_{i + 1}");
        }
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
        if (isAttacking) return;

        if (skill1Timer > 0f) skill1Timer -= Time.deltaTime;
        if (skill2Timer > 0f) skill2Timer -= Time.deltaTime;
        if (skill3Timer > 0f) skill3Timer -= Time.deltaTime;

        // Unlock Skill 3 saat HP turun ke 50%
        if (!skill3Unlocked && currentHealth <= maxHealth * 0.5f)
        {
            skill3Unlocked = true;
            skill3Timer    = 1.0f;
            StartCoroutine(Skill3UnlockWarning());
        }

        FlipToPlayer();

        float hDist = Mathf.Abs(player.position.x - transform.position.x);
        float vDist = Mathf.Abs(player.position.y - transform.position.y);
        bool  close = hDist <= skill2Range && vDist <= 3.5f;

        // Prioritas Skill:
        // 1. Skill 3 (Magma Cataclysm) jika HP <= 50% & ready
        if (skill3Unlocked && skill3Timer <= 0f && hDist <= 16f)
        {
            StartCoroutine(MagmaCataclysmRoutine());
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

    protected virtual void LateUpdate()
    {
        // Mencegah teks dan health bar bos terbalik (mirror) saat bos menghadap kiri/kanan
        if (bossHealthBar != null)
        {
            Canvas c = bossHealthBar.GetComponentInParent<Canvas>();
            if (c != null && c.transform.parent == transform)
            {
                Vector3 s = c.transform.localScale;
                float sign = transform.localScale.x < 0 ? -1f : 1f;
                s.x = Mathf.Abs(s.x) * sign;
                c.transform.localScale = s;
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SKILL 1 — LAVA DASH
    // Boss melesat kencang ke arah player, meninggalkan jejak lava di lantai
    // ═════════════════════════════════════════════════════════════════════════
    private IEnumerator LavaDashRoutine()
    {
        isAttacking = true;
        skill1Timer = skill1Cooldown;
        rb.velocity = Vector2.zero;

        if (anim != null) anim.SetBool("isMoving", false);

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

        Vector3 poolGround = new Vector3(transform.position.x, transform.position.y - 0.55f, 0f);
        SpawnFX(poolGround, fxLavaPool,
                new Vector3(2.0f, 0.85f, 1f), 0f, 1.5f,
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

        // ── AYUNAN 2 ─────────────────────────────────────────────────────────
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
                        new Vector3(1.3f, 0.65f, 1f), 0f, 1.2f,
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
    // SKILL 3 — MAGMA CATACLYSM (HANYA SAAT HP <= 50%)
    //
    // FASE 1: BOLA API LAVA
    //         - Bos mengayunkan pedang raksasa (`Cleave` penuh) 3x berturut-turut!
    //         - Setiap sabetan melontarkan bola api magma tebal melengkung ke posisi player!
    // FASE 2: SEMBURAN LAHAR VULKANIK (TEBAL & TEPAT DI LANTAI/TUBUH PLAYER)
    //         - Bos meraung menggelegar (`Roar` penuh), tubuh bergetar hebat.
    //         - Menyemburkan arus lahar TEBAL, PADAT & MENYALA langsung di atas lantai
    //           dan ke arah tubuh player, bukan melayang di atas langit!
    // ═════════════════════════════════════════════════════════════════════════
    private IEnumerator MagmaCataclysmRoutine()
    {
        isAttacking = true;
        skill3Timer = skill3Cooldown;
        rb.velocity = Vector2.zero;

        ForceFacePlayer();
        float faceDir = player != null && player.position.x < transform.position.x ? -1f : 1f;

        // ─────────────────────────────────────────────────────────────────────
        // FASE 1: 3x AYUNAN PEDANG MELONTARKAN BOLA API MAGMA (CLEAVE)
        // ─────────────────────────────────────────────────────────────────────
        int orbCount = 3;
        for (int i = 0; i < orbCount; i++)
        {
            if (isDead) break;

            // Pastikan bos selalu menghadap player
            ForceFacePlayer();
            faceDir = player != null && player.position.x < transform.position.x ? -1f : 1f;

            // 1. Bos aktif mengangkat dan mengayunkan pedang raksasa (`Cleave` trigger penuh)
            if (anim != null)
            {
                anim.ResetTrigger("Cleave");
                anim.SetTrigger("Cleave");
            }

            // Suara sabetan pedang berat
            if (attackSound != null && audioSource != null)
                audioSource.PlayOneShot(attackSound, 1.15f);

            // Tunggu hingga frame ayunan pedang menebas ke depan (sekitar 0.35s)
            yield return new WaitForSeconds(0.35f);

            if (isDead) break;

            // 2. DETIK PENEBASAN: Bola api magma raksasa terlontar dari ujung pedang!
            Vector3 swordTipPos = transform.position + new Vector3(faceDir * 1.5f, -0.4f, 0f);

            // Flash api di ujung pedang saat bola terlontar
            SpawnFX(swordTipPos, fxLavaSplash, new Vector3(1.0f, 1.1f, 1f),
                    faceDir * 30f, 0.25f, new Color(1f, 0.7f, 0.2f, 1f));

            if (CameraFollow.instance != null)
                CameraFollow.instance.TriggerScreenShake(0.18f, 0.15f);

            // Target mendarat di posisi lantai player saat ini
            Vector3 targetLandingPos = player != null
                ? new Vector3(player.position.x + (i - 1) * 1.4f, transform.position.y - 1.4f, 0f)
                : transform.position + new Vector3(faceDir * (4.5f + i * 2f), -1.4f, 0f);

            // Luncurkan proyektil bola lava tebal
            StartCoroutine(LaunchLavaBall(swordTipPos, targetLandingPos, 0.58f, skill3BallDamage));

            // Tunggu sisa ayunan pedang sebelum ayunan berikutnya
            yield return new WaitForSeconds(0.40f);
        }

        yield return new WaitForSeconds(0.20f);
        if (isDead) { isAttacking = false; yield break; }

        // ─────────────────────────────────────────────────────────────────────
        // FASE 2: SEMBURAN LAHAR VULKANIK (MOLTEN MAGMA BREATH TORRENT)
        // ─────────────────────────────────────────────────────────────────────
        ForceFacePlayer();
        faceDir = player != null && player.position.x < transform.position.x ? -1f : 1f;

        // Suara raungan monster keras
        if (roarSound != null && audioSource != null)
            audioSource.PlayOneShot(roarSound, 1.4f);

        // 1. ANICANG-ANCANG RAUNGAN (Mouth opening progressive sequence)
        if (anim != null) anim.enabled = false;

        if (roarAnimSprites != null && roarAnimSprites.Length >= 6 && sr != null)
        {
            if (roarAnimSprites[0] != null) sr.sprite = roarAnimSprites[0];
            yield return new WaitForSeconds(0.06f);
            if (roarAnimSprites[1] != null) sr.sprite = roarAnimSprites[1];
            yield return new WaitForSeconds(0.06f);
            if (roarAnimSprites[2] != null) sr.sprite = roarAnimSprites[2];
            yield return new WaitForSeconds(0.08f);
        }

        // Tubuh bos membara oranye-kuning terang saat magma memuncak di kerongkongan
        yield return StartCoroutine(ColorPulse(new Color(2.4f, 0.4f, 0.05f, 1f), 0.25f));

        if (isDead)
        {
            if (anim != null) { anim.enabled = true; anim.Play("BosDeath"); }
            isAttacking = false;
            yield break;
        }

        // 2. POSISI SEMBURAN: Mulut bos tepat di depan kepala (menembak langsung ke arah player)
        Vector3 mouthPos = transform.position + new Vector3(faceDir * 1.0f, -0.2f, 0f);
        float floorLevelY = transform.position.y - 1.4f;
        Vector3 originalBodyPos = transform.position;

        // 3. Durasi Semburan: 2.4 Detik penuh!
        // - Mulut bos aktif bergetar meraung (demon_roar_4 & demon_roar_5 bergantian cepat)
        // - Tubuh bos bergetar hebat (violent recoil shake)
        // - 40 partikel lahar mengalir per detik menukik dari mulut membanjiri lantai & player
        // - Kolam lahar mendidih muncul di sepanjang tanah
        float breathDuration   = 2.4f;
        float breathElapsed    = 0f;
        float streamDropCd     = 0.025f; // Semburan partikel lahar tiap 0.025s (40 drops/s)
        float streamDropTimer  = 0f;
        float muzzleCd         = 0.07f;  // Letupan api di bibir mulut
        float muzzleTimer      = 0f;
        float poolCd           = 0.12f;  // Kolam lahar di tanah
        float poolTimer        = 0f;
        float damageTickCd     = 0.15f;  // Damage tick multi-hit cepat
        float damageTimer      = 0f;
        float maxStreamRange   = 8.5f;

        while (breathElapsed < breathDuration)
        {
            if (isDead) break;

            breathElapsed    += Time.deltaTime;
            streamDropTimer  += Time.deltaTime;
            muzzleTimer      += Time.deltaTime;
            poolTimer        += Time.deltaTime;
            damageTimer      += Time.deltaTime;

            // ANIMASI MULUT MERAUNG: Bergantian demon_roar_4 & demon_roar_5 tiap 0.08s
            if (sr != null && roarAnimSprites != null && roarAnimSprites.Length >= 6)
            {
                int fIdx = ((int)(breathElapsed * 12f) % 2 == 0) ? 3 : 4;
                if (roarAnimSprites[fIdx] != null) sr.sprite = roarAnimSprites[fIdx];
            }

            // ANIMASI GETARAN TUBUH (Violent recoil shudder saat memuntahkan lahar)
            float shakeX = Random.Range(-0.06f, 0.06f);
            float shakeY = Random.Range(-0.04f, 0.04f);
            transform.position = originalBodyPos + new Vector3(shakeX, shakeY, 0f);

            // Warna tubuh berdenyut panas membakar
            if (sr != null)
            {
                float p = (Mathf.Sin(breathElapsed * 28f) + 1f) * 0.5f;
                sr.color = Color.Lerp(new Color(2.2f, 0.5f, 0.1f, 1f), new Color(1.3f, 0.1f, 0.0f, 1f), p);
            }

            // SEMBURAN ARUS LAHAR: Semburkan partikel magma bertekanan tinggi setiap 0.025s
            if (streamDropTimer >= streamDropCd)
            {
                streamDropTimer = 0f;
                Vector3 sprayOrigin = mouthPos + new Vector3(shakeX, shakeY, 0f);
                StartCoroutine(LavaStreamDropRoutine(sprayOrigin, faceDir, floorLevelY));
            }

            // LETUPAN API DI BIBIR MULUT (Muzzle blast)
            if (muzzleTimer >= muzzleCd)
            {
                muzzleTimer = 0f;
                SpawnFX(mouthPos, fxLavaSplash,
                        new Vector3(0.85f, 1.0f, 1f),
                        faceDir * Random.Range(-25f, 25f), 0.16f,
                        new Color(1f, 0.85f, 0.25f, 1f));
            }

            // KOLAM LAHAR MENDIDIH DI TANAH
            if (poolTimer >= poolCd)
            {
                poolTimer = 0f;
                float rDist = Random.Range(1.5f, maxStreamRange);
                Vector3 poolPos = new Vector3(transform.position.x + faceDir * rDist, floorLevelY, 0f);
                SpawnFX(poolPos, fxLavaPool,
                        new Vector3(Random.Range(1.4f, 2.0f), 0.75f, 1f),
                        0f, 1.2f,
                        new Color(1f, 0.25f, 0.0f, 0.85f), true, 1.05f);
            }

            // Getaran kamera terus-menerus selama semburan
            if (CameraFollow.instance != null)
                CameraFollow.instance.TriggerScreenShake(0.14f, 0.16f);

            // Suara semburan api berkobar
            if (attackSound != null && audioSource != null && Random.value < 0.28f)
                audioSource.PlayOneShot(attackSound, 0.75f);

            // DAMAGE HITBOX TEPAT DI ATAS LANTAI (Menjangkau tubuh player secara akurat)
            if (damageTimer >= damageTickCd)
            {
                damageTimer = 0f;

                // Area semburan: dari kaki (floorLevelY) hingga setinggi player (tinggi 2.4 unit)
                Vector3 breathBoxCenter = new Vector3(transform.position.x + faceDir * (maxStreamRange * 0.5f), floorLevelY + 0.95f, 0f);
                Collider2D[] hits = Physics2D.OverlapBoxAll(breathBoxCenter, new Vector2(maxStreamRange, 2.4f), 0f, playerLayer);

                foreach (var h in hits)
                {
                    Entity ent = h.GetComponent<Entity>();
                    if (ent != null)
                    {
                        ent.TakeDamage(skill3BreathDmg, false);
                        // Efek benturan api di tubuh player
                        SpawnFX(ent.transform.position, fxLavaSplash,
                                Vector3.one * 1.0f, faceDir * 25f, 0.20f,
                                new Color(1f, 0.8f, 0.2f, 1f));
                    }
                }
            }

            yield return null;
        }

        // Kembalikan posisi dan warna badan bos ke normal
        transform.position = originalBodyPos;
        if (sr != null) sr.color = originalColor;

        // Penutupan mulut meraung
        if (sr != null && roarAnimSprites != null && roarAnimSprites.Length >= 6 && roarAnimSprites[5] != null)
        {
            sr.sprite = roarAnimSprites[5];
            yield return new WaitForSeconds(0.12f);
        }

        // Nyalakan kembali animator bos
        if (anim != null)
        {
            anim.enabled = true;
            anim.Play("BosIdle");
        }

        // LETUSAN PILAR LAHAR PENUTUP (3 pilar magma meletus dari tanah!)
        if (!isDead)
        {
            if (attackSound != null && audioSource != null)
                audioSource.PlayOneShot(attackSound, 1.35f);

            if (CameraFollow.instance != null)
                CameraFollow.instance.TriggerScreenShake(0.40f, 0.35f);

            for (int p = 1; p <= 3; p++)
            {
                float pillarDist = p * 2.4f;
                Vector3 pillarPos = new Vector3(transform.position.x + faceDir * pillarDist, floorLevelY, 0f);

                SpawnFX(pillarPos, fxLavaPillar != null ? fxLavaPillar : fxLavaSplash,
                        new Vector3(1.2f, 1.6f, 1f), 0f, 0.55f,
                        new Color(1f, 0.7f, 0.15f, 1f), true, 1.2f);

                SpawnFX(pillarPos, fxLavaPool, new Vector3(2.5f, 1.0f, 1f), 0f, 2.0f,
                        new Color(1f, 0.25f, 0.0f, 0.90f), true, 1.1f);
            }
        }

        ForceFacePlayer();
        yield return new WaitForSeconds(0.35f);
        isAttacking = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PARTIKEL ALIRAN LAHAR (FLUID MAGMA STREAM PARTICLE)
    // ─────────────────────────────────────────────────────────────────────────
    private IEnumerator LavaStreamDropRoutine(Vector3 startPos, float dir, float floorY)
    {
        GameObject drop = new GameObject("LavaStreamParticle");
        drop.transform.position = startPos;
        drop.transform.localScale = Vector3.one * 0.40f;

        SpriteRenderer dropSR = drop.AddComponent<SpriteRenderer>();
        dropSR.sprite = (Random.value < 0.65f && fxLavaBall != null) ? fxLavaBall : fxLavaSplash;
        dropSR.sortingLayerName = sr != null ? sr.sortingLayerName : "Default";
        dropSR.sortingOrder = (sr != null ? sr.sortingOrder : 0) + 3;

        Material mat = Resources.Load<Material>("Materials/UnlitAdditive");
        if (mat != null) dropSR.material = mat;

        dropSR.color = Color.Lerp(new Color(1f, 0.95f, 0.4f, 1f), new Color(1f, 0.45f, 0.05f, 1f), Random.value);

        float speedX = dir * Random.Range(11.0f, 16.5f);
        float speedY = Random.Range(-0.5f, 1.2f);
        float gravity = 10f;
        float maxLife = 0.60f;
        float elapsed = 0f;

        Vector3 curPos = startPos;
        float finalScale = Random.Range(1.1f, 1.55f);

        while (elapsed < maxLife)
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            speedY -= gravity * dt;
            curPos.x += speedX * dt;
            curPos.y += speedY * dt;
            drop.transform.position = curPos;

            float t = elapsed / maxLife;
            float currentScale = Mathf.Lerp(0.40f, finalScale, t);
            drop.transform.localScale = Vector3.one * currentScale;

            drop.transform.Rotate(0f, 0f, (speedX > 0 ? -360f : 360f) * dt);

            if (curPos.y <= floorY + 0.15f)
            {
                SpawnFX(new Vector3(curPos.x, floorY, 0f), fxLavaPool,
                        new Vector3(Random.Range(1.2f, 1.8f), 0.6f, 1f),
                        0f, 1.2f,
                        new Color(1f, 0.35f, 0.0f, 0.85f), true, 1.05f);

                SpawnFX(new Vector3(curPos.x, floorY + 0.3f, 0f), fxLavaSplash,
                        new Vector3(0.7f, 0.9f, 1f),
                        dir * Random.Range(-20f, 20f), 0.22f,
                        new Color(1f, 0.7f, 0.2f, 1f));
                break;
            }

            Collider2D pHit = Physics2D.OverlapCircle(curPos, 0.60f, playerLayer);
            if (pHit != null)
            {
                Entity ent = pHit.GetComponent<Entity>();
                if (ent != null)
                {
                    ent.TakeDamage(Mathf.Max(3, skill3BreathDmg / 2), false);
                    SpawnFX(curPos, fxLavaSplash, Vector3.one * 0.7f, 0f, 0.18f, new Color(1f, 0.8f, 0.2f, 1f));
                }
                break;
            }

            yield return null;
        }

        Destroy(drop);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PROYEKTIL BOLA LAVA TEBAL (HIGH IMPACT VOLCANIC MAGMA ORB)
    // ─────────────────────────────────────────────────────────────────────────
    private IEnumerator LaunchLavaBall(Vector3 start, Vector3 target, float duration, int directDmg)
    {
        GameObject orbGO = new GameObject("LavaOrb_ThickProjectile");
        orbGO.transform.position = start;
        orbGO.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

        SpriteRenderer srOrb = orbGO.AddComponent<SpriteRenderer>();
        srOrb.sprite = fxLavaBall != null ? fxLavaBall : fxLavaSplash;
        srOrb.color  = new Color(1f, 0.9f, 0.35f, 1f);
        srOrb.sortingOrder = 53;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive Glow
        srOrb.material = mat;

        float elapsed = 0f;
        float arcHeight = 2.6f;
        float rotSpeed = 420f;
        bool hasHitPlayer = false;

        while (elapsed < duration)
        {
            if (orbGO == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float currentX = Mathf.Lerp(start.x, target.x, t);
            float baseY    = Mathf.Lerp(start.y, target.y, t);
            float arcY     = Mathf.Sin(t * Mathf.PI) * arcHeight;

            Vector3 currentPos = new Vector3(currentX, baseY + arcY, 0f);
            orbGO.transform.position = currentPos;
            orbGO.transform.Rotate(0f, 0f, rotSpeed * Time.deltaTime);

            SpawnFX(currentPos, fxLavaSplash,
                    new Vector3(0.65f, 0.65f, 1f),
                    Random.Range(0f, 360f), 0.20f,
                    new Color(1f, 0.45f, 0.05f, 0.85f));

            if (!hasHitPlayer)
            {
                Collider2D airHit = Physics2D.OverlapCircle(currentPos, 0.9f, playerLayer);
                if (airHit != null)
                {
                    Entity ent = airHit.GetComponent<Entity>();
                    if (ent != null)
                    {
                        hasHitPlayer = true;
                        ent.TakeDamage(directDmg, false);
                        break;
                    }
                }
            }

            yield return null;
        }

        Vector3 impactPos = orbGO != null ? orbGO.transform.position : target;
        if (orbGO != null) Destroy(orbGO);

        if (CameraFollow.instance != null)
            CameraFollow.instance.TriggerScreenShake(0.22f, 0.18f);

        SpawnFX(impactPos, fxLavaSplash, new Vector3(1.4f, 1.6f, 1f),
                0f, 0.45f, new Color(1f, 0.6f, 0.15f, 1f), true, 1.25f);

        SpawnFX(new Vector3(impactPos.x, transform.position.y - 1.4f, 0f), fxLavaPool,
                new Vector3(1.8f, 0.85f, 1f), 0f, 2.0f,
                new Color(1f, 0.28f, 0.0f, 0.88f), true, 1.06f);

        if (!hasHitPlayer)
        {
            Collider2D[] groundHits = Physics2D.OverlapCircleAll(impactPos, 1.5f, playerLayer);
            foreach (var h in groundHits)
            {
                Entity ent = h.GetComponent<Entity>();
                if (ent != null) ent.TakeDamage(directDmg, false);
            }
        }
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
            if (anim != null) { anim.speed = 1.0f; anim.SetTrigger("Hit"); }
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
        if (anim != null) { anim.enabled = true; anim.speed = 1.0f; }
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
        if (anim != null) { anim.speed = 1.0f; anim.enabled = false; }
        transform.rotation = Quaternion.identity;
        Time.timeScale = 1.0f;
        isAttacking = false;
        isKnocked   = false;
        canMove     = false;
        if (rb   != null) { rb.velocity = Vector2.zero; rb.simulated = false; }
        enabled = false;
    }

    private void FlipToPlayer()
    {
        if (isAttacking || player == null) return;
        ForceFacePlayer();
    }

    private void ForceFacePlayer()
    {
        if (player == null) return;
        float d = player.position.x - transform.position.x;
        float s = Mathf.Clamp(bossScale, 1.5f, 2.0f);
        if (d >= 0f) transform.localScale = new Vector3(-s, s, 1f); // Menghadap kanan ke player
        else         transform.localScale = new Vector3(s,  s, 1f); // Menghadap kiri ke player
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
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
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
    }
}
