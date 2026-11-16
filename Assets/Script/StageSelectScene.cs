using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// StageSelectScene — Scene Pemilihan Stage Dark Fantasy BATH:
/// - Terinspirasi dari layout Level Select (Kartu stage, Rating Bintang, Status Gembok/Lock, Navigasi Panah & Titik Halaman).
/// - Dilengkapi Selektor Tingkat Kesulitan 4 Tingkat: EASY, NORMAL, HARD, ABYSS.
/// - Scaling multiplier HP & ATK musuh dan bos diintegrasikan via DifficultyManager.
/// </summary>
public class StageSelectScene : MonoBehaviour
{
    // ── Asset Refs ────────────────────────────────────────────────────────
    private Sprite modalPanelSprite;
    private Sprite btnNormal;
    private Sprite btnHover;
    private Sprite btnPressed;
    private Sprite bgSprite;
    private Sprite fireGlowSprite;
    private Sprite screenVignette;
    private Sprite starGoldSprite;
    private Sprite starEmptySprite;
    private Sprite lockSprite;
    private Sprite arrowLeftSprite;
    private Sprite arrowRightSprite;

    private TMP_FontAsset tmpFont;
    private AudioClip sfxClick;
    private AudioClip sfxHover;
    private AudioClip sfxStar;
    private AudioSource audioSource;

    // ── State ─────────────────────────────────────────────────────────────
    private Canvas canvas;
    private Image bgImage;
    private RectTransform bgRect;
    private float bgBaseX, bgBaseY;
    private Image vigImg;
    private float vigBaseAlpha = 0.65f;

    // Difficulty UI
    private Button[] diffButtons = new Button[4];
    private Image[] diffBtnBorders = new Image[4];
    private TMP_Text[] diffBtnTexts = new TMP_Text[4];
    private TMP_Text txtDifficultyDesc;

    // Kartu Stage
    private readonly string[] stageNames = {
        "THE AWAKENING",
        "LEMBAH RERUNTUHAN",
        "KAWAH MAGMA MEMBARA",
        "TAKHTA KEABADIAN"
    };
    private readonly string[] stageSubNames = {
        "Hutan Terlarang & Demon Slime",
        "Ancient Ruins - Chapter I",
        "Inferno Crater (Segera Hadir)",
        "Eternal Throne (Segera Hadir)"
    };
    private readonly bool[] stageLocked   = { false, false, true, true };
    private readonly int[]  stageBuildIdx = { 3, 2, -1, -1 }; // 3=GamePlay, 2=CharacterSelect

    private RectTransform[] cardRTs;
    private Vector2[] cardBasePos;
    private TMP_Text[] cardDiffBadges;

    private int currentPage = 0;
    private const int totalPages = 2;
    private Image[] pageDots;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        LoadAssets();
        FindCanvas();
        BuildAtmosphere();
        BuildHeaderBanner();
        BuildMainModalPanel();

        // Refresh difficulty selection state
        RefreshDifficultyUI(DifficultyManager.CurrentDifficulty);

