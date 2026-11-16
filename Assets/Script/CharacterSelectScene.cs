using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// CharacterSelectScene — Scene Pemilihan Karakter Dark Fantasy BATH:
/// - Kolom Kiri: Spotlight Karakter (Idle Portrait dengan animasi napas/bob, Nama, Role, Status, Lore).
/// - Kolom Kanan: Showcase 3 Skill [A], [S], [W] lengkap dengan IKON grafis, cooldown, dan deskripsi detail.
/// - Navigasi Pindah Karakter: Tombol Panah [ ◄ ] dan [ ► ] (bisa juga via Keyboard Panah Kiri/Kanan).
/// - Tombol Bawah: Kembali ke Stage & Konfirmasi Mainkan.
/// </summary>
public class CharacterSelectScene : MonoBehaviour
{
    // ── Asset Refs ────────────────────────────────────────────────────────
    private Sprite modalPanelSprite;
    private Sprite btnFantasyNormal;
    private Sprite btnFantasyHover;
    private Sprite btnFantasyPressed;
    private Sprite screenVignette;
    private TMP_FontAsset tmpFont;

    // Portraits
    private Sprite portraitKaelen;
    private Sprite portraitValeria;

    // Skill Icons
    private Sprite iconKaelenThrust;
    private Sprite iconKaelenCyclone;
    private Sprite iconKaelenUlt;
    private Sprite iconValeriaFang;
    private Sprite iconValeriaVortex;
    private Sprite iconValeriaWrath;

    private AudioClip sfxClick;
    private AudioClip sfxSwitch;
    private AudioSource audioSource;

    // ── UI References ────────────────────────────────────────────────────
    private Canvas canvas;

    // Kolom Kiri: Character Spotlight
    private RectTransform leftPanelRT;
    private Image imgCharacterPortrait;
    private RectTransform portraitRT;
    private TMP_Text txtHeroName;
    private TMP_Text txtHeroTitle;
    private TMP_Text txtHeroRole;
    private TMP_Text txtHeroLore;
    private TMP_Text txtHeroStats;
    private TMP_Text txtPageIndicator;

    // Kolom Kanan: Skill Showcase
    private Image imgSkill1Icon;
    private TMP_Text txtSkill1Header;
    private TMP_Text txtSkill1Desc;

    private Image imgSkill2Icon;
    private TMP_Text txtSkill2Header;
    private TMP_Text txtSkill2Desc;

    private Image imgSkill3Icon;
    private TMP_Text txtSkill3Header;
    private TMP_Text txtSkill3Desc;

    // Tombol Konfirmasi Bawah
    private TMP_Text txtPlayBtnLabel;

    // ── Data Struktur Karakter ───────────────────────────────────────────
    private class CharacterEntry
    {
        public string name;
        public string title;
        public string role;
        public string lore;
        public Sprite portrait;
        public Color themeColor;

        // Stats
        public string statHP;
        public string statATK;
        public string statDEF;
        public string statSPD;

        // Skill 1 [A]
        public string skill1Title;
        public string skill1Cooldown;
        public string skill1Desc;
        public Sprite skill1Icon;

        // Skill 2 [S]
        public string skill2Title;
        public string skill2Cooldown;
        public string skill2Desc;
        public Sprite skill2Icon;

        // Skill 3 [W] (Ultimate)
        public string skill3Title;
        public string skill3Cooldown;
        public string skill3Desc;
        public Sprite skill3Icon;
    }

    private CharacterEntry[] characters;
    private int currentIndex = 0;
    private string targetScene = "Next";

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        LoadAssets();
        InitCharacterData();

