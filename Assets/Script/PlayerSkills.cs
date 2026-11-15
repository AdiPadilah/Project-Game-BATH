using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PlayerSkills - Mengelola 3 Skill Pedang Khusus Ksatria:
/// - Skill 1 [A]: Phantom Thrust (Tebasan Terobos Cepat)
/// - Skill 2 [S]: Cyclone Crescent (Tebasan Berputar 360 Area)
/// - Skill 3 [W]: Judgement Execution (Ultimate Tebasan Dimensi Seribu Pedang)
/// </summary>
public class PlayerSkills : MonoBehaviour
{
    private Player player;
    private Rigidbody2D rb;
    private Animator anim;
    private AudioSource audioSource;

    [Header("Skill 1 - Phantom Thrust [A]")]
    [SerializeField] private int   skill1Cost = 25;
    [SerializeField] private float skill1Cooldown = 3.5f;
    [SerializeField] private int   skill1Damage = 35;
    private float skill1Timer = 0f;

    [Header("Skill 2 - Cyclone Crescent [S]")]
    [SerializeField] private int   skill2Cost = 40;
    [SerializeField] private float skill2Cooldown = 6.0f;
    [SerializeField] private int   skill2Damage = 55;
    private float skill2Timer = 0f;

    [Header("Skill 3 - Judgement Execution (Ult) [W]")]
    [SerializeField] private int   skill3Cost = 80;
    [SerializeField] private float skill3Cooldown = 18.0f;
    [SerializeField] private int   skill3DamageTotal = 125;
    private float skill3Timer = 0f;

    [Header("Audio SFX")]
    private AudioClip sfxThrust;
    private AudioClip sfxCyclone;
    private AudioClip sfxUltimate;

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
        player = GetComponent<Player>();
        rb     = GetComponent<Rigidbody2D>();
        anim   = GetComponentInChildren<Animator>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        LoadSkillSFX();
    }

    private void Start()
    {
        BuildSkillBarUI();
    }

    private void LoadSkillSFX()
    {
        sfxThrust   = Resources.Load<AudioClip>("Audio/skill_thrust");
        sfxCyclone  = Resources.Load<AudioClip>("Audio/skill_cyclone");
        sfxUltimate = Resources.Load<AudioClip>("Audio/skill_ultimate");
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
    // SKILL 1 [A]: Phantom Thrust
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
            audioSource.PlayOneShot(sfxThrust, 1.0f);

        if (anim != null)
            anim.SetTrigger("dash");

        float dir = player.facingRight ? 1f : -1f;
        float dashSpeed = 19f;
        float duration = 0.26f;
        float elapsed = 0f;

        HashSet<Entity> hitEnemies = new HashSet<Entity>();
        Vector2 startPos = transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rb.velocity = new Vector2(dir * dashSpeed, 0f);

            // Deteksi tabrakan tebasan di sepanjang lintasan
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.6f, player.GetEnemyLayer());
            foreach (var hit in hits)
            {
                Entity enemy = hit.GetComponent<Entity>();
                if (enemy != null && enemy != player && !hitEnemies.Contains(enemy))
                {
                    hitEnemies.Add(enemy);
                    enemy.TakeDamage(skill1Damage, false);
                    player.RestoreMana(6);
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
    // SKILL 2 [S]: Cyclone Crescent (360 Area)
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
            audioSource.PlayOneShot(sfxCyclone, 1.1f);

        // Sedikit melompat ke atas saat berputar
        rb.velocity = new Vector2(rb.velocity.x * 0.3f, 4.5f);

        if (anim != null)
        {
            anim.SetTrigger("attackCombo");
        }

        // Sapuan 360 derajat pertama (0.15s)
        yield return new WaitForSeconds(0.12f);
        HitCycloneArea(skill2Damage / 2, false);

        // Sapuan 360 derajat kedua (0.28s) - Knockback musuh
        yield return new WaitForSeconds(0.16f);
        HitCycloneArea(skill2Damage / 2, true);

        yield return new WaitForSeconds(0.18f);

        player.canMove = true;
        isCastingSkill = false;
    }

    private void HitCycloneArea(int damage, bool applyKnockback)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 3.0f, player.GetEnemyLayer());
        foreach (var hit in hits)
        {
            Entity enemy = hit.GetComponent<Entity>();
            if (enemy != null && enemy != player)
            {
                enemy.TakeDamage(damage, applyKnockback);
                player.RestoreMana(5);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SKILL 3 [W]: Judgement Execution (Ultimate)
    // ─────────────────────────────────────────────────────────────────────────
    private void TryCastSkill3()
    {
        if (skill3Timer > 0f) return;
        if (player.GetCurrentMana() < skill3Cost) return;

        player.UseMana(skill3Cost);
        skill3Timer = skill3Cooldown;

        StartCoroutine(JudgementExecutionRoutine());
    }

    private IEnumerator JudgementExecutionRoutine()
    {
        isCastingSkill = true;
        player.canMove = false;
        player.SetInvincible(true);

        if (sfxUltimate != null && audioSource != null)
            audioSource.PlayOneShot(sfxUltimate, 1.25f);

        // 1. Efek Dramatic Slow Motion Sejenak
        Time.timeScale = 0.15f;
        yield return new WaitForSecondsRealtime(0.18f);
        Time.timeScale = 1.0f;

        // 2. Dash menembus target dengan kecepatan kilat
        float dir = player.facingRight ? 1f : -1f;
        rb.velocity = new Vector2(dir * 16f, 0f);

        if (anim != null)
            anim.SetTrigger("attackCombo");

        // 3. Multi-hit tebasan serentak (5 tebasan bertubi-tubi)
        int perHitDmg = skill3DamageTotal / 5;
        for (int i = 0; i < 5; i++)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 5.0f, player.GetEnemyLayer());
            foreach (var hit in hits)
            {
                Entity enemy = hit.GetComponent<Entity>();
                if (enemy != null && enemy != player)
                {
                    enemy.TakeDamage(perHitDmg, (i == 4)); // Hit terakhir memberikan knockback berat
                }
            }
            yield return new WaitForSeconds(0.08f);
        }

        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(0.25f);

        player.SetInvincible(false);
        player.canMove = true;
        isCastingSkill = false;
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
