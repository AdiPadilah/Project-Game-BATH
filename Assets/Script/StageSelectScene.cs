using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Stage Select Scene — membangun seluruh UI secara prosedural.
/// Tema: Dark Fantasy "Born After The End"
/// </summary>
public class StageSelectScene : MonoBehaviour
{
    // ── Asset refs ────────────────────────────────────────────────────────
    private Sprite modalPanelSprite;
    private Sprite btnNormal;
    private Sprite btnHover;
    private Sprite btnPressed;
    private Sprite bgSprite;          // latar dragon dari main menu (opsional)
    private Sprite fireGlowSprite;
    private Sprite screenVignette;
    private TMP_FontAsset tmpFont;
    private AudioClip sfxClick;
    private AudioClip sfxHover;
    private AudioSource audioSource;

    // ── State ─────────────────────────────────────────────────────────────
    private Canvas canvas;
    private Image bgImage;
    private RectTransform bgRect;
    private float bgBaseX, bgBaseY;
    private Image vigImg;
    private float vigBaseAlpha = 0.65f;

    // Kartu stage
    private readonly string[] stageNames = {
        "THE AWAKENING",
        "LEMBAH RERUNTUHAN KUNO",
        "KAWAH MAGMA MEMBARA",
        "TAKHTA KEABADIAN"
    };
    private readonly string[] stageSubNames = {
        "Hutan Terlarang\n& Demon Slime",
        "Ancient Ruins\nChapter I",
        "Inferno Crater\nSegera Hadir",
        "Eternal Throne\nSegera Hadir"
    };
    private readonly bool[] stageLocked   = { false, false, true, true };
    private readonly int[]  stageBuildIdx = { 2, 3, -1, -1 };   // 0=MainMenu,1=StageSelect,2=GamePlay,3=Next

