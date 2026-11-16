using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PlayerSkills - Mengelola 3 Skill Pedang Sakti Ksatria:
/// - Skill 1 [A]: Phantom Thrust (Tebasan Terobos Kilat Berapi Emas)
/// - Skill 2 [S]: Cyclone Crescent (Badai Tebasan Emas 360 Derajat)
/// - Skill 3 [W]: Judgement of the End (ULTIMATE Tebasan Dimensi Seribu Pedang)
/// </summary>
public class PlayerSkills : MonoBehaviour
{
    private Player player;
    private Rigidbody2D rb;
    private Animator anim;
    private AudioSource audioSource;
    private SpriteRenderer playerSR;

    // ─── Stat & Biaya Mana (Hemat & Sakti!) ──────────────────────────────────
    [Header("Skill 1 - Phantom Thrust [A]")]
    [SerializeField] private int   skill1Cost = 12;
    [SerializeField] private float skill1Cooldown = 2.2f;
    [SerializeField] private int   skill1Damage = 65;
    private float skill1Timer = 0f;

    [Header("Skill 2 - Cyclone Crescent [S]")]
    [SerializeField] private int   skill2Cost = 20;
    [SerializeField] private float skill2Cooldown = 4.0f;
    [SerializeField] private int   skill2Damage = 95;
    private float skill2Timer = 0f;

    [Header("Skill 3 - Judgement of the End (ULTIMATE) [W]")]
    [SerializeField] private int   skill3Cost = 45;
    [SerializeField] private float skill3Cooldown = 11.0f;
    [SerializeField] private int   skill3DamageTotal = 300;
    private float skill3Timer = 0f;

    [Header("Audio SFX")]
    private AudioClip sfxThrust;
    private AudioClip sfxCyclone;
    private AudioClip sfxUltimate;
    private AudioClip sfxUltimatePop;

    // Combat FX Sprites
    private Sprite fxSlashArc;
    private Sprite fxSlashCross;
    private Sprite fxThrustStreak;
    private Sprite fxWhirlwind;

    // Cut-In Assets
    private Sprite cutInBanner;
    private Sprite playerPortrait;

    // State
    public bool isCastingSkill = false;

    // HUD Elements
    private Image cdOverlay1;
    private Image cdOverlay2;
    private Image cdOverlay3;
    private TextMeshProUGUI cdText1;
    private TextMeshProUGUI cdText2;
    private TextMeshProUGUI cdText3;

    private void Awake()
    {
        player   = GetComponent<Player>();
        rb       = GetComponent<Rigidbody2D>();
        anim     = GetComponentInChildren<Animator>();
        playerSR = GetComponentInChildren<SpriteRenderer>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        LoadAssets();
    }

    private void Start()
    {
        BuildSkillBarUI();
    }

    private void LoadAssets()
    {
        sfxThrust      = Resources.Load<AudioClip>("Audio/skill_thrust");
        sfxCyclone     = Resources.Load<AudioClip>("Audio/skill_cyclone");
        sfxUltimate    = Resources.Load<AudioClip>("Audio/skill_ultimate");
        sfxUltimatePop = Resources.Load<AudioClip>("Audio/skill_ultimate_pop");

        fxSlashArc      = Resources.Load<Sprite>("FX/fx_slash_arc_gold");
        fxSlashCross    = Resources.Load<Sprite>("FX/fx_slash_cross");
        fxThrustStreak  = Resources.Load<Sprite>("FX/fx_thrust_streak");
        fxWhirlwind     = Resources.Load<Sprite>("FX/fx_whirlwind_ring");

        cutInBanner   = Resources.Load<Sprite>("UI/ult_cutin_banner");
        playerPortrait = Resources.Load<Sprite>("UI/hud_player_portrait");
    }

