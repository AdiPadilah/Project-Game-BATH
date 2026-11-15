using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// MainMenuTheme — Memperindah tampilan Main Menu dengan tema Dark Fantasy
/// Tambahkan script ini ke GameObject mana saja di scene Main Menu.
/// Script ini akan otomatis mencari tombol dan menghias UI saat game dimulai.
/// </summary>
public class MainMenuTheme : MonoBehaviour
{
    // ── Warna Tema Dark Fantasy ──────────────────────────────────────────
    static readonly Color PanelBg       = new Color(0.03f, 0.01f, 0.06f, 0.88f);  // ungu-hitam gelap
    static readonly Color PanelEdge     = new Color(0.55f, 0.38f, 0.08f, 0.90f);  // emas tua
    static readonly Color BtnNormal     = new Color(0.07f, 0.03f, 0.10f, 0.82f);  // gelap
    static readonly Color BtnHover      = new Color(0.22f, 0.09f, 0.04f, 0.95f);  // coklat merah membara
    static readonly Color BtnPressed    = new Color(0.40f, 0.15f, 0.02f, 1.00f);  // terang api
    static readonly Color TextGold      = new Color(1.00f, 0.84f, 0.28f, 1.00f);  // emas berkilau
    static readonly Color TextGoldDim   = new Color(0.80f, 0.65f, 0.18f, 1.00f);  // emas redup
    static readonly Color BorderGold    = new Color(0.72f, 0.50f, 0.08f, 1.00f);  // bingkai emas
    static readonly Color BorderGoldBright = new Color(1.00f, 0.90f, 0.40f, 1.00f); // emas cerah
    static readonly Color SeparatorColor = new Color(0.60f, 0.42f, 0.10f, 0.70f); // garis emas
    static readonly Color SubtitleColor  = new Color(0.72f, 0.55f, 0.60f, 0.85f); // merah muda gelap

    // ── State Internal ───────────────────────────────────────────────────
    private Canvas mainCanvas;
    private GameObject menuPanel;
    private List<Button>  styledButtons = new List<Button>();
    private List<Image>   btnImages     = new List<Image>();
    private List<Text>    btnTexts      = new List<Text>();
    private Coroutine     glowRoutine;

    // ── Nama tombol yang dicari ──────────────────────────────────────────
    static readonly string[] ButtonNames = {
        "Play Button", "Options Button", "Quit Button (1)", "Quit Button"
    };
    static readonly string[] ButtonLabels = {
        "⚔  PLAY", "⚙  OPTIONS", "✦  ABOUT", "✕  QUIT"
    };

    // ────────────────────────────────────────────────────────────────────
    void Start()
    {
        mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas == null) { Debug.LogWarning("[MainMenuTheme] Canvas tidak ditemukan."); return; }

