using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// CharacterSelectScene — Mengelola pemilihan karakter (minimal 2 karakter: Kaelen & Valeria)
/// lengkap dengan peragaan status, deskripsi 3 skill pedang sakti [A], [S], [W], dan transisi ke gameplay.
/// </summary>
public class CharacterSelectScene : MonoBehaviour
{
    // ── Asset Refs ────────────────────────────────────────────────────────
    private Sprite modalPanelSprite;
    private Sprite btnFantasyNormal;
    private Sprite btnFantasyHover;
    private Sprite btnFantasyPressed;
    private Sprite portraitKaelen;
    private Sprite portraitValeria;
    private Sprite screenVignette;
    private TMP_FontAsset tmpFont;
    private AudioClip sfxClick;
    private AudioClip sfxSelect;
    private AudioSource audioSource;

    // ── UI Refs ───────────────────────────────────────────────────────────
    private Canvas canvas;
    private GameObject[] characterCards;
    private Image[] cardBorderHighlights;
    private TMP_Text[] txtCardSelectionBadges;

    // Detail Panel Refs
    private TMP_Text txtSelectedHeroName;
    private TMP_Text txtSelectedHeroTitle;
    private TMP_Text txtSelectedHeroDesc;
    private TMP_Text txtStatHP;
    private TMP_Text txtStatATK;
    private TMP_Text txtStatSPD;

    // Skill Boxes Refs
    private TMP_Text txtSkill1Title;
    private TMP_Text txtSkill1Desc;
    private TMP_Text txtSkill2Title;
    private TMP_Text txtSkill2Desc;
    private TMP_Text txtSkill3Title;
    private TMP_Text txtSkill3Desc;

    private Button btnConfirmPlay;
    private TMP_Text txtConfirmBtnLabel;

    // ── Data Karakter ─────────────────────────────────────────────────────
    private class CharacterData
    {
        public string name;
        public string title;
        public string role;
        public string lore;
        public Sprite portrait;
        public string statHP;
        public string statATK;
        public string statSPD;
        public Color themeColor;

        // Skill 1 [A]
        public string skill1Name;
        public string skill1Desc;

        // Skill 2 [S]
        public string skill2Name;
        public string skill2Desc;

        // Skill 3 [W] (Ultimate)
        public string skill3Name;
        public string skill3Desc;
    }

    private CharacterData[] characters;
    private int selectedIndex = 0;
    private string targetScene = "Next";

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        LoadAssets();
        InitCharacterData();

        targetScene = PlayerPrefs.GetString("TargetSceneName", "Next");
        selectedIndex = PlayerPrefs.GetInt("SelectedCharacterID", 0);
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

