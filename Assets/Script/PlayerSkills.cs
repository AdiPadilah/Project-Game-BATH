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
    [SerializeField] private int   skill1Cost = 12;         // Hemat (sebelumnya 25)
    [SerializeField] private float skill1Cooldown = 2.2f;   // Cepat (sebelumnya 3.5s)
    [SerializeField] private int   skill1Damage = 65;       // Sakti (sebelumnya 35)
    private float skill1Timer = 0f;

    [Header("Skill 2 - Cyclone Crescent [S]")]
    [SerializeField] private int   skill2Cost = 20;         // Hemat (sebelumnya 40)
    [SerializeField] private float skill2Cooldown = 4.0f;   // Cepat (sebelumnya 6.0s)
    [SerializeField] private int   skill2Damage = 95;       // Sakti (sebelumnya 55)
    private float skill2Timer = 0f;

    [Header("Skill 3 - Judgement of the End (ULTIMATE) [W]")]
    [SerializeField] private int   skill3Cost = 45;         // Hemat (sebelumnya 80)
    [SerializeField] private float skill3Cooldown = 11.0f;  // Cepat (sebelumnya 18.0s)
    [SerializeField] private int   skill3DamageTotal = 300; // Super Sakti (sebelumnya 125)
    private float skill3Timer = 0f;

    [Header("Audio SFX")]
    private AudioClip sfxThrust;
    private AudioClip sfxCyclone;
    private AudioClip sfxUltimate;

    // Combat FX Sprites
    private Sprite fxSlashArc;
    private Sprite fxSlashCross;
    private Sprite fxThrustStreak;
    private Sprite fxWhirlwind;

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
        sfxThrust   = Resources.Load<AudioClip>("Audio/skill_thrust");
        sfxCyclone  = Resources.Load<AudioClip>("Audio/skill_cyclone");
        sfxUltimate = Resources.Load<AudioClip>("Audio/skill_ultimate");

        fxSlashArc      = Resources.Load<Sprite>("FX/fx_slash_arc_gold");
        fxSlashCross    = Resources.Load<Sprite>("FX/fx_slash_cross");
        fxThrustStreak  = Resources.Load<Sprite>("FX/fx_thrust_streak");
        fxWhirlwind     = Resources.Load<Sprite>("FX/fx_whirlwind_ring");
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
    private void TryCastSkill1()
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

            // Munculkan bayangan karakter (Afterimage)
            SpawnAfterimage(new Color(1f, 0.85f, 0.25f, 0.65f));

            // Deteksi tabrakan tebasan di lintasan
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.8f, player.GetEnemyLayer());
            foreach (var hit in hits)
            {
                Entity enemy = hit.GetComponent<Entity>();
                if (enemy != null && enemy != player && !hitEnemies.Contains(enemy))
                {
                    hitEnemies.Add(enemy);
                    enemy.TakeDamage(skill1Damage, false);
                    player.RestoreMana(8); // Refund mana saat kena musuh

                    // Efek tebasan menyilang di badan musuh
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
    private void TryCastSkill2()
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

        // Sedikit melompat ke udara saat mengayunkan pedang melingkar
        rb.velocity = new Vector2(rb.velocity.x * 0.3f, 5.0f);

        if (anim != null)
            anim.SetTrigger("attackCombo");

        // 1. Munculkan cincin gelombang angin shockwave emas membesar
        SpawnVisualFX(transform.position, fxWhirlwind, Vector3.one * 1.2f, 0f, 0.35f, new Color(1f, 0.88f, 0.35f, 0.9f), true, 3.2f);

        // Screen shake sedang
        if (CameraFollow.instance != null)
            CameraFollow.instance.TriggerScreenShake(0.32f, 0.28f);

        // Sapuan pertama (depan & atas)
        yield return new WaitForSeconds(0.10f);
        SpawnVisualFX(transform.position + new Vector3(0.5f, 0.3f, 0f), fxSlashArc, Vector3.one * 1.5f, 45f, 0.22f, Color.white);
        HitCycloneArea(skill2Damage / 2, false);

        // Sapuan kedua (belakang & bawah) - Knockback musuh melayang
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
    private void TryCastSkill3()
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

        // ── PHASE 1: Dramatic Anime Time Freeze (0.18s) ───────────────────────
        Time.timeScale = 0.08f;

        // Suara charge pembuka ultimate
        if (sfxUltimate != null && audioSource != null)
            audioSource.PlayOneShot(sfxUltimate, 1.35f);

        // Efek aura cahaya emas di sekitar ksatria
        SpawnVisualFX(transform.position, fxWhirlwind, Vector3.one * 1.8f, 0f, 0.25f, new Color(1f, 0.85f, 0.2f, 0.95f), true, 2.5f);

        yield return new WaitForSecondsRealtime(0.18f);

        // ── PHASE 2: Dimensional Blade Blitz (Tebasan Menembus Ruang) ────────
        Time.timeScale = 1.0f;

        // Gempa layar dahsyat (Screen Shake intens)
        if (CameraFollow.instance != null)
            CameraFollow.instance.TriggerScreenShake(0.85f, 0.45f);

        if (anim != null)
            anim.SetTrigger("attackCombo");

        float dir = player.facingRight ? 1f : -1f;
        rb.velocity = new Vector2(dir * 18f, 0f);

        // 6 Titik Tebasan Dimensi Silang di seluruh area musuh
        int hitCount = 6;
        int perHitDmg = skill3DamageTotal / hitCount; // 300 / 6 = 50 damage per hit!

        float[] angles = new float[] { 0f, 45f, -30f, 75f, -60f, 15f };

        for (int i = 0; i < hitCount; i++)
        {
            // Munculkan bayangan melesat
            SpawnAfterimage(new Color(1f, 0.2f, 0.2f, 0.75f)); // Afterimage merah darah/emas

            // Hitung posisi tebasan di depan player
            Vector3 slashPos = transform.position + new Vector3(Random.Range(-1.5f, 3.5f) * dir, Random.Range(-0.8f, 1.8f), 0f);

            // Tebasan Silang Raksasa & Busur Emas
            SpawnVisualFX(slashPos, fxSlashCross, Vector3.one * Random.Range(1.6f, 2.4f), angles[i], 0.28f, Color.white);
            SpawnVisualFX(slashPos, fxSlashArc, Vector3.one * Random.Range(1.8f, 2.6f), -angles[i], 0.25f, new Color(1f, 0.9f, 0.4f, 0.95f));

            // Berikan damage ke seluruh musuh di zona tebasan
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 6.0f, player.GetEnemyLayer());
            foreach (var hit in hits)
            {
                Entity enemy = hit.GetComponent<Entity>();
                if (enemy != null && enemy != player)
                {
                    enemy.TakeDamage(perHitDmg, (i == hitCount - 1)); // Hit terakhir knockback terpental
                }
            }

            yield return new WaitForSeconds(0.07f);
        }

        rb.velocity = Vector2.zero;

        // ── PHASE 3: Detonasi Tebasan Pamungkas ("CLANG / CHING!") ────────────
        Vector3 centerPos = transform.position + new Vector3(dir * 1.5f, 0.5f, 0f);
        SpawnVisualFX(centerPos, fxSlashCross, Vector3.one * 3.2f, 0f, 0.45f, new Color(1f, 1f, 0.9f, 1f), true, 4.5f);
        SpawnVisualFX(centerPos, fxWhirlwind, Vector3.one * 2.0f, 0f, 0.45f, new Color(1f, 0.85f, 0.3f, 0.9f), true, 5.0f);

        yield return new WaitForSeconds(0.20f);

        player.SetInvincible(false);
        player.canMove = true;
        isCastingSkill = false;
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
        sr.sortingOrder = 55; // Di atas karakter dan background

        // Material Additive agar efeknya menyala terang (Glow)
        Material glowMat = new Material(Shader.Find("Sprites/Default"));
        glowMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        glowMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive blending!
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

        // Icon Image
        Image iconImg = slotGO.AddComponent<Image>();
        if (iconSprite != null) iconImg.sprite = iconSprite;
        else iconImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        iconImg.raycastTarget = false;

        // Cooldown Overlay (Gelap saat CD)
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

        // Key Label Badge ("A", "S", "W")
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

        // Cooldown Seconds Text
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
        // Update Slot 1 [A]
        if (cdOverlay1 != null)
        {
            float pct = Mathf.Clamp01(skill1Timer / skill1Cooldown);
            cdOverlay1.fillAmount = pct;
            if (cdText1 != null)
                cdText1.text = (skill1Timer > 0.1f) ? skill1Timer.ToString("F1") : "";
        }

        // Update Slot 2 [S]
        if (cdOverlay2 != null)
        {
            float pct = Mathf.Clamp01(skill2Timer / skill2Cooldown);
            cdOverlay2.fillAmount = pct;
            if (cdText2 != null)
                cdText2.text = (skill2Timer > 0.1f) ? skill2Timer.ToString("F1") : "";
        }

        // Update Slot 3 [W]
        if (cdOverlay3 != null)
        {
            float pct = Mathf.Clamp01(skill3Timer / skill3Cooldown);
            cdOverlay3.fillAmount = pct;
            if (cdText3 != null)
                cdText3.text = (skill3Timer > 0.1f) ? skill3Timer.ToString("F1") : "";
        }
    }
}