        StartCoroutine(BuildTheme());
    }

    IEnumerator BuildTheme()
    {
        yield return null; // tunggu satu frame agar Unity settle

        BuildLeftPanel();
        StyleButtons();
        AddTitleBadge();
        AddVersionText();

        // Animasi entrance: panel + tombol fade-in
        yield return StartCoroutine(EntranceAnimation());

        // Loop glow pada border
        glowRoutine = StartCoroutine(BorderGlowLoop());
    }

    // ── 1. Panel gelap di kiri ───────────────────────────────────────────
    void BuildLeftPanel()
    {
        menuPanel = new GameObject("MenuPanel_Dark");
        menuPanel.transform.SetParent(mainCanvas.transform, false);
        // Taruh di belakang tombol
        menuPanel.transform.SetSiblingIndex(0);

        RectTransform rt = menuPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.sizeDelta = new Vector2(340f, 0f); // lebar panel kiri

        // Latar belakang gelap
        Image bg = menuPanel.AddComponent<Image>();
        bg.color = PanelBg;

        // Garis emas di sisi kanan panel
        AddVerticalBorder(menuPanel.transform, rt.sizeDelta.x);

        // Gradient overlay (sedikit lebih gelap di bawah)
        AddPanelGradient(menuPanel.transform, rt.sizeDelta.x);
    }

    void AddVerticalBorder(Transform parent, float panelWidth)
    {
        GameObject border = new GameObject("PanelBorder");
        border.transform.SetParent(parent, false);

        RectTransform rt = border.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0.05f);
        rt.anchorMax = new Vector2(1f, 0.95f);
        rt.pivot     = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(3f, 0f);
        rt.anchoredPosition = Vector2.zero;

        Image img = border.AddComponent<Image>();
        img.color = PanelEdge;
    }

    void AddPanelGradient(Transform parent, float panelWidth)
    {
        // Dekorasi: garis-garis horizontal tipis
        for (int i = 0; i < 3; i++)
        {
            GameObject line = new GameObject($"Deco_Line_{i}");
            line.transform.SetParent(parent, false);

            RectTransform rt = line.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0f);
            rt.anchorMax = new Vector2(0.90f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            float yPos = 0.15f + i * 0.25f;
            rt.anchorMin = new Vector2(0.05f, yPos - 0.001f);
            rt.anchorMax = new Vector2(0.90f, yPos + 0.001f);
            rt.sizeDelta = Vector2.zero;

            Image img = line.AddComponent<Image>();
            img.color = new Color(SeparatorColor.r, SeparatorColor.g, SeparatorColor.b,
                                   0.15f - i * 0.03f);
        }
    }

    // ── 2. Styling tombol ────────────────────────────────────────────────
    void StyleButtons()
    {
        // Cari semua tombol berdasarkan nama
        Button[] allButtons = FindObjectsOfType<Button>(true);
        var btnMap = new Dictionary<string, Button>();
        foreach (var b in allButtons)
            btnMap[b.gameObject.name] = b;

        for (int i = 0; i < ButtonNames.Length; i++)
        {
            if (!btnMap.TryGetValue(ButtonNames[i], out Button btn)) continue;

            // ── Tambah Image background ke tombol ──
            Image img = btn.GetComponent<Image>();
            if (img == null) img = btn.gameObject.AddComponent<Image>();
            img.color = BtnNormal;
            img.type  = Image.Type.Sliced;

            // ── Set warna transisi hover/press ──
            ColorBlock cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(1f, 0.75f, 0.35f, 1f);
            cb.pressedColor     = new Color(1f, 0.55f, 0.15f, 1f);
            cb.selectedColor    = new Color(1f, 0.80f, 0.40f, 1f);
            cb.fadeDuration     = 0.15f;
            cb.colorMultiplier  = 1f;
            btn.colors = cb;

            // ── Bingkai emas di sekeliling tombol ──
            AddButtonBorder(btn.gameObject);

            // ── Style teks ──
            Text txt = btn.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.text      = (i < ButtonLabels.Length) ? ButtonLabels[i] : txt.text;
                txt.fontSize  = 22;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleLeft;
                txt.color     = TextGold;

                // Shadow untuk kedalaman
                Shadow sh = txt.GetComponent<Shadow>();
                if (sh == null) sh = txt.gameObject.AddComponent<Shadow>();
                sh.effectColor    = new Color(0f, 0f, 0f, 0.8f);
                sh.effectDistance = new Vector2(2f, -2f);

                // Outline tipis
                Outline ol = txt.GetComponent<Outline>();
                if (ol == null) ol = txt.gameObject.AddComponent<Outline>();
                ol.effectColor    = new Color(0.4f, 0.25f, 0f, 0.6f);
                ol.effectDistance = new Vector2(1f, -1f);

                btnTexts.Add(txt);
            }

            // ── Tambah hover listener ──
            AddHoverEffect(btn, img);

            styledButtons.Add(btn);
            btnImages.Add(img);
        }
    }

    void AddButtonBorder(GameObject btnGO)
    {
        // Border luar: 4 garis tipis membentuk kotak
        string[] sides = { "Top", "Bottom", "Left", "Right" };
        foreach (string side in sides)
        {
            GameObject b = new GameObject($"Border_{side}");
            b.transform.SetParent(btnGO.transform, false);
            b.transform.SetSiblingIndex(0);

            RectTransform rt = b.AddComponent<RectTransform>();
            Image img = b.AddComponent<Image>();
            img.color = BorderGold;

            switch (side)
            {
                case "Top":
                    rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.sizeDelta = new Vector2(0f, 2f);
                    rt.anchoredPosition = Vector2.zero;
                    break;
                case "Bottom":
                    rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
                    rt.pivot = new Vector2(0.5f, 0f);
                    rt.sizeDelta = new Vector2(0f, 2f);
                    rt.anchoredPosition = Vector2.zero;
                    break;
                case "Left":
                    rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 0.5f);
                    rt.sizeDelta = new Vector2(2f, 0f);
                    rt.anchoredPosition = Vector2.zero;
                    break;
                case "Right":
                    rt.anchorMin = new Vector2(1f, 0f); rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(1f, 0.5f);
                    rt.sizeDelta = new Vector2(2f, 0f);
                    rt.anchoredPosition = Vector2.zero;
                    break;
            }
        }
    }

    void AddHoverEffect(Button btn, Image bgImage)
    {
        var trigger = btn.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

        // Hover Enter
        var enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((_) =>
        {
            if (bgImage != null)
                StartCoroutine(LerpImageColor(bgImage, BtnHover, 0.12f));
            // Naikkan ukuran sedikit
            StartCoroutine(LerpScale(btn.transform, Vector3.one * 1.04f, 0.1f));
        });
        trigger.triggers.Add(enterEntry);

        // Hover Exit
        var exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((_) =>
        {
            if (bgImage != null)
                StartCoroutine(LerpImageColor(bgImage, BtnNormal, 0.15f));
            StartCoroutine(LerpScale(btn.transform, Vector3.one, 0.12f));
        });
        trigger.triggers.Add(exitEntry);

        // Click press
        var pressEntry = new EventTrigger.Entry();
        pressEntry.eventID = EventTriggerType.PointerDown;
        pressEntry.callback.AddListener((_) =>
        {
            if (bgImage != null)
                StartCoroutine(LerpImageColor(bgImage, BtnPressed, 0.06f));
        });
        trigger.triggers.Add(pressEntry);
    }

    // ── 3. Badge judul kecil di atas tombol ─────────────────────────────
    void AddTitleBadge()
    {
        // Cari parent Main Menu
        Transform mainMenuParent = null;
        foreach (var t in FindObjectsOfType<Transform>(true))
            if (t.gameObject.name == "Main Menu" && t.parent != null)
            { mainMenuParent = t; break; }

        Transform target = mainMenuParent != null ? mainMenuParent : mainCanvas.transform;

        // Label "— MAIN MENU —"
        GameObject label = new GameObject("MenuLabel");
        label.transform.SetParent(target, false);

        RectTransform rt = label.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-270f, 240f);
        rt.sizeDelta = new Vector2(240f, 30f);

        Text txt = label.AddComponent<Text>();
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text      = "― MAIN  MENU ―";
        txt.fontSize  = 14;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color     = SeparatorColor;

        Shadow sh = label.AddComponent<Shadow>();
        sh.effectColor    = new Color(0f, 0f, 0f, 0.9f);
        sh.effectDistance = new Vector2(1f, -1f);

        // Garis ornamen di bawah label
        AddSeparatorLine(target, new Vector2(-270f, 220f), 200f);

        // Subtitle
        GameObject sub = new GameObject("SubtitleLabel");
        sub.transform.SetParent(target, false);

        RectTransform subRT = sub.AddComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.5f, 0.5f);
        subRT.anchorMax = new Vector2(0.5f, 0.5f);
        subRT.pivot     = new Vector2(0.5f, 0.5f);
        subRT.anchoredPosition = new Vector2(-270f, -170f);
        subRT.sizeDelta = new Vector2(240f, 25f);

        Text subTxt = sub.AddComponent<Text>();
        subTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subTxt.text      = "~ Born After The End ~";
        subTxt.fontSize  = 11;
        subTxt.fontStyle = FontStyle.Italic;
        subTxt.alignment = TextAnchor.MiddleCenter;
        subTxt.color     = SubtitleColor;

        AddSeparatorLine(target, new Vector2(-270f, -158f), 160f);
    }

    void AddSeparatorLine(Transform parent, Vector2 pos, float width)
    {
        GameObject sep = new GameObject("Separator");
        sep.transform.SetParent(parent, false);

        RectTransform rt = sep.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, 1.5f);

        Image img = sep.AddComponent<Image>();
        img.color = SeparatorColor;
    }

    void AddVersionText()
    {
        GameObject ver = new GameObject("VersionText");
        ver.transform.SetParent(mainCanvas.transform, false);

        RectTransform rt = ver.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot     = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(10f, 8f);
        rt.sizeDelta = new Vector2(200f, 20f);

        Text txt = ver.AddComponent<Text>();
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text      = "v0.1.0 - Tutorial";
        txt.fontSize  = 10;
        txt.fontStyle = FontStyle.Normal;
        txt.alignment = TextAnchor.LowerLeft;
        txt.color     = new Color(0.6f, 0.5f, 0.4f, 0.5f);
    }

    // ── 4. Animasi Entrance ──────────────────────────────────────────────
    IEnumerator EntranceAnimation()
    {
        // Fade-in panel
        if (menuPanel != null)
        {
            Image bg = menuPanel.GetComponent<Image>();
            if (bg != null)
            {
                Color c = bg.color; c.a = 0f; bg.color = c;
                yield return StartCoroutine(LerpImageAlpha(bg, 0.88f, 0.5f));
            }
        }

        // Slide-in tombol dari kiri satu per satu
        for (int i = 0; i < styledButtons.Count; i++)
        {
            if (styledButtons[i] == null) continue;
            RectTransform rt = styledButtons[i].GetComponent<RectTransform>();
            if (rt == null) continue;

            Vector2 finalPos = rt.anchoredPosition;
            Vector2 startPos = finalPos + new Vector2(-60f, 0f);
            rt.anchoredPosition = startPos;

            // Transparansi awal
            if (btnImages[i] != null)
            {
                Color c = btnImages[i].color; c.a = 0f; btnImages[i].color = c;
            }

            float t = 0f;
            float dur = 0.25f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, t / dur);
                rt.anchoredPosition = Vector2.Lerp(startPos, finalPos, p);
                if (btnImages[i] != null)
                {
                    Color c = btnImages[i].color;
                    c.a = Mathf.Lerp(0f, BtnNormal.a, p);
                    btnImages[i].color = c;
                }
                yield return null;
            }
            rt.anchoredPosition = finalPos;

            yield return new WaitForSeconds(0.07f); // delay antar tombol
        }
    }

    // ── 5. Efek glow border berkedip lembut ─────────────────────────────
    IEnumerator BorderGlowLoop()
    {
        // Ambil semua border image dari tombol
        var borders = new List<Image>();
        foreach (var btn in styledButtons)
        {
            if (btn == null) continue;
            foreach (var img in btn.GetComponentsInChildren<Image>())
                if (img.gameObject.name.StartsWith("Border_"))
                    borders.Add(img);
        }

        while (true)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 0.5f;
                float glow = Mathf.Sin(t * Mathf.PI);
                Color c = Color.Lerp(BorderGold, BorderGoldBright, glow * 0.5f);
                foreach (var img in borders)
                    if (img != null) img.color = c;
                yield return null;
            }
        }
    }

    // ── Helpers Lerp ─────────────────────────────────────────────────────
    IEnumerator LerpImageColor(Image img, Color target, float dur)
    {
        Color start = img.color;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            img.color = Color.Lerp(start, target, t / dur);
            yield return null;
        }
        img.color = target;
    }

    IEnumerator LerpImageAlpha(Image img, float targetAlpha, float dur)
    {
        Color start = img.color;
        Color end   = new Color(start.r, start.g, start.b, targetAlpha);
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            img.color = Color.Lerp(start, end, t / dur);
            yield return null;
        }
        img.color = end;
    }

    IEnumerator LerpScale(Transform tr, Vector3 target, float dur)
    {
        Vector3 start = tr.localScale;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            tr.localScale = Vector3.Lerp(start, target, t / dur);
            yield return null;
        }
        tr.localScale = target;
    }
}