        targetScene = PlayerPrefs.GetString("TargetSceneName", "Next");
        currentIndex = PlayerPrefs.GetInt("SelectedCharacterID", 0);
        if (currentIndex < 0 || currentIndex >= characters.Length) currentIndex = 0;
    }

    private void LoadAssets()
    {
        modalPanelSprite  = Resources.Load<Sprite>("UI/panel_fantasy_modal");
        btnFantasyNormal   = Resources.Load<Sprite>("UI/btn_fantasy_normal");
        btnFantasyHover    = Resources.Load<Sprite>("UI/btn_fantasy_hover");
        btnFantasyPressed  = Resources.Load<Sprite>("UI/btn_fantasy_pressed");
        screenVignette     = Resources.Load<Sprite>("UI/screen_vignette");
        tmpFont            = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        portraitKaelen     = Resources.Load<Sprite>("UI/hud_player_portrait");
        portraitValeria    = Resources.Load<Sprite>("UI/hud_valeria_portrait");

        // Skill Icons
        iconKaelenThrust   = Resources.Load<Sprite>("UI/skill_icon_thrust");
        iconKaelenCyclone  = Resources.Load<Sprite>("UI/skill_icon_cyclone");
        iconKaelenUlt      = Resources.Load<Sprite>("UI/skill_icon_ultimate");

        iconValeriaFang    = Resources.Load<Sprite>("UI/skill_valeria_fang");
        iconValeriaVortex  = Resources.Load<Sprite>("UI/skill_valeria_vortex");
        iconValeriaWrath   = Resources.Load<Sprite>("UI/skill_valeria_wrath");

        sfxClick           = Resources.Load<AudioClip>("Audio/ui_click");
        sfxSwitch          = Resources.Load<AudioClip>("Audio/star_1");
        if (sfxSwitch == null) sfxSwitch = sfxClick;
    }

    private void InitCharacterData()
    {
        characters = new CharacterEntry[]
        {
            // ── 1. KAELEN (The Shadow Blader) ────────────────────────────────
            new CharacterEntry
            {
                name = "KAELEN",
                title = "THE SHADOW BLADER",
                role = "⚔ BLADE STRIKER / BALANCED",
                lore = "Ksatria berpedang api suci yang menjelajahi sisa-sisa reruntuhan masa lalu. Memadukan kecepatan tebasan kilat dengan ketahanan bertarung yang kokoh.",
                portrait = portraitKaelen,
                themeColor = new Color(0.35f, 0.85f, 1.0f, 1f),

                statHP  = "★★★★☆ (100)",
                statATK = "★★★★☆ (85)",
                statDEF = "★★★★☆ (80)",
                statSPD = "★★★☆☆ (75)",

                skill1Title = "SKILL 1 [A] — PHANTOM THRUST",
                skill1Cooldown = "Cooldown: 2.2s  |  Biaya: 12 MP",
                skill1Desc = "Menerobos secepat kilat ke depan dengan bilah pedang emas berapi, menembus dan melukai seluruh musuh di jalurnya.",
                skill1Icon = iconKaelenThrust,

                skill2Title = "SKILL 2 [S] — CYCLONE CRESCENT",
                skill2Cooldown = "Cooldown: 4.5s  |  Biaya: 22 MP",
                skill2Desc = "Memutar tubuh dan pedang 360 derajat menciptakan badai sabit emas yang menyapu area dan memukul mundur musuh di sekitar.",
                skill2Icon = iconKaelenCyclone,

                skill3Title = "ULTIMATE [W] — JUDGEMENT OF THE END",
                skill3Cooldown = "Cooldown: 9.0s  |  Biaya: 40 MP",
                skill3Desc = "Menghentikan aliran waktu sesaat, lalu memanggil hujan badai seribu pedang dimensi yang meluluhlantakkan seisi medan laga!",
                skill3Icon = iconKaelenUlt
            },

            // ── 2. VALERIA (The Scarlet Valkyrie) ───────────────────────────
            new CharacterEntry
            {
                name = "VALERIA",
                title = "THE SCARLET VALKYRIE",
                role = "🗡 CRIMSON ASSASSIN / BURST DPS",
                lore = "Pendekar wanita bertaring bayangan dari ordo Crimson. Menggunakan sepasang belati darah berkecepatan tinggi yang mampu menghabisi musuh dalam kedipan mata.",
                portrait = portraitValeria,
                themeColor = new Color(1.0f, 0.40f, 0.40f, 1f),

                statHP  = "★★★☆☆ (85)",
                statATK = "★★★★★ (98)",
                statDEF = "★★★☆☆ (70)",
                statSPD = "★★★★★ (95)",

                skill1Title = "SKILL 1 [A] — CRIMSON FANG",
                skill1Cooldown = "Cooldown: 2.0s  |  Biaya: 10 MP",
                skill1Desc = "Dash bayangan merah menyayat musuh dengan luka dalam dan menyedot darah lawan untuk memulihkan sebagian HP ksatria.",
                skill1Icon = iconValeriaFang,

                skill2Title = "SKILL 2 [S] — SCARLET VORTEX",
                skill2Cooldown = "Cooldown: 4.0s  |  Biaya: 20 MP",
                skill2Desc = "Putaran pusaran pisau ganda api merah neraka yang menarik musuh ke tengah pusaran dan membakar mereka secara beruntun.",
                skill2Icon = iconValeriaVortex,

                skill3Title = "ULTIMATE [W] — WRATH OF THE VALKYRIE",
                skill3Cooldown = "Cooldown: 8.5s  |  Biaya: 38 MP",
                skill3Desc = "Mengepakkan sayap api crimson ke angkasa, menukik tajam dari udara dan memicu ledakan magma darah raksasa berdaya hancur dahsyat!",
                skill3Icon = iconValeriaWrath
            }
        };
    }

    private void Start()
    {
        FindCanvas();
        BuildAtmosphere();
        BuildHeader();
        BuildTwoColumnLayout();
        BuildBottomControls();

        UpdateCharacterDisplay(false);
    }

    private void FindCanvas()
    {
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject cgo = new GameObject("CharacterSelectCanvas");
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            cgo.AddComponent<GraphicRaycaster>();
        }
    }

    private void BuildAtmosphere()
    {
        // Dark Base BG
        GameObject baseBG = new GameObject("BG_Dark");
        baseBG.transform.SetParent(canvas.transform, false);
        baseBG.transform.SetSiblingIndex(0);

        RectTransform brt = baseBG.AddComponent<RectTransform>();
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;

        Image bgi = baseBG.AddComponent<Image>();
        bgi.color = new Color(0.05f, 0.03f, 0.10f, 1f);

        // Screen Vignette
        if (screenVignette != null)
        {
            GameObject vigGO = new GameObject("Atmosphere_Vignette");
            vigGO.transform.SetParent(canvas.transform, false);
            vigGO.transform.SetSiblingIndex(1);

            RectTransform vrt = vigGO.AddComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;

            Image vi = vigGO.AddComponent<Image>();
            vi.sprite = screenVignette;
            vi.color  = new Color(1f, 1f, 1f, 0.75f);
            vi.raycastTarget = false;
        }
    }

    private void BuildHeader()
    {
        GameObject header = new GameObject("HeaderPanel");
        header.transform.SetParent(canvas.transform, false);

        RectTransform rt = header.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -22f);
        rt.sizeDelta = new Vector2(900f, 68f);

        Image img = header.AddComponent<Image>();
        img.color = new Color(0.08f, 0.05f, 0.16f, 0.94f);
        if (modalPanelSprite != null) { img.sprite = modalPanelSprite; img.type = Image.Type.Sliced; }

        CreateGoldDivider(header.transform, 1f, 0f);
        CreateGoldDivider(header.transform, 0f, 0f);

        TMP_Text title = CreateTMP(header.transform, "Title", 28f, FontStyles.Bold,
            new Color(1f, 0.88f, 0.40f, 1f), TextAlignmentOptions.Center);
        title.rectTransform.anchorMin = Vector2.zero; title.rectTransform.anchorMax = Vector2.one;
        title.rectTransform.offsetMin = Vector2.zero; title.rectTransform.offsetMax = Vector2.zero;
        title.characterSpacing = 4f;
        title.text = "✦  PILIH KESATRIA ANDA  ✦";
    }

    // ─────────────────────────────────────────────────────────────────────
    // TWO-COLUMN MAIN LAYOUT (Kiri: Idle Spotlight | Kanan: Skill Showcase)
    // ─────────────────────────────────────────────────────────────────────
    private void BuildTwoColumnLayout()
    {
        // ── KOLOM KIRI: CHARACTER SPOTLIGHT (Lebar: 480, Tinggi: 540) ────────
        GameObject leftCol = new GameObject("Col_Left_Spotlight");
        leftCol.transform.SetParent(canvas.transform, false);

        leftPanelRT = leftCol.AddComponent<RectTransform>();
        leftPanelRT.anchorMin = new Vector2(0.5f, 0.5f);
        leftPanelRT.anchorMax = new Vector2(0.5f, 0.5f);
        leftPanelRT.pivot     = new Vector2(0.5f, 0.5f);
        leftPanelRT.anchoredPosition = new Vector2(-360f, 25f);
        leftPanelRT.sizeDelta = new Vector2(480f, 540f);

        Image leftBg = leftCol.AddComponent<Image>();
        if (modalPanelSprite != null) { leftBg.sprite = modalPanelSprite; leftBg.type = Image.Type.Sliced; }
        leftBg.color = new Color(1f, 1f, 1f, 0.98f);

        CreateGoldDivider(leftCol.transform, 1f, 0f);
        CreateGoldDivider(leftCol.transform, 0f, 0f);

        // Tombol Panah KIRI [ ◄ ] & KANAN [ ► ] untuk ganti karakter
        BuildCarouselArrows(leftCol.transform);

        // Portrait Karakter (Idle Figure) di tengah-atas kolom kiri
        GameObject portGO = new GameObject("HeroPortrait");
        portGO.transform.SetParent(leftCol.transform, false);
        portraitRT = portGO.AddComponent<RectTransform>();
        portraitRT.anchorMin = new Vector2(0.5f, 1f);
        portraitRT.anchorMax = new Vector2(0.5f, 1f);
        portraitRT.pivot     = new Vector2(0.5f, 1f);
        portraitRT.anchoredPosition = new Vector2(0f, -40f);
        portraitRT.sizeDelta = new Vector2(170f, 170f);

        imgCharacterPortrait = portGO.AddComponent<Image>();
        imgCharacterPortrait.color = Color.white;

        // Nama Karakter (Besar)
        txtHeroName = CreateTMP(leftCol.transform, "HeroName", 28f, FontStyles.Bold,
            new Color(0.35f, 0.85f, 1.0f, 1f), TextAlignmentOptions.Center);
        txtHeroName.characterSpacing = 3f;
        RectTransform hnRT = txtHeroName.rectTransform;
        hnRT.anchorMin = new Vector2(0f, 1f); hnRT.anchorMax = new Vector2(1f, 1f);
        hnRT.pivot     = new Vector2(0.5f, 1f);
        hnRT.anchoredPosition = new Vector2(0f, -220f);
        hnRT.sizeDelta = new Vector2(0f, 32f);

        // Gelar Karakter
        txtHeroTitle = CreateTMP(leftCol.transform, "HeroTitle", 13.5f, FontStyles.Normal,
            new Color(0.85f, 0.80f, 0.75f, 1f), TextAlignmentOptions.Center);
        RectTransform htRT = txtHeroTitle.rectTransform;
        htRT.anchorMin = new Vector2(0f, 1f); htRT.anchorMax = new Vector2(1f, 1f);
        htRT.pivot     = new Vector2(0.5f, 1f);
        htRT.anchoredPosition = new Vector2(0f, -252f);
        htRT.sizeDelta = new Vector2(0f, 22f);

        // Role Badge Box
        GameObject roleBox = new GameObject("RoleBadge");
        roleBox.transform.SetParent(leftCol.transform, false);
        RectTransform rrt = roleBox.AddComponent<RectTransform>();
        rrt.anchorMin = new Vector2(0.5f, 1f); rrt.anchorMax = new Vector2(0.5f, 1f);
        rrt.pivot     = new Vector2(0.5f, 1f);
        rrt.anchoredPosition = new Vector2(0f, -282f);
        rrt.sizeDelta = new Vector2(300f, 30f);

        Image rImg = roleBox.AddComponent<Image>();
        rImg.color = new Color(0.12f, 0.08f, 0.22f, 0.90f);
        if (btnFantasyNormal != null) { rImg.sprite = btnFantasyNormal; rImg.type = Image.Type.Sliced; }

        txtHeroRole = CreateTMP(roleBox.transform, "RoleTxt", 12.5f, FontStyles.Bold,
            new Color(1f, 0.88f, 0.40f, 1f), TextAlignmentOptions.Center);
        txtHeroRole.rectTransform.anchorMin = Vector2.zero; txtHeroRole.rectTransform.anchorMax = Vector2.one;
        txtHeroRole.rectTransform.offsetMin = Vector2.zero; txtHeroRole.rectTransform.offsetMax = Vector2.zero;

        // Status Attributes (HP, ATK, DEF, SPD)
        GameObject statsBox = new GameObject("StatsBox");
        statsBox.transform.SetParent(leftCol.transform, false);
        RectTransform sbRT = statsBox.AddComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(0.5f, 1f); sbRT.anchorMax = new Vector2(0.5f, 1f);
        sbRT.pivot     = new Vector2(0.5f, 1f);
        sbRT.anchoredPosition = new Vector2(0f, -325f);
        sbRT.sizeDelta = new Vector2(400f, 50f);

        txtHeroStats = CreateTMP(statsBox.transform, "TxtStats", 13f, FontStyles.Bold,
            new Color(0.95f, 0.90f, 0.80f, 1f), TextAlignmentOptions.Center);
        txtHeroStats.rectTransform.anchorMin = Vector2.zero; txtHeroStats.rectTransform.anchorMax = Vector2.one;
        txtHeroStats.rectTransform.offsetMin = Vector2.zero; txtHeroStats.rectTransform.offsetMax = Vector2.zero;

        // Lore Box (Tertata rapi, tidak akan kelabasan!)
        GameObject loreBox = new GameObject("LoreBox");
        loreBox.transform.SetParent(leftCol.transform, false);
        RectTransform lbRT = loreBox.AddComponent<RectTransform>();
        lbRT.anchorMin = new Vector2(0.5f, 0f); lbRT.anchorMax = new Vector2(0.5f, 0f);
        lbRT.pivot     = new Vector2(0.5f, 0f);
        lbRT.anchoredPosition = new Vector2(0f, 25f);
        lbRT.sizeDelta = new Vector2(420f, 120f);

        Image lbImg = loreBox.AddComponent<Image>();
        lbImg.color = new Color(0.08f, 0.05f, 0.16f, 0.70f);

        txtHeroLore = CreateTMP(loreBox.transform, "TxtLore", 12.5f, FontStyles.Normal,
            new Color(0.85f, 0.82f, 0.76f, 1f), TextAlignmentOptions.Center);
        txtHeroLore.rectTransform.anchorMin = Vector2.zero; txtHeroLore.rectTransform.anchorMax = Vector2.one;
        txtHeroLore.rectTransform.offsetMin = new Vector2(16f, 10f); txtHeroLore.rectTransform.offsetMax = new Vector2(-16f, -10f);

        // ── KOLOM KANAN: SKILL SHOWCASE (Lebar: 700, Tinggi: 540) ────────────
        GameObject rightCol = new GameObject("Col_Right_Skills");
        rightCol.transform.SetParent(canvas.transform, false);

        RectTransform rightRT = rightCol.AddComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(0.5f, 0.5f);
        rightRT.anchorMax = new Vector2(0.5f, 0.5f);
        rightRT.pivot     = new Vector2(0.5f, 0.5f);
        rightRT.anchoredPosition = new Vector2(280f, 25f);
        rightRT.sizeDelta = new Vector2(700f, 540f);

        Image rightBg = rightCol.AddComponent<Image>();
        if (modalPanelSprite != null) { rightBg.sprite = modalPanelSprite; rightBg.type = Image.Type.Sliced; }
        rightBg.color = new Color(1f, 1f, 1f, 0.98f);

        CreateGoldDivider(rightCol.transform, 1f, 0f);
        CreateGoldDivider(rightCol.transform, 0f, 0f);

        // Header Skill Panel
        TMP_Text rHeader = CreateTMP(rightCol.transform, "SkillHeader", 17f, FontStyles.Bold,
            new Color(1f, 0.88f, 0.40f, 1f), TextAlignmentOptions.Left);
        rHeader.rectTransform.anchorMin = new Vector2(0f, 1f); rHeader.rectTransform.anchorMax = new Vector2(1f, 1f);
        rHeader.rectTransform.pivot     = new Vector2(0f, 1f);
        rHeader.rectTransform.anchoredPosition = new Vector2(30f, -20f);
        rHeader.rectTransform.sizeDelta = new Vector2(-60f, 26f);
        rHeader.text = "✦  JURUS PEDANG & KEMAMPUAN SAKTI  ✦";

        // 3 Baris Kotak Skill Lengkap dengan Ikon
        float startY = -62f;
        float slotHeight = 138f;
        float slotGap = 16f;

        // Skill 1 [A]
        var sk1 = BuildSkillSlot(rightCol.transform, "Slot_Skill1", startY, new Color(1.0f, 0.60f, 0.15f, 1f));
        imgSkill1Icon = sk1.icon; txtSkill1Header = sk1.header; txtSkill1Desc = sk1.desc;

        // Skill 2 [S]
        var sk2 = BuildSkillSlot(rightCol.transform, "Slot_Skill2", startY - (slotHeight + slotGap), new Color(1.0f, 0.45f, 0.20f, 1f));
        imgSkill2Icon = sk2.icon; txtSkill2Header = sk2.header; txtSkill2Desc = sk2.desc;

        // Skill 3 [W] (Ultimate)
        var sk3 = BuildSkillSlot(rightCol.transform, "Slot_Skill3", startY - (slotHeight + slotGap) * 2, new Color(1.0f, 0.25f, 0.25f, 1f));
        imgSkill3Icon = sk3.icon; txtSkill3Header = sk3.header; txtSkill3Desc = sk3.desc;
    }

    private void BuildCarouselArrows(Transform parent)
    {
        // Tombol Panah KIRI [ ◄ ]
        GameObject btnLeft = new GameObject("BtnArrowLeft");
        btnLeft.transform.SetParent(parent, false);
        RectTransform blRT = btnLeft.AddComponent<RectTransform>();
        blRT.anchorMin = new Vector2(0f, 1f); blRT.anchorMax = new Vector2(0f, 1f);
        blRT.pivot     = new Vector2(0.5f, 0.5f);
        blRT.anchoredPosition = new Vector2(40f, -125f);
        blRT.sizeDelta = new Vector2(52f, 52f);

        Image blImg = btnLeft.AddComponent<Image>();
        if (btnFantasyNormal != null) { blImg.sprite = btnFantasyNormal; blImg.type = Image.Type.Sliced; }
        blImg.color = Color.white;

        Button blBtn = btnLeft.AddComponent<Button>();
        blBtn.onClick.AddListener(() => SwitchCharacter(-1));

        TMP_Text blTxt = CreateTMP(btnLeft.transform, "TxtLeft", 22f, FontStyles.Bold,
            new Color(1f, 0.88f, 0.40f, 1f), TextAlignmentOptions.Center);
        blTxt.rectTransform.anchorMin = Vector2.zero; blTxt.rectTransform.anchorMax = Vector2.one;
        blTxt.rectTransform.offsetMin = Vector2.zero; blTxt.rectTransform.offsetMax = Vector2.zero;
        blTxt.text = "◄";

        // Tombol Panah KANAN [ ► ]
        GameObject btnRight = new GameObject("BtnArrowRight");
        btnRight.transform.SetParent(parent, false);
        RectTransform brRT = btnRight.AddComponent<RectTransform>();
        brRT.anchorMin = new Vector2(1f, 1f); brRT.anchorMax = new Vector2(1f, 1f);
        brRT.pivot     = new Vector2(0.5f, 0.5f);
        brRT.anchoredPosition = new Vector2(-40f, -125f);
        brRT.sizeDelta = new Vector2(52f, 52f);

        Image brImg = btnRight.AddComponent<Image>();
        if (btnFantasyNormal != null) { brImg.sprite = btnFantasyNormal; brImg.type = Image.Type.Sliced; }
        brImg.color = Color.white;

        Button brBtn = btnRight.AddComponent<Button>();
        brBtn.onClick.AddListener(() => SwitchCharacter(1));

        TMP_Text brTxt = CreateTMP(btnRight.transform, "TxtRight", 22f, FontStyles.Bold,
            new Color(1f, 0.88f, 0.40f, 1f), TextAlignmentOptions.Center);
        brTxt.rectTransform.anchorMin = Vector2.zero; brTxt.rectTransform.anchorMax = Vector2.one;
        brTxt.rectTransform.offsetMin = Vector2.zero; brTxt.rectTransform.offsetMax = Vector2.zero;
        brTxt.text = "►";

        // Page Indicator di bawah ("Karakter 1 / 2")
        txtPageIndicator = CreateTMP(parent, "PageIndicator", 12f, FontStyles.Bold,
            new Color(0.70f, 0.70f, 0.70f, 0.85f), TextAlignmentOptions.Center);
        RectTransform piRT = txtPageIndicator.rectTransform;
        piRT.anchorMin = new Vector2(0.5f, 1f); piRT.anchorMax = new Vector2(0.5f, 1f);
        piRT.pivot     = new Vector2(0.5f, 1f);
        piRT.anchoredPosition = new Vector2(0f, -12f);
        piRT.sizeDelta = new Vector2(200f, 20f);
    }

    private (Image icon, TMP_Text header, TMP_Text desc) BuildSkillSlot(Transform parent, string name, float posY, Color accent)
    {
        GameObject slot = new GameObject(name);
        slot.transform.SetParent(parent, false);

        RectTransform srt = slot.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 1f); srt.anchorMax = new Vector2(1f, 1f);
        srt.pivot     = new Vector2(0.5f, 1f);
        srt.anchoredPosition = new Vector2(0f, posY);
        srt.sizeDelta = new Vector2(-48f, 134f);

        Image slotBg = slot.AddComponent<Image>();
        slotBg.color = new Color(0.11f, 0.08f, 0.20f, 0.88f);
        if (btnFantasyNormal != null) { slotBg.sprite = btnFantasyNormal; slotBg.type = Image.Type.Sliced; }

        // Strip Accent di kiri slot
        GameObject strip = new GameObject("Strip");
        strip.transform.SetParent(slot.transform, false);
        RectTransform stRT = strip.AddComponent<RectTransform>();
        stRT.anchorMin = new Vector2(0f, 0f); stRT.anchorMax = new Vector2(0f, 1f);
        stRT.pivot     = new Vector2(0f, 0.5f);
        stRT.anchoredPosition = Vector2.zero;
        stRT.sizeDelta = new Vector2(6f, 0f);
        strip.AddComponent<Image>().color = accent;

        // Skill Icon Frame (84 x 84 px)
        GameObject iconBox = new GameObject("IconBox");
        iconBox.transform.SetParent(slot.transform, false);
        RectTransform ibRT = iconBox.AddComponent<RectTransform>();
        ibRT.anchorMin = new Vector2(0f, 0.5f); ibRT.anchorMax = new Vector2(0f, 0.5f);
        ibRT.pivot     = new Vector2(0f, 0.5f);
        ibRT.anchoredPosition = new Vector2(20f, 0f);
        ibRT.sizeDelta = new Vector2(88f, 88f);

        Image ibBg = iconBox.AddComponent<Image>();
        ibBg.color = new Color(0.06f, 0.04f, 0.14f, 0.95f);

        // Icon Sprite Image
        GameObject iconImgGO = new GameObject("IconImg");
        iconImgGO.transform.SetParent(iconBox.transform, false);
        RectTransform iiRT = iconImgGO.AddComponent<RectTransform>();
        iiRT.anchorMin = Vector2.zero; iiRT.anchorMax = Vector2.one;
        iiRT.offsetMin = new Vector2(6f, 6f); iiRT.offsetMax = new Vector2(-6f, -6f);
        Image iconImg = iconImgGO.AddComponent<Image>();
        iconImg.color = Color.white;

        // Skill Header (Judul Jurus + Cooldown)
        TMP_Text header = CreateTMP(slot.transform, "Header", 15.5f, FontStyles.Bold,
            accent, TextAlignmentOptions.Left);
        header.rectTransform.anchorMin = new Vector2(0f, 1f); header.rectTransform.anchorMax = new Vector2(1f, 1f);
        header.rectTransform.pivot     = new Vector2(0f, 1f);
        header.rectTransform.anchoredPosition = new Vector2(125f, -14f);
        header.rectTransform.sizeDelta = new Vector2(-140f, 26f);

        // Skill Description (Tertata rapi di kanan)
        TMP_Text desc = CreateTMP(slot.transform, "Desc", 13f, FontStyles.Normal,
            new Color(0.90f, 0.86f, 0.80f, 1f), TextAlignmentOptions.TopLeft);
        desc.rectTransform.anchorMin = new Vector2(0f, 0f); desc.rectTransform.anchorMax = new Vector2(1f, 1f);
        desc.rectTransform.pivot     = new Vector2(0.5f, 0.5f);
        desc.rectTransform.offsetMin = new Vector2(125f, 12f);
        desc.rectTransform.offsetMax = new Vector2(-20f, -44f);

        return (iconImg, header, desc);
    }

    private void BuildBottomControls()
    {
        // Tombol Kembali ke Stage Select (Kiri Bawah)
        GameObject btnBack = new GameObject("BtnBack");
        btnBack.transform.SetParent(canvas.transform, false);
        RectTransform bRT = btnBack.AddComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.5f, 0f); bRT.anchorMax = new Vector2(0.5f, 0f);
        bRT.pivot     = new Vector2(0.5f, 0f);
        bRT.anchoredPosition = new Vector2(-360f, 25f);
        bRT.sizeDelta = new Vector2(280f, 52f);

        Image bImg = btnBack.AddComponent<Image>();
        if (btnFantasyNormal != null) { bImg.sprite = btnFantasyNormal; bImg.type = Image.Type.Sliced; }
        bImg.color = Color.white;

        Button btn = btnBack.AddComponent<Button>();
        btn.onClick.AddListener(OnBackClicked);

        TMP_Text bTxt = CreateTMP(btnBack.transform, "TxtBack", 16f, FontStyles.Bold,
            new Color(0.90f, 0.82f, 0.70f, 1f), TextAlignmentOptions.Center);
        bTxt.rectTransform.anchorMin = Vector2.zero; bTxt.rectTransform.anchorMax = Vector2.one;
        bTxt.rectTransform.offsetMin = Vector2.zero; bTxt.rectTransform.offsetMax = Vector2.zero;
        bTxt.text = "←  KEMBALI KE STAGE";

        // Tombol Mainkan Sebagai [Hero] (Kanan Bawah)
        GameObject btnPlay = new GameObject("BtnPlay");
        btnPlay.transform.SetParent(canvas.transform, false);
        RectTransform pRT = btnPlay.AddComponent<RectTransform>();
        pRT.anchorMin = new Vector2(0.5f, 0f); pRT.anchorMax = new Vector2(0.5f, 0f);
        pRT.pivot     = new Vector2(0.5f, 0f);
        pRT.anchoredPosition = new Vector2(280f, 25f);
        pRT.sizeDelta = new Vector2(420f, 56f);

        Image pImg = btnPlay.AddComponent<Image>();
        if (btnFantasyNormal != null) { pImg.sprite = btnFantasyNormal; pImg.type = Image.Type.Sliced; }
        pImg.color = Color.white;

        Button playBtn = btnPlay.AddComponent<Button>();
        if (btnFantasyHover != null)
        {
            playBtn.transition = Selectable.Transition.SpriteSwap;
            SpriteState ss = new SpriteState();
            ss.highlightedSprite = btnFantasyHover;
            ss.pressedSprite     = btnFantasyHover;
            ss.selectedSprite    = btnFantasyHover;
            playBtn.spriteState = ss;
        }
        playBtn.onClick.AddListener(OnPlayClicked);

        txtPlayBtnLabel = CreateTMP(btnPlay.transform, "TxtPlay", 18f, FontStyles.Bold,
            new Color(1f, 0.92f, 0.40f, 1f), TextAlignmentOptions.Center);
        txtPlayBtnLabel.characterSpacing = 2f;
        txtPlayBtnLabel.rectTransform.anchorMin = Vector2.zero; txtPlayBtnLabel.rectTransform.anchorMax = Vector2.one;
        txtPlayBtnLabel.rectTransform.offsetMin = Vector2.zero; txtPlayBtnLabel.rectTransform.offsetMax = Vector2.zero;
    }

    private void CreateGoldDivider(Transform parent, float anchorY, float posY)
    {
        GameObject line = new GameObject("GoldDivider");
        line.transform.SetParent(parent, false);
        RectTransform rt = line.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, anchorY);
        rt.anchorMax = new Vector2(1f, anchorY);
        rt.pivot     = new Vector2(0.5f, anchorY);
        rt.sizeDelta = new Vector2(0f, 3f);
        rt.anchoredPosition = new Vector2(0f, posY);

        Image img = line.AddComponent<Image>();
        img.color = new Color(0.98f, 0.78f, 0.30f, 0.85f);
        img.raycastTarget = false;
    }

    private TMP_Text CreateTMP(Transform parent, string name, float size, FontStyles style, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = tmpFont;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    // ─────────────────────────────────────────────────────────────────────
    // LOGIKA PERGANTIAN KARAKTER (CAROUSEL SWITCH)
    // ─────────────────────────────────────────────────────────────────────
    private void SwitchCharacter(int direction)
    {
        currentIndex += direction;
        if (currentIndex < 0) currentIndex = characters.Length - 1;
        if (currentIndex >= characters.Length) currentIndex = 0;

        UpdateCharacterDisplay(true);
    }

    private void UpdateCharacterDisplay(bool playSfx)
    {
        if (playSfx && audioSource != null && sfxSwitch != null)
        {
            audioSource.PlayOneShot(sfxSwitch, 0.9f);
        }

        CharacterEntry c = characters[currentIndex];

        // 1. Update Kolom Kiri
        if (imgCharacterPortrait != null && c.portrait != null)
        {
            imgCharacterPortrait.sprite = c.portrait;
        }

        if (txtHeroName != null)
        {
            txtHeroName.text = c.name;
            txtHeroName.color = c.themeColor;
        }

        if (txtHeroTitle != null) txtHeroTitle.text = c.title;
        if (txtHeroRole != null)  txtHeroRole.text  = c.role;
        if (txtHeroLore != null)  txtHeroLore.text  = $"\"{c.lore}\"";

        if (txtHeroStats != null)
        {
            txtHeroStats.text = $"HP: <color=#66FF88>{c.statHP}</color>   ATK: <color=#FF6666>{c.statATK}</color>\nDEF: <color=#FFCC44>{c.statDEF}</color>   SPD: <color=#66CCFF>{c.statSPD}</color>";
        }

        if (txtPageIndicator != null)
        {
            txtPageIndicator.text = $"[ KARAKTER  {currentIndex + 1}  /  {characters.Length} ]";
        }

        // 2. Update Kolom Kanan (Skill Showcase)
        if (imgSkill1Icon != null && c.skill1Icon != null) imgSkill1Icon.sprite = c.skill1Icon;
        if (txtSkill1Header != null) txtSkill1Header.text = $"{c.skill1Title}   <size=11><color=#FFDD88>({c.skill1Cooldown})</color></size>";
        if (txtSkill1Desc != null)   txtSkill1Desc.text   = c.skill1Desc;

        if (imgSkill2Icon != null && c.skill2Icon != null) imgSkill2Icon.sprite = c.skill2Icon;
        if (txtSkill2Header != null) txtSkill2Header.text = $"{c.skill2Title}   <size=11><color=#FFDD88>({c.skill2Cooldown})</color></size>";
        if (txtSkill2Desc != null)   txtSkill2Desc.text   = c.skill2Desc;

        if (imgSkill3Icon != null && c.skill3Icon != null) imgSkill3Icon.sprite = c.skill3Icon;
        if (txtSkill3Header != null) txtSkill3Header.text = $"{c.skill3Title}   <size=11><color=#FFDD88>({c.skill3Cooldown})</color></size>";
        if (txtSkill3Desc != null)   txtSkill3Desc.text   = c.skill3Desc;

        // 3. Update Tombol Konfirmasi Mainkan
        if (txtPlayBtnLabel != null)
        {
            txtPlayBtnLabel.text = $"▶  MAINKAN SEBAGAI {c.name}";
        }

        // Trigger Pop animation pada portrait
        StartCoroutine(PortraitPopRoutine());
    }

    private IEnumerator PortraitPopRoutine()
    {
        if (portraitRT == null) yield break;

        portraitRT.localScale = new Vector3(0.85f, 0.85f, 1f);
        float elapsed = 0f;
        float dur = 0.20f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            float s = Mathf.SmoothStep(0.85f, 1f, t);
            portraitRT.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        portraitRT.localScale = Vector3.one;
    }

    private void Update()
    {
        // Navigasi Keyboard Panah Kiri / Kanan atau A / D
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            SwitchCharacter(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            SwitchCharacter(1);
        }

        // Konfirmasi dengan Enter / Space
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnPlayClicked();
        }

        // Idle bobbing / bernapas lembut pada portrait
        if (portraitRT != null)
        {
            float bob = Mathf.Sin(Time.time * 2.2f) * 4f;
            portraitRT.anchoredPosition = new Vector2(0f, -40f + bob);
        }
    }

    private void OnPlayClicked()
    {
        CharacterEntry chosen = characters[currentIndex];

        // Simpan ke PlayerPrefs
        PlayerPrefs.SetInt("SelectedCharacterID", currentIndex);
        PlayerPrefs.SetString("SelectedCharacterName", chosen.name);
        PlayerPrefs.Save();

        if (audioSource != null && sfxClick != null) audioSource.PlayOneShot(sfxClick);

        StartCoroutine(LoadSceneWithFade(targetScene));
    }

    private void OnBackClicked()
    {
        if (audioSource != null && sfxClick != null) audioSource.PlayOneShot(sfxClick);
        StartCoroutine(LoadSceneWithFade("StageSelect"));
    }

    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        GameObject fadeGO = new GameObject("FadeOut");
        fadeGO.transform.SetParent(canvas.transform, false);
        fadeGO.transform.SetAsLastSibling();

        RectTransform frt = fadeGO.AddComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;

        Image fi = fadeGO.AddComponent<Image>();
        fi.color = new Color(0f, 0f, 0f, 0f);

        float dur = 0.35f, elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            fi.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 1f, elapsed / dur));
            yield return null;
        }

        SceneManager.LoadScene(sceneName);
    }
}