        StartCoroutine(IntroAnim());
    }

    private void LoadAssets()
    {
        modalPanelSprite = Resources.Load<Sprite>("UI/panel_fantasy_modal");
        btnNormal        = Resources.Load<Sprite>("UI/btn_fantasy_normal");
        btnHover         = Resources.Load<Sprite>("UI/btn_fantasy_hover");
        btnPressed       = Resources.Load<Sprite>("UI/btn_fantasy_pressed");
        bgSprite         = Resources.Load<Sprite>("UI/menu_background");
        fireGlowSprite   = Resources.Load<Sprite>("UI/fire_glow");
        screenVignette   = Resources.Load<Sprite>("UI/screen_vignette");

        starGoldSprite   = Resources.Load<Sprite>("UI/ui_star_gold");
        starEmptySprite  = Resources.Load<Sprite>("UI/ui_star_empty");
        lockSprite       = Resources.Load<Sprite>("UI/ui_icon_lock");
        arrowLeftSprite  = Resources.Load<Sprite>("UI/ui_arrow_left");
        arrowRightSprite = Resources.Load<Sprite>("UI/ui_arrow_right");

        sfxClick         = Resources.Load<AudioClip>("Audio/ui_click");
        sfxHover         = Resources.Load<AudioClip>("Audio/ui_hover");
        sfxStar          = Resources.Load<AudioClip>("Audio/star_1");

        tmpFont          = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    private void FindCanvas()
    {
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject cgo = new GameObject("StageSelectCanvas");
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

    // ─────────────────────────────────────────────────────────────────────
    // ATMOSPHERE & BACKGROUND
    // ─────────────────────────────────────────────────────────────────────
    private void BuildAtmosphere()
    {
        // Dark Base BG
        GameObject baseBG = new GameObject("BG_Dark");
        baseBG.transform.SetParent(canvas.transform, false);
        baseBG.transform.SetSiblingIndex(0);

        RectTransform brt = baseBG.AddComponent<RectTransform>();
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;

        Image bi = baseBG.AddComponent<Image>();
        bi.color = new Color(0.04f, 0.02f, 0.08f, 1f);

        // Dragon Art Image
        if (bgSprite != null)
        {
            GameObject bgImgGO = new GameObject("BG_Image");
            bgImgGO.transform.SetParent(canvas.transform, false);
            bgImgGO.transform.SetSiblingIndex(1);

            bgRect = bgImgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = new Vector2(-40f, -30f);
            bgRect.offsetMax = new Vector2(40f, 30f);

            bgImage = bgImgGO.AddComponent<Image>();
            bgImage.sprite = bgSprite;
            bgImage.type = Image.Type.Simple;
            bgImage.preserveAspect = false;
            bgImage.color = new Color(1f, 1f, 1f, 0.32f);
            bgImage.raycastTarget = false;

            bgBaseX = bgRect.anchoredPosition.x;
            bgBaseY = bgRect.anchoredPosition.y;
        }

        // Screen Vignette
        if (screenVignette != null)
        {
            GameObject vigGO = new GameObject("Atmosphere_Vignette");
            vigGO.transform.SetParent(canvas.transform, false);
            vigGO.transform.SetSiblingIndex(2);

            RectTransform vrt = vigGO.AddComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;

            vigImg = vigGO.AddComponent<Image>();
            vigImg.sprite = screenVignette;
            vigImg.color  = new Color(1f, 1f, 1f, vigBaseAlpha);
            vigImg.raycastTarget = false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // HEADER BANNER
    // ─────────────────────────────────────────────────────────────────────
    private void BuildHeaderBanner()
    {
        GameObject bannerGO = new GameObject("HeaderBanner");
        bannerGO.transform.SetParent(canvas.transform, false);

        RectTransform brt = bannerGO.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 1f);
        brt.anchorMax = new Vector2(0.5f, 1f);
        brt.pivot     = new Vector2(0.5f, 1f);
        brt.sizeDelta = new Vector2(860f, 62f);
        brt.anchoredPosition = new Vector2(0f, -20f);

        Image bi = bannerGO.AddComponent<Image>();
        bi.color = new Color(0.08f, 0.05f, 0.16f, 0.95f);
        if (modalPanelSprite != null) { bi.sprite = modalPanelSprite; bi.type = Image.Type.Sliced; }

        CreateGoldDivider(bannerGO.transform, 1f, 0f);
        CreateGoldDivider(bannerGO.transform, 0f, 0f);

        TMP_Text title = CreateTMP(bannerGO.transform, "TitleText", 28f, FontStyles.Bold,
            new Color(1f, 0.88f, 0.40f, 1f), TextAlignmentOptions.Center);
        title.rectTransform.anchorMin = Vector2.zero; title.rectTransform.anchorMax = Vector2.one;
        title.rectTransform.offsetMin = Vector2.zero; title.rectTransform.offsetMax = Vector2.zero;
        title.characterSpacing = 4f;
        title.text = "[ PILIH TAHAP PERJALANAN ]";
    }

    // ─────────────────────────────────────────────────────────────────────
    // MAIN MODAL PANEL & LEVEL SELECT LAYOUT (Sesuai Referensi Gambar 4)
    // ─────────────────────────────────────────────────────────────────────
    private void BuildMainModalPanel()
    {
        // Central Modal Frame (Lebar: 1140, Tinggi: 680)
        GameObject panelGO = new GameObject("LevelSelectModal");
        panelGO.transform.SetParent(canvas.transform, false);

        RectTransform panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot     = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(1140f, 680f);
        panelRT.anchoredPosition = new Vector2(0f, -28f);

        Image pImg = panelGO.AddComponent<Image>();
        if (modalPanelSprite != null) { pImg.sprite = modalPanelSprite; pImg.type = Image.Type.Sliced; }
        pImg.color = new Color(1f, 1f, 1f, 0.98f);

        // 1. Difficulty Selector Bar di bagian atas modal
        BuildDifficultySelector(panelGO.transform);

        // 2. 4 Kartu Stage (Grid Horizontal / Carousel)
        int count = stageNames.Length;
        cardRTs = new RectTransform[count];
        cardBasePos = new Vector2[count];
        cardDiffBadges = new TMP_Text[count];

        float spacing = 250f;
        float startX  = -(spacing * (count - 1)) / 2f;

        for (int i = 0; i < count; i++)
        {
            float xPos = startX + i * spacing;
            cardRTs[i] = BuildStageCard(panelGO.transform, i, xPos);
            cardBasePos[i] = cardRTs[i].anchoredPosition;
        }

        // 3. Tombol Panah Navigasi Kiri & Kanan (Sesuai Referensi Gambar 4)
        BuildArrowButton(panelGO.transform, -535f, false);
        BuildArrowButton(panelGO.transform,  535f, true);

        // 4. Titik Halaman Pagination (Sesuai Referensi Gambar 4)
        BuildPaginationDots(panelGO.transform);

        // 5. Tombol KEMBALI KE MENU di Bawah
        BuildBackButton(panelGO.transform);

        // 6. Corner Ornaments
        BuildCornerDiamonds(panelGO.transform);
    }

    // ─────────────────────────────────────────────────────────────────────
    // SELEKTOR TINGKAT KESULITAN (EASY, NORMAL, HARD, ABYSS)
    // ─────────────────────────────────────────────────────────────────────
    private void BuildDifficultySelector(Transform parent)
    {
        GameObject diffBar = new GameObject("DifficultyBar");
        diffBar.transform.SetParent(parent, false);

        RectTransform dRT = diffBar.AddComponent<RectTransform>();
        dRT.anchorMin = new Vector2(0.5f, 1f);
        dRT.anchorMax = new Vector2(0.5f, 1f);
        dRT.pivot     = new Vector2(0.5f, 1f);
        dRT.sizeDelta = new Vector2(900f, 90f);
        dRT.anchoredPosition = new Vector2(0f, -22f);

        // Label Bar
        TMP_Text lbl = CreateTMP(diffBar.transform, "LblDiff", 14f, FontStyles.Bold,
            new Color(0.95f, 0.85f, 0.50f, 1f), TextAlignmentOptions.Center);
        lbl.rectTransform.anchorMin = new Vector2(0f, 1f); lbl.rectTransform.anchorMax = new Vector2(1f, 1f);
        lbl.rectTransform.pivot = new Vector2(0.5f, 1f);
        lbl.rectTransform.anchoredPosition = new Vector2(0f, 0f);
        lbl.rectTransform.sizeDelta = new Vector2(0f, 20f);
        lbl.text = "✦ TINGKAT KESULITAN PETUALANGAN ✦";

        // 4 Tombol Kesulitan: EASY, NORMAL, HARD, ABYSS
        DifficultyLevel[] levels = {
            DifficultyLevel.Easy,
            DifficultyLevel.Normal,
            DifficultyLevel.Hard,
            DifficultyLevel.Abyss
        };

        float btnW = 180f;
        float btnH = 38f;
        float spacing = 205f;
        float startX = -(spacing * 3f) / 2f;

        for (int i = 0; i < 4; i++)
        {
            DifficultyLevel lv = levels[i];
            float xPos = startX + i * spacing;

            GameObject btnGO = new GameObject("BtnDiff_" + lv);
            btnGO.transform.SetParent(diffBar.transform, false);

            RectTransform brt = btnGO.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot     = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(btnW, btnH);
            brt.anchoredPosition = new Vector2(xPos, -2f);

            Image bImg = btnGO.AddComponent<Image>();
            if (btnNormal != null) { bImg.sprite = btnNormal; bImg.type = Image.Type.Sliced; }
            bImg.color = new Color(0.12f, 0.08f, 0.20f, 0.95f);
            diffBtnBorders[i] = bImg;

            Button btn = btnGO.AddComponent<Button>();
            diffButtons[i] = btn;

            // Hover SFX
            AddHoverTrigger(btnGO);

            // Label Teks
            TMP_Text t = CreateTMP(btnGO.transform, "Text", 15f, FontStyles.Bold,
                DifficultyManager.GetDifficultyColor(lv), TextAlignmentOptions.Center);
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = Vector2.zero; t.rectTransform.offsetMax = Vector2.zero;
            t.text = DifficultyManager.GetDifficultyTitle(lv);
            diffBtnTexts[i] = t;

            int captureIndex = i;
            DifficultyLevel captureLevel = lv;
            btn.onClick.AddListener(() => {
                PlayClickSfx();
                SelectDifficulty(captureLevel);
            });
        }

        // Subtitle deskripsi multiplier kesulitan
        GameObject descGO = new GameObject("DifficultyDescription");
        descGO.transform.SetParent(diffBar.transform, false);

        RectTransform descRT = descGO.AddComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0f, 0f);
        descRT.anchorMax = new Vector2(1f, 0f);
        descRT.pivot     = new Vector2(0.5f, 0f);
        descRT.sizeDelta = new Vector2(0f, 22f);
        descRT.anchoredPosition = new Vector2(0f, 2f);

        txtDifficultyDesc = descGO.AddComponent<TextMeshProUGUI>();
        txtDifficultyDesc.font = tmpFont;
        txtDifficultyDesc.fontSize = 13.5f;
        txtDifficultyDesc.fontStyle = FontStyles.Normal;
        txtDifficultyDesc.alignment = TextAlignmentOptions.Center;
        txtDifficultyDesc.color = new Color(0.85f, 0.85f, 0.90f, 0.95f);
        txtDifficultyDesc.text = DifficultyManager.GetDifficultyDesc(DifficultyManager.CurrentDifficulty);
    }

    private void SelectDifficulty(DifficultyLevel level)
    {
        DifficultyManager.CurrentDifficulty = level;
        RefreshDifficultyUI(level);

        if (sfxStar != null && audioSource != null)
            audioSource.PlayOneShot(sfxStar, 0.7f);
    }

    private void RefreshDifficultyUI(DifficultyLevel current)
    {
        DifficultyLevel[] levels = {
            DifficultyLevel.Easy,
            DifficultyLevel.Normal,
            DifficultyLevel.Hard,
            DifficultyLevel.Abyss
        };

        for (int i = 0; i < 4; i++)
        {
            bool isSelected = (levels[i] == current);
            Color themeColor = DifficultyManager.GetDifficultyColor(levels[i]);

            if (diffBtnBorders[i] != null)
            {
                if (isSelected)
                {
                    diffBtnBorders[i].color = new Color(themeColor.r * 1.2f, themeColor.g * 1.2f, themeColor.b * 1.2f, 1f);
                    diffButtons[i].transform.localScale = new Vector3(1.06f, 1.06f, 1f);
                }
                else
                {
                    diffBtnBorders[i].color = new Color(0.18f, 0.14f, 0.28f, 0.75f);
                    diffButtons[i].transform.localScale = Vector3.one;
                }
            }

            if (diffBtnTexts[i] != null)
            {
                diffBtnTexts[i].color = isSelected ? Color.white : new Color(themeColor.r, themeColor.g, themeColor.b, 0.60f);
            }
        }

        if (txtDifficultyDesc != null)
        {
            txtDifficultyDesc.text = "✦ " + DifficultyManager.GetDifficultyDesc(current) + " ✦";
            txtDifficultyDesc.color = DifficultyManager.GetDifficultyColor(current);
        }

        // Update badge kesulitan di kartu yang tidak terkunci
        if (cardDiffBadges != null)
        {
            for (int i = 0; i < cardDiffBadges.Length; i++)
            {
                if (cardDiffBadges[i] != null && !stageLocked[i])
                {
                    cardDiffBadges[i].text = "[ " + DifficultyManager.GetDifficultyTitle(current) + " ]";
                    cardDiffBadges[i].color = DifficultyManager.GetDifficultyColor(current);
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // KARTU STAGE (Sesuai Referensi Gambar 4: Bintang, Gembok, Label Stage)
    // ─────────────────────────────────────────────────────────────────────
    private RectTransform BuildStageCard(Transform parent, int idx, float xPos)
    {
        bool locked = stageLocked[idx];

        GameObject card = new GameObject("Card_Stage_" + idx);
        card.transform.SetParent(parent, false);

        RectTransform crt = card.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot     = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(220f, 370f);
        crt.anchoredPosition = new Vector2(xPos, -20f);

        Image cardImg = card.AddComponent<Image>();
        if (modalPanelSprite != null) { cardImg.sprite = modalPanelSprite; cardImg.type = Image.Type.Sliced; }

        if (locked)
            cardImg.color = new Color(0.08f, 0.06f, 0.14f, 0.88f);
        else
            cardImg.color = new Color(0.11f, 0.08f, 0.22f, 0.98f);

        // Accent strip atas
        Color[] accentColors = {
            new Color(0.20f, 0.85f, 0.45f, 1f), // Hijau Tutorial
            new Color(0.25f, 0.70f, 1.00f, 1f), // Biru Stage 1
            new Color(0.55f, 0.50f, 0.65f, 0.5f),// Abu Locked
            new Color(0.55f, 0.50f, 0.65f, 0.5f)
        };
        Color accent = accentColors[idx];

        GameObject strip = new GameObject("TopStrip");
        strip.transform.SetParent(card.transform, false);
        RectTransform srt = strip.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 1f); srt.anchorMax = new Vector2(1f, 1f);
        srt.pivot     = new Vector2(0.5f, 1f);
        srt.sizeDelta = new Vector2(0f, 6f);
        srt.anchoredPosition = Vector2.zero;
        strip.AddComponent<Image>().color = accent;

        // 1. Stage Number Badge
        TMP_Text txtNum = CreateTMP(card.transform, "StageNum", 26f, FontStyles.Bold,
            locked ? new Color(0.55f, 0.55f, 0.60f, 0.7f) : accent,
            TextAlignmentOptions.Center);
        txtNum.rectTransform.anchorMin = new Vector2(0f, 1f); txtNum.rectTransform.anchorMax = new Vector2(1f, 1f);
        txtNum.rectTransform.pivot = new Vector2(0.5f, 1f);
        txtNum.rectTransform.anchoredPosition = new Vector2(0f, -14f);
        txtNum.rectTransform.sizeDelta = new Vector2(0f, 32f);
        txtNum.text = (idx == 0) ? "STAGE 0" : "STAGE " + idx;

        // 2. Stage Name
        TMP_Text txtName = CreateTMP(card.transform, "StageName", 15f, FontStyles.Bold,
            locked ? new Color(0.55f, 0.55f, 0.55f, 0.8f) : new Color(1f, 0.94f, 0.78f, 1f),
            TextAlignmentOptions.Center);
        txtName.rectTransform.anchorMin = new Vector2(0.05f, 1f); txtName.rectTransform.anchorMax = new Vector2(0.95f, 1f);
        txtName.rectTransform.pivot = new Vector2(0.5f, 1f);
        txtName.rectTransform.anchoredPosition = new Vector2(0f, -50f);
        txtName.rectTransform.sizeDelta = new Vector2(0f, 40f);
        txtName.text = stageNames[idx];

        // 3. Stage Subname / Chapter Lore
        TMP_Text txtSub = CreateTMP(card.transform, "StageSub", 11.5f, FontStyles.Normal,
            locked ? new Color(0.45f, 0.45f, 0.50f, 0.65f) : new Color(0.80f, 0.76f, 0.72f, 0.95f),
            TextAlignmentOptions.Center);
        txtSub.rectTransform.anchorMin = new Vector2(0.05f, 1f); txtSub.rectTransform.anchorMax = new Vector2(0.95f, 1f);
        txtSub.rectTransform.pivot = new Vector2(0.5f, 1f);
        txtSub.rectTransform.anchoredPosition = new Vector2(0f, -92f);
        txtSub.rectTransform.sizeDelta = new Vector2(0f, 32f);
        txtSub.text = stageSubNames[idx];

        // 4. Area Bintang atau Gembok (Sesuai Referensi Gambar 4)
        GameObject starArea = new GameObject("StarOrLockArea");
        starArea.transform.SetParent(card.transform, false);

        RectTransform saRT = starArea.AddComponent<RectTransform>();
        saRT.anchorMin = new Vector2(0.5f, 0.5f);
        saRT.anchorMax = new Vector2(0.5f, 0.5f);
        saRT.pivot     = new Vector2(0.5f, 0.5f);
        saRT.sizeDelta = new Vector2(160f, 50f);
        saRT.anchoredPosition = new Vector2(0f, 20f);

        if (!locked)
        {
            // 3 Bintang Emas Berjajar (Sesuai Referensi Gambar 4)
            float starSize = 34f;
            float[] starX = { -42f, 0f, 42f };
            for (int s = 0; s < 3; s++)
            {
                GameObject starGO = new GameObject("Star_" + s);
                starGO.transform.SetParent(starArea.transform, false);

                RectTransform srt2 = starGO.AddComponent<RectTransform>();
                srt2.anchorMin = new Vector2(0.5f, 0.5f);
                srt2.anchorMax = new Vector2(0.5f, 0.5f);
                srt2.pivot     = new Vector2(0.5f, 0.5f);
                srt2.sizeDelta = new Vector2(starSize, starSize);
                srt2.anchoredPosition = new Vector2(starX[s], 0f);

                Image simg = starGO.AddComponent<Image>();
                simg.sprite = starGoldSprite != null ? starGoldSprite : btnNormal;
                simg.color  = Color.white;
            }
        }
        else
        {
            // Gembok Padlock & 3 Bintang Abu Kosong (Sesuai Referensi Gambar 4)
            GameObject lockGO = new GameObject("LockIcon");
            lockGO.transform.SetParent(starArea.transform, false);

            RectTransform lrt = lockGO.AddComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.5f, 0.5f);
            lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.pivot     = new Vector2(0.5f, 0.5f);
            lrt.sizeDelta = new Vector2(44f, 44f);
            lrt.anchoredPosition = new Vector2(0f, 6f);

            Image limg = lockGO.AddComponent<Image>();
            limg.sprite = lockSprite != null ? lockSprite : btnNormal;
            limg.color  = new Color(0.85f, 0.85f, 0.90f, 0.85f);

            // 3 Bintang Kosong redup di bawah gembok
            float starSize = 22f;
            float[] starX = { -32f, 0f, 32f };
            for (int s = 0; s < 3; s++)
            {
                GameObject starGO = new GameObject("EmptyStar_" + s);
                starGO.transform.SetParent(starArea.transform, false);

                RectTransform srt2 = starGO.AddComponent<RectTransform>();
                srt2.anchorMin = new Vector2(0.5f, 0.5f);
                srt2.anchorMax = new Vector2(0.5f, 0.5f);
                srt2.pivot     = new Vector2(0.5f, 0.5f);
                srt2.sizeDelta = new Vector2(starSize, starSize);
                srt2.anchoredPosition = new Vector2(starX[s], -22f);

                Image simg = starGO.AddComponent<Image>();
                simg.sprite = starEmptySprite != null ? starEmptySprite : btnNormal;
                simg.color  = new Color(1f, 1f, 1f, 0.55f);
            }
        }

        // 5. Badge Tingkat Kesulitan / Status
        TMP_Text badge = CreateTMP(card.transform, "DiffBadge", 12f, FontStyles.Bold,
            locked ? new Color(0.55f, 0.55f, 0.55f, 0.70f) : DifficultyManager.GetDifficultyColor(DifficultyManager.CurrentDifficulty),
            TextAlignmentOptions.Center);
        badge.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        badge.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        badge.rectTransform.pivot     = new Vector2(0.5f, 0.5f);
        badge.rectTransform.sizeDelta = new Vector2(0f, 22f);
        badge.rectTransform.anchoredPosition = new Vector2(0f, -42f);
        badge.text = locked ? "[ TERKUNCI ]" : "[ " + DifficultyManager.GetDifficultyTitle(DifficultyManager.CurrentDifficulty) + " ]";
        cardDiffBadges[idx] = badge;

        // 6. Tombol MAINKAN atau Label SEGERA HADIR
        if (!locked)
        {
            int captureIdx = idx;
            Button btn = BuildCardActionBtn(card.transform, "BtnMainkan", "MAINKAN",
                170f, 46f, 0f, -125f, accent);
            btn.onClick.AddListener(() => OnStageClicked(captureIdx));
        }
        else
        {
            TMP_Text txtLock = CreateTMP(card.transform, "TxtLocked", 14f, FontStyles.Normal,
                new Color(0.55f, 0.55f, 0.60f, 0.75f), TextAlignmentOptions.Center);
            txtLock.rectTransform.anchorMin = new Vector2(0f, 0f);
            txtLock.rectTransform.anchorMax = new Vector2(1f, 0f);
            txtLock.rectTransform.pivot     = new Vector2(0.5f, 0f);
            txtLock.rectTransform.sizeDelta = new Vector2(0f, 44f);
            txtLock.rectTransform.anchoredPosition = new Vector2(0f, 25f);
            txtLock.text = "SEGERA\nHADIR";
        }

        // Hover scale card
        AddCardHover(card);

        return crt;
    }

    // ─────────────────────────────────────────────────────────────────────
    // TOMBOL PANAH CAROUSEL KIRI & KANAN (Sesuai Referensi Gambar 4)
    // ─────────────────────────────────────────────────────────────────────
    private void BuildArrowButton(Transform parent, float xPos, bool isRight)
    {
        GameObject arrowGO = new GameObject(isRight ? "BtnArrowRight" : "BtnArrowLeft");
        arrowGO.transform.SetParent(parent, false);

        RectTransform art = arrowGO.AddComponent<RectTransform>();
        art.anchorMin = new Vector2(0.5f, 0.5f);
        art.anchorMax = new Vector2(0.5f, 0.5f);
        art.pivot     = new Vector2(0.5f, 0.5f);
        art.sizeDelta = new Vector2(58f, 58f);
        art.anchoredPosition = new Vector2(xPos, -20f);

        Image aImg = arrowGO.AddComponent<Image>();
        Sprite arrowSprite = isRight ? arrowRightSprite : arrowLeftSprite;
        if (arrowSprite != null)
        {
            aImg.sprite = arrowSprite;
            aImg.color = Color.white;
        }
        else
        {
            aImg.color = new Color(0.95f, 0.75f, 0.25f, 0.90f);
        }

        Button btn = arrowGO.AddComponent<Button>();
        btn.onClick.AddListener(() => {
            PlayClickSfx();
            SwitchPage(isRight);
        });

        AddHoverTrigger(arrowGO);
    }

    private void SwitchPage(bool forward)
    {
        currentPage = forward ? (currentPage + 1) % totalPages : (currentPage - 1 + totalPages) % totalPages;
        UpdatePaginationDots();

        // Animasi feedback bounce pada kartu
        StartCoroutine(PageSwitchAnim(forward));
    }

    private IEnumerator PageSwitchAnim(bool forward)
    {
        float shift = forward ? -15f : 15f;
        foreach (var rt in cardRTs)
        {
            if (rt != null) rt.anchoredPosition += new Vector2(shift, 0f);
        }
        yield return new WaitForSeconds(0.04f);
        for (int i = 0; i < cardRTs.Length; i++)
        {
            if (cardRTs[i] != null) cardRTs[i].anchoredPosition = cardBasePos[i];
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // TITIK HALAMAN PAGINATION (Sesuai Referensi Gambar 4)
    // ─────────────────────────────────────────────────────────────────────
    private void BuildPaginationDots(Transform parent)
    {
        GameObject dotsGO = new GameObject("PaginationDots");
        dotsGO.transform.SetParent(parent, false);

        RectTransform drt = dotsGO.AddComponent<RectTransform>();
        drt.anchorMin = new Vector2(0.5f, 0f);
        drt.anchorMax = new Vector2(0.5f, 0f);
        drt.pivot     = new Vector2(0.5f, 0f);
        drt.sizeDelta = new Vector2(100f, 20f);
        drt.anchoredPosition = new Vector2(0f, 85f);

        pageDots = new Image[totalPages];
        float[] dotX = { -14f, 14f };

        for (int i = 0; i < totalPages; i++)
        {
            GameObject dot = new GameObject("Dot_" + i);
            dot.transform.SetParent(dotsGO.transform, false);

            RectTransform rt = dot.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(12f, 12f);
            rt.anchoredPosition = new Vector2(dotX[i], 0f);

            Image img = dot.AddComponent<Image>();
            pageDots[i] = img;
        }

        UpdatePaginationDots();
    }

    private void UpdatePaginationDots()
    {
        if (pageDots == null) return;
        for (int i = 0; i < pageDots.Length; i++)
        {
            if (pageDots[i] != null)
            {
                pageDots[i].color = (i == currentPage)
                    ? new Color(0.98f, 0.78f, 0.25f, 1f)
                    : new Color(0.35f, 0.30f, 0.45f, 0.65f);
                pageDots[i].transform.localScale = (i == currentPage)
                    ? new Vector3(1.25f, 1.25f, 1f)
                    : Vector3.one;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // TOMBOL KEMBALI KE MENU
    // ─────────────────────────────────────────────────────────────────────
    private void BuildBackButton(Transform parent)
    {
        GameObject go = new GameObject("BtnBackToMenu");
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(280f, 48f);
        rt.anchoredPosition = new Vector2(0f, 22f);

        Image img = go.AddComponent<Image>();
        if (btnNormal != null) { img.sprite = btnNormal; img.type = Image.Type.Sliced; }
        img.color = Color.white;

        Button btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() => {
            PlayClickSfx();
            StartCoroutine(LoadSceneByNameRoutine("Main Menu"));
        });

        AddHoverTrigger(go);

        TMP_Text t = CreateTMP(go.transform, "Label", 18f, FontStyles.Bold,
            new Color(1f, 0.92f, 0.70f, 1f), TextAlignmentOptions.Center);
        t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.offsetMin = Vector2.zero; t.rectTransform.offsetMax = Vector2.zero;
        t.characterSpacing = 2f;
        t.text = "[ ◄ KEMBALI KE MENU ]";
    }

    private void BuildCornerDiamonds(Transform parent)
    {
        float[] dx = { -545f,  545f, -545f,  545f };
        float[] dy = {  315f,  315f, -315f, -315f };
        for (int i = 0; i < 4; i++)
        {
            GameObject dia = new GameObject("Diamond_" + i);
            dia.transform.SetParent(parent, false);

            RectTransform drt = dia.AddComponent<RectTransform>();
            drt.anchorMin = new Vector2(0.5f, 0.5f);
            drt.anchorMax = new Vector2(0.5f, 0.5f);
            drt.pivot     = new Vector2(0.5f, 0.5f);
            drt.sizeDelta = new Vector2(18f, 18f);
            drt.anchoredPosition = new Vector2(dx[i], dy[i]);
            drt.localEulerAngles = new Vector3(0f, 0f, 45f);

            Image img = dia.AddComponent<Image>();
            img.color = new Color(0.98f, 0.78f, 0.30f, 0.90f);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // INTERAKSI KLIK STAGE & NAVIGASI
    // ─────────────────────────────────────────────────────────────────────
    private void OnStageClicked(int idx)
    {
        PlayClickSfx();
        if (idx == 0)
        {
            // Stage 0: Langsung ke GamePlay Tutorial
            StartCoroutine(LoadSceneByNameRoutine("GamePlay"));
        }
        else
        {
            // Stage 1 dan seterusnya: Masuk ke Scene Pemilihan Karakter
            PlayerPrefs.SetInt("TargetStageIndex", idx);
            PlayerPrefs.SetString("TargetStageName", stageNames[idx]);
            PlayerPrefs.SetString("TargetSceneName", "Next");
            PlayerPrefs.Save();

            StartCoroutine(LoadSceneByNameRoutine("CharacterSelect"));
        }
    }

    private IEnumerator LoadSceneByNameRoutine(string sceneName)
    {
        yield return StartCoroutine(FadeOut());
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeOut()
    {
        GameObject fadeGO = new GameObject("FadeOut");
        fadeGO.transform.SetParent(canvas.transform, false);
        fadeGO.transform.SetAsLastSibling();

        RectTransform frt = fadeGO.AddComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;

        Image fi = fadeGO.AddComponent<Image>();
        fi.color = new Color(0f, 0f, 0f, 0f);

        float dur = 0.30f, elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            fi.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 1f, elapsed / dur));
            yield return null;
        }
    }

    private IEnumerator IntroAnim()
    {
        if (cardRTs == null) yield break;

        foreach (var rt in cardRTs)
            if (rt != null) rt.anchoredPosition += new Vector2(0f, -40f);

        yield return new WaitForSeconds(0.04f);

        for (int i = 0; i < cardRTs.Length; i++)
        {
            if (cardRTs[i] == null) continue;
            Vector2 target = cardBasePos[i];
            Vector2 start = cardRTs[i].anchoredPosition;
            float dur = 0.22f, elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / dur);
                cardRTs[i].anchoredPosition = Vector2.Lerp(start, target, t);
                yield return null;
            }
            cardRTs[i].anchoredPosition = target;
        }
    }

    private void Update()
    {
        // Parallax background
        if (bgRect != null && bgImage != null)
        {
            float t = Time.time;
            float mx = (Input.mousePosition.x / Screen.width  - 0.5f) * -22f;
            float my = (Input.mousePosition.y / Screen.height - 0.5f) * -12f;
            bgRect.anchoredPosition = Vector2.Lerp(
                bgRect.anchoredPosition,
                new Vector2(bgBaseX + mx, bgBaseY + my),
                Time.deltaTime * 3.5f);

            float breathe = Mathf.Sin(t * 0.45f) * 0.5f + 0.5f;
            bgImage.color = new Color(1f, 1f, 1f, 0.28f + breathe * 0.08f);
        }

        if (vigImg != null)
        {
            float pulse = Mathf.Sin(Time.time * 0.5f) * 0.5f + 0.5f;
            vigImg.color = new Color(1f, 1f, 1f, vigBaseAlpha - pulse * 0.06f);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // HELPERS & UI COMPONENTS
    // ─────────────────────────────────────────────────────────────────────
    private TMP_Text CreateTMP(Transform parent, string name, float size, FontStyles style,
                              Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        TMP_Text t = go.AddComponent<TextMeshProUGUI>();
        t.font = tmpFont;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.enableWordWrapping = true;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    private Button BuildCardActionBtn(Transform parent, string name, string label,
                                     float w, float h, float ax, float ay, Color accent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(ax, ay);

        Image img = go.AddComponent<Image>();
        if (btnNormal != null) { img.sprite = btnNormal; img.type = Image.Type.Sliced; img.color = Color.white; }
        else img.color = new Color(0.12f, 0.08f, 0.22f, 0.95f);

        Button btn = go.AddComponent<Button>();
        if (btnHover != null && btnPressed != null)
        {
            btn.transition = Selectable.Transition.SpriteSwap;
            SpriteState ss = new SpriteState();
            ss.highlightedSprite = btnHover;
            ss.pressedSprite     = btnPressed;
            ss.selectedSprite    = btnHover;
            btn.spriteState      = ss;
        }

        AddHoverTrigger(go);

        TMP_Text t = CreateTMP(go.transform, "Label", 18f, FontStyles.Bold,
            new Color(1f, 0.94f, 0.80f, 1f), TextAlignmentOptions.Center);
        t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.offsetMin = Vector2.zero; t.rectTransform.offsetMax = Vector2.zero;
        t.characterSpacing = 2f;
        t.text = label;

        return btn;
    }

    private void CreateGoldDivider(Transform parent, float anchorY, float posY)
    {
        GameObject line = new GameObject("GoldDivider");
        line.transform.SetParent(parent, false);

        RectTransform rt = line.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, anchorY);
        rt.anchorMax = new Vector2(1f, anchorY);
        rt.pivot     = new Vector2(0.5f, anchorY);
        rt.sizeDelta = new Vector2(0f, 2.5f);
        rt.anchoredPosition = new Vector2(0f, posY);

        Image img = line.AddComponent<Image>();
        img.color = new Color(0.98f, 0.78f, 0.30f, 0.85f);
    }

    private void AddHoverTrigger(GameObject target)
    {
        var ev = target.GetComponent<EventTrigger>();
        if (ev == null) ev = target.AddComponent<EventTrigger>();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => {
            PlayHoverSfx();
            target.transform.localScale = new Vector3(1.05f, 1.05f, 1f);
        });
        ev.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => {
            target.transform.localScale = Vector3.one;
        });
        ev.triggers.Add(exit);
    }

    private void AddCardHover(GameObject card)
    {
        var ev = card.AddComponent<EventTrigger>();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => {
            PlayHoverSfx();
            card.GetComponent<RectTransform>().localScale = new Vector3(1.03f, 1.03f, 1f);
        });
        ev.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => {
            card.GetComponent<RectTransform>().localScale = Vector3.one;
        });
        ev.triggers.Add(exit);
    }

    private void PlayClickSfx()
    {
        if (sfxClick != null && audioSource != null)
            audioSource.PlayOneShot(sfxClick);
    }

    private void PlayHoverSfx()
    {
        if (sfxHover != null && audioSource != null)
            audioSource.PlayOneShot(sfxHover, 0.5f);
    }
}