    // Referensi kartu (untuk animasi hover)
    private RectTransform[] cardRTs;
    private Vector2[]        cardBasePos;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        LoadAssets();
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        BuildBackground();
        BuildVignette();
        BuildStagePanel();
        StartCoroutine(IntroAnim());
    }

    // ─────────────────────────────────────────────────────────────────────
    void LoadAssets()
    {
        modalPanelSprite = Resources.Load<Sprite>("UI/panel_fantasy_modal");
        btnNormal        = Resources.Load<Sprite>("UI/btn_fantasy_normal");
        btnHover         = Resources.Load<Sprite>("UI/btn_fantasy_hover");
        btnPressed       = Resources.Load<Sprite>("UI/btn_fantasy_pressed");
        bgSprite         = Resources.Load<Sprite>("UI/menu_background");
        fireGlowSprite   = Resources.Load<Sprite>("UI/fire_glow");
        screenVignette   = Resources.Load<Sprite>("UI/screen_vignette");
        sfxClick         = Resources.Load<AudioClip>("Audio/ui_click");
        sfxHover         = Resources.Load<AudioClip>("Audio/ui_hover");
        tmpFont          = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    // ── Background gelap dengan gambar latar ──────────────────────────────
    void BuildBackground()
    {
        // Fullscreen dark base
        GameObject baseBG = new GameObject("BG_Dark");
        baseBG.transform.SetParent(canvas.transform, false);
        baseBG.transform.SetSiblingIndex(0);
        RectTransform brt = baseBG.AddComponent<RectTransform>();
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
        Image bi = baseBG.AddComponent<Image>();
        bi.color = new Color(0.04f, 0.03f, 0.10f, 1f);
        bi.raycastTarget = false;

        // Gambar latar (pakai bgSprite kalau ada, fallback gradient bawaan)
        if (bgSprite != null)
        {
            GameObject bgGO = new GameObject("BG_Image");
            bgGO.transform.SetParent(canvas.transform, false);
            bgGO.transform.SetSiblingIndex(1);
            bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = new Vector2(-40f, -40f); bgRect.offsetMax = new Vector2(40f, 40f);
            bgImage = bgGO.AddComponent<Image>();
            bgImage.sprite = bgSprite;
            bgImage.color  = new Color(1f, 1f, 1f, 0.35f);
            bgImage.raycastTarget = false;
            bgBaseX = bgRect.anchoredPosition.x;
            bgBaseY = bgRect.anchoredPosition.y;
        }
    }

    // ── Vignette overlay ──────────────────────────────────────────────────
    void BuildVignette()
    {
        if (screenVignette == null) return;
        GameObject vgo = new GameObject("Vignette");
        vgo.transform.SetParent(canvas.transform, false);
        RectTransform vrt = vgo.AddComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
        vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;
        vigImg = vgo.AddComponent<Image>();
        vigImg.sprite = screenVignette;
        vigImg.color  = new Color(1f, 1f, 1f, vigBaseAlpha);
        vigImg.raycastTarget = false;
    }

    // ── Panel Utama Stage Select ──────────────────────────────────────────
    void BuildStagePanel()
    {
        // ── Judul floating di atas panel ──────────────────────────────────
        BuildTitle();

        // ── Panel kartu utama ─────────────────────────────────────────────
        GameObject panelGO = new GameObject("StagePanel");
        panelGO.transform.SetParent(canvas.transform, false);
        panelGO.transform.SetAsLastSibling();

        RectTransform panelRt = panelGO.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot     = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(960f, 500f);
        panelRt.anchoredPosition = new Vector2(0f, -30f);

        Image panelImg = panelGO.AddComponent<Image>();
        if (modalPanelSprite != null)
        { panelImg.sprite = modalPanelSprite; panelImg.type = Image.Type.Sliced; panelImg.color = new Color(1f,1f,1f,0.97f); }
        else panelImg.color = new Color(0.07f, 0.04f, 0.14f, 0.96f);

        // ── 4 kartu stage ─────────────────────────────────────────────────
        int count = stageNames.Length;
        cardRTs = new RectTransform[count];
        cardBasePos = new Vector2[count];

        float spacing = 220f;
        float startX  = -(spacing * (count - 1)) / 2f;

        for (int i = 0; i < count; i++)
        {
            float xPos = startX + i * spacing;
            cardRTs[i] = BuildStageCard(panelGO.transform, i, xPos);
            cardBasePos[i] = cardRTs[i].anchoredPosition;
        }

        // ── Tombol KEMBALI ─────────────────────────────────────────────────
        BuildBackButton(panelGO.transform);

        // ── Dekorasi diamond ornament ──────────────────────────────────────
        BuildDiamonds(panelGO.transform);
    }

    void BuildTitle()
    {
        // Banner latar judul
        GameObject bannerGO = new GameObject("TitleBanner");
        bannerGO.transform.SetParent(canvas.transform, false);
        bannerGO.transform.SetAsLastSibling();

        RectTransform brt = bannerGO.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot     = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(540f, 72f);
        brt.anchoredPosition = new Vector2(0f, 230f);

        Image bi = bannerGO.AddComponent<Image>();
        bi.color = new Color(0.06f, 0.04f, 0.16f, 0.90f);
        if (modalPanelSprite != null) { bi.sprite = modalPanelSprite; bi.type = Image.Type.Sliced; }

        // Garis atas bawah banner
        for (int s = 0; s < 2; s++)
        {
            GameObject line = new GameObject("Line" + s);
            line.transform.SetParent(bannerGO.transform, false);
            RectTransform lrt = line.AddComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, s == 0 ? 1f : 0f);
            lrt.anchorMax = new Vector2(1f, s == 0 ? 1f : 0f);
            lrt.pivot     = new Vector2(0.5f, s == 0 ? 1f : 0f);
            lrt.sizeDelta = new Vector2(0f, 3f);
            lrt.anchoredPosition = Vector2.zero;
            Image li = line.AddComponent<Image>();
            li.color = new Color(0.98f, 0.78f, 0.30f, 0.9f);
        }

        // Teks judul
        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(bannerGO.transform, false);
        RectTransform trt = titleGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        TMP_Text t = titleGO.AddComponent<TextMeshProUGUI>();
        t.text = "[ PILIH STAGE ]";
        t.font = tmpFont;
        t.fontSize = 36f;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(1f, 0.86f, 0.34f, 1f);
        t.characterSpacing = 6f;
        t.enableWordWrapping = false;
    }

    RectTransform BuildStageCard(Transform parent, int idx, float xPos)
    {
        bool locked = stageLocked[idx];

        // ── Root kartu ────────────────────────────────────────────────────
        GameObject card = new GameObject("Stage_" + idx);
        card.transform.SetParent(parent, false);

        RectTransform crt = card.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot     = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(190f, 340f);
        crt.anchoredPosition = new Vector2(xPos, 10f);

        Image cardImg = card.AddComponent<Image>();
        if (locked)
            cardImg.color = new Color(0.08f, 0.07f, 0.17f, 0.88f);
        else
            cardImg.color = new Color(0.09f, 0.06f, 0.20f, 0.97f);

        // ── Accent strip atas (warna berbeda tiap stage) ──────────────────
        Color[] accentColors = {
            new Color(0.20f, 0.80f, 0.40f, 1f),  // hijau – tersedia
            new Color(0.25f, 0.60f, 1.00f, 1f),  // biru  – tersedia
            new Color(0.60f, 0.60f, 0.60f, 0.6f), // abu   – terkunci
            new Color(0.60f, 0.60f, 0.60f, 0.6f)
        };
        Color accent = accentColors[idx];

        GameObject strip = new GameObject("Strip");
        strip.transform.SetParent(card.transform, false);
        RectTransform srt = strip.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 1f); srt.anchorMax = new Vector2(1f, 1f);
        srt.pivot     = new Vector2(0.5f, 1f);
        srt.sizeDelta = new Vector2(0f, 7f);
        srt.anchoredPosition = Vector2.zero;
        strip.AddComponent<Image>().color = accent;

        // ── Nomor stage di kartu ──────────────────────────────────────────
        BuildCardText(card.transform, "NumLabel",
            locked ? "STAGE " + idx : "STAGE " + idx,
            24f, FontStyles.Bold,
            locked ? new Color(0.5f, 0.5f, 0.5f, 0.7f) : accent,
            130f, 40f);

        // ── Icon / simbol ─────────────────────────────────────────────────
        string icon = locked ? "TERKUNCI" : (idx == 0 ? "TUTORIAL" : "TERSEDIA");
        Color iconColor = locked ? new Color(0.45f, 0.45f, 0.45f, 0.7f)
                                 : new Color(0.98f, 0.85f, 0.40f, 1f);
        BuildCardText(card.transform, "IconLabel",
            icon, 14f, FontStyles.Normal,
            iconColor, 90f, 30f);

        // ── Nama stage ────────────────────────────────────────────────────
        BuildCardText(card.transform, "StageName",
            stageNames[idx], 15f, FontStyles.Bold,
            locked ? new Color(0.55f, 0.55f, 0.55f, 0.9f)
                   : new Color(1f, 0.90f, 0.75f, 1f),
            35f, 50f);

        // ── Sub-nama / keterangan ─────────────────────────────────────────
        BuildCardText(card.transform, "SubName",
            stageSubNames[idx], 12f, FontStyles.Normal,
            locked ? new Color(0.45f, 0.45f, 0.45f, 0.7f)
                   : new Color(0.78f, 0.72f, 0.65f, 1f),
            -15f, 80f);

        // ── Tombol MAINKAN atau label TERKUNCI ────────────────────────────
        if (!locked)
        {
            int captureIdx = idx;
            Button btn = BuildFancyBtn(card.transform, "BtnMain", "MAINKAN",
                160f, 46f, 0f, -120f, accent);
            btn.onClick.AddListener(() => OnStageClicked(captureIdx));
        }
        else
        {
            BuildCardText(card.transform, "LockedLbl",
                "SEGERA\nHADIR", 15f, FontStyles.Normal,
                new Color(0.55f, 0.55f, 0.55f, 0.75f),
                -120f, 70f);
        }

        // ── Bottom divider ────────────────────────────────────────────────
        GameObject div = new GameObject("Divider");
        div.transform.SetParent(card.transform, false);
        RectTransform drt = div.AddComponent<RectTransform>();
        drt.anchorMin = new Vector2(0.1f, 0f); drt.anchorMax = new Vector2(0.9f, 0f);
        drt.pivot     = new Vector2(0.5f, 0f);
        drt.sizeDelta = new Vector2(0f, 2f);
        drt.anchoredPosition = new Vector2(0f, 30f);
        div.AddComponent<Image>().color = new Color(accent.r, accent.g, accent.b, 0.4f);

        // Hover effect untuk kartu yang tidak terkunci
        if (!locked) AddCardHover(card);

        return crt;
    }

    void BuildBackButton(Transform parent)
    {
        Button btn = BuildFancyBtn(parent, "BtnBack", "KEMBALI KE MENU",
            260f, 50f, 0f, -210f, new Color(0.9f, 0.75f, 0.30f, 1f));
        btn.onClick.AddListener(GoBackToMenu);
    }

    void BuildDiamonds(Transform parent)
    {
        // 4 ornamen diamond di sudut panel
        float[] dx = { -440f,  440f, -440f,  440f };
        float[] dy = {  220f,  220f, -220f, -220f };
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
            dia.AddComponent<Image>().color = new Color(0.98f, 0.78f, 0.30f, 0.85f);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    TMP_Text BuildCardText(Transform parent, string name, string text,
                           float size, FontStyles style, Color color,
                           float anchoredY, float height)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, 0.5f);
        rt.anchorMax = new Vector2(0.95f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0f, height);
        rt.anchoredPosition = new Vector2(0f, anchoredY);
        TMP_Text t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.font = tmpFont;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color;
        t.enableWordWrapping = true;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    Button BuildFancyBtn(Transform parent, string name, string label,
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
        else img.color = new Color(0.08f, 0.05f, 0.15f, 0.95f);

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
        else
        {
            btn.transition = Selectable.Transition.ColorTint;
            ColorBlock cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(1.3f, 1.1f, 0.65f, 1f);
            cb.pressedColor     = new Color(1.5f, 0.9f, 0.30f, 1f);
            btn.colors = cb;
        }
        btn.onClick.AddListener(PlayClickSfx);

        // Hover sound
        var ev = go.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        var entry = new UnityEngine.EventSystems.EventTrigger.Entry();
        entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        entry.callback.AddListener(_ => PlayHoverSfx());
        ev.triggers.Add(entry);

        // Label teks
        GameObject textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        RectTransform trt = textGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        TMP_Text tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.font = tmpFont;
        tmp.fontSize = 20f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.95f, 0.80f, 1f);
        tmp.characterSpacing = 2f;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;

        return btn;
    }

    // Hover effect ringan pada kartu stage
    void AddCardHover(GameObject card)
    {
        var ev = card.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        var enter = new UnityEngine.EventSystems.EventTrigger.Entry();
        enter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        enter.callback.AddListener(_ => {
            PlayHoverSfx();
            card.GetComponent<RectTransform>().localScale = new Vector3(1.03f, 1.03f, 1f);
        });
        ev.triggers.Add(enter);

        var exit = new UnityEngine.EventSystems.EventTrigger.Entry();
        exit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        exit.callback.AddListener(_ => {
            card.GetComponent<RectTransform>().localScale = Vector3.one;
        });
        ev.triggers.Add(exit);
    }

    // ── Aksi ─────────────────────────────────────────────────────────────

    void OnStageClicked(int idx)
    {
        PlayClickSfx();
        if (idx == 0)
        {
            // Stage 0: Langsung masuk ke Tutorial Gameplay
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

    void GoBackToMenu()
    {
        PlayClickSfx();
        StartCoroutine(LoadSceneByNameRoutine("Main Menu"));
    }

    IEnumerator LoadSceneRoutine(int buildIdx)
    {
        // Fade out singkat sebelum pindah scene
        yield return StartCoroutine(FadeOut());
        SceneManager.LoadScene(buildIdx);
    }

    IEnumerator LoadSceneByNameRoutine(string sceneName)
    {
        yield return StartCoroutine(FadeOut());
        SceneManager.LoadScene(sceneName);
    }

    // ── Animasi Intro (kartu muncul dari bawah) ───────────────────────────
    IEnumerator IntroAnim()
    {
        if (cardRTs == null) yield break;

        // Sembunyikan kartu dulu
        foreach (var rt in cardRTs)
            if (rt != null) rt.anchoredPosition += new Vector2(0f, -60f);

        yield return new WaitForSeconds(0.05f);

        for (int i = 0; i < cardRTs.Length; i++)
        {
            if (cardRTs[i] == null) continue;
            Vector2 target = cardBasePos[i];
            Vector2 start  = cardBasePos[i] - new Vector2(0f, 60f);
            float elapsed = 0f;
            float dur = 0.28f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / dur);
                cardRTs[i].anchoredPosition = Vector2.Lerp(start, target, t);
                yield return null;
            }
            cardRTs[i].anchoredPosition = target;
            yield return new WaitForSeconds(0.05f);
        }
    }

    // ── Fade Out ──────────────────────────────────────────────────────────
    IEnumerator FadeOut()
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
    }

    // ── Update: animasi parallax latar ───────────────────────────────────
    void Update()
    {
        if (bgRect != null && bgImage != null)
        {
            float t = Time.time;
            float mx = (Input.mousePosition.x / Screen.width  - 0.5f) * -24f;
            float my = (Input.mousePosition.y / Screen.height - 0.5f) * -14f;
            bgRect.anchoredPosition = Vector2.Lerp(
                bgRect.anchoredPosition,
                new Vector2(bgBaseX + mx, bgBaseY + my),
                Time.deltaTime * 3f);

            float breathe = Mathf.Sin(t * 0.4f) * 0.5f + 0.5f;
            bgImage.color = new Color(1f, 1f, 1f, 0.28f + breathe * 0.10f);
        }

        if (vigImg != null)
        {
            float pulse = Mathf.Sin(Time.time * 0.5f) * 0.5f + 0.5f;
            vigImg.color = new Color(1f, 1f, 1f, vigBaseAlpha - pulse * 0.07f);
        }
    }

    // ── Suara ─────────────────────────────────────────────────────────────
    void PlayClickSfx()
    {
        if (sfxClick != null && audioSource != null)
            audioSource.PlayOneShot(sfxClick);
    }
    void PlayHoverSfx()
    {
        if (sfxHover != null && audioSource != null)
            audioSource.PlayOneShot(sfxHover, 0.5f);
    }
}
