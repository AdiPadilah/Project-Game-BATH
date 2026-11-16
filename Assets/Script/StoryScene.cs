using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// StoryScene — Cutscene Latar Belakang Karakter bergaya Manhwa Sinematik.
/// Dipicu otomatis setelah Tutorial selesai (Kematian Boss Slime King di GamePlay)
/// atau dapat dibaca kapan saja melalui menu Pemilihan Karakter.
/// 
/// Mengisahkan masa lalu tragis, alasan bertahan hidup, dan sumpah/ambisi ksatria.
/// Dilengkapi potret ekspresif, efek typewriter, navigasi panel, dan transisi ke Stage 1 (Penyelamatan).
/// </summary>
public class StoryScene : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        Scene s = SceneManager.GetActiveScene();
        if (s.name == "StoryScene")
        {
            if (FindFirstObjectByType<StoryScene>() == null)
            {
                new GameObject("StoryController").AddComponent<StoryScene>();
            }
        }

        SceneManager.sceneLoaded += (scene, mode) =>
        {
            if (scene.name == "StoryScene")
            {
                if (FindFirstObjectByType<StoryScene>() == null)
                    new GameObject("StoryController").AddComponent<StoryScene>();
            }
        };
    }

    // ── Asset Refs ────────────────────────────────────────────────────────
    private Sprite modalPanelSprite;
    private Sprite btnFantasyNormal;
    private Sprite btnFantasyHover;
    private Sprite screenVignette;
    private Sprite portraitKaelen;
    private Sprite portraitValeria;
    private TMP_FontAsset tmpFont;
    private AudioClip sfxClick;
    private AudioClip sfxPage;
    private AudioSource audioSource;

    // ── UI Root ───────────────────────────────────────────────────────────
    private Canvas canvas;
    private Image fadeOverlay;
    private RectTransform fadeRT;

    // Current panel UI
    private GameObject panelRoot;
    private TMP_Text txtNarrative;
    private TMP_Text txtSpeaker;
    private TMP_Text txtPageNum;
    private Image imgBackground;
    private Image leftVignette;
    private Image imgHeroPortrait;
    private RectTransform heroPortraitRT;
    private TMP_Text txtHeroTitleBadge;
    private TMP_Text txtChapterQuote;
    private Button btnNext;
    private TMP_Text btnNextLabel;

    // ── State ─────────────────────────────────────────────────────────────
    private List<StoryPanel> panels = new List<StoryPanel>();
    private int currentPanel = 0;
    private bool isTransitioning = false;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private string currentHeroName = "Kaelen";

    // ── Story Panel Data ──────────────────────────────────────────────────
    private class StoryPanel
    {
        public string speaker;        // Nama pembicara / narasi
        public string chapterQuote;   // Kutipan dramatis ala manhwa di atas
        public string text;           // Isi narasi/dialog
        public Color textColor;       // Warna teks narasi
        public Color bgColor;         // Warna latar panel
        public Color vignetteColor;   // Warna vignette
        public string btnLabel;       // Label tombol next
        public bool isFullNarration;  // True = narasi penuh (bukan dialog)
        public string mood;           // "sad", "rage", "resolve", "normal"
    }

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        currentHeroName = PlayerPrefs.GetString("SelectedCharacterName", "Kaelen");
        LoadAssets();
        BuildStoryPanels();
    }

    private void LoadAssets()
    {
        modalPanelSprite = Resources.Load<Sprite>("UI/panel_fantasy_modal");
        btnFantasyNormal = Resources.Load<Sprite>("UI/btn_fantasy_normal");
        btnFantasyHover  = Resources.Load<Sprite>("UI/btn_fantasy_hover");
        screenVignette   = Resources.Load<Sprite>("UI/screen_vignette");
        portraitKaelen   = Resources.Load<Sprite>("UI/hud_player_portrait");
        portraitValeria  = Resources.Load<Sprite>("UI/hud_valeria_portrait");

        sfxClick         = Resources.Load<AudioClip>("Audio/ui_click");
        sfxPage          = Resources.Load<AudioClip>("Audio/ui_hover");
        tmpFont          = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    private void BuildStoryPanels()
    {
        bool isKaelen = (currentHeroName != "Valeria");

        if (isKaelen)
            BuildKaelenStory();
        else
            BuildValeriaStory();
    }

    private void BuildKaelenStory()
    {
        // ── KAELEN: THE SHADOW BLADE ──────────────────────────────────────
        panels.Add(new StoryPanel {
            speaker       = "[ PROLOG: HARI KEHANCURAN ]",
            chapterQuote  = "\"Dunia runtuh bukan dengan jeritan... melainkan dengan kobaran api yang membakar habis semua kenangan.\"",
            text          = "10 TAHUN LALU — DI BALIK KABUT GUNUNG ASHENVALE\n\nKaelen lahir sebagai anak seorang pandai besi biasa. Dia tidak mewarisi darah ksatria bangsawan, melainkan sebilah palu tempa dan mimpi sederhana: melihat laut lepas bersama adik perempuannya, <color=#FFD6A5>Elora</color>.",
            textColor     = new Color(1f, 0.95f, 0.88f, 1f),
            bgColor       = new Color(0.06f, 0.04f, 0.10f, 1f),
            vignetteColor = new Color(0.12f, 0.02f, 0.05f, 0.65f),
            btnLabel      = "▶  Lanjutkan",
            isFullNarration = true,
            mood          = "normal"
        });

        panels.Add(new StoryPanel {
            speaker       = "TRAGEDI DESA ASHENVALE",
            chapterQuote  = "\"Malam dimana langit terbelah dan tanah memuntahkan jutaan taring neraka.\"",
            text          = "Namun takdir kejam tidak memberinya waktu.\n\nGerbang Abyssal runtuh terbuka tepat di atas desa mereka. Langit berubah menjadi merah darah. Makhluk-makhluk kegelapan membakar setiap rumah dan membantai setiap nyawa.\n\nAyah Kaelen gugur tertembus tombak iblis saat menahan pintu. Ibunya hilang ditelan api.",
            textColor     = new Color(1f, 0.80f, 0.80f, 1f),
            bgColor       = new Color(0.14f, 0.02f, 0.03f, 1f),
            vignetteColor = new Color(0.65f, 0.05f, 0.05f, 0.80f),
            btnLabel      = "▶  Teruskan...",
            isFullNarration = true,
            mood          = "sad"
        });

        panels.Add(new StoryPanel {
            speaker       = "KAELEN (MASA LALU)",
            chapterQuote  = "\"Genggaman tanganku terlepas... dan sejak saat itu, nafasku hanyalah rasa bersalah.\"",
            text          = "\"ELORA! PEGANG TANGANKU! JANGAN LEPASKAN!\"\n\n\"Kakak... larilah... hidup... lah...\"\n\nSosok iblis bertanduk raksasa merenggut Elora ke dalam kegelapan portal. Kaelen terpental dengan pedang tumpul patah di tangannya. Menangis darah di atas abu keluarganya.",
            textColor     = new Color(1f, 0.70f, 0.70f, 1f),
            bgColor       = new Color(0.12f, 0.02f, 0.04f, 1f),
            vignetteColor = new Color(0.55f, 0.0f, 0.0f, 0.85f),
            btnLabel      = "▶  Teruskan...",
            isFullNarration = false,
            mood          = "sad"
        });

        panels.Add(new StoryPanel {
            speaker       = "PENGKHIANATAN & KEPUTUSASAAN",
            chapterQuote  = "\"Mereka yang bersumpah melindungi rakyat... hanyalah pengecut bertopeng emas.\"",
            text          = "Dengan tubuh bersimbah darah, Kaelen merangkak ke markas <color=#FFD6A5>Ordo Ksatria Suci Kerajaan</color> memohon pertolongan.\n\nTetapi para ksatria agung menolak melangkah ke zona terlarang.\n\n\"Lembah itu terkutuk. Gadis kecil itu sudah mati. Menyerahlah, bocah abu.\"",
            textColor     = new Color(0.95f, 0.80f, 0.65f, 1f),
            bgColor       = new Color(0.09f, 0.05f, 0.04f, 1f),
            vignetteColor = new Color(0.40f, 0.15f, 0.02f, 0.70f),
            btnLabel      = "▶  Teruskan...",
            isFullNarration = true,
            mood          = "rage"
        });

        panels.Add(new StoryPanel {
            speaker       = "SUMPAH SANG KSATRIA BAYANGAN",
            chapterQuote  = "\"Jika dewa dan ksatria suci berpaling... maka aku akan memeluk kegelapan demi menyelamatkannya!\"",
            text          = "Di malam dingin tanpa bintang itu, Kaelen menancapkan pedang berkarat ke tanah dan menelan pecahan <color=#8060FF>Batu Inti Bayangan Terlarang</color>.\n\n\"Kelemahan adalah dosa terbesar di dunia yang telah binasa ini.\"\n\n\"Aku bersumpah... aku akan menjadi lebih kuat dari siapapun. Aku akan membelah setiap monster di dunia ini hingga Elora kembali!\"",
            textColor     = new Color(1f, 0.70f, 0.35f, 1f),
            bgColor       = new Color(0.06f, 0.03f, 0.12f, 1f),
            vignetteColor = new Color(0.40f, 0.05f, 0.50f, 0.85f),
            btnLabel      = "▶  Menuju Medan Tempur",
            isFullNarration = false,
            mood          = "resolve"
        });

        panels.Add(new StoryPanel {
            speaker       = "PETUNJUK DI LEMBAH RERUNTUHAN",
            chapterQuote  = "\"Secercah cahaya di ujung keputusasaan... seorang penyintas memegang kuncinya.\"",
            text          = "10 TAHUN BERLALU — SEUSAI MENAKLUKKAN HUTAN TERLARANG\n\nKaelen menemukan kabar angin: Seorang tawanan penyintas dari kuil kuno berhasil melihat tahanan yang dibawa ke kedalaman <color=#70D0FF>Lembah Reruntuhan Kuno</color>!\n\nOrang itu disekap oleh regu algojo iblis. Kaelen harus menyelamatkannya sebelum rahasia lokasi adiknya terkubur selamanya!",
            textColor     = new Color(0.90f, 0.95f, 1.0f, 1f),
            bgColor       = new Color(0.04f, 0.06f, 0.14f, 1f),
            vignetteColor = new Color(0.05f, 0.20f, 0.45f, 0.75f),
            btnLabel      = "⚔  Mulai Penyelamatan Stage 1",
            isFullNarration = true,
            mood          = "resolve"
        });
    }

    private void BuildValeriaStory()
    {
        // ── VALERIA: THE CRIMSON VALKYRIE ─────────────────────────────────
        panels.Add(new StoryPanel {
            speaker       = "[ PROLOG: MAWAR CRIMSON ]",
            chapterQuote  = "\"Mawar yang tumbuh di atas genangan darah tidak pernah mengenal arti kehangatan mentari.\"",
            text          = "15 TAHUN LALU — DI WILAYAH PERBATASAN TIMUR\n\nValeria terlahir dengan mata crimson yang menyala. Di masa kecilnya, keluarganya adalah tabib pengelana yang dicintai rakyat jelata karena menyembuhkan wabah tanpa pamrih.\n\nKakak lelakinya, <color=#FF8080>Lian</color>, adalah pelindung terbaik yang selalu menggenggam tangannya saat malam tiba.",
            textColor     = new Color(1f, 0.90f, 0.85f, 1f),
            bgColor       = new Color(0.08f, 0.03f, 0.08f, 1f),
            vignetteColor = new Color(0.20f, 0.02f, 0.10f, 0.65f),
            btnLabel      = "▶  Lanjutkan",
            isFullNarration = true,
            mood          = "normal"
        });

        panels.Add(new StoryPanel {
            speaker       = "MALAM PENYERGAPAN",
            chapterQuote  = "\"Keserakahan manusia lebih berbisa daripada racun iblis manapun.\"",
            text          = "Sebuah ordo pembunuh bayaran gelap menginginkan pusaka <color=#FF6060>Elixir Darah Murni</color> warisan klan mereka.\n\nDi tengah malam badai salju, rumah mereka diserang. Ayah dan ibu tewas membeku di depan pintu. Lian menarik Valeria kecil ke dalam lemari sembunyi dan menguncinya dari luar.\n\n\"Valeria... jangan buat suara apapun. Hiduplah. Untuk kakak.\"",
            textColor     = new Color(1f, 0.75f, 0.75f, 1f),
            bgColor       = new Color(0.15f, 0.02f, 0.04f, 1f),
            vignetteColor = new Color(0.65f, 0.05f, 0.05f, 0.85f),
            btnLabel      = "▶  Teruskan...",
            isFullNarration = true,
            mood          = "sad"
        });

        panels.Add(new StoryPanel {
            speaker       = "VALERIA (MASA LALU)",
            chapterQuote  = "\"Air mata yang membeku menjadi racun dendam tak berujung.\"",
            text          = "Dari celah kayu lemari, Valeria menyaksikan kakaknya disiksa dan diseret hidup-hidup ke dalam gerbang jurang iblis.\n\nIa menangis tanpa suara hingga pita suaranya berdarah. Sendirian di dunia yang membeku tanpa siapapun yang memeluknya lagi.",
            textColor     = new Color(1f, 0.65f, 0.65f, 1f),
            bgColor       = new Color(0.12f, 0.02f, 0.03f, 1f),
            vignetteColor = new Color(0.55f, 0.0f, 0.0f, 0.85f),
            btnLabel      = "▶  Teruskan...",
            isFullNarration = false,
            mood          = "sad"
        });

        panels.Add(new StoryPanel {
            speaker       = "PELATIHAN DI NERAKA BAYANGAN",
            chapterQuote  = "\"Aku meminum racun darah ini bukan untuk mati... tapi untuk menjadi algojo kematian itu sendiri!\"",
            text          = "Valeria bertahan hidup dengan meminum darah iblis terkutuk dan melatih pisau kembarnya di jurang kematian selama belasan tahun.\n\nSetiap luka sayat adalah pengingat. Setiap tetes darah adalah api dendam.\n\n\"Aku telah membuang kelemahan hatiku. Tidak ada lagi tangisan.\"",
            textColor     = new Color(1f, 0.60f, 0.60f, 1f),
            bgColor       = new Color(0.07f, 0.02f, 0.06f, 1f),
            vignetteColor = new Color(0.40f, 0.0f, 0.15f, 0.80f),
            btnLabel      = "▶  Teruskan...",
            isFullNarration = false,
            mood          = "rage"
        });

        panels.Add(new StoryPanel {
            speaker       = "HARAPAN TERAKHIR DI STAGE 1",
            chapterQuote  = "\"Kakak masih hidup... dan aku akan membongkar gerbang neraka untuk membawanya pulang!\"",
            text          = "SEUSAI MEMBANTAI RAJA SLIME DI HUTAN TERLARANG\n\nValeria menemukan medali perak berlumuran darah milik kakaknya pada salah satu jasad iblis!\n\nJejak itu mengarah ke <color=#FF8080>Lembah Reruntuhan Kuno</color> — dimana seorang sandera penting ditawan oleh pasukan kegelapan. Sandera itu adalah satu-satunya saksi yang tahu dimana ruang penyiksaan kakaknya disembunyikan!\n\n\"Tunggu aku, Kakak. Aku datang.\"",
            textColor     = new Color(1f, 0.85f, 0.85f, 1f),
            bgColor       = new Color(0.06f, 0.02f, 0.08f, 1f),
            vignetteColor = new Color(0.30f, 0.05f, 0.20f, 0.80f),
            btnLabel      = "⚔  Mulai Penyelamatan Stage 1",
            isFullNarration = true,
            mood          = "resolve"
        });
    }

    private void Start()
    {
        FindOrBuildCanvas();
        BuildStaticUI();
        StartCoroutine(FadeInThenShowPanel(0));
    }

    private void FindOrBuildCanvas()
    {
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject cgo = new GameObject("StoryCanvas");
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler sc = cgo.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 0.5f;

            cgo.AddComponent<GraphicRaycaster>();
        }

        // Fade overlay
        GameObject fadeGO = new GameObject("FadeOverlay");
        fadeGO.transform.SetParent(canvas.transform, false);
        fadeRT = fadeGO.AddComponent<RectTransform>();
        fadeRT.anchorMin = Vector2.zero; fadeRT.anchorMax = Vector2.one;
        fadeRT.offsetMin = Vector2.zero; fadeRT.offsetMax = Vector2.zero;
        fadeGO.transform.SetAsLastSibling();
        fadeOverlay = fadeGO.AddComponent<Image>();
        fadeOverlay.color = Color.black;
        fadeOverlay.raycastTarget = false;
    }

    private void BuildStaticUI()
    {
        // ── FULLSCREEN BACKGROUND ─────────────────────────────────────────
        panelRoot = new GameObject("StoryPanel");
        panelRoot.transform.SetParent(canvas.transform, false);
        panelRoot.transform.SetSiblingIndex(0);

        RectTransform rootRT = panelRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero; rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero; rootRT.offsetMax = Vector2.zero;

        imgBackground = panelRoot.AddComponent<Image>();
        imgBackground.color = new Color(0.07f, 0.04f, 0.12f, 1f);

        // Screen Vignette
        if (screenVignette != null)
        {
            GameObject vigGO = new GameObject("ScreenVignette");
            vigGO.transform.SetParent(panelRoot.transform, false);
            RectTransform vrt = vigGO.AddComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;

            leftVignette = vigGO.AddComponent<Image>();
            leftVignette.sprite = screenVignette;
            leftVignette.color = new Color(0.5f, 0f, 0f, 0.7f);
            leftVignette.raycastTarget = false;
        }

        // ── TOP HEADER BAR ────────────────────────────────────────────────
        GameObject headerGO = new GameObject("Header");
        headerGO.transform.SetParent(panelRoot.transform, false);
        RectTransform hRT = headerGO.AddComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0f, 1f); hRT.anchorMax = new Vector2(1f, 1f);
        hRT.pivot = new Vector2(0.5f, 1f);
        hRT.anchoredPosition = Vector2.zero;
        hRT.sizeDelta = new Vector2(0f, 68f);

        Image hImg = headerGO.AddComponent<Image>();
        hImg.color = new Color(0.04f, 0.02f, 0.08f, 0.96f);

        CreateGoldLine(headerGO.transform, 0f, 0f, true);

        TMP_Text txtGameTitle = CreateTMP(headerGO.transform, "GameTitle", 15f, FontStyles.Bold,
            new Color(0.95f, 0.85f, 0.45f, 0.95f), TextAlignmentOptions.Left);
        txtGameTitle.rectTransform.anchorMin = new Vector2(0f, 0f);
        txtGameTitle.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        txtGameTitle.rectTransform.offsetMin = new Vector2(36f, 0f);
        txtGameTitle.rectTransform.offsetMax = new Vector2(0f, 0f);
        txtGameTitle.text = "✦ BORN AFTER THE END  •  KISAH ASAL-USUL KSATRIA ✦";
        txtGameTitle.characterSpacing = 2f;

        txtPageNum = CreateTMP(headerGO.transform, "PageNum", 14f, FontStyles.Normal,
            new Color(0.70f, 0.65f, 0.60f, 0.85f), TextAlignmentOptions.Right);
        txtPageNum.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        txtPageNum.rectTransform.anchorMax = new Vector2(1f, 1f);
        txtPageNum.rectTransform.offsetMin = new Vector2(0f, 0f);
        txtPageNum.rectTransform.offsetMax = new Vector2(-180f, 0f);

        // Skip button top-right
        GameObject skipGO = new GameObject("BtnSkip");
        skipGO.transform.SetParent(headerGO.transform, false);
        RectTransform skipRT = skipGO.AddComponent<RectTransform>();
        skipRT.anchorMin = new Vector2(1f, 0.5f);
        skipRT.anchorMax = new Vector2(1f, 0.5f);
        skipRT.pivot     = new Vector2(1f, 0.5f);
        skipRT.sizeDelta = new Vector2(130f, 36f);
        skipRT.anchoredPosition = new Vector2(-24f, 0f);

        Image skipImg = skipGO.AddComponent<Image>();
        if (btnFantasyNormal != null) { skipImg.sprite = btnFantasyNormal; skipImg.type = Image.Type.Sliced; }
        skipImg.color = new Color(0.20f, 0.15f, 0.30f, 0.90f);

        Button skipBtn = skipGO.AddComponent<Button>();
        skipBtn.onClick.AddListener(OnSkipAll);

        TMP_Text skipTxt = CreateTMP(skipGO.transform, "SkipLabel", 13f, FontStyles.Normal,
            new Color(0.85f, 0.80f, 0.70f, 1f), TextAlignmentOptions.Center);
        skipTxt.rectTransform.anchorMin = Vector2.zero; skipTxt.rectTransform.anchorMax = Vector2.one;
        skipTxt.rectTransform.offsetMin = Vector2.zero; skipTxt.rectTransform.offsetMax = Vector2.zero;
        skipTxt.text = "LEWATI [ESC]";

        // ── MANHWA HERO PORTRAIT (Sisi Kiri Atas) ─────────────────────────
        GameObject portraitContainer = new GameObject("HeroPortraitBox");
        portraitContainer.transform.SetParent(panelRoot.transform, false);

        RectTransform pcRT = portraitContainer.AddComponent<RectTransform>();
        pcRT.anchorMin = new Vector2(0f, 0.5f);
        pcRT.anchorMax = new Vector2(0f, 0.5f);
        pcRT.pivot     = new Vector2(0.5f, 0.5f);
        pcRT.anchoredPosition = new Vector2(280f, 110f);
        pcRT.sizeDelta = new Vector2(320f, 340f);

        Image pcBg = portraitContainer.AddComponent<Image>();
        if (modalPanelSprite != null) { pcBg.sprite = modalPanelSprite; pcBg.type = Image.Type.Sliced; }
        pcBg.color = new Color(0.08f, 0.05f, 0.16f, 0.95f);

        // Portrait Image
        GameObject portImgGO = new GameObject("HeroPortrait");
        portImgGO.transform.SetParent(portraitContainer.transform, false);

        heroPortraitRT = portImgGO.AddComponent<RectTransform>();
        heroPortraitRT.anchorMin = new Vector2(0.5f, 0.5f);
        heroPortraitRT.anchorMax = new Vector2(0.5f, 0.5f);
        heroPortraitRT.pivot     = new Vector2(0.5f, 0.5f);
        heroPortraitRT.anchoredPosition = new Vector2(0f, 20f);
        heroPortraitRT.sizeDelta = new Vector2(220f, 220f);

        imgHeroPortrait = portImgGO.AddComponent<Image>();
        Sprite chosenPortrait = (currentHeroName == "Valeria") ? portraitValeria : portraitKaelen;
        imgHeroPortrait.sprite = chosenPortrait;
        imgHeroPortrait.preserveAspect = true;

        // Hero Title Badge
        txtHeroTitleBadge = CreateTMP(portraitContainer.transform, "HeroTitleBadge", 14f, FontStyles.Bold,
            new Color(1f, 0.88f, 0.40f, 1f), TextAlignmentOptions.Center);
        txtHeroTitleBadge.rectTransform.anchorMin = new Vector2(0f, 0f);
        txtHeroTitleBadge.rectTransform.anchorMax = new Vector2(1f, 0f);
        txtHeroTitleBadge.rectTransform.pivot     = new Vector2(0.5f, 0f);
        txtHeroTitleBadge.rectTransform.anchoredPosition = new Vector2(0f, 16f);
        txtHeroTitleBadge.rectTransform.sizeDelta = new Vector2(-20f, 28f);
        txtHeroTitleBadge.text = (currentHeroName == "Valeria")
            ? "VALERIA  •  CRIMSON VALKYRIE"
            : "KAELEN  •  THE SHADOW BLADER";

        // ── MANHWA CHAPTER QUOTE HEADER (Kanan Atas) ──────────────────────
        GameObject quoteGO = new GameObject("ChapterQuoteBox");
        quoteGO.transform.SetParent(panelRoot.transform, false);

        RectTransform qRT = quoteGO.AddComponent<RectTransform>();
        qRT.anchorMin = new Vector2(0f, 0.5f);
        qRT.anchorMax = new Vector2(1f, 0.5f);
        qRT.pivot     = new Vector2(0.5f, 0.5f);
        qRT.anchoredPosition = new Vector2(180f, 120f);
        qRT.sizeDelta = new Vector2(-540f, 180f);

        txtChapterQuote = CreateTMP(quoteGO.transform, "TxtQuote", 22f, FontStyles.Italic,
            new Color(1f, 0.92f, 0.70f, 1f), TextAlignmentOptions.Left);
        txtChapterQuote.rectTransform.anchorMin = Vector2.zero;
        txtChapterQuote.rectTransform.anchorMax = Vector2.one;
        txtChapterQuote.rectTransform.offsetMin = Vector2.zero;
        txtChapterQuote.rectTransform.offsetMax = Vector2.zero;
        txtChapterQuote.lineSpacing = 14f;

        // ── SPEAKER NAME BAND (Ala Webtoon Manhwa) ────────────────────────
        GameObject speakerBand = new GameObject("SpeakerBand");
        speakerBand.transform.SetParent(panelRoot.transform, false);
        RectTransform sbRT = speakerBand.AddComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(0f, 0f);
        sbRT.anchorMax = new Vector2(0f, 0f);
        sbRT.pivot     = new Vector2(0f, 0f);
        sbRT.sizeDelta = new Vector2(460f, 44f);
        sbRT.anchoredPosition = new Vector2(60f, 340f);

        Image spImg = speakerBand.AddComponent<Image>();
        spImg.color = new Color(0.80f, 0.60f, 0.20f, 0.95f);
        if (modalPanelSprite != null) { spImg.sprite = modalPanelSprite; spImg.type = Image.Type.Sliced; }

        txtSpeaker = CreateTMP(speakerBand.transform, "SpeakerName", 16f, FontStyles.Bold,
            new Color(0.06f, 0.04f, 0.08f, 1f), TextAlignmentOptions.Left);
        txtSpeaker.rectTransform.anchorMin = Vector2.zero;
        txtSpeaker.rectTransform.anchorMax = Vector2.one;
        txtSpeaker.rectTransform.offsetMin = new Vector2(20f, 0f);
        txtSpeaker.rectTransform.offsetMax = new Vector2(-20f, 0f);
        txtSpeaker.characterSpacing = 2f;

        // ── MAIN NARRATIVE DIALOGUE BOX (Bagian Bawah Layar) ──────────────
        GameObject textBox = new GameObject("NarrativeBox");
        textBox.transform.SetParent(panelRoot.transform, false);
        RectTransform tbRT = textBox.AddComponent<RectTransform>();
        tbRT.anchorMin = new Vector2(0f, 0f);
        tbRT.anchorMax = new Vector2(1f, 0f);
        tbRT.pivot     = new Vector2(0.5f, 0f);
        tbRT.sizeDelta = new Vector2(-120f, 250f);
        tbRT.anchoredPosition = new Vector2(0f, 90f);

        Image tbImg = textBox.AddComponent<Image>();
        tbImg.color = new Color(0.05f, 0.03f, 0.12f, 0.96f);
        if (modalPanelSprite != null) { tbImg.sprite = modalPanelSprite; tbImg.type = Image.Type.Sliced; }

        CreateGoldLine(textBox.transform, 1f, 0f, false);
        CreateGoldLine(textBox.transform, 0f, 0f, true);

        txtNarrative = CreateTMP(textBox.transform, "NarrativeText", 21f, FontStyles.Normal,
            new Color(1f, 0.95f, 0.88f, 1f), TextAlignmentOptions.Left);
        txtNarrative.rectTransform.anchorMin = Vector2.zero;
        txtNarrative.rectTransform.anchorMax = Vector2.one;
        txtNarrative.rectTransform.offsetMin = new Vector2(40f, 24f);
        txtNarrative.rectTransform.offsetMax = new Vector2(-40f, -24f);
        txtNarrative.lineSpacing = 12f;

        // ── NEXT / CONTINUE BUTTON ────────────────────────────────────────
        GameObject btnGO = new GameObject("BtnNext");
        btnGO.transform.SetParent(panelRoot.transform, false);
        RectTransform bRT = btnGO.AddComponent<RectTransform>();
        bRT.anchorMin = new Vector2(1f, 0f);
        bRT.anchorMax = new Vector2(1f, 0f);
        bRT.pivot     = new Vector2(1f, 0f);
        bRT.sizeDelta = new Vector2(320f, 54f);
        bRT.anchoredPosition = new Vector2(-60f, 22f);

        Image bImg = btnGO.AddComponent<Image>();
        if (btnFantasyNormal != null) { bImg.sprite = btnFantasyNormal; bImg.type = Image.Type.Sliced; bImg.color = Color.white; }
        else bImg.color = new Color(0.80f, 0.60f, 0.20f, 0.95f);

        btnNext = btnGO.AddComponent<Button>();
        btnNext.onClick.AddListener(OnNextClicked);

        // Hover scale
        var ev = btnGO.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        var enterE = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
        enterE.callback.AddListener(_ => btnGO.transform.localScale = new Vector3(1.04f, 1.04f, 1f));
        ev.triggers.Add(enterE);
        var exitE = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
        exitE.callback.AddListener(_ => btnGO.transform.localScale = Vector3.one);
        ev.triggers.Add(exitE);

        btnNextLabel = CreateTMP(btnGO.transform, "BtnLabel", 18f, FontStyles.Bold,
            new Color(1f, 0.92f, 0.65f, 1f), TextAlignmentOptions.Center);
        btnNextLabel.rectTransform.anchorMin = Vector2.zero;
        btnNextLabel.rectTransform.anchorMax = Vector2.one;
        btnNextLabel.rectTransform.offsetMin = Vector2.zero;
        btnNextLabel.rectTransform.offsetMax = Vector2.zero;
        btnNextLabel.text = "▶  Lanjutkan [SPACE]";
    }

    // ─────────────────────────────────────────────────────────────────────
    // SHOW PANEL & ANIMASI
    // ─────────────────────────────────────────────────────────────────────
    private IEnumerator FadeInThenShowPanel(int idx)
    {
        float elapsed = 0f;
        while (elapsed < 0.6f)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(1f, 0f, elapsed / 0.6f));
            yield return null;
        }
        fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
        fadeOverlay.raycastTarget = false;

        ShowPanel(idx);
    }

    private void ShowPanel(int idx)
    {
        if (idx < 0 || idx >= panels.Count) return;

        currentPanel = idx;
        StoryPanel p = panels[idx];

        imgBackground.color = p.bgColor;
        if (leftVignette != null) leftVignette.color = p.vignetteColor;

        // Speaker band
        txtSpeaker.text = p.speaker;
        Image spBandImg = txtSpeaker.transform.parent.GetComponent<Image>();
        if (spBandImg != null) spBandImg.color = GetMoodBandColor(p.mood);

        // Chapter quote header
        if (txtChapterQuote != null)
        {
            txtChapterQuote.text = p.chapterQuote;
            txtChapterQuote.color = GetMoodQuoteColor(p.mood);
        }

        // Page number
        txtPageNum.text = $"Halaman {idx + 1} / {panels.Count}";

        // Button label
        btnNextLabel.text = p.btnLabel;
        txtNarrative.color = p.textColor;

        // Typewriter effect
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypewriterEffect(p.text, 0.024f));
    }

    private Color GetMoodBandColor(string mood)
    {
        switch (mood)
        {
            case "sad":     return new Color(0.40f, 0.15f, 0.45f, 0.95f); // Ungu Kelam
            case "rage":    return new Color(0.75f, 0.15f, 0.10f, 0.95f); // Merah Membara
            case "resolve": return new Color(0.15f, 0.45f, 0.85f, 0.95f); // Biru Ksatria
            default:        return new Color(0.85f, 0.65f, 0.20f, 0.95f); // Emas
        }
    }

    private Color GetMoodQuoteColor(string mood)
    {
        switch (mood)
        {
            case "sad":     return new Color(0.85f, 0.70f, 0.95f, 0.90f);
            case "rage":    return new Color(1.00f, 0.50f, 0.40f, 0.95f);
            case "resolve": return new Color(0.60f, 0.90f, 1.00f, 0.95f);
            default:        return new Color(1.00f, 0.90f, 0.70f, 0.90f);
        }
    }

    private IEnumerator TypewriterEffect(string fullText, float charDelay)
    {
        isTyping = true;
        txtNarrative.text = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            if (fullText[i] == '<')
            {
                int close = fullText.IndexOf('>', i);
                if (close > i)
                {
                    i = close;
                    txtNarrative.text = fullText.Substring(0, i + 1);
                    continue;
                }
            }

            txtNarrative.text = fullText.Substring(0, i + 1);
            yield return new WaitForSeconds(charDelay);
        }

        txtNarrative.text = fullText;
        isTyping = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    // TOMBOL NEXT & NAVIGASI
    // ─────────────────────────────────────────────────────────────────────
    private void OnNextClicked()
    {
        if (isTransitioning) return;

        PlayClick();

        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            isTyping = false;
            txtNarrative.text = panels[currentPanel].text;
            return;
        }

        int next = currentPanel + 1;
        if (next >= panels.Count)
        {
            StartCoroutine(FadeAndLoadStage1());
        }
        else
        {
            StartCoroutine(PanelTransition(next));
        }
    }

    private void OnSkipAll()
    {
        if (isTransitioning) return;
        PlayClick();
        StartCoroutine(FadeAndLoadStage1());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnNextClicked();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnSkipAll();
        }

        // Gentle breathing animation pada portrait
        if (heroPortraitRT != null)
        {
            float bob = Mathf.Sin(Time.time * 2f) * 4f;
            heroPortraitRT.anchoredPosition = new Vector2(0f, 20f + bob);
        }
    }

    private IEnumerator PanelTransition(int nextIdx)
    {
        isTransitioning = true;
        fadeOverlay.raycastTarget = true;

        float elapsed = 0f;
        while (elapsed < 0.22f)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 1f, elapsed / 0.22f));
            yield return null;
        }
        fadeOverlay.color = Color.black;

        PlayPageSfx();
        ShowPanel(nextIdx);

        elapsed = 0f;
        while (elapsed < 0.28f)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(1f, 0f, elapsed / 0.28f));
            yield return null;
        }
        fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
        fadeOverlay.raycastTarget = false;

        isTransitioning = false;
    }

    private IEnumerator FadeAndLoadStage1()
    {
        isTransitioning = true;
        fadeOverlay.raycastTarget = true;

        float elapsed = 0f;
        while (elapsed < 0.65f)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 1f, elapsed / 0.65f));
            yield return null;
        }
        fadeOverlay.color = Color.black;

        string targetScene = PlayerPrefs.GetString("TargetSceneName", "CharacterSelect");
        yield return new WaitForSeconds(0.2f);

        SceneManager.LoadScene(targetScene);
    }

    // ─────────────────────────────────────────────────────────────────────
    // HELPERS
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
        t.richText = true;
        return t;
    }

    private void CreateGoldLine(Transform parent, float anchorY, float posY, bool bottom)
    {
        GameObject go = new GameObject("GoldLine");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, anchorY);
        rt.anchorMax = new Vector2(1f, anchorY);
        rt.pivot     = new Vector2(0.5f, bottom ? 0f : 1f);
        rt.sizeDelta = new Vector2(0f, 2.5f);
        rt.anchoredPosition = new Vector2(0f, posY);
        go.AddComponent<Image>().color = new Color(0.98f, 0.78f, 0.30f, 0.80f);
    }

    private void PlayClick()
    {
        if (sfxClick != null && audioSource != null)
            audioSource.PlayOneShot(sfxClick);
    }

    private void PlayPageSfx()
    {
        if (sfxPage != null && audioSource != null)
            audioSource.PlayOneShot(sfxPage, 0.6f);
    }
}