    private void Update()
    {
        if (player == null || (UI.instance != null && UI.instance.isGameOver))
            return;

        // Hitung mundur cooldown
        if (skill1Timer > 0f) skill1Timer -= Time.deltaTime;
        if (skill2Timer > 0f) skill2Timer -= Time.deltaTime;
        if (skill3Timer > 0f) skill3Timer -= Time.deltaTime;

        UpdateSkillUI();

        if (isCastingSkill) return;

        // ── Input Skill 1 [A] ───────────────────────────────────────────────
        if (Input.GetKeyDown(KeyCode.A))
        {
            TryCastSkill1();
        }

        // ── Input Skill 2 [S] ───────────────────────────────────────────────
        if (Input.GetKeyDown(KeyCode.S))
        {
            TryCastSkill2();
        }

        // ── Input Skill 3 [W] (Ultimate) ────────────────────────────────────
        if (Input.GetKeyDown(KeyCode.W))
        {
            TryCastSkill3();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SKILL 1 [A]: Phantom Thrust (Tebasan Terobos Kilat Cahaya Emas)
    // ─────────────────────────────────────────────────────────────────────────
    public void TryCastSkill1()
    {
        if (skill1Timer > 0f) return;
        if (player.GetCurrentMana() < skill1Cost) return;

        player.UseMana(skill1Cost);
        skill1Timer = skill1Cooldown;

        StartCoroutine(PhantomThrustRoutine());
    }

    private IEnumerator PhantomThrustRoutine()
    {
        isCastingSkill = true;
        player.canMove = false;
        player.SetInvincible(true);

        if (sfxThrust != null && audioSource != null)
            audioSource.PlayOneShot(sfxThrust, 1.1f);

        if (anim != null)
            anim.SetTrigger("dash");

        float dir = player.facingRight ? 1f : -1f;
        Vector3 startPos = transform.position;

        // 1. Munculkan visual streak laser energi tebasan menusuk
        Vector3 streakPos = startPos + new Vector3(dir * 2.2f, 0.1f, 0f);
        Vector3 streakScale = new Vector3(dir * 1.6f, 1.3f, 1f);
        SpawnVisualFX(streakPos, fxThrustStreak, streakScale, 0f, 0.28f, new Color(1f, 0.95f, 0.7f, 0.95f));

        // Screen shake ringan
        if (CameraFollow.instance != null)
            CameraFollow.instance.TriggerScreenShake(0.18f, 0.22f);

        float dashSpeed = 22f;
        float duration  = 0.24f;
        float elapsed   = 0f;

        HashSet<Entity> hitEnemies = new HashSet<Entity>();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rb.velocity = new Vector2(dir * dashSpeed, 0f);

            SpawnAfterimage(new Color(1f, 0.85f, 0.25f, 0.65f));

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.8f, player.GetEnemyLayer());
            foreach (var hit in hits)
            {
                Entity enemy = hit.GetComponent<Entity>();
                if (enemy != null && enemy != player && !hitEnemies.Contains(enemy))
                {
                    hitEnemies.Add(enemy);
                    enemy.TakeDamage(skill1Damage, false);
                    player.RestoreMana(8);

                    SpawnVisualFX(enemy.transform.position, fxSlashCross, Vector3.one * 0.9f, Random.Range(-25f, 25f), 0.22f, Color.white);
                }
            }

            yield return null;
        }

        rb.velocity = Vector2.zero;
        player.SetInvincible(false);
        player.canMove = true;
        isCastingSkill = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SKILL 2 [S]: Cyclone Crescent (Badai Tebasan Emas 360 Derajat Area)
    // ─────────────────────────────────────────────────────────────────────────
    public void TryCastSkill2()
    {
        if (skill2Timer > 0f) return;
        if (player.GetCurrentMana() < skill2Cost) return;

        player.UseMana(skill2Cost);
        skill2Timer = skill2Cooldown;

        StartCoroutine(CycloneCrescentRoutine());
    }

    private IEnumerator CycloneCrescentRoutine()
    {
        isCastingSkill = true;
        player.canMove = false;

        if (sfxCyclone != null && audioSource != null)
            audioSource.PlayOneShot(sfxCyclone, 1.15f);

        rb.velocity = new Vector2(rb.velocity.x * 0.3f, 5.0f);

        if (anim != null)
            anim.SetTrigger("attackCombo");

        SpawnVisualFX(transform.position, fxWhirlwind, Vector3.one * 1.2f, 0f, 0.35f, new Color(1f, 0.88f, 0.35f, 0.9f), true, 3.2f);

        if (CameraFollow.instance != null)
            CameraFollow.instance.TriggerScreenShake(0.32f, 0.28f);

        yield return new WaitForSeconds(0.10f);
        SpawnVisualFX(transform.position + new Vector3(0.5f, 0.3f, 0f), fxSlashArc, Vector3.one * 1.5f, 45f, 0.22f, Color.white);
        HitCycloneArea(skill2Damage / 2, false);

        yield return new WaitForSeconds(0.14f);
        SpawnVisualFX(transform.position + new Vector3(-0.5f, 0.2f, 0f), fxSlashArc, new Vector3(-1.6f, 1.6f, 1f), -45f, 0.22f, Color.white);
        HitCycloneArea(skill2Damage / 2, true);

        yield return new WaitForSeconds(0.15f);

        player.canMove = true;
        isCastingSkill = false;
    }

    private void HitCycloneArea(int damage, bool applyKnockback)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 3.4f, player.GetEnemyLayer());
        foreach (var hit in hits)
        {
            Entity enemy = hit.GetComponent<Entity>();
            if (enemy != null && enemy != player)
            {
                enemy.TakeDamage(damage, applyKnockback);
                player.RestoreMana(6);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SKILL 3 [W]: Judgement of the End (ULTIMATE Mahadewa Tebasan Dimensi)
    // ─────────────────────────────────────────────────────────────────────────
    public void TryCastSkill3()
    {
        if (skill3Timer > 0f) return;
        if (player.GetCurrentMana() < skill3Cost) return;

        player.UseMana(skill3Cost);
        skill3Timer = skill3Cooldown;

        StartCoroutine(JudgementUltimateRoutine());
    }

    private IEnumerator JudgementUltimateRoutine()
    {
        isCastingSkill = true;
        player.canMove = false;
        player.SetInvincible(true);

        float dir = player.facingRight ? 1f : -1f;

        // ══════════════════════════════════════════════════════════════════════
        // PRE-ULTIMATE: Layar perlahan menggelap (atmosfer sebelum ultimate)
        // ══════════════════════════════════════════════════════════════════════
        yield return StartCoroutine(DarkenScreen(0.30f, 0.75f));   // fade ke gelap 0.3s

        // Tahan gelap sebentar (dramatic pause)
        yield return new WaitForSecondsRealtime(0.40f);

        // Layar terang kembali saat ultimate mulai
        yield return StartCoroutine(BrightenScreen(0.18f));

        // ── FASE 1: Dramatic Anime Time Freeze ────────────────────────────────
        Time.timeScale = 0.08f;

        if (sfxUltimate != null && audioSource != null)
            audioSource.PlayOneShot(sfxUltimate, 1.25f);

        SpawnVisualFX(transform.position, fxWhirlwind, Vector3.one * 1.8f, 0f, 0.25f, new Color(1f, 0.85f, 0.2f, 0.95f), true, 2.5f);

        yield return new WaitForSecondsRealtime(0.16f);

        // ── FASE 2: Dimensional Blade Blitz ────────────────────────────────────
        Time.timeScale = 1.0f;

        if (CameraFollow.instance != null)
            CameraFollow.instance.TriggerScreenShake(0.35f, 0.30f);

        if (anim != null)
            anim.SetTrigger("attackCombo");

        rb.velocity = new Vector2(dir * 20f, 0f);

        int hitCount = 6;
        int sliceDmg = 25;

        float[] angles = new float[] { 15f, -35f, 60f, -60f, 40f, -10f };

        for (int i = 0; i < hitCount; i++)
        {
            SpawnAfterimage(new Color(1f, 0.25f, 0.2f, 0.75f));

            Vector3 slashPos = transform.position + new Vector3(Random.Range(-1.0f, 2.8f) * dir, Random.Range(-0.5f, 1.5f), 0f);

            SpawnVisualFX(slashPos, fxSlashCross, Vector3.one * Random.Range(1.8f, 2.4f), angles[i], 0.90f, new Color(1f, 0.95f, 0.8f, 0.95f));
            SpawnVisualFX(slashPos, fxSlashArc, Vector3.one * Random.Range(1.9f, 2.6f), -angles[i], 0.85f, new Color(1f, 0.85f, 0.3f, 0.9f));

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 6.0f, player.GetEnemyLayer());
            foreach (var hit in hits)
            {
                Entity enemy = hit.GetComponent<Entity>();
                if (enemy != null && enemy != player)
                {
                    enemy.TakeDamage(sliceDmg, false);
                }
            }

            yield return new WaitForSeconds(0.06f);
        }

        rb.velocity = Vector2.zero;

        // ── FASE 3: DETONASI LANGSUNG (tanpa delay pop-out) ───────────────────
        if (sfxUltimatePop != null && audioSource != null)
            audioSource.PlayOneShot(sfxUltimatePop, 1.4f);

        if (CameraFollow.instance != null)
            CameraFollow.instance.TriggerScreenShake(0.70f, 0.55f);

        Vector3 blastCenter = transform.position - new Vector3(dir * 2.0f, -0.4f, 0f);
        SpawnVisualFX(blastCenter, fxSlashCross, Vector3.one * 4.5f, 0f, 0.55f, Color.white, true, 3.5f);
        SpawnVisualFX(blastCenter, fxWhirlwind, Vector3.one * 3.2f, 0f, 0.55f, new Color(1f, 0.85f, 0.25f, 1f), true, 4.0f);

        int finisherBurstDmg = 150;
        Collider2D[] finalHits = Physics2D.OverlapCircleAll(blastCenter, 7.5f, player.GetEnemyLayer());
        foreach (var hit in finalHits)
        {
            Entity enemy = hit.GetComponent<Entity>();
            if (enemy != null && enemy != player)
            {
                enemy.TakeDamage(finisherBurstDmg, true);
            }
        }

        yield return new WaitForSeconds(0.35f);

        player.SetInvincible(false);
        player.canMove = true;
        isCastingSkill = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SCREEN DARKENING: Layar menggelap perlahan sebelum ultimate
    // ─────────────────────────────────────────────────────────────────────────
    private GameObject _darkOverlayGO;

    private IEnumerator DarkenScreen(float duration, float targetAlpha)
    {
        // Buat canvas overlay gelap di atas segalanya (order 89)
        _darkOverlayGO = new GameObject("UltDark_Overlay");
        Canvas c = _darkOverlayGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 89;

        GameObject imgGO = new GameObject("Dark_Panel");
        imgGO.transform.SetParent(_darkOverlayGO.transform, false);
        RectTransform rt = imgGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        Image img = imgGO.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = false;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            img.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, targetAlpha, elapsed / duration));
            yield return null;
        }
        img.color = new Color(0f, 0f, 0f, targetAlpha);
    }

    private IEnumerator BrightenScreen(float duration)
    {
        if (_darkOverlayGO == null) yield break;
        Image img = _darkOverlayGO.GetComponentInChildren<Image>();
        if (img == null) { Object.Destroy(_darkOverlayGO); yield break; }

        float startAlpha = img.color.a;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            img.color = new Color(0f, 0f, 0f, Mathf.Lerp(startAlpha, 0f, elapsed / duration));
            yield return null;
        }
        Object.Destroy(_darkOverlayGO);
        _darkOverlayGO = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ANIME CUT-IN ANIMATION - Slide masuk dari kanan seperti Mobile Legends
    // ─────────────────────────────────────────────────────────────────────────
    private IEnumerator ShowUltimateCutIn()
    {
        // ── 1. Buat Canvas khusus cut-in (sorting order 90, di atas gameplay) ──
        GameObject canvasGO = new GameObject("UltimateCutIn_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // ── 2. Screen flash putih kilat (efek aura sebelum banner muncul) ───────
        GameObject flashGO = new GameObject("CutIn_Flash");
        flashGO.transform.SetParent(canvasGO.transform, false);
        RectTransform flashRT = flashGO.AddComponent<RectTransform>();
        flashRT.anchorMin = Vector2.zero;
        flashRT.anchorMax = Vector2.one;
        flashRT.sizeDelta = Vector2.zero;
        flashRT.anchoredPosition = Vector2.zero;
        Image flashImg = flashGO.AddComponent<Image>();
        flashImg.color = new Color(1f, 0.95f, 0.7f, 0f);
        flashImg.raycastTarget = false;

        // Flash in
        float fe = 0f;
        while (fe < 0.08f)
        {
            fe += Time.unscaledDeltaTime;
            flashImg.color = new Color(1f, 0.95f, 0.7f, Mathf.Lerp(0f, 0.55f, fe / 0.08f));
            yield return null;
        }
        // Flash out
        fe = 0f;
        while (fe < 0.12f)
        {
            fe += Time.unscaledDeltaTime;
            flashImg.color = new Color(1f, 0.95f, 0.7f, Mathf.Lerp(0.55f, 0f, fe / 0.12f));
            yield return null;
        }

        // ── 3. Banner Panel Utama (Diagonal, lebar penuh seperti ML) ───────────
        GameObject bannerGO = new GameObject("CutIn_Banner");
        bannerGO.transform.SetParent(canvasGO.transform, false);
        RectTransform bannerRT = bannerGO.AddComponent<RectTransform>();
        bannerRT.anchorMin = new Vector2(0f, 0.5f);
        bannerRT.anchorMax = new Vector2(0f, 0.5f);
        bannerRT.pivot     = new Vector2(0f, 0.5f);
        bannerRT.sizeDelta = new Vector2(1300f, 300f);
        // Mulai dari kanan layar (off-screen)
        bannerRT.anchoredPosition = new Vector2(2200f, -60f);

        Image bannerImg = bannerGO.AddComponent<Image>();
        if (cutInBanner != null)
            bannerImg.sprite = cutInBanner;
        else
            bannerImg.color = new Color(0.05f, 0.03f, 0.12f, 0.95f);
        bannerImg.raycastTarget = false;

        // ── 4. Dark overlay strip atas & bawah banner (gaya ML) ─────────────
        GameObject topStripGO = new GameObject("CutIn_TopStrip");
        topStripGO.transform.SetParent(canvasGO.transform, false);
        RectTransform topStripRT = topStripGO.AddComponent<RectTransform>();
        topStripRT.anchorMin = new Vector2(0f, 0.5f);
        topStripRT.anchorMax = new Vector2(1f, 0.5f);
        topStripRT.pivot = new Vector2(0.5f, 0.5f);
        topStripRT.sizeDelta = new Vector2(0f, 300f);
        topStripRT.anchoredPosition = new Vector2(0f, -60f);
        Image topStripImg = topStripGO.AddComponent<Image>();
        topStripImg.color = new Color(0f, 0f, 0f, 0f);
        topStripImg.raycastTarget = false;

        // ── 5. Portrait foto karakter (pojok kiri banner) ─────────────────────
        GameObject portraitGO = new GameObject("CutIn_Portrait");
        portraitGO.transform.SetParent(canvasGO.transform, false);
        RectTransform portraitRT = portraitGO.AddComponent<RectTransform>();
        portraitRT.anchorMin = new Vector2(0f, 0.5f);
        portraitRT.anchorMax = new Vector2(0f, 0.5f);
        portraitRT.pivot = new Vector2(0f, 0.5f);
        portraitRT.sizeDelta = new Vector2(290f, 290f);
        portraitRT.anchoredPosition = new Vector2(2200f, -55f); // Start off-screen bersama banner
        Image portraitImg = portraitGO.AddComponent<Image>();
        if (playerPortrait != null)
            portraitImg.sprite = playerPortrait;
        else
        {
            // Fallback: kotak emas solid jika portrait null
            portraitImg.color = new Color(0.8f, 0.6f, 0.1f, 1f);
        }
        portraitImg.preserveAspect = true;
        portraitImg.raycastTarget = false;

        // ── 6. Portrait border frame (cincin emas mewah) ─────────────────────
        GameObject frameGO = new GameObject("CutIn_PortraitFrame");
        frameGO.transform.SetParent(canvasGO.transform, false);
        RectTransform frameRT = frameGO.AddComponent<RectTransform>();
        frameRT.anchorMin = new Vector2(0f, 0.5f);
        frameRT.anchorMax = new Vector2(0f, 0.5f);
        frameRT.pivot = new Vector2(0f, 0.5f);
        frameRT.sizeDelta = new Vector2(300f, 300f);
        frameRT.anchoredPosition = new Vector2(2200f, -55f);
        Image frameImg = frameGO.AddComponent<Image>();
        frameImg.color = new Color(1f, 0.82f, 0.2f, 0f); // Frame emas, mulai transparan
        frameImg.raycastTarget = false;

        // ── 7. Nama skill text ("JUDGEMENT OF THE END") ───────────────────────
        GameObject skillNameGO = new GameObject("CutIn_SkillName");
        skillNameGO.transform.SetParent(canvasGO.transform, false);
        RectTransform skillNameRT = skillNameGO.AddComponent<RectTransform>();
        skillNameRT.anchorMin = new Vector2(0f, 0.5f);
        skillNameRT.anchorMax = new Vector2(0f, 0.5f);
        skillNameRT.pivot = new Vector2(0f, 0.5f);
        skillNameRT.sizeDelta = new Vector2(900f, 90f);
        skillNameRT.anchoredPosition = new Vector2(3000f, -20f); // Off-screen lebih jauh

        TextMeshProUGUI skillTMP = skillNameGO.AddComponent<TextMeshProUGUI>();
        skillTMP.text = "JUDGEMENT OF THE END";
        skillTMP.fontSize = 52f;
        skillTMP.fontStyle = FontStyles.Bold | FontStyles.Italic;
        skillTMP.alignment = TextAlignmentOptions.Left;
        skillTMP.color = new Color(1f, 0.88f, 0.3f, 1f);
        skillTMP.outlineColor = new Color32(80, 40, 0, 255);
        skillTMP.outlineWidth = 0.25f;
        skillTMP.margin = Vector4.zero;
        skillTMP.raycastTarget = false;

        // ── 8. Sub-text karakter ("VALE - KNIGHT OF ETERNITY") ───────────────
        GameObject charNameGO = new GameObject("CutIn_CharName");
        charNameGO.transform.SetParent(canvasGO.transform, false);
        RectTransform charNameRT = charNameGO.AddComponent<RectTransform>();
        charNameRT.anchorMin = new Vector2(0f, 0.5f);
        charNameRT.anchorMax = new Vector2(0f, 0.5f);
        charNameRT.pivot = new Vector2(0f, 0.5f);
        charNameRT.sizeDelta = new Vector2(900f, 50f);
        charNameRT.anchoredPosition = new Vector2(3000f, -78f); // Start off-screen
        TextMeshProUGUI charTMP = charNameGO.AddComponent<TextMeshProUGUI>();
        charTMP.text = "KNIGHT OF ETERNITY";
        charTMP.fontSize = 26f;
        charTMP.fontStyle = FontStyles.Bold;
        charTMP.alignment = TextAlignmentOptions.Left;
        charTMP.color = new Color(0.9f, 0.75f, 0.95f, 1f); // Lavender muda
        charTMP.margin = Vector4.zero;
        charTMP.raycastTarget = false;

        // ── 9. Gold accent line (garis emas horizontal) ───────────────────────
        GameObject linGO = new GameObject("CutIn_GoldLine");
        linGO.transform.SetParent(canvasGO.transform, false);
        RectTransform linRT = linGO.AddComponent<RectTransform>();
        linRT.anchorMin = new Vector2(0f, 0.5f);
        linRT.anchorMax = new Vector2(0f, 0.5f);
        linRT.pivot = new Vector2(0f, 0.5f);
        linRT.sizeDelta = new Vector2(0f, 3f);
        linRT.anchoredPosition = new Vector2(310f, -55f);
        Image linImg = linGO.AddComponent<Image>();
        linImg.color = new Color(1f, 0.85f, 0.2f, 0f);
        linImg.raycastTarget = false;

        // ══════════════════════════════════════════════════════════════════════
        // ANIMASI: Slide-In dari kanan, semua elemen simultan
        // ══════════════════════════════════════════════════════════════════════

        // Target posisi akhir (di layar)
        Vector2 bannerTarget   = new Vector2(0f, -60f);
        Vector2 portraitTarget = new Vector2(10f, -55f);
        Vector2 frameTarget    = new Vector2(10f, -55f);
        Vector2 skillTarget    = new Vector2(310f, -15f);
        Vector2 charTarget     = new Vector2(310f, -78f);

        // Offset mulai (off-screen kanan)
        Vector2 bannerStart   = new Vector2(2200f, -60f);
        Vector2 portraitStart = new Vector2(2200f, -55f);
        Vector2 frameStart    = new Vector2(2200f, -55f);
        Vector2 skillStart    = new Vector2(3000f, -15f);
        Vector2 charStart     = new Vector2(3200f, -78f);

        // Animasi strip top (fade in)
        Color stripTargetColor = new Color(0f, 0f, 0f, 0.72f);

        // Slide-In (0.22 detik — cepat & responsif)
        float slideInDur = 0.22f;
        float elapsed = 0f;
        while (elapsed < slideInDur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideInDur);

            bannerRT.anchoredPosition   = Vector2.Lerp(bannerStart, bannerTarget, t);
            portraitRT.anchoredPosition = Vector2.Lerp(portraitStart, portraitTarget, t);
            frameRT.anchoredPosition    = Vector2.Lerp(frameStart, frameTarget, t);
            skillNameRT.anchoredPosition = Vector2.Lerp(skillStart, skillTarget, t * 0.9f);
            charNameRT.anchoredPosition  = Vector2.Lerp(charStart, charTarget, t * 0.85f);

            // Strip fade
            topStripImg.color = Color.Lerp(Color.clear, stripTargetColor, t);

            // Frame emas muncul
            frameImg.color = new Color(1f, 0.82f, 0.2f, Mathf.Lerp(0f, 0.9f, t));

            yield return null;
        }

        // Snap ke posisi final
        bannerRT.anchoredPosition    = bannerTarget;
        portraitRT.anchoredPosition  = portraitTarget;
        frameRT.anchoredPosition     = frameTarget;
        skillNameRT.anchoredPosition = skillTarget;
        charNameRT.anchoredPosition  = charTarget;
        topStripImg.color = stripTargetColor;

        // Gold line expand (garis emas muncul memanjang ke kanan)
        float lineDur = 0.18f;
        elapsed = 0f;
        while (elapsed < lineDur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / lineDur;
            linRT.sizeDelta = new Vector2(Mathf.Lerp(0f, 850f, t), 3f);
            linImg.color = new Color(1f, 0.85f, 0.2f, Mathf.Lerp(0f, 1f, t));
            yield return null;
        }
        linRT.sizeDelta = new Vector2(850f, 3f);

        // ── TAHAN di layar (0.55 detik — cukup untuk baca nama skill) ─────────
        yield return new WaitForSecondsRealtime(0.55f);

        // ══════════════════════════════════════════════════════════════════════
        // ANIMASI: Slide-Out ke kiri (cepat 0.18 detik)
        // ══════════════════════════════════════════════════════════════════════
        Vector2 bannerExit   = new Vector2(-1500f, -60f);
        Vector2 portraitExit = new Vector2(-400f, -55f);
        Vector2 skillExit    = new Vector2(-1200f, -15f);
        Vector2 charExit     = new Vector2(-1000f, -78f);

        float slideOutDur = 0.18f;
        elapsed = 0f;
        while (elapsed < slideOutDur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideOutDur);

            bannerRT.anchoredPosition    = Vector2.Lerp(bannerTarget, bannerExit, t);
            portraitRT.anchoredPosition  = Vector2.Lerp(portraitTarget, portraitExit, t);
            frameRT.anchoredPosition     = Vector2.Lerp(frameTarget, portraitExit, t);
            skillNameRT.anchoredPosition = Vector2.Lerp(skillTarget, skillExit, t);
            charNameRT.anchoredPosition  = Vector2.Lerp(charTarget, charExit, t);
            topStripImg.color = Color.Lerp(stripTargetColor, Color.clear, t);
            linImg.color      = Color.Lerp(new Color(1f, 0.85f, 0.2f, 1f), Color.clear, t);
            flashImg.color    = new Color(1f, 0.95f, 0.7f, 0f);
            yield return null;
        }

        // Bersihkan canvas setelah selesai
        Object.Destroy(canvasGO);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // COMBAT FX SPAWNER (Visual Tebasan & Afterimage Nyata di Layar)
    // ─────────────────────────────────────────────────────────────────────────
    private void SpawnVisualFX(Vector3 pos, Sprite sprite, Vector3 scale, float rotZ, float duration, Color color, bool expand = false, float endScaleMul = 1.3f)
    {
        if (sprite == null) return;

        GameObject fxGO = new GameObject("Combat_Visual_FX");
        fxGO.transform.position = pos;
        fxGO.transform.rotation = Quaternion.Euler(0, 0, rotZ);
        fxGO.transform.localScale = scale;

        SpriteRenderer sr = fxGO.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 55;

        Material glowMat = new Material(Shader.Find("Sprites/Default"));
        glowMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        glowMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        sr.material = glowMat;

        Vector3 targetScale = expand ? scale * endScaleMul : scale * 1.15f;
        StartCoroutine(FadeAndScaleFX(fxGO, sr, targetScale, duration));
    }

    private void SpawnAfterimage(Color tint)
    {
        if (playerSR == null || playerSR.sprite == null) return;

        GameObject ghost = new GameObject("Player_Ghost_Afterimage");
        ghost.transform.position = playerSR.transform.position;
        ghost.transform.rotation = playerSR.transform.rotation;
        ghost.transform.localScale = playerSR.transform.lossyScale;

        SpriteRenderer sr = ghost.AddComponent<SpriteRenderer>();
        sr.sprite = playerSR.sprite;
        sr.flipX  = playerSR.flipX;
        sr.color  = tint;
        sr.sortingOrder = playerSR.sortingOrder - 1;

        StartCoroutine(FadeAndScaleFX(ghost, sr, ghost.transform.localScale, 0.30f));
    }

    private IEnumerator FadeAndScaleFX(GameObject go, SpriteRenderer sr, Vector3 targetScale, float duration)
    {
        if (go == null) yield break;
        Vector3 startScale = go.transform.localScale;
        Color startCol = sr.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (go == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            go.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            sr.color = new Color(startCol.r, startCol.g, startCol.b, Mathf.Lerp(startCol.a, 0f, t));
            yield return null;
        }

        if (go != null) Destroy(go);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UI BUILDER: 3 Slot Skill Mini di HUD
    // ─────────────────────────────────────────────────────────────────────────
    private void BuildSkillBarUI()
    {
        GameObject hudFrame = GameObject.Find("Player_HUD_Frame");
        if (hudFrame == null) return;

        Transform existingBar = hudFrame.transform.Find("Skills_Container");
        if (existingBar != null) return;

        GameObject skillsGO = new GameObject("Skills_Container");
        skillsGO.transform.SetParent(hudFrame.transform, false);

        RectTransform skillsRT = skillsGO.AddComponent<RectTransform>();
        skillsRT.anchorMin = new Vector2(0f, 1f);
        skillsRT.anchorMax = new Vector2(0f, 1f);
        skillsRT.pivot     = new Vector2(0f, 1f);
        skillsRT.anchoredPosition = new Vector2(108f, -68f);
        skillsRT.sizeDelta        = new Vector2(160f, 36f);

        Sprite icon1 = Resources.Load<Sprite>("UI/skill_icon_thrust");
        Sprite icon2 = Resources.Load<Sprite>("UI/skill_icon_cyclone");
        Sprite icon3 = Resources.Load<Sprite>("UI/skill_icon_ultimate");

        CreateSkillSlot(skillsGO.transform, 0,  icon1, "A", out cdOverlay1, out cdText1);
        CreateSkillSlot(skillsGO.transform, 46, icon2, "S", out cdOverlay2, out cdText2);
        CreateSkillSlot(skillsGO.transform, 92, icon3, "W", out cdOverlay3, out cdText3);
    }

    private void CreateSkillSlot(Transform parent, float xPos, Sprite iconSprite, string keyLabel,
                                 out Image overlayOut, out TextMeshProUGUI textOut)
    {
        GameObject slotGO = new GameObject($"Slot_{keyLabel}");
        slotGO.transform.SetParent(parent, false);

        RectTransform slotRT = slotGO.AddComponent<RectTransform>();
        slotRT.anchorMin = new Vector2(0f, 0.5f);
        slotRT.anchorMax = new Vector2(0f, 0.5f);
        slotRT.pivot     = new Vector2(0f, 0.5f);
        slotRT.anchoredPosition = new Vector2(xPos, 0f);
        slotRT.sizeDelta        = new Vector2(36f, 36f);

        Image iconImg = slotGO.AddComponent<Image>();
        if (iconSprite != null) iconImg.sprite = iconSprite;
        else iconImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        iconImg.raycastTarget = false;

        GameObject cdGO = new GameObject("CD_Overlay");
        cdGO.transform.SetParent(slotGO.transform, false);

        RectTransform cdRT = cdGO.AddComponent<RectTransform>();
        cdRT.anchorMin = Vector2.zero;
        cdRT.anchorMax = Vector2.one;
        cdRT.sizeDelta = Vector2.zero;

        Image cdImg = cdGO.AddComponent<Image>();
        cdImg.color = new Color(0f, 0f, 0f, 0.65f);
        cdImg.type  = Image.Type.Filled;
        cdImg.fillMethod = Image.FillMethod.Radial360;
        cdImg.fillOrigin = (int)Image.Origin360.Top;
        cdImg.fillAmount = 0f;
        cdImg.raycastTarget = false;

        overlayOut = cdImg;

        GameObject badgeGO = new GameObject("Key_Badge");
        badgeGO.transform.SetParent(slotGO.transform, false);

        RectTransform badgeRT = badgeGO.AddComponent<RectTransform>();
        badgeRT.anchorMin = new Vector2(1f, 0f);
        badgeRT.anchorMax = new Vector2(1f, 0f);
        badgeRT.pivot     = new Vector2(1f, 0f);
        badgeRT.anchoredPosition = new Vector2(3f, -3f);
        badgeRT.sizeDelta        = new Vector2(16f, 16f);

        Image badgeImg = badgeGO.AddComponent<Image>();
        badgeImg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);
        badgeImg.raycastTarget = false;

        GameObject badgeTextGO = new GameObject("Text");
        badgeTextGO.transform.SetParent(badgeGO.transform, false);

        RectTransform btRT = badgeTextGO.AddComponent<RectTransform>();
        btRT.anchorMin = Vector2.zero;
        btRT.anchorMax = Vector2.one;
        btRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI btTMP = badgeTextGO.AddComponent<TextMeshProUGUI>();
        btTMP.text = keyLabel;
        btTMP.fontSize = 11f;
        btTMP.fontStyle = FontStyles.Bold;
        btTMP.alignment = TextAlignmentOptions.Center;
        btTMP.color = new Color(1f, 0.88f, 0.35f, 1f);
        btTMP.raycastTarget = false;
        btTMP.margin = Vector4.zero;

        GameObject cdTextGO = new GameObject("CD_Text");
        cdTextGO.transform.SetParent(slotGO.transform, false);

        RectTransform cdtRT = cdTextGO.AddComponent<RectTransform>();
        cdtRT.anchorMin = Vector2.zero;
        cdtRT.anchorMax = Vector2.one;
        cdtRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI cdtTMP = cdTextGO.AddComponent<TextMeshProUGUI>();
        cdtTMP.text = "";
        cdtTMP.fontSize = 12f;
        cdtTMP.fontStyle = FontStyles.Bold;
        cdtTMP.alignment = TextAlignmentOptions.Center;
        cdtTMP.color = Color.white;
        cdtTMP.raycastTarget = false;
        cdtTMP.margin = Vector4.zero;

        textOut = cdtTMP;
    }

    private void UpdateSkillUI()
    {
        if (cdOverlay1 != null)
        {
            float pct = Mathf.Clamp01(skill1Timer / skill1Cooldown);
            cdOverlay1.fillAmount = pct;
            if (cdText1 != null)
                cdText1.text = (skill1Timer > 0.1f) ? skill1Timer.ToString("F1") : "";
        }

        if (cdOverlay2 != null)
        {
            float pct = Mathf.Clamp01(skill2Timer / skill2Cooldown);
            cdOverlay2.fillAmount = pct;
            if (cdText2 != null)
                cdText2.text = (skill2Timer > 0.1f) ? skill2Timer.ToString("F1") : "";
        }

        if (cdOverlay3 != null)
        {
            float pct = Mathf.Clamp01(skill3Timer / skill3Cooldown);
            cdOverlay3.fillAmount = pct;
            if (cdText3 != null)
                cdText3.text = (skill3Timer > 0.1f) ? skill3Timer.ToString("F1") : "";
        }
    }
}