        sfxClick           = Resources.Load<AudioClip>("Audio/ui_click");
        sfxSelect          = Resources.Load<AudioClip>("Audio/star_1");
        if (sfxSelect == null) sfxSelect = sfxClick;
    }

    private void InitCharacterData()
    {
        characters = new CharacterData[]
        {
            // ── Karakter 1: KAELEN ──────────────────────────────────────────
            new CharacterData
            {
                name = "KAELEN",
                title = "THE SHADOW BLADER",
                role = "⚔ BLADE STRIKER / BALANCED",
                lore = "Ksatria pengembara pembawa pedang api suci & bayangan. Menguasai tebasan kilat terobos dan badai pedang pelindung.",
                portrait = portraitKaelen,
                statHP  = "★★★★☆ (100 HP)",
                statATK = "★★★★☆ (SEIMBANG & KUAT)",
                statSPD = "★★★☆☆ (LINCAH)",
                themeColor = new Color(0.30f, 0.85f, 1.0f, 1f),

                skill1Name = "[A]  PHANTOM THRUST",
                skill1Desc = "Menerobos kilat menusuk barisan musuh dengan bilah pedang emas berapi (Cooldown: 2.2s).",

                skill2Name = "[S]  CYCLONE CRESCENT",
                skill2Desc = "Memutar pedang 360° menciptakan badai sabit emas yang melibas dan memukul mundur musuh (Cooldown: 4.5s).",

                skill3Name = "[W]  JUDGEMENT OF THE END  (ULTIMATE)",
                skill3Desc = "Menghentikan waktu sesaat dan memanggil badai seribu pedang dimensi yang menghancurkan seluruh arena (Cooldown: 9.0s)."
            },

            // ── Karakter 2: VALERIA ─────────────────────────────────────────
            new CharacterData
            {
                name = "VALERIA",
                title = "THE SCARLET VALKYRIE",
                role = "🗡 CRIMSON ASSASSIN / HIGH DPS",
                lore = "Pendekar wanita pembantai berkecepatan tinggi dari klan Crimson. Menghabisi musuh sebelum mereka sempat bereaksi.",
                portrait = portraitValeria,
                statHP  = "★★★☆☆ (85 HP)",
                statATK = "★★★★★ (BURST SANGAT TINGGI)",
                statSPD = "★★★★★ (SANGAT KILAT)",
                themeColor = new Color(1.0f, 0.35f, 0.35f, 1f),

                skill1Name = "[A]  CRIMSON FANG",
                skill1Desc = "Dash bayangan merah menyayat lawan dengan cepat dan menyedot darah musuh untuk memulihkan sebagian HP (Cooldown: 2.0s).",

                skill2Name = "[S]  SCARLET VORTEX",
                skill2Desc = "Pusaran pisau ganda api merah neraka yang menarik musuh ke pusaran dan membakar mereka (Cooldown: 4.0s).",

                skill3Name = "[W]  WRATH OF THE VALKYRIE  (ULTIMATE)",
                skill3Desc = "Membuka sayap api crimson, menukik tajam dari udara dan memicu ledakan magma darah raksasa! (Cooldown: 8.5s)."
            }
        };
    }

    private void Start()
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

        BuildAtmosphere();
        BuildUI();
        SelectCharacter(selectedIndex, false);
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
            vi.color  = new Color(1f, 1f, 1f, 0.70f);
            vi.raycastTarget = false;
        }
    }

    private void BuildUI()
    {
        // Header Banner
        BuildHeaderBanner();

        // 2 Kartu Karakter di sebelah kiri (Kaelen & Valeria)
        BuildCharacterCards();

        // Panel Detail Status & Deskripsi 3 Skill di sebelah kanan
        BuildSkillDetailPanel();

        // Tombol Navigasi Bawah
        BuildBottomActionButtons();
    }

    private void BuildHeaderBanner()
    {
        GameObject banner = new GameObject("HeaderBanner");
        banner.transform.SetParent(canvas.transform, false);

        RectTransform rt = banner.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -25f);
        rt.sizeDelta = new Vector2(760f, 75f);

        Image bImg = banner.AddComponent<Image>();
        bImg.color = new Color(0.08f, 0.05f, 0.16f, 0.92f);
        if (modalPanelSprite != null) { bImg.sprite = modalPanelSprite; bImg.type = Image.Type.Sliced; }

        CreateGoldDivider(banner.transform, 1f, 0f);
        CreateGoldDivider(banner.transform, 0f, 0f);

        TMP_Text title = CreateTMP(banner.transform, "Title", 30f, FontStyles.Bold,
            new Color(1f, 0.88f, 0.40f, 1f), TextAlignmentOptions.Center);
        title.rectTransform.anchorMin = Vector2.zero; title.rectTransform.anchorMax = Vector2.one;
        title.rectTransform.offsetMin = Vector2.zero; title.rectTransform.offsetMax = Vector2.zero;
        title.characterSpacing = 4f;
        title.text = "✦  PILIH KESATRIA ANDA  ✦";
    }

    private void BuildCharacterCards()
    {
        characterCards = new GameObject[characters.Length];
        cardBorderHighlights = new Image[characters.Length];
        txtCardSelectionBadges = new TMP_Text[characters.Length];

        float startX = -460f;
        float cardWidth = 300f;
        float cardHeight = 460f;
        float gap = 30f;

        for (int i = 0; i < characters.Length; i++)
        {
            int idx = i;
            CharacterData data = characters[i];

            GameObject card = new GameObject("Card_" + data.name);
            card.transform.SetParent(canvas.transform, false);
            characterCards[i] = card;

            RectTransform crt = card.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot     = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(startX + i * (cardWidth + gap), 30f);
            crt.sizeDelta = new Vector2(cardWidth, cardHeight);

            Image cardBg = card.AddComponent<Image>();
            if (modalPanelSprite != null)
            {
                cardBg.sprite = modalPanelSprite;
                cardBg.type = Image.Type.Sliced;
                cardBg.color = new Color(1f, 1f, 1f, 0.96f);
            }
            else
            {
                cardBg.color = new Color(0.10f, 0.07f, 0.20f, 0.95f);
            }

            // Card Highlight Border (Glow saat terpilih)
            GameObject borderGO = new GameObject("SelectionGlow");
            borderGO.transform.SetParent(card.transform, false);
            RectTransform brt = borderGO.AddComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(-4f, -4f); brt.offsetMax = new Vector2(4f, 4f);
            Image borderImg = borderGO.AddComponent<Image>();
            borderImg.color = new Color(1f, 0.85f, 0.35f, 0f);
            borderImg.raycastTarget = false;
            cardBorderHighlights[i] = borderImg;

            // Character Portrait
            GameObject portGO = new GameObject("Portrait");
            portGO.transform.SetParent(card.transform, false);
            RectTransform prt = portGO.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 1f); prt.anchorMax = new Vector2(0.5f, 1f);
            prt.pivot     = new Vector2(0.5f, 1f);
            prt.anchoredPosition = new Vector2(0f, -25f);
            prt.sizeDelta = new Vector2(170f, 170f);

            Image portImg = portGO.AddComponent<Image>();
            if (data.portrait != null) portImg.sprite = data.portrait;
            portImg.color = Color.white;

            // Character Name
            TMP_Text nameTxt = CreateTMP(card.transform, "Name", 24f, FontStyles.Bold,
                data.themeColor, TextAlignmentOptions.Center);
            nameTxt.rectTransform.anchorMin = new Vector2(0f, 1f); nameTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
            nameTxt.rectTransform.pivot     = new Vector2(0.5f, 1f);
            nameTxt.rectTransform.anchoredPosition = new Vector2(0f, -208f);
            nameTxt.rectTransform.sizeDelta = new Vector2(0f, 30f);
            nameTxt.characterSpacing = 2f;
            nameTxt.text = data.name;

            // Title Tag
            TMP_Text titleTxt = CreateTMP(card.transform, "Title", 13f, FontStyles.Normal,
                new Color(0.85f, 0.80f, 0.75f, 1f), TextAlignmentOptions.Center);
            titleTxt.rectTransform.anchorMin = new Vector2(0f, 1f); titleTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleTxt.rectTransform.pivot     = new Vector2(0.5f, 1f);
            titleTxt.rectTransform.anchoredPosition = new Vector2(0f, -238f);
            titleTxt.rectTransform.sizeDelta = new Vector2(0f, 22f);
            titleTxt.text = data.title;

            // Role Tag Box
            GameObject roleBox = new GameObject("RoleBox");
            roleBox.transform.SetParent(card.transform, false);
            RectTransform rrt = roleBox.AddComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.5f, 1f); rrt.anchorMax = new Vector2(0.5f, 1f);
            rrt.pivot     = new Vector2(0.5f, 1f);
            rrt.anchoredPosition = new Vector2(0f, -270f);
            rrt.sizeDelta = new Vector2(240f, 28f);

            Image rImg = roleBox.AddComponent<Image>();
            rImg.color = new Color(0.12f, 0.08f, 0.22f, 0.85f);
            TMP_Text rTxt = CreateTMP(roleBox.transform, "RoleTxt", 11.5f, FontStyles.Bold,
                new Color(1f, 0.88f, 0.40f, 1f), TextAlignmentOptions.Center);
            rTxt.rectTransform.anchorMin = Vector2.zero; rTxt.rectTransform.anchorMax = Vector2.one;
            rTxt.rectTransform.offsetMin = Vector2.zero; rTxt.rectTransform.offsetMax = Vector2.zero;
            rTxt.text = data.role;

            // Lore Snippet
            TMP_Text loreTxt = CreateTMP(card.transform, "Lore", 12f, FontStyles.Normal,
                new Color(0.80f, 0.75f, 0.70f, 1f), TextAlignmentOptions.Center);
            loreTxt.rectTransform.anchorMin = new Vector2(0f, 0f); loreTxt.rectTransform.anchorMax = new Vector2(1f, 0f);
            loreTxt.rectTransform.pivot     = new Vector2(0.5f, 0f);
            loreTxt.rectTransform.anchoredPosition = new Vector2(0f, 75f);
            loreTxt.rectTransform.sizeDelta = new Vector2(250f, 65f);
            loreTxt.text = data.lore;

            // Selection Button
            Button cardBtn = card.AddComponent<Button>();
            cardBtn.onClick.AddListener(() => SelectCharacter(idx, true));

            // Selection Badge at bottom of card
            GameObject badgeGO = new GameObject("SelectBadge");
            badgeGO.transform.SetParent(card.transform, false);
            RectTransform badgRT = badgeGO.AddComponent<RectTransform>();
            badgRT.anchorMin = new Vector2(0.5f, 0f); badgRT.anchorMax = new Vector2(0.5f, 0f);
            badgRT.pivot     = new Vector2(0.5f, 0f);
            badgRT.anchoredPosition = new Vector2(0f, 20f);
            badgRT.sizeDelta = new Vector2(220f, 38f);

            Image badgImg = badgeGO.AddComponent<Image>();
            if (btnFantasyNormal != null) { badgImg.sprite = btnFantasyNormal; badgImg.type = Image.Type.Sliced; }
            badgImg.color = Color.white;

            TMP_Text badgeTxt = CreateTMP(badgeGO.transform, "TxtBadge", 14f, FontStyles.Bold,
                new Color(1f, 0.95f, 0.80f, 1f), TextAlignmentOptions.Center);
            badgeTxt.rectTransform.anchorMin = Vector2.zero; badgeTxt.rectTransform.anchorMax = Vector2.one;
            badgeTxt.rectTransform.offsetMin = Vector2.zero; badgeTxt.rectTransform.offsetMax = Vector2.zero;
            txtCardSelectionBadges[i] = badgeTxt;
        }
    }

    private void BuildSkillDetailPanel()
    {
        // Panel Detail di sebelah kanan (X = 260)
        GameObject panel = new GameObject("DetailPanel");
        panel.transform.SetParent(canvas.transform, false);

        RectTransform prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot     = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = new Vector2(250f, 30f);
        prt.sizeDelta = new Vector2(640f, 460f);

        Image pImg = panel.AddComponent<Image>();
        if (modalPanelSprite != null)
        {
            pImg.sprite = modalPanelSprite;
            pImg.type   = Image.Type.Sliced;
            pImg.color  = new Color(1f, 1f, 1f, 0.98f);
        }
        else
        {
            pImg.color = new Color(0.08f, 0.05f, 0.16f, 0.96f);
        }

        CreateGoldDivider(panel.transform, 1f, 0f);
        CreateGoldDivider(panel.transform, 0f, 0f);

        // Header Detail
        TMP_Text pHeader = CreateTMP(panel.transform, "Header", 16f, FontStyles.Bold,
            new Color(1f, 0.88f, 0.40f, 1f), TextAlignmentOptions.Left);
        pHeader.rectTransform.anchorMin = new Vector2(0f, 1f); pHeader.rectTransform.anchorMax = new Vector2(1f, 1f);
        pHeader.rectTransform.pivot     = new Vector2(0f, 1f);
        pHeader.rectTransform.anchoredPosition = new Vector2(25f, -18f);
        pHeader.rectTransform.sizeDelta = new Vector2(-50f, 25f);
        pHeader.text = "✦  STATUS & JURUS PEDANG SAKTI  ✦";

        // Stats Display (HP, ATK, SPD)
        GameObject statsRow = new GameObject("StatsRow");
        statsRow.transform.SetParent(panel.transform, false);
        RectTransform srt = statsRow.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 1f); srt.anchorMax = new Vector2(1f, 1f);
        srt.pivot     = new Vector2(0.5f, 1f);
        srt.anchoredPosition = new Vector2(0f, -48f);
        srt.sizeDelta = new Vector2(-50f, 30f);

        txtStatHP = CreateTMP(statsRow.transform, "StatHP", 13f, FontStyles.Bold, new Color(0.4f, 1f, 0.5f, 1f), TextAlignmentOptions.Left);
        txtStatHP.rectTransform.anchorMin = new Vector2(0f, 0f); txtStatHP.rectTransform.anchorMax = new Vector2(0.33f, 1f);
        txtStatHP.rectTransform.offsetMin = Vector2.zero; txtStatHP.rectTransform.offsetMax = Vector2.zero;

        txtStatATK = CreateTMP(statsRow.transform, "StatATK", 13f, FontStyles.Bold, new Color(1f, 0.4f, 0.4f, 1f), TextAlignmentOptions.Center);
        txtStatATK.rectTransform.anchorMin = new Vector2(0.33f, 0f); txtStatATK.rectTransform.anchorMax = new Vector2(0.66f, 1f);
        txtStatATK.rectTransform.offsetMin = Vector2.zero; txtStatATK.rectTransform.offsetMax = Vector2.zero;

        txtStatSPD = CreateTMP(statsRow.transform, "StatSPD", 13f, FontStyles.Bold, new Color(0.4f, 0.85f, 1f, 1f), TextAlignmentOptions.Right);
        txtStatSPD.rectTransform.anchorMin = new Vector2(0.66f, 0f); txtStatSPD.rectTransform.anchorMax = new Vector2(1f, 1f);
        txtStatSPD.rectTransform.offsetMin = Vector2.zero; txtStatSPD.rectTransform.offsetMax = Vector2.zero;

        // 3 Skill Boxes [A], [S], [W]
        float startY = -92f;
        float boxHeight = 110f;
        float gap = 8f;

        // Skill 1
        var sk1 = BuildSingleSkillBox(panel.transform, "Skill1Box", startY, new Color(1.0f, 0.55f, 0.15f, 1f));
        txtSkill1Title = sk1.title; txtSkill1Desc = sk1.desc;

        // Skill 2
        var sk2 = BuildSingleSkillBox(panel.transform, "Skill2Box", startY - (boxHeight + gap), new Color(1.0f, 0.40f, 0.20f, 1f));
        txtSkill2Title = sk2.title; txtSkill2Desc = sk2.desc;

        // Skill 3 (Ultimate)
        var sk3 = BuildSingleSkillBox(panel.transform, "Skill3Box", startY - (boxHeight + gap) * 2, new Color(1.0f, 0.25f, 0.25f, 1f));
        txtSkill3Title = sk3.title; txtSkill3Desc = sk3.desc;
    }

    private (TMP_Text title, TMP_Text desc) BuildSingleSkillBox(Transform parent, string name, float posY, Color accent)
    {
        GameObject box = new GameObject(name);
        box.transform.SetParent(parent, false);

        RectTransform brt = box.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0f, 1f); brt.anchorMax = new Vector2(1f, 1f);
        brt.pivot     = new Vector2(0.5f, 1f);
        brt.anchoredPosition = new Vector2(0f, posY);
        brt.sizeDelta = new Vector2(-40f, 106f);

        Image img = box.AddComponent<Image>();
        img.color = new Color(0.12f, 0.08f, 0.22f, 0.85f);
        if (btnFantasyNormal != null) { img.sprite = btnFantasyNormal; img.type = Image.Type.Sliced; }

        // Strip Accent Kiri
        GameObject strip = new GameObject("Strip");
        strip.transform.SetParent(box.transform, false);
        RectTransform srt = strip.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 0f); srt.anchorMax = new Vector2(0f, 1f);
        srt.pivot     = new Vector2(0f, 0.5f);
        srt.anchoredPosition = Vector2.zero;
        srt.sizeDelta = new Vector2(6f, 0f);
        strip.AddComponent<Image>().color = accent;

        // Title
        TMP_Text title = CreateTMP(box.transform, "Title", 15.5f, FontStyles.Bold, accent, TextAlignmentOptions.Left);
        title.rectTransform.anchorMin = new Vector2(0f, 1f); title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot     = new Vector2(0f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(18f, -10f);
        title.rectTransform.sizeDelta = new Vector2(-36f, 26f);

        // Description
        TMP_Text desc = CreateTMP(box.transform, "Desc", 12.5f, FontStyles.Normal,
            new Color(0.88f, 0.84f, 0.78f, 1f), TextAlignmentOptions.TopLeft);
        desc.rectTransform.anchorMin = new Vector2(0f, 0f); desc.rectTransform.anchorMax = new Vector2(1f, 1f);
        desc.rectTransform.pivot     = new Vector2(0.5f, 0.5f);
        desc.rectTransform.offsetMin = new Vector2(18f, 8f);
        desc.rectTransform.offsetMax = new Vector2(-18f, -38f);

        return (title, desc);
    }

    private void BuildBottomActionButtons()
    {
        // Tombol Kembali ke Stage Select (Kiri Bawah)
        GameObject btnBackObj = new GameObject("BtnBackToStages");
        btnBackObj.transform.SetParent(canvas.transform, false);
        RectTransform backRT = btnBackObj.AddComponent<RectTransform>();
        backRT.anchorMin = new Vector2(0.5f, 0f); backRT.anchorMax = new Vector2(0.5f, 0f);
        backRT.pivot     = new Vector2(0.5f, 0f);
        backRT.anchoredPosition = new Vector2(-280f, 30f);
        backRT.sizeDelta = new Vector2(280f, 52f);

        Image backImg = btnBackObj.AddComponent<Image>();
        if (btnFantasyNormal != null) { backImg.sprite = btnFantasyNormal; backImg.type = Image.Type.Sliced; }
        backImg.color = Color.white;

        Button backBtn = btnBackObj.AddComponent<Button>();
        backBtn.onClick.AddListener(OnBackToStagesClicked);

        TMP_Text backTxt = CreateTMP(btnBackObj.transform, "TxtBack", 16f, FontStyles.Bold,
            new Color(0.90f, 0.82f, 0.70f, 1f), TextAlignmentOptions.Center);
        backTxt.rectTransform.anchorMin = Vector2.zero; backTxt.rectTransform.anchorMax = Vector2.one;
        backTxt.rectTransform.offsetMin = Vector2.zero; backTxt.rectTransform.offsetMax = Vector2.zero;
        backTxt.text = "←  KEMBALI KE STAGE";

        // Tombol Konfirmasi Mainkan (Kanan Bawah)
        GameObject btnConfirmObj = new GameObject("BtnConfirmPlay");
        btnConfirmObj.transform.SetParent(canvas.transform, false);
        RectTransform confRT = btnConfirmObj.AddComponent<RectTransform>();
        confRT.anchorMin = new Vector2(0.5f, 0f); confRT.anchorMax = new Vector2(0.5f, 0f);
        confRT.pivot     = new Vector2(0.5f, 0f);
        confRT.anchoredPosition = new Vector2(250f, 30f);
        confRT.sizeDelta = new Vector2(360f, 56f);

        Image confImg = btnConfirmObj.AddComponent<Image>();
        if (btnFantasyNormal != null) { confImg.sprite = btnFantasyNormal; confImg.type = Image.Type.Sliced; }
        confImg.color = Color.white;

        btnConfirmPlay = btnConfirmObj.AddComponent<Button>();
        if (btnFantasyHover != null)
        {
            btnConfirmPlay.transition = Selectable.Transition.SpriteSwap;
            SpriteState ss = new SpriteState();
            ss.highlightedSprite = btnFantasyHover;
            ss.pressedSprite     = btnFantasyHover;
            ss.selectedSprite    = btnFantasyHover;
            btnConfirmPlay.spriteState = ss;
        }
        btnConfirmPlay.onClick.AddListener(OnConfirmPlayClicked);

        txtConfirmBtnLabel = CreateTMP(btnConfirmObj.transform, "TxtConfirm", 19f, FontStyles.Bold,
            new Color(1f, 0.92f, 0.40f, 1f), TextAlignmentOptions.Center);
        txtConfirmBtnLabel.characterSpacing = 2f;
        txtConfirmBtnLabel.rectTransform.anchorMin = Vector2.zero; txtConfirmBtnLabel.rectTransform.anchorMax = Vector2.one;
        txtConfirmBtnLabel.rectTransform.offsetMin = Vector2.zero; txtConfirmBtnLabel.rectTransform.offsetMax = Vector2.zero;
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
    // INTERAKSI PEMILIHAN KARAKTER
    // ─────────────────────────────────────────────────────────────────────
    private void SelectCharacter(int idx, bool playSfx)
    {
        if (idx < 0 || idx >= characters.Length) return;
        selectedIndex = idx;

        if (playSfx && audioSource != null && sfxSelect != null)
        {
            audioSource.PlayOneShot(sfxSelect, 0.9f);
        }

        CharacterData data = characters[idx];

        // Update Card Highlights
        for (int i = 0; i < characters.Length; i++)
        {
            bool isCurrent = (i == idx);
            if (cardBorderHighlights[i] != null)
            {
                cardBorderHighlights[i].color = isCurrent
                    ? new Color(1f, 0.85f, 0.35f, 0.90f)
                    : new Color(1f, 0.85f, 0.35f, 0f);
            }

            if (txtCardSelectionBadges[i] != null)
            {
                txtCardSelectionBadges[i].text = isCurrent ? "✔ TERPILIH" : "KLIK UNTUK MEMILIH";
                txtCardSelectionBadges[i].color = isCurrent
                    ? new Color(0.35f, 1f, 0.55f, 1f)
                    : new Color(0.75f, 0.70f, 0.65f, 0.9f);
            }

            if (characterCards[i] != null)
            {
                characterCards[i].GetComponent<RectTransform>().localScale = isCurrent
                    ? new Vector3(1.04f, 1.04f, 1f)
                    : Vector3.one;
            }
        }

        // Update Detail Panel
        if (txtStatHP != null)  txtStatHP.text  = $"HP: {data.statHP}";
        if (txtStatATK != null) txtStatATK.text = $"ATK: {data.statATK}";
        if (txtStatSPD != null) txtStatSPD.text = $"SPD: {data.statSPD}";

        if (txtSkill1Title != null) txtSkill1Title.text = data.skill1Name;
        if (txtSkill1Desc != null)  txtSkill1Desc.text  = data.skill1Desc;

        if (txtSkill2Title != null) txtSkill2Title.text = data.skill2Name;
        if (txtSkill2Desc != null)  txtSkill2Desc.text  = data.skill2Desc;

        if (txtSkill3Title != null) txtSkill3Title.text = data.skill3Name;
        if (txtSkill3Desc != null)  txtSkill3Desc.text  = data.skill3Desc;

        if (txtConfirmBtnLabel != null)
        {
            txtConfirmBtnLabel.text = $"▶  MAINKAN SEBAGAI {data.name}";
        }
    }

    private void OnConfirmPlayClicked()
    {
        CharacterData chosen = characters[selectedIndex];

        // Simpan karakter terpilih ke PlayerPrefs
        PlayerPrefs.SetInt("SelectedCharacterID", selectedIndex);
        PlayerPrefs.SetString("SelectedCharacterName", chosen.name);
        PlayerPrefs.Save();

        if (audioSource != null && sfxClick != null) audioSource.PlayOneShot(sfxClick);

        StartCoroutine(LoadSceneWithFade(targetScene));
    }

    private void OnBackToStagesClicked()
    {
        if (audioSource != null && sfxClick != null) audioSource.PlayOneShot(sfxClick);
        StartCoroutine(LoadSceneWithFade("StageSelect"));
    }

    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        // Simple Fade Out
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
