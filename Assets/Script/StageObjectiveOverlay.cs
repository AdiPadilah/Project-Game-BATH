using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// StageObjectiveOverlay — Menampilkan Panel Sasaran Misi (Mission Briefing) saat stage dimulai,
/// lalu memunculkan Compact Objective Tracker di layar agar pemain selalu tahu apa yang harus dilakukan.
/// </summary>
public class StageObjectiveOverlay : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name == "Next" || activeScene.name == "GamePlay")
        {
            EnsureObjectiveInstance();
        }

        SceneManager.sceneLoaded += (scene, mode) =>
        {
            if (scene.name == "Next" || scene.name == "GamePlay")
            {
                EnsureObjectiveInstance();
            }
        };
    }

    private static void EnsureObjectiveInstance()
    {
        if (FindFirstObjectByType<StageObjectiveOverlay>() == null)
        {
            GameObject go = new GameObject("StageObjectiveController");
            go.AddComponent<StageObjectiveOverlay>();
        }
    }

    // ── Asset Refs ────────────────────────────────────────────────────────
    private Sprite modalPanelSprite;
    private Sprite btnFantasyNormal;
    private Sprite btnFantasyHover;
    private TMP_FontAsset tmpFont;
    private AudioClip sfxClick;
    private AudioClip sfxObjectiveStart;
    private AudioSource audioSource;

    // ── UI Refs ───────────────────────────────────────────────────────────
    private Canvas targetCanvas;
    private GameObject briefingDimmer;
    private GameObject briefingModal;
    private GameObject compactTrackerRoot;
    private TMP_Text txtTrackerObjective;

    // ── State ─────────────────────────────────────────────────────────────
    private bool isBriefingActive = false;
    private string selectedHeroName = "Kaelen";

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        LoadAssets();
        selectedHeroName = PlayerPrefs.GetString("SelectedCharacterName", "Kaelen");
    }

    private void LoadAssets()
    {
        modalPanelSprite = Resources.Load<Sprite>("UI/panel_fantasy_modal");
        btnFantasyNormal = Resources.Load<Sprite>("UI/btn_fantasy_normal");
        btnFantasyHover  = Resources.Load<Sprite>("UI/btn_fantasy_hover");
        tmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        sfxClick = Resources.Load<AudioClip>("Audio/ui_click");
        sfxObjectiveStart = Resources.Load<AudioClip>("Audio/star_1");
        if (sfxObjectiveStart == null) sfxObjectiveStart = sfxClick;
    }

    private void Start()
    {
        FindCanvas();
        BuildBriefingModal();
        BuildCompactTracker();

        // Tampilkan briefing saat scene baru dimulai
        StartCoroutine(ShowBriefingWithDelay());
    }

    private IEnumerator ShowBriefingWithDelay()
    {
        yield return new WaitForSeconds(0.4f);
        ShowBriefingModal();
    }

    private void FindCanvas()
    {
        if (targetCanvas != null) return;

        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (var c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
            {
                targetCanvas = c;
                break;
            }
        }

        if (targetCanvas == null)
        {
            GameObject canvasObj = new GameObject("ObjectiveCanvas");
            targetCanvas = canvasObj.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            targetCanvas.sortingOrder = 90;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // BANGUN MODAL BRIEFING MISI
    // ─────────────────────────────────────────────────────────────────────
    private void BuildBriefingModal()
    {
        if (targetCanvas == null) return;

        // 1. Fullscreen Dimmer
        briefingDimmer = new GameObject("Briefing_Dimmer");
        briefingDimmer.transform.SetParent(targetCanvas.transform, false);
        briefingDimmer.transform.SetAsLastSibling();

        RectTransform dimRT = briefingDimmer.AddComponent<RectTransform>();
        dimRT.anchorMin = Vector2.zero;
        dimRT.anchorMax = Vector2.one;
        dimRT.offsetMin = Vector2.zero;
        dimRT.offsetMax = Vector2.zero;

        Image dimImg = briefingDimmer.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.75f);

        // 2. Ornate Central Modal Panel (760 x 500)
        briefingModal = new GameObject("Briefing_Modal");
        briefingModal.transform.SetParent(briefingDimmer.transform, false);

        RectTransform modalRT = briefingModal.AddComponent<RectTransform>();
        modalRT.anchorMin = new Vector2(0.5f, 0.5f);
        modalRT.anchorMax = new Vector2(0.5f, 0.5f);
        modalRT.pivot     = new Vector2(0.5f, 0.5f);
        modalRT.sizeDelta = new Vector2(760f, 500f);
        modalRT.anchoredPosition = Vector2.zero;

        Image modalImg = briefingModal.AddComponent<Image>();
        if (modalPanelSprite != null)
        {
            modalImg.sprite = modalPanelSprite;
            modalImg.type   = Image.Type.Sliced;
            modalImg.color  = new Color(1f, 1f, 1f, 0.98f);
        }
        else
        {
            modalImg.color = new Color(0.08f, 0.05f, 0.16f, 0.96f);
        }

        // Gold Dividers
        CreateGoldDivider(briefingModal.transform, 1f, 0f);
        CreateGoldDivider(briefingModal.transform, 0f, 0f);

        // Header Tag (dengan Tingkat Kesulitan)
        string diffTitle = DifficultyManager.GetDifficultyTitle(DifficultyManager.CurrentDifficulty);
        Color diffColor  = DifficultyManager.GetDifficultyColor(DifficultyManager.CurrentDifficulty);

        string stageName  = PlayerPrefs.GetString("TargetStageName", "LEMBAH RERUNTUHAN KUNO");
        string heroName   = PlayerPrefs.GetString("SelectedCharacterName", "Kaelen");
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // Tentukan konten briefing berdasarkan scene aktif
        bool isGamePlay = (currentSceneName == "GamePlay");
        string briefingTitle    = isGamePlay ? "THE AWAKENING" : stageName;
        string narrativeText    = isGamePlay
            ? "\"Ini hanya awal... kekuatan sejati tersimpan di balik kegelapan yang lebih dalam.\"\n— Suara misterius dari balik kabut"
            : "Seorang gadis penyintas bernama <color=#FFD6A5>Lyra</color> telah dikurung di dalam sel besi untuk dijadikan tumbal persembahan Raja Iblis. Bantai semua iblis penjaga dan selamatkan dia sebelum ritual pengorbanan selesai!";

        string[] objectivesList = isGamePlay
            ? new string[] {
                "<b>OBJEKTIF:</b>  Latihan tempur — kuasai semua teknik dasar",
                "<b>OBJEKTIF:</b>  Kalahkan Raja Monster Slime di ujung hutan",
                "<b>TIPS:</b>  Gunakan skill [A], [S], [W] untuk combo mematikan"
              }
            : new string[] {
                "<b>OBJEKTIF UTAMA:</b>  Bantai seluruh Iblis Penjaga di reruntuhan",
                "<b>OBJEKTIF KUNCI:</b>  Rebut Kunci Sel dari Komandan Iblis",
                "<b>OBJEKTIF PENYELAMATAN:</b>  Buka Sel dan selamatkan Gadis Penyintas (Lyra)"
              };

        TMP_Text txtHeader = CreateTMP(briefingModal.transform, "TxtHeader", 15f, FontStyles.Bold,
            diffColor, TextAlignmentOptions.Center);
        RectTransform hRT = txtHeader.rectTransform;
        hRT.anchorMin = new Vector2(0f, 1f); hRT.anchorMax = new Vector2(1f, 1f);
        hRT.pivot     = new Vector2(0.5f, 1f);
        hRT.anchoredPosition = new Vector2(0f, -22f);
        hRT.sizeDelta = new Vector2(0f, 26f);
        txtHeader.text = $"✦ SASARAN MISI: {briefingTitle.ToUpper()}  •  [{diffTitle}] ✦";

        // Stage Title
        TMP_Text txtTitle = CreateTMP(briefingModal.transform, "TxtTitle", 28f, FontStyles.Bold,
            new Color(1f, 0.96f, 0.88f, 1f), TextAlignmentOptions.Center);
        RectTransform titleRT = txtTitle.rectTransform;
        titleRT.anchorMin = new Vector2(0f, 1f); titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot     = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -54f);
        titleRT.sizeDelta = new Vector2(-60f, 38f);
        txtTitle.text = briefingTitle;

        // Hero Info Badge
        TMP_Text txtHeroInfo = CreateTMP(briefingModal.transform, "TxtHeroInfo", 14f, FontStyles.Bold,
            new Color(0.35f, 0.85f, 1.0f, 1f), TextAlignmentOptions.Center);
        RectTransform heroRT = txtHeroInfo.rectTransform;
        heroRT.anchorMin = new Vector2(0f, 1f); heroRT.anchorMax = new Vector2(1f, 1f);
        heroRT.pivot     = new Vector2(0.5f, 1f);
        heroRT.anchoredPosition = new Vector2(0f, -94f);
        heroRT.sizeDelta = new Vector2(0f, 24f);
        txtHeroInfo.text = $"[ KESATRIA TERPILIH: {selectedHeroName.ToUpper()} ]";

        // Narrative / Sinopsis Box
        GameObject storyBox = new GameObject("StoryBox");
        storyBox.transform.SetParent(briefingModal.transform, false);
        RectTransform sbRT = storyBox.AddComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(0.5f, 0.5f);
        sbRT.anchorMax = new Vector2(0.5f, 0.5f);
        sbRT.pivot     = new Vector2(0.5f, 0.5f);
        sbRT.anchoredPosition = new Vector2(0f, 65f);
        sbRT.sizeDelta = new Vector2(680f, 72f);

        Image sbImg = storyBox.AddComponent<Image>();
        sbImg.color = new Color(0.12f, 0.08f, 0.20f, 0.85f);
        if (btnFantasyNormal != null) { sbImg.sprite = btnFantasyNormal; sbImg.type = Image.Type.Sliced; }

        TMP_Text txtStory = CreateTMP(storyBox.transform, "TxtStory", 13.5f, FontStyles.Normal,
            new Color(0.92f, 0.86f, 0.80f, 1f), TextAlignmentOptions.Center);
        txtStory.rectTransform.anchorMin = Vector2.zero; txtStory.rectTransform.anchorMax = Vector2.one;
        txtStory.rectTransform.offsetMin = new Vector2(16f, 8f); txtStory.rectTransform.offsetMax = new Vector2(-16f, -8f);
        txtStory.richText = true;
        txtStory.text = narrativeText;

        // Objectives List Box
        GameObject objListBox = new GameObject("ObjectivesListBox");
        objListBox.transform.SetParent(briefingModal.transform, false);
        RectTransform olbRT = objListBox.AddComponent<RectTransform>();
        olbRT.anchorMin = new Vector2(0.5f, 0.5f);
        olbRT.anchorMax = new Vector2(0.5f, 0.5f);
        olbRT.pivot     = new Vector2(0.5f, 0.5f);
        olbRT.anchoredPosition = new Vector2(0f, -45f);
        olbRT.sizeDelta = new Vector2(680f, 120f);

        string[] objectives = objectivesList;

        for (int i = 0; i < objectives.Length; i++)
        {
            GameObject row = new GameObject("ObjRow_" + i);
            row.transform.SetParent(objListBox.transform, false);
            RectTransform rowRT = row.AddComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0f, 1f); rowRT.anchorMax = new Vector2(1f, 1f);
            rowRT.pivot     = new Vector2(0.5f, 1f);
            rowRT.anchoredPosition = new Vector2(0f, -i * 38f);
            rowRT.sizeDelta = new Vector2(0f, 34f);

            Image rowBg = row.AddComponent<Image>();
            rowBg.color = new Color(0.14f, 0.10f, 0.24f, 0.80f);

            TMP_Text rowTxt = CreateTMP(row.transform, "TxtRow", 14f, FontStyles.Normal,
                new Color(0.95f, 0.90f, 0.82f, 1f), TextAlignmentOptions.Left);
            rowTxt.rectTransform.anchorMin = Vector2.zero; rowTxt.rectTransform.anchorMax = Vector2.one;
            rowTxt.rectTransform.offsetMin = new Vector2(20f, 0f); rowTxt.rectTransform.offsetMax = new Vector2(-20f, 0f);
            rowTxt.text = objectives[i];
        }

        // Tombol MULAI MISI (Start Mission)
        GameObject btnStartObj = new GameObject("BtnStartMission");
        btnStartObj.transform.SetParent(briefingModal.transform, false);
        RectTransform bRT = btnStartObj.AddComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.5f, 0f); bRT.anchorMax = new Vector2(0.5f, 0f);
        bRT.pivot     = new Vector2(0.5f, 0f);
        bRT.anchoredPosition = new Vector2(0f, 25f);
        bRT.sizeDelta = new Vector2(280f, 52f);

        Image btnImg = btnStartObj.AddComponent<Image>();
        if (btnFantasyNormal != null)
        {
            btnImg.sprite = btnFantasyNormal;
            btnImg.type   = Image.Type.Sliced;
            btnImg.color  = Color.white;
        }
        else
        {
            btnImg.color = new Color(0.18f, 0.12f, 0.30f, 0.95f);
        }

        Button btn = btnStartObj.AddComponent<Button>();
        if (btnFantasyHover != null)
        {
            btn.transition = Selectable.Transition.SpriteSwap;
            SpriteState ss = new SpriteState();
            ss.highlightedSprite = btnFantasyHover;
            ss.pressedSprite     = btnFantasyHover;
            ss.selectedSprite    = btnFantasyHover;
            btn.spriteState = ss;
        }
        btn.onClick.AddListener(OnStartMissionClicked);

        TMP_Text btnTxt = CreateTMP(btnStartObj.transform, "TxtBtn", 20f, FontStyles.Bold,
            new Color(1f, 0.92f, 0.45f, 1f), TextAlignmentOptions.Center);
        btnTxt.characterSpacing = 2f;
        btnTxt.rectTransform.anchorMin = Vector2.zero; btnTxt.rectTransform.anchorMax = Vector2.one;
        btnTxt.rectTransform.offsetMin = Vector2.zero; btnTxt.rectTransform.offsetMax = Vector2.zero;
        btnTxt.text = "▶  MULAI MISI  [ENTER]";

        briefingDimmer.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    // BANGUN COMPACT OBJECTIVE TRACKER (HUD KANAN ATAS)
    // ─────────────────────────────────────────────────────────────────────
    private void BuildCompactTracker()
    {
        if (targetCanvas == null) return;

        compactTrackerRoot = new GameObject("CompactObjectiveTracker");
        compactTrackerRoot.transform.SetParent(targetCanvas.transform, false);
        compactTrackerRoot.transform.SetAsLastSibling();

        RectTransform trkRT = compactTrackerRoot.AddComponent<RectTransform>();
        trkRT.anchorMin = new Vector2(1f, 1f);
        trkRT.anchorMax = new Vector2(1f, 1f);
        trkRT.pivot     = new Vector2(1f, 1f);
        trkRT.anchoredPosition = new Vector2(-25f, -25f);
        trkRT.sizeDelta = new Vector2(380f, 85f);

        Image trkBg = compactTrackerRoot.AddComponent<Image>();
        if (modalPanelSprite != null)
        {
            trkBg.sprite = modalPanelSprite;
            trkBg.type   = Image.Type.Sliced;
            trkBg.color  = new Color(1f, 1f, 1f, 0.92f);
        }
        else
        {
            trkBg.color = new Color(0.08f, 0.05f, 0.16f, 0.88f);
        }

        // Header Tracker
        TMP_Text trkHeader = CreateTMP(compactTrackerRoot.transform, "TrkHeader", 11.5f, FontStyles.Bold,
            new Color(1f, 0.82f, 0.35f, 1f), TextAlignmentOptions.Left);
        trkHeader.rectTransform.anchorMin = new Vector2(0f, 1f); trkHeader.rectTransform.anchorMax = new Vector2(1f, 1f);
        trkHeader.rectTransform.pivot     = new Vector2(0f, 1f);
        trkHeader.rectTransform.anchoredPosition = new Vector2(18f, -10f);
        trkHeader.rectTransform.sizeDelta = new Vector2(-36f, 20f);
        trkHeader.text = "✦ SASARAN MISI AKTIF ✦";

        // Teks Objektif Aktif
        txtTrackerObjective = CreateTMP(compactTrackerRoot.transform, "TxtTrkObj", 13.5f, FontStyles.Normal,
            new Color(0.95f, 0.92f, 0.85f, 1f), TextAlignmentOptions.Left);
        txtTrackerObjective.rectTransform.anchorMin = new Vector2(0f, 0f); txtTrackerObjective.rectTransform.anchorMax = new Vector2(1f, 1f);
        txtTrackerObjective.rectTransform.pivot     = new Vector2(0.5f, 0.5f);
        txtTrackerObjective.rectTransform.offsetMin = new Vector2(18f, 10f);
        txtTrackerObjective.rectTransform.offsetMax = new Vector2(-18f, -28f);
        bool isGamePlay = (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "GamePlay");
        txtTrackerObjective.text = isGamePlay
            ? "• Kuasai kombo & jurus pedang\n• Taklukkan Raja Monster Slime"
            : "• Selamatkan Penyintas dari Belenggu Iblis\n• Musnahkan Pasukan Penjaga Reruntuhan";

        compactTrackerRoot.SetActive(false);
    }

    private void CreateGoldDivider(Transform parent, float anchorY, float posY)
    {
        GameObject line = new GameObject("GoldDivider");
        line.transform.SetParent(parent, false);
        RectTransform rt = line.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, anchorY);
        rt.anchorMax = new Vector2(1f, anchorY);
        rt.pivot     = new Vector2(0.5f, anchorY);
        rt.sizeDelta = new Vector2(0f, 4f);
        rt.anchoredPosition = new Vector2(0f, posY);

        Image img = line.AddComponent<Image>();
        img.color = new Color(0.98f, 0.78f, 0.30f, 0.90f);
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
    // BRIEFING FLOW
    // ─────────────────────────────────────────────────────────────────────
    private void ShowBriefingModal()
    {
        isBriefingActive = true;
        Time.timeScale = 0f; // Freeze game saat briefing sasaran misi

        if (briefingDimmer != null) briefingDimmer.SetActive(true);
        if (compactTrackerRoot != null) compactTrackerRoot.SetActive(false);
    }

    private void Update()
    {
        if (isBriefingActive)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                OnStartMissionClicked();
            }
        }
    }

    private void OnStartMissionClicked()
    {
        if (!isBriefingActive) return;
        isBriefingActive = false;

        if (audioSource != null && sfxObjectiveStart != null)
        {
            audioSource.PlayOneShot(sfxObjectiveStart, 1.0f);
        }

        // Unfreeze gameplay
        Time.timeScale = 1.0f;

        StartCoroutine(CloseBriefingRoutine());
    }

    private IEnumerator CloseBriefingRoutine()
    {
        if (briefingDimmer != null)
        {
            float elapsed = 0f;
            float dur = 0.35f;
            Image dimImg = briefingDimmer.GetComponent<Image>();
            Color baseC = dimImg != null ? dimImg.color : Color.black;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Lerp(baseC.a, 0f, elapsed / dur);
                if (dimImg != null) dimImg.color = new Color(baseC.r, baseC.g, baseC.b, a);
                yield return null;
            }

            briefingDimmer.SetActive(false);
        }

        // Tampilkan compact tracker di kanan atas
        if (compactTrackerRoot != null)
        {
            compactTrackerRoot.SetActive(true);
        }
    }
}
