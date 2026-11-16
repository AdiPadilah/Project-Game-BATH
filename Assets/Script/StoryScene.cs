using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// StoryScene — Cutscene Latar Belakang Karakter bergaya Manhwa Sinematik Hand-Drawn 2D:
/// - Dimulai dengan masa lalu bahagia keluarga di desa pegunungan yang damai.
/// - Berlanjut ke tragedi malam kehancuran, hilangnya orang tercinta, keputusasaan.
/// - Sumpah dan ambisi untuk menjadi lebih kuat agar tidak ada lagi penyesalan.
/// - Berlanjut ke Stage 1: Misi Penyelamatan Tawanan di Lembah Reruntuhan Kuno.
/// - Dilengkapi ilustrasi manhwa 2D, onomatopoeia SFX, layout dialog proporsional tanpa offset.
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
    private Sprite btnFantasyPressed;
    private Sprite screenVignette;

    // Manhwa Scene Art Sprites (2D Hand-drawn Comic Inked Art)
    private Sprite artPeacefulDays;
    private Sprite artAshenvaleFall;
    private Sprite artEloraKidnapped;
    private Sprite artKaelenAwakening;
    private Sprite artAncientRuins;

    private TMP_FontAsset tmpFont;
    private AudioClip sfxClick;
    private AudioClip sfxPage;
    private AudioClip sfxImpact;
    private AudioSource audioSource;

    // ── UI Root Elements ──────────────────────────────────────────────────
    private Canvas canvas;
    private Image fadeOverlay;
    private RectTransform fadeRT;

    // Central Manhwa Artwork Frame
    private GameObject manhwaFrameGO;
    private Image imgManhwaArt;
    private TMP_Text txtManhwaSFX;          // Onomatopoeia floating text (e.g. 콰아아앙!)
    private TMP_Text txtChapterTag;         // Episode/Chapter tag top-right of art

    // Dialogue / Caption Overlay Box
    private GameObject captionBoxGO;
    private TMP_Text txtSpeakerBadge;
    private Image imgSpeakerPill;
    private TMP_Text txtNarrativeDialogue;
    private TMP_Text txtChapterSubtitle;

    // Navigation Controls
    private Button btnPrev;
    private Button btnNext;
    private TMP_Text txtBtnNextLabel;
    private Button btnSkip;
    private TMP_Text txtPageCounter;
    private Image[] pageDotImgs;

    // ── Story State ───────────────────────────────────────────────────────
    private class StoryBeat
    {
        public string chapterTag;       // e.g. "PROLOG • HARI-HARI DAMAI"
        public string onomatopoeia;     // e.g. "《 ♬ 하하호호 (SENYUM & TAWA) 》"
        public string speaker;          // e.g. "NARASI", "KAELEN", "TRAGEDI"
        public string subtitle;         // e.g. "Lembah Bunga Ashenvale yang Penuh Kedamaian"
        public string text;             // Isi cerita lengkap
        public Sprite artSprite;        // Gambar ilustrasi manhwa
        public Color moodColor;         // Warna tema badge
        public Color sfxColor;          // Warna teks onomatopoeia
        public string nextBtnLabel;     // e.g. "▶ Lanjutkan [SPACE]"
    }

    private List<StoryBeat> beats = new List<StoryBeat>();
    private int currentBeatIndex = 0;
    private bool isTransitioning = false;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private string selectedHeroName = "Kaelen";

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        selectedHeroName = PlayerPrefs.GetString("SelectedCharacterName", "Kaelen");
        LoadAssets();
        BuildStoryBeats();
    }

    private void LoadAssets()
    {
        modalPanelSprite = Resources.Load<Sprite>("UI/panel_fantasy_modal");
        btnFantasyNormal = Resources.Load<Sprite>("UI/btn_fantasy_normal");
        btnFantasyHover  = Resources.Load<Sprite>("UI/btn_fantasy_hover");
        btnFantasyPressed= Resources.Load<Sprite>("UI/btn_fantasy_pressed");
        screenVignette   = Resources.Load<Sprite>("UI/screen_vignette");

        // Manhwa 2D Hand-drawn Art Sprites
        artPeacefulDays    = Resources.Load<Sprite>("UI/Story/manhwa_peaceful_days");
        artAshenvaleFall   = Resources.Load<Sprite>("UI/Story/manhwa_ashenvale_fall");
        artEloraKidnapped  = Resources.Load<Sprite>("UI/Story/manhwa_elora_kidnapped");
        artKaelenAwakening = Resources.Load<Sprite>("UI/Story/manhwa_kaelen_awakening");
        artAncientRuins    = Resources.Load<Sprite>("UI/Story/manhwa_ancient_ruins_rescue");

        // Fallback jika salah satu null
        if (artPeacefulDays == null) artPeacefulDays = artAshenvaleFall;
        if (artAshenvaleFall == null) artAshenvaleFall = Resources.Load<Sprite>("UI/menu_background");
        if (artEloraKidnapped == null) artEloraKidnapped = artAshenvaleFall;
        if (artKaelenAwakening == null) artKaelenAwakening = artAshenvaleFall;
        if (artAncientRuins == null) artAncientRuins = artAshenvaleFall;

        sfxClick  = Resources.Load<AudioClip>("Audio/ui_click");
        sfxPage   = Resources.Load<AudioClip>("Audio/ui_hover");
        sfxImpact = Resources.Load<AudioClip>("Audio/star_1");
        tmpFont   = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    private void BuildStoryBeats()
    {
        bool isKaelen = (selectedHeroName != "Valeria");

        if (isKaelen)
        {
            // ── KISAH KAELEN (THE SHADOW BLADER) ──────────────────────────
            // Beat 0: Masa Lalu Bahagia
            beats.Add(new StoryBeat {
                chapterTag    = "PROLOG  •  HARI-HARI DAMAI & BAHAGIA",
                onomatopoeia  = "[ HAHAHA! (SENYUM & TAWA) ]",
                speaker       = "NARASI",
                subtitle      = "10 Tahun Lalu — Lembah Bunga Ashenvale yang Penuh Kedamaian",
                text          = "Dahulu kala, sebelum dunia hancur lebur... keluarga kami hidup begitu damai dan bahagia.\n\nAyah adalah pandai besi yang tekun, ibu selalu menyambut kami dengan roti hangat, dan aku bersama adikku tercinta, <color=#FFD6A5>Elora</color>, menghabiskan masa kecil bermain riang di padang bunga pegunungan. Elora tersenyum memeluk mahkota bunga, berjanji kita akan selalu bersama selamanya.",
                artSprite     = artPeacefulDays,
                moodColor     = new Color(0.35f, 0.85f, 0.50f, 1f), // Hijau Damai
                sfxColor      = new Color(1.00f, 0.90f, 0.40f, 1f), // Kuning Cerah
                nextBtnLabel  = "▶  Lanjutkan [SPACE]"
            });

            // Beat 1: Hingga Pada Suatu Malam (Tragedi Dimulai)
            beats.Add(new StoryBeat {
                chapterTag    = "EPISODE 1  •  HINGGA PADA SUATU MALAM...",
                onomatopoeia  = "[ KRAAA-BOOOM! (HANCUR LEBUR) ]",
                speaker       = "TRAGEDI DESA ASHENVALE",
                subtitle      = "Langit Terbelah & Serbuan Pasukan Jurang Abyssal",
                text          = "Sampai pada suatu malam yang terkutuk... langit tiba-tiba berubah merah darah.\n\nGerbang Abyssal runtuh tepat di atas desa kami. Ribuan monster kegelapan memuntahkan api dan membakar rumah-rumah. Ayah gugur menahan pintu agar kami bisa lari. Ibu lenyap ditelan kobaran api. Dalam sekejap, tawa bahagia kami berganti menjadi jeritan keputusasaan.",
                artSprite     = artAshenvaleFall,
                moodColor     = new Color(0.95f, 0.25f, 0.15f, 1f), // Merah Api
                sfxColor      = new Color(1.00f, 0.35f, 0.15f, 1f),
                nextBtnLabel  = "▶  Teruskan..."
            });

            // Beat 2: Perpisahan yang Menyayat Hati
            beats.Add(new StoryBeat {
                chapterTag    = "EPISODE 2  •  PERPISAHAN YANG TRAGIS",
                onomatopoeia  = "[ CRACK! (TERPUTUS TRAGIS) ]",
                speaker       = "KAELEN (MASA LALU)",
                subtitle      = "Genggaman Tangan yang Terlepas di Hadapan Maut",
                text          = "\"ELORA! PEGANG TANGANKU! JANGAN LEPASKAN!\"\n\n\"Kakak... larilah... hiduplah untukku...\"\n\nSosok demon bertanduk bayangan merenggut Elora ke dalam portal kegelapan. Pedang kecil di tangan Kaelen patah berkeping-keping. Genggaman tangan mereka terlepas... meninggalkan Kaelen bersimbah darah di atas tanah abu, menangis dalam keputusasaan yang tak terobati.",
                artSprite     = artEloraKidnapped,
                moodColor     = new Color(0.70f, 0.25f, 0.90f, 1f), // Ungu Abyssal
                sfxColor      = new Color(0.85f, 0.35f, 1.00f, 1f),
                nextBtnLabel  = "▶  Teruskan..."
            });

            // Beat 3: Kebangkitan & Sumpah Pedang Bayangan
            beats.Add(new StoryBeat {
                chapterTag    = "EPISODE 3  •  SUMPAH PEDANG BAYANGAN",
                onomatopoeia  = "[ SHWAAA! (SHADOW AWAKENING) ]",
                speaker       = "SUMPAH KSATRIA BAYANGAN",
                subtitle      = "Kelemahan Adalah Dosa Terbesar di Dunia Ini",
                text          = "Kaelen memohon pertolongan pada Ksatria Kerajaan, namun mereka menolak dan mengejeknya: \"Adikmu sudah mati. Menyerahlah, bocah abu.\"\n\nDi malam kelam itu, Kaelen menelan serpihan <color=#8060FF>Inti Bayangan Terlarang</color>.\n\n\"Jika dunia dan ksatria suci berpaling... maka aku akan merangkul kegelapan! Aku bersumpah akan menjadi ksatria terkuat di dunia ini, dan aku tidak akan berhenti hingga Elora kembali!\"",
                artSprite     = artKaelenAwakening,
                moodColor     = new Color(0.30f, 0.70f, 1.00f, 1f), // Biru Listrik
                sfxColor      = new Color(0.40f, 0.85f, 1.00f, 1f),
                nextBtnLabel  = "▶  Menuju Medan Tempur"
            });

            // Beat 4: Petunjuk Penyelamatan Stage 1
            beats.Add(new StoryBeat {
                chapterTag    = "EPISODE 4  •  PETUNJUK PENYELAMATAN STAGE 1",
                onomatopoeia  = "[ CLASH! (MISI PENYELAMATAN) ]",
                speaker       = "PANGGILAN PENYELAMATAN",
                subtitle      = "Tawanan Gadis Penyintas di Sel Reruntuhan Kuno",
                text          = "10 TAHUN BERLALU — SEUSAI MEMBANTAI RAJA SLIME DI HUTAN TERLARANG\n\nKaelen menemukan kabar pasti: Seorang gadis penyintas disekap di dalam sel besi kuil oleh para Iblis Penjaga untuk dijadikan tumbal Raja Iblis!\nGadis ini memegang rahasia kunci gerbang tahanan Elora.\n\n\"Bertahanlah sedikit lagi, Elora... Kakak datang untuk menyelamatkanmu!\"",
                artSprite     = artAncientRuins,
                moodColor     = new Color(1.00f, 0.85f, 0.35f, 1f), // Emas Harapan
                sfxColor      = new Color(1.00f, 0.90f, 0.40f, 1f),
                nextBtnLabel  = "▶  MULAI PENYELAMATAN STAGE 1"
            });
        }
        else
        {
            // ── KISAH VALERIA (THE CRIMSON VALKYRIE) ──────────────────────
            beats.Add(new StoryBeat {
                chapterTag    = "PROLOG  •  HARI-HARI DAMAI PONDOK MAWAR",
                onomatopoeia  = "[ WHISPER (ANGIN SEGAR) ]",
                speaker       = "NARASI",
                subtitle      = "15 Tahun Lalu — Pondok Tabib Damai di Perbatasan Timur",
                text          = "Sebelum salju berlumur darah... keluarga Valeria adalah tabib pengelana yang penuh tawa.\n\nAyah dan ibu meracik obat penyembuh untuk warga desa, sementara kakak laki-lakinya, <color=#FF8080>Lian</color>, selalu menggendong Valeria kecil dan berjanji akan menjaganya dari bahaya apapun. Masa kecil yang hangat dan penuh kasih sayang.",
                artSprite     = artPeacefulDays,
                moodColor     = new Color(0.35f, 0.85f, 0.50f, 1f),
                sfxColor      = new Color(1.00f, 0.90f, 0.40f, 1f),
                nextBtnLabel  = "▶  Lanjutkan [SPACE]"
            });

            beats.Add(new StoryBeat {
                chapterTag    = "EPISODE 1  •  MALAM PENYERGAPAN BADAI SALJU",
                onomatopoeia  = "[ CLASH! (SERANGAN MAUT) ]",
                speaker       = "TRAGEDI KELUARGA VALERIA",
                subtitle      = "Keserakahan Klan Pembunuh Bayangan",
                text          = "Sampai pada suatu malam badai salju... ordo pembunuh bayaran gelap menyerbu rumah mereka demi merebut pusaka <color=#FF5050>Elixir Darah Murni</color>.\n\nAyah dan ibu tewas membeku di depan pintu. Lian menarik Valeria kecil ke dalam lemari tersembunyi dan menguncinya dari luar.\n\"Valeria... jangan menangis. Hiduplah. Untuk kakak!\"",
                artSprite     = artAshenvaleFall,
                moodColor     = new Color(0.95f, 0.20f, 0.20f, 1f),
                sfxColor      = new Color(1.00f, 0.25f, 0.25f, 1f),
                nextBtnLabel  = "▶  Teruskan..."
            });

            beats.Add(new StoryBeat {
                chapterTag    = "EPISODE 2  •  PENGORBANAN SANG KAKAK",
                onomatopoeia  = "[ DESPAIR! (KEPUTUSASAAN) ]",
                speaker       = "VALERIA (MASA LALU)",
                subtitle      = "Jeritan Bisu di Balik Celah Pintu Kayu",
                text          = "Dari celah kayu lemari, Valeria menyaksikan kakaknya disiksa dan diseret hidup-hidup ke dalam gerbang jurang iblis.\n\nIa menggigit bibirnya hingga berdarah agar tidak menjerit. Sejak malam itu, air matanya membeku menjadi racun dendam yang takkan pernah padam.",
                artSprite     = artEloraKidnapped,
                moodColor     = new Color(0.70f, 0.15f, 0.40f, 1f),
                sfxColor      = new Color(0.85f, 0.20f, 0.50f, 1f),
                nextBtnLabel  = "▶  Teruskan..."
            });

            beats.Add(new StoryBeat {
                chapterTag    = "EPISODE 3  •  ALGOJO KEMATIAN DARI JURANG",
                onomatopoeia  = "[ SLASH! (CRIMSON FANG) ]",
                speaker       = "SUMPAH PISAU CRIMSON",
                subtitle      = "Meminum Darah Iblis Demi Menjadi Predator Terkuat",
                text          = "Valeria bertahan hidup belasan tahun di jurang kegelapan dengan meminum darah iblis terkutuk dan melatih pisau kembarnya hingga secepat kilat.\n\n\"Aku telah membuang kelemahan hatiku. Pedang kembarku akan menari dalam darah para pembantai itu hingga Lian kembali ke pelukanku!\"",
                artSprite     = artKaelenAwakening,
                moodColor     = new Color(1.00f, 0.20f, 0.25f, 1f),
                sfxColor      = new Color(1.00f, 0.35f, 0.35f, 1f),
                nextBtnLabel  = "▶  Menuju Medan Tempur"
            });

            beats.Add(new StoryBeat {
                chapterTag    = "EPISODE 4  •  PENYELAMATAN STAGE 1: LEMBAH RERUNTUHAN",
                onomatopoeia  = "[ STRIKE! (PERBURUAN DIMULAI) ]",
                speaker       = "PANGGILAN PENYELAMATAN",
                subtitle      = "Medali Perak Sang Kakak Ditemukan di Reruntuhan Kuno",
                text          = "SEUSAI MEMBANTAI RAJA SLIME DI HUTAN TERLARANG\n\nValeria menemukan medali perak milik Lian pada salah satu jasad iblis!\nJejak itu mengarah ke <color=#FF8080>Lembah Reruntuhan Kuno</color> — dimana seorang gadis sandera disekap dalam sel besi oleh algojo iblis sebagai tumbal Raja Iblis. Sandera ini mengetahui lokasi ruang bawah tanah rahasia tempat kakaknya disekap!\n\n\"Tunggu aku, Kakak. Siapapun yang menghalangi jalan ini akan kubakar habis!\"",
                artSprite     = artAncientRuins,
                moodColor     = new Color(1.00f, 0.80f, 0.30f, 1f),
                sfxColor      = new Color(1.00f, 0.85f, 0.35f, 1f),
                nextBtnLabel  = "▶  MULAI PENYELAMATAN STAGE 1"
            });
        }
    }

    private void Start()
    {
        FindOrBuildCanvas();
        BuildAtmosphere();
        BuildTopHeader();
        BuildCentralManhwaFrame();
        BuildCaptionDialogueBox();
        BuildBottomNavBar();

        ShowBeat(0, false);
        StartCoroutine(FadeInOnStart());
    }

    private void FindOrBuildCanvas()
    {
        // Pastikan EventSystem aktif agar tombol UI selalu dapat diklik
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

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
        fadeOverlay.color = new Color(0f, 0f, 0f, 1f);
        fadeOverlay.raycastTarget = false;
    }

    private IEnumerator FadeInOnStart()
    {
        float elapsed = 0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(1f, 0f, elapsed / 0.35f));
            yield return null;
        }
        fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
    }

    private void BuildAtmosphere()
    {
        // Dark Base
        GameObject baseBG = new GameObject("BaseDarkBG");
        baseBG.transform.SetParent(canvas.transform, false);
        baseBG.transform.SetSiblingIndex(0);
        RectTransform brt = baseBG.AddComponent<RectTransform>();
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
        Image bi = baseBG.AddComponent<Image>();
        bi.color = new Color(0.04f, 0.02f, 0.08f, 1f);

        // Screen Vignette
        if (screenVignette != null)
        {
            GameObject vigGO = new GameObject("ScreenVignette");
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

    private void BuildTopHeader()
    {
        GameObject headerGO = new GameObject("HeaderBar");
        headerGO.transform.SetParent(canvas.transform, false);
        RectTransform hRT = headerGO.AddComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0.5f, 1f); hRT.anchorMax = new Vector2(0.5f, 1f);
        hRT.pivot     = new Vector2(0.5f, 1f);
        hRT.anchoredPosition = new Vector2(0f, 0f);
        hRT.sizeDelta = new Vector2(1920f, 54f);

        Image hImg = headerGO.AddComponent<Image>();
        hImg.color = new Color(0.05f, 0.03f, 0.10f, 0.95f);

        CreateGoldDivider(headerGO.transform, 0f, 0f);

        // Title
        TMP_Text txtTitle = CreateTMP(headerGO.transform, "Title", 15f, FontStyles.Bold,
            new Color(1f, 0.88f, 0.45f, 1f), TextAlignmentOptions.Left);
        txtTitle.rectTransform.anchorMin = new Vector2(0f, 0f); txtTitle.rectTransform.anchorMax = new Vector2(0.6f, 1f);
        txtTitle.rectTransform.offsetMin = new Vector2(40f, 0f); txtTitle.rectTransform.offsetMax = Vector2.zero;
        txtTitle.characterSpacing = 2f;
        txtTitle.text = "✦ BORN AFTER THE END  •  PROLOG MANHWA KSATRIA ✦";

        // Page Counter
        txtPageCounter = CreateTMP(headerGO.transform, "PageCounter", 14f, FontStyles.Normal,
            new Color(0.75f, 0.70f, 0.65f, 0.85f), TextAlignmentOptions.Right);
        txtPageCounter.rectTransform.anchorMin = new Vector2(0.6f, 0f); txtPageCounter.rectTransform.anchorMax = new Vector2(1f, 1f);
        txtPageCounter.rectTransform.offsetMin = Vector2.zero; txtPageCounter.rectTransform.offsetMax = new Vector2(-180f, 0f);

        // Skip Button
        GameObject skipGO = new GameObject("BtnSkip");
        skipGO.transform.SetParent(headerGO.transform, false);
        RectTransform sRT = skipGO.AddComponent<RectTransform>();
        sRT.anchorMin = new Vector2(1f, 0.5f); sRT.anchorMax = new Vector2(1f, 0.5f);
        sRT.pivot     = new Vector2(1f, 0.5f);
        sRT.sizeDelta = new Vector2(120f, 32f);
        sRT.anchoredPosition = new Vector2(-30f, 0f);

        Image sImg = skipGO.AddComponent<Image>();
        if (btnFantasyNormal != null) { sImg.sprite = btnFantasyNormal; sImg.type = Image.Type.Sliced; }
        sImg.color = new Color(0.25f, 0.18f, 0.35f, 0.85f);

        btnSkip = skipGO.AddComponent<Button>();
        btnSkip.onClick.AddListener(OnSkipAllClicked);

        TMP_Text sTxt = CreateTMP(skipGO.transform, "Lbl", 12f, FontStyles.Bold,
            new Color(0.90f, 0.85f, 0.75f, 1f), TextAlignmentOptions.Center);
        sTxt.rectTransform.anchorMin = Vector2.zero; sTxt.rectTransform.anchorMax = Vector2.one;
        sTxt.rectTransform.offsetMin = Vector2.zero; sTxt.rectTransform.offsetMax = Vector2.zero;
        sTxt.text = "LEWATI [ESC]";
    }

    // ─────────────────────────────────────────────────────────────────────
    // CENTRAL MANHWA ARTWORK PANEL (1140 x 540)
    // ─────────────────────────────────────────────────────────────────────
    private void BuildCentralManhwaFrame()
    {
        manhwaFrameGO = new GameObject("ManhwaArtFrame");
        manhwaFrameGO.transform.SetParent(canvas.transform, false);

        RectTransform mfRT = manhwaFrameGO.AddComponent<RectTransform>();
        mfRT.anchorMin = new Vector2(0.5f, 0.5f);
        mfRT.anchorMax = new Vector2(0.5f, 0.5f);
        mfRT.pivot     = new Vector2(0.5f, 0.5f);
        mfRT.sizeDelta = new Vector2(1140f, 460f);
        mfRT.anchoredPosition = new Vector2(0f, 130f);

        // Frame Background & Sliced Modal Border
        Image frameBg = manhwaFrameGO.AddComponent<Image>();
        if (modalPanelSprite != null) { frameBg.sprite = modalPanelSprite; frameBg.type = Image.Type.Sliced; }
        frameBg.color = new Color(0.06f, 0.04f, 0.14f, 0.98f);

        // Inner Mask for Manhwa Artwork
        GameObject artMaskGO = new GameObject("ArtMask");
        artMaskGO.transform.SetParent(manhwaFrameGO.transform, false);
        RectTransform amRT = artMaskGO.AddComponent<RectTransform>();
        amRT.anchorMin = Vector2.zero; amRT.anchorMax = Vector2.one;
        amRT.offsetMin = new Vector2(12f, 12f); amRT.offsetMax = new Vector2(-12f, -12f);

        artMaskGO.AddComponent<Mask>().showMaskGraphic = false;
        artMaskGO.AddComponent<Image>().color = Color.white;

        // Manhwa Illustration Image
        GameObject artImgGO = new GameObject("ManhwaArtImage");
        artImgGO.transform.SetParent(artMaskGO.transform, false);
        RectTransform aiRT = artImgGO.AddComponent<RectTransform>();
        aiRT.anchorMin = Vector2.zero; aiRT.anchorMax = Vector2.one;
        aiRT.offsetMin = Vector2.zero; aiRT.offsetMax = Vector2.zero;

        imgManhwaArt = artImgGO.AddComponent<Image>();
        imgManhwaArt.sprite = artPeacefulDays;
        imgManhwaArt.preserveAspect = false; // Fit perfectly into container
        imgManhwaArt.color = Color.white;

        // Subtle dark gradient vignette over illustration
        GameObject artVigGO = new GameObject("ArtInnerVignette");
        artVigGO.transform.SetParent(artMaskGO.transform, false);
        RectTransform avRT = artVigGO.AddComponent<RectTransform>();
        avRT.anchorMin = Vector2.zero; avRT.anchorMax = Vector2.one;
        avRT.offsetMin = Vector2.zero; avRT.offsetMax = Vector2.zero;

        if (screenVignette != null)
        {
            Image avi = artVigGO.AddComponent<Image>();
            avi.sprite = screenVignette;
            avi.color = new Color(0f, 0f, 0f, 0.40f);
            avi.raycastTarget = false;
        }

        // Onomatopoeia / SFX Floating Text (ala Manhwa Korea)
        GameObject sfxGO = new GameObject("ManhwaSFX");
        sfxGO.transform.SetParent(manhwaFrameGO.transform, false);
        RectTransform sfxRT = sfxGO.AddComponent<RectTransform>();
        sfxRT.anchorMin = new Vector2(0f, 1f); sfxRT.anchorMax = new Vector2(0f, 1f);
        sfxRT.pivot     = new Vector2(0f, 1f);
        sfxRT.anchoredPosition = new Vector2(30f, -22f);
        sfxRT.sizeDelta = new Vector2(450f, 50f);

        txtManhwaSFX = sfxGO.AddComponent<TextMeshProUGUI>();
        txtManhwaSFX.font = tmpFont;
        txtManhwaSFX.fontSize = 26f;
        txtManhwaSFX.fontStyle = FontStyles.Bold | FontStyles.Italic;
        txtManhwaSFX.color = new Color(1.0f, 0.85f, 0.35f, 1f);
        txtManhwaSFX.outlineWidth = 0.25f;
        txtManhwaSFX.outlineColor = Color.black;
        txtManhwaSFX.characterSpacing = 3f;
        txtManhwaSFX.text = "[ HAHAHA! (SENYUM & TAWA) ]";

        // Chapter / Episode Tag Top-Right
        GameObject tagGO = new GameObject("ChapterTag");
        tagGO.transform.SetParent(manhwaFrameGO.transform, false);
        RectTransform tagRT = tagGO.AddComponent<RectTransform>();
        tagRT.anchorMin = new Vector2(1f, 1f); tagRT.anchorMax = new Vector2(1f, 1f);
        tagRT.pivot     = new Vector2(1f, 1f);
        tagRT.anchoredPosition = new Vector2(-30f, -22f);
        tagRT.sizeDelta = new Vector2(450f, 32f);

        txtChapterTag = tagGO.AddComponent<TextMeshProUGUI>();
        txtChapterTag.font = tmpFont;
        txtChapterTag.fontSize = 14f;
        txtChapterTag.fontStyle = FontStyles.Bold;
        txtChapterTag.color = new Color(1f, 0.88f, 0.45f, 1f);
        txtChapterTag.alignment = TextAlignmentOptions.Right;
        txtChapterTag.characterSpacing = 2f;
        txtChapterTag.text = "PROLOG • HARI-HARI DAMAI";

        // 4 Corner Gold Diamonds
        BuildFrameDiamonds(manhwaFrameGO.transform, 570f, 270f);
    }

    // ─────────────────────────────────────────────────────────────────────
    // CAPTION & DIALOGUE BOX (Proporsional, Rapi, Bebas Offset)
    // ─────────────────────────────────────────────────────────────────────
    private void BuildCaptionDialogueBox()
    {
        captionBoxGO = new GameObject("CaptionDialogueBox");
        captionBoxGO.transform.SetParent(canvas.transform, false);

        RectTransform cbRT = captionBoxGO.AddComponent<RectTransform>();
        cbRT.anchorMin = new Vector2(0.5f, 0.5f);
        cbRT.anchorMax = new Vector2(0.5f, 0.5f);
        cbRT.pivot     = new Vector2(0.5f, 0.5f);
        cbRT.sizeDelta = new Vector2(1140f, 290f);
        cbRT.anchoredPosition = new Vector2(0f, -196f);

        Image cbImg = captionBoxGO.AddComponent<Image>();
        if (modalPanelSprite != null) { cbImg.sprite = modalPanelSprite; cbImg.type = Image.Type.Sliced; }
        cbImg.color = new Color(0.06f, 0.04f, 0.12f, 0.98f);

        CreateGoldDivider(captionBoxGO.transform, 1f, 0f);
        CreateGoldDivider(captionBoxGO.transform, 0f, 0f);

        // Speaker Pill / Name Badge
        GameObject pillGO = new GameObject("SpeakerPill");
        pillGO.transform.SetParent(captionBoxGO.transform, false);
        RectTransform pRT = pillGO.AddComponent<RectTransform>();
        pRT.anchorMin = new Vector2(0f, 1f); pRT.anchorMax = new Vector2(0f, 1f);
        pRT.pivot     = new Vector2(0f, 1f);
        pRT.anchoredPosition = new Vector2(30f, -14f);
        pRT.sizeDelta = new Vector2(320f, 32f);

        imgSpeakerPill = pillGO.AddComponent<Image>();
        if (btnFantasyNormal != null) { imgSpeakerPill.sprite = btnFantasyNormal; imgSpeakerPill.type = Image.Type.Sliced; }
        imgSpeakerPill.color = new Color(0.85f, 0.60f, 0.20f, 0.95f);

        txtSpeakerBadge = CreateTMP(pillGO.transform, "TxtBadge", 13.5f, FontStyles.Bold,
            Color.white, TextAlignmentOptions.Center);
        txtSpeakerBadge.rectTransform.anchorMin = Vector2.zero; txtSpeakerBadge.rectTransform.anchorMax = Vector2.one;
        txtSpeakerBadge.rectTransform.offsetMin = Vector2.zero; txtSpeakerBadge.rectTransform.offsetMax = Vector2.zero;
        txtSpeakerBadge.characterSpacing = 2f;
        txtSpeakerBadge.text = "[ NARASI ]";

        // Subtitle Context (Di samping speaker pill)
        txtChapterSubtitle = CreateTMP(captionBoxGO.transform, "TxtSubtitle", 13f, FontStyles.Italic,
            new Color(0.92f, 0.88f, 0.75f, 0.95f), TextAlignmentOptions.Left);
        txtChapterSubtitle.rectTransform.anchorMin = new Vector2(0f, 1f); txtChapterSubtitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        txtChapterSubtitle.rectTransform.pivot     = new Vector2(0f, 1f);
        txtChapterSubtitle.rectTransform.anchoredPosition = new Vector2(370f, -15f);
        txtChapterSubtitle.rectTransform.sizeDelta = new Vector2(-400f, 30f);
        txtChapterSubtitle.text = "10 Tahun Lalu — Lembah Bunga Ashenvale";

        // Narrative / Dialogue Body Text
        GameObject bodyGO = new GameObject("NarrativeBody");
        bodyGO.transform.SetParent(captionBoxGO.transform, false);
        RectTransform bRT = bodyGO.AddComponent<RectTransform>();
        bRT.anchorMin = Vector2.zero; bRT.anchorMax = Vector2.one;
        bRT.offsetMin = new Vector2(30f, 14f);
        bRT.offsetMax = new Vector2(-30f, -55f);

        txtNarrativeDialogue = bodyGO.AddComponent<TextMeshProUGUI>();
        txtNarrativeDialogue.font = tmpFont;
        txtNarrativeDialogue.enableAutoSizing = true;
        txtNarrativeDialogue.fontSizeMin = 12f;
        txtNarrativeDialogue.fontSizeMax = 16f;
        txtNarrativeDialogue.fontStyle = FontStyles.Normal;
        txtNarrativeDialogue.color = new Color(1f, 0.95f, 0.90f, 1f);
        txtNarrativeDialogue.alignment = TextAlignmentOptions.TopLeft;
        txtNarrativeDialogue.enableWordWrapping = true;
        txtNarrativeDialogue.lineSpacing = 2f;
        txtNarrativeDialogue.overflowMode = TextOverflowModes.Ellipsis;
        txtNarrativeDialogue.richText = true;
    }

    // ─────────────────────────────────────────────────────────────────────
    // BOTTOM NAVIGATION CONTROLS
    // ─────────────────────────────────────────────────────────────────────
    private void BuildBottomNavBar()
    {
        GameObject navBar = new GameObject("BottomNavBar");
        navBar.transform.SetParent(canvas.transform, false);

        RectTransform nbRT = navBar.AddComponent<RectTransform>();
        nbRT.anchorMin = new Vector2(0.5f, 0f); nbRT.anchorMax = new Vector2(0.5f, 0f);
        nbRT.pivot     = new Vector2(0.5f, 0f);
        nbRT.sizeDelta = new Vector2(1140f, 54f);
        nbRT.anchoredPosition = new Vector2(0f, 18f);

        // 1. Button PREV [ ◄ SEBELUMNYA ]
        GameObject prevGO = new GameObject("BtnPrev");
        prevGO.transform.SetParent(navBar.transform, false);
        RectTransform pRT = prevGO.AddComponent<RectTransform>();
        pRT.anchorMin = new Vector2(0f, 0.5f); pRT.anchorMax = new Vector2(0f, 0.5f);
        pRT.pivot     = new Vector2(0f, 0.5f);
        pRT.sizeDelta = new Vector2(210f, 46f);
        pRT.anchoredPosition = new Vector2(0f, 0f);

        Image pImg = prevGO.AddComponent<Image>();
        if (btnFantasyNormal != null) { pImg.sprite = btnFantasyNormal; pImg.type = Image.Type.Sliced; }
        pImg.color = new Color(0.18f, 0.12f, 0.28f, 0.95f);

        btnPrev = prevGO.AddComponent<Button>();
        btnPrev.onClick.AddListener(OnPrevClicked);

        TMP_Text pTxt = CreateTMP(prevGO.transform, "Lbl", 14f, FontStyles.Bold,
            new Color(0.90f, 0.85f, 0.75f, 1f), TextAlignmentOptions.Center);
        pTxt.rectTransform.anchorMin = Vector2.zero; pTxt.rectTransform.anchorMax = Vector2.one;
        pTxt.rectTransform.offsetMin = Vector2.zero; pTxt.rectTransform.offsetMax = Vector2.zero;
        pTxt.text = "[ ◄ SEBELUMNYA ]";

        // 2. Button NEXT [ LANJUTKAN ► ]
        GameObject nextGO = new GameObject("BtnNext");
        nextGO.transform.SetParent(navBar.transform, false);
        RectTransform nRT = nextGO.AddComponent<RectTransform>();
        nRT.anchorMin = new Vector2(1f, 0.5f); nRT.anchorMax = new Vector2(1f, 0.5f);
        nRT.pivot     = new Vector2(1f, 0.5f);
        nRT.sizeDelta = new Vector2(340f, 50f);
        nRT.anchoredPosition = new Vector2(0f, 0f);

        Image nImg = nextGO.AddComponent<Image>();
        if (btnFantasyNormal != null) { nImg.sprite = btnFantasyNormal; nImg.type = Image.Type.Sliced; }
        nImg.color = Color.white;

        btnNext = nextGO.AddComponent<Button>();
        btnNext.onClick.AddListener(OnNextClicked);

        txtBtnNextLabel = CreateTMP(nextGO.transform, "Lbl", 16f, FontStyles.Bold,
            new Color(1f, 0.92f, 0.65f, 1f), TextAlignmentOptions.Center);
        txtBtnNextLabel.rectTransform.anchorMin = Vector2.zero; txtBtnNextLabel.rectTransform.anchorMax = Vector2.one;
        txtBtnNextLabel.rectTransform.offsetMin = Vector2.zero; txtBtnNextLabel.rectTransform.offsetMax = Vector2.zero;
        txtBtnNextLabel.characterSpacing = 2f;
        txtBtnNextLabel.text = "▶  Lanjutkan [SPACE]";

        // 3. Pagination Dots (Tengah)
        BuildPaginationDots(navBar.transform);
    }

    private void BuildPaginationDots(Transform parent)
    {
        GameObject dotsGO = new GameObject("DotsContainer");
        dotsGO.transform.SetParent(parent, false);
        RectTransform dRT = dotsGO.AddComponent<RectTransform>();
        dRT.anchorMin = new Vector2(0.5f, 0.5f); dRT.anchorMax = new Vector2(0.5f, 0.5f);
        dRT.pivot     = new Vector2(0.5f, 0.5f);
        dRT.sizeDelta = new Vector2(180f, 30f);
        dRT.anchoredPosition = Vector2.zero;

        int count = beats.Count;
        pageDotImgs = new Image[count];
        float spacing = 28f;
        float startX = -(spacing * (count - 1)) / 2f;

        for (int i = 0; i < count; i++)
        {
            GameObject dot = new GameObject("Dot_" + i);
            dot.transform.SetParent(dotsGO.transform, false);
            RectTransform drt = dot.AddComponent<RectTransform>();
            drt.anchorMin = new Vector2(0.5f, 0.5f); drt.anchorMax = new Vector2(0.5f, 0.5f);
            drt.pivot     = new Vector2(0.5f, 0.5f);
            drt.sizeDelta = new Vector2(14f, 14f);
            drt.anchoredPosition = new Vector2(startX + i * spacing, 0f);

            Image di = dot.AddComponent<Image>();
            pageDotImgs[i] = di;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // SHOW BEAT & ANIMASI
    // ─────────────────────────────────────────────────────────────────────
    private void ShowBeat(int idx, bool playPageSound = true)
    {
        if (idx < 0 || idx >= beats.Count) return;

        currentBeatIndex = idx;
        StoryBeat beat = beats[idx];

        if (playPageSound) PlayPageSfx();

        // 1. Manhwa Artwork
        if (imgManhwaArt != null && beat.artSprite != null)
        {
            imgManhwaArt.sprite = beat.artSprite;
        }

        // 2. Onomatopoeia & Chapter Tag
        if (txtManhwaSFX != null)
        {
            txtManhwaSFX.text  = beat.onomatopoeia;
            txtManhwaSFX.color = beat.sfxColor;
        }
        if (txtChapterTag != null)
        {
            txtChapterTag.text  = beat.chapterTag;
            txtChapterTag.color = beat.moodColor;
        }

        // 3. Speaker Badge & Subtitle
        if (txtSpeakerBadge != null)
        {
            txtSpeakerBadge.text = $"[ {beat.speaker} ]";
            txtSpeakerBadge.color = Color.white;
            txtSpeakerBadge.outlineWidth = 0.25f;
            txtSpeakerBadge.outlineColor = Color.black;
            imgSpeakerPill.color = beat.moodColor;
        }
        if (txtChapterSubtitle != null)
        {
            txtChapterSubtitle.text = beat.subtitle;
        }

        // 4. Button Next Label & Prev State
        if (txtBtnNextLabel != null) txtBtnNextLabel.text = beat.nextBtnLabel;
        if (btnPrev != null) btnPrev.gameObject.SetActive(idx > 0);

        // 5. Page Counter & Dots
        if (txtPageCounter != null) txtPageCounter.text = $"Halaman {idx + 1} / {beats.Count}";
        UpdateDots(idx);

        // 6. Typewriter Effect
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypewriterRoutine(beat.text, 0.020f));
    }

    private void UpdateDots(int activeIdx)
    {
        if (pageDotImgs == null) return;
        for (int i = 0; i < pageDotImgs.Length; i++)
        {
            if (pageDotImgs[i] == null) continue;
            bool isActive = (i == activeIdx);
            pageDotImgs[i].color = isActive
                ? new Color(1f, 0.85f, 0.35f, 1f)
                : new Color(0.35f, 0.30f, 0.45f, 0.65f);
            pageDotImgs[i].transform.localScale = isActive ? new Vector3(1.3f, 1.3f, 1f) : Vector3.one;
        }
    }

    private IEnumerator TypewriterRoutine(string fullText, float charDelay)
    {
        isTyping = true;
        txtNarrativeDialogue.text = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            if (fullText[i] == '<')
            {
                int close = fullText.IndexOf('>', i);
                if (close > i)
                {
                    i = close;
                    txtNarrativeDialogue.text = fullText.Substring(0, i + 1);
                    continue;
                }
            }

            txtNarrativeDialogue.text = fullText.Substring(0, i + 1);
            yield return new WaitForSeconds(charDelay);
        }

        txtNarrativeDialogue.text = fullText;
        isTyping = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    // NAVIGASI TOMBOL
    // ─────────────────────────────────────────────────────────────────────
    private void OnNextClicked()
    {
        if (isTransitioning) return;

        PlayClickSfx();

        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            isTyping = false;
            txtNarrativeDialogue.text = beats[currentBeatIndex].text;
            return;
        }

        int next = currentBeatIndex + 1;
        if (next >= beats.Count)
        {
            StartCoroutine(FinishStoryAndProceed());
        }
        else
        {
            StartCoroutine(TransitionToBeat(next));
        }
    }

    private void OnPrevClicked()
    {
        if (isTransitioning || currentBeatIndex <= 0) return;
        PlayClickSfx();
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        StartCoroutine(TransitionToBeat(currentBeatIndex - 1));
    }

    private void OnSkipAllClicked()
    {
        if (isTransitioning) return;
        PlayClickSfx();
        StartCoroutine(FinishStoryAndProceed());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            OnNextClicked();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) && currentBeatIndex > 0)
        {
            OnPrevClicked();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnSkipAllClicked();
        }
    }

    private IEnumerator TransitionToBeat(int targetIdx)
    {
        isTransitioning = true;
        fadeOverlay.raycastTarget = true;

        float elapsed = 0f;
        while (elapsed < 0.20f)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 0.85f, elapsed / 0.20f));
            yield return null;
        }

        ShowBeat(targetIdx, true);

        elapsed = 0f;
        while (elapsed < 0.25f)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.85f, 0f, elapsed / 0.25f));
            yield return null;
        }
        fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
        fadeOverlay.raycastTarget = false;
        isTransitioning = false;
    }

    private IEnumerator FinishStoryAndProceed()
    {
        isTransitioning = true;
        fadeOverlay.raycastTarget = true;

        float elapsed = 0f;
        while (elapsed < 0.60f)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 1f, elapsed / 0.60f));
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
        goImage(line, new Color(0.98f, 0.78f, 0.30f, 0.85f));
    }

    private void BuildFrameDiamonds(Transform parent, float w, float h)
    {
        float[] dx = { -w,  w, -w,  w };
        float[] dy = {  h,  h, -h, -h };
        for (int i = 0; i < 4; i++)
        {
            GameObject dia = new GameObject("Diamond_" + i);
            dia.transform.SetParent(parent, false);
            RectTransform drt = dia.AddComponent<RectTransform>();
            drt.anchorMin = new Vector2(0.5f, 0.5f); drt.anchorMax = new Vector2(0.5f, 0.5f);
            drt.pivot     = new Vector2(0.5f, 0.5f);
            drt.sizeDelta = new Vector2(16f, 16f);
            drt.anchoredPosition = new Vector2(dx[i], dy[i]);
            drt.localEulerAngles = new Vector3(0f, 0f, 45f);
            goImage(dia, new Color(0.98f, 0.80f, 0.30f, 0.90f));
        }
    }

    private Image goImage(GameObject go, Color color)
    {
        Image img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private void PlayClickSfx()
    {
        if (sfxClick != null && audioSource != null) audioSource.PlayOneShot(sfxClick);
    }

    private void PlayPageSfx()
    {
        if (sfxPage != null && audioSource != null) audioSource.PlayOneShot(sfxPage, 0.6f);
    }
}
