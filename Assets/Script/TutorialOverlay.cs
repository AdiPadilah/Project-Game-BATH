using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// TutorialOverlay — Sistem panduan interaktif dark fantasy untuk scene GamePlay (Tutorial).
/// Memberikan arahan jelas langkah demi langkah untuk gerakan dasar, serangan kombo,
/// dash menghindar, hingga 3 skill pedang sakti (Phantom Thrust, Cyclone Crescent, Ultimate).
/// </summary>
public class TutorialOverlay : MonoBehaviour
{
    // ── Auto Initialization pada Scene GamePlay ──────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name == "GamePlay")
        {
            EnsureOverlayInstance();
        }

        SceneManager.sceneLoaded += (scene, mode) =>
        {
            if (scene.name == "GamePlay")
            {
                EnsureOverlayInstance();
            }
        };
    }

    private static void EnsureOverlayInstance()
    {
        if (FindFirstObjectByType<TutorialOverlay>() == null)
        {
            GameObject go = new GameObject("TutorialController");
            go.AddComponent<TutorialOverlay>();
        }
    }

    // ── Asset & Style ────────────────────────────────────────────────────
    private Sprite modalPanelSprite;
    private Sprite btnFantasyNormal;
    private TMP_FontAsset tmpFont;
    private AudioClip sfxSuccess;
    private AudioClip sfxClick;
    private AudioSource audioSource;

    // ── UI References ────────────────────────────────────────────────────
    private Canvas targetCanvas;
    private GameObject panelRoot;
    private Image panelBackground;
    private TMP_Text txtCategory;
    private TMP_Text txtTitle;
    private TMP_Text txtKeyBadge;
    private TMP_Text txtDescription;
    private TMP_Text txtProgress;
    private GameObject successBadge;
    private Image successFlashImg;

    // ── Player Reference ─────────────────────────────────────────────────
    private Player player;

    // ── Tutorial Step Data ───────────────────────────────────────────────
    private class StepInfo
    {
        public string category;
        public string title;
        public string keyBadge;
        public string description;
        public KeyCode[] triggerKeys;
        public bool requireHold;
        public float holdTimeRequired;
        public Color badgeColor;
    }

    private StepInfo[] steps;
    private int currentStepIndex = 0;
    private bool isStepCompleted = false;
    private float holdAccumulator = 0f;
    private bool isTutorialFinished = false;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        LoadAssets();
        InitSteps();
    }

    private void LoadAssets()
    {
        modalPanelSprite = Resources.Load<Sprite>("UI/panel_fantasy_modal");
        btnFantasyNormal = Resources.Load<Sprite>("UI/btn_fantasy_normal");
        tmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        sfxSuccess = Resources.Load<AudioClip>("Audio/star_1");
        if (sfxSuccess == null) sfxSuccess = Resources.Load<AudioClip>("Audio/ui_click");
        sfxClick = Resources.Load<AudioClip>("Audio/ui_click");
    }

    private void InitSteps()
    {
        steps = new StepInfo[]
        {
            // Step 1: Gerak Kiri / Kanan
            new StepInfo
            {
                category = "LANGKAH 1 — GERAKAN DASAR",
                title = "BERJALAN MENJELAJAH",
                keyBadge = "[ ← ]  atau  [ → ]",
                description = "Gunakan tombol PANAH KIRI atau PANAH KANAN pada keyboard untuk menggerakkan ksatria ke arah yang dituju.",
                triggerKeys = new KeyCode[] { KeyCode.LeftArrow, KeyCode.RightArrow },
                requireHold = true,
                holdTimeRequired = 0.5f,
                badgeColor = new Color(0.35f, 0.85f, 1.0f, 1f)
            },

            // Step 2: Melompat
            new StepInfo
            {
                category = "LANGKAH 2 — AKROBATIK",
                title = "MELOMPATI RINTANGAN",
                keyBadge = "[ SPASI ]",
                description = "Tekan tombol SPASI untuk melompat melewati platform atau menghindari tebasan bawah musuh.",
                triggerKeys = new KeyCode[] { KeyCode.Space },
                requireHold = false,
                holdTimeRequired = 0f,
                badgeColor = new Color(0.35f, 1.0f, 0.65f, 1f)
            },

            // Step 3: Serangan Dasar & Kombo
            new StepInfo
            {
                category = "LANGKAH 3 — SENI BERPEDANG",
                title = "TEBASAN KOMBO BERUNTUN",
                keyBadge = "[ ↑ ]  Panah Atas",
                description = "Tekan PANAH ATAS untuk menebas pedang! Tekan berkali-kali secara berirama untuk menghasilkan KOMBO 4 TEBASAN!",
                triggerKeys = new KeyCode[] { KeyCode.UpArrow },
                requireHold = false,
                holdTimeRequired = 0f,
                badgeColor = new Color(1.0f, 0.85f, 0.25f, 1f)
            },

            // Step 4: Dash Menghindar
            new StepInfo
            {
                category = "LANGKAH 4 — KELINCAHAN",
                title = "DASH KILAT MENGHINDAR",
                keyBadge = "[ D ] + [ ← / → ]",
                description = "Tekan tombol D bersamaan dengan arah gerak untuk melakukan DASH kilat! Sangat ampuh menembus jebakan dan serangan musuh.",
                triggerKeys = new KeyCode[] { KeyCode.D },
                requireHold = false,
                holdTimeRequired = 0f,
                badgeColor = new Color(0.40f, 0.95f, 0.50f, 1f)
            },

            // Step 5: Skill 1 (Phantom Thrust)
            new StepInfo
            {
                category = "LANGKAH 5 — JURUS PEDANG I",
                title = "PHANTOM THRUST",
                keyBadge = "[ A ]",
                description = "Tekan A untuk melancarkan PHANTOM THRUST! Ksatria menerobos cepat ke depan sambil menebaskan kilatan emas berapi.",
                triggerKeys = new KeyCode[] { KeyCode.A },
                requireHold = false,
                holdTimeRequired = 0f,
                badgeColor = new Color(1.0f, 0.55f, 0.15f, 1f)
            },

            // Step 6: Skill 2 (Cyclone Crescent)
            new StepInfo
            {
                category = "LANGKAH 6 — JURUS PEDANG II",
                title = "CYCLONE CRESCENT",
                keyBadge = "[ S ]",
                description = "Tekan S untuk memutar pedang 360 derajat menciptakan BADAI SABIT EMAS yang melibas semua musuh di sekitarmu!",
                triggerKeys = new KeyCode[] { KeyCode.S },
                requireHold = false,
                holdTimeRequired = 0f,
                badgeColor = new Color(1.0f, 0.40f, 0.20f, 1f)
            },

            // Step 7: Skill 3 (Judgement of the End - Ultimate)
            new StepInfo
            {
                category = "LANGKAH 7 — JURUS PAMUNGKAS (ULTIMATE)",
                title = "JUDGEMENT OF THE END",
                keyBadge = "[ W ]",
                description = "Tekan W untuk membangkitkan ULTIMATE: Badai seribu pedang dimensi yang menghujam musuh tanpa ampun!",
                triggerKeys = new KeyCode[] { KeyCode.W },
                requireHold = false,
                holdTimeRequired = 0f,
                badgeColor = new Color(1.0f, 0.25f, 0.25f, 1f)
            }
        };
    }

    private void Start()
    {
        FindCanvas();
        BuildUI();
        player = FindFirstObjectByType<Player>();
        DisplayCurrentStep();
    }

    private void FindCanvas()
    {
        if (targetCanvas != null) return;

        // Cari Canvas yang sudah ada di GamePlay
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
            GameObject canvasObj = new GameObject("TutorialCanvas");
            targetCanvas = canvasObj.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            targetCanvas.sortingOrder = 95;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
        }
    }

    private void BuildUI()
    {
        if (targetCanvas == null) return;

        // Root Panel di pojok kanan atas (Top-Right) agar tidak menimpa avatar/HP di Top-Left
        panelRoot = new GameObject("TutorialOverlay_Root");
        panelRoot.transform.SetParent(targetCanvas.transform, false);
        panelRoot.transform.SetAsLastSibling();

        RectTransform rootRT = panelRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(1f, 1f);
        rootRT.anchorMax = new Vector2(1f, 1f);
        rootRT.pivot     = new Vector2(1f, 1f);
        rootRT.sizeDelta = new Vector2(450f, 240f);
        rootRT.anchoredPosition = new Vector2(-30f, -30f);

        // Background Ornate Panel
        panelBackground = panelRoot.AddComponent<Image>();
        if (modalPanelSprite != null)
        {
            panelBackground.sprite = modalPanelSprite;
            panelBackground.type   = Image.Type.Sliced;
            panelBackground.color  = new Color(1f, 1f, 1f, 0.96f);
        }
        else
        {
            panelBackground.color = new Color(0.08f, 0.05f, 0.15f, 0.94f);
        }

        // Header Gold Strip
        CreateGoldDivider(panelRoot.transform, 1f, 0f);
        CreateGoldDivider(panelRoot.transform, 0f, 0f);

        // Kategori / Badge Langkah
        txtCategory = CreateTMP(panelRoot.transform, "TxtCategory", 13f, FontStyles.Bold,
            new Color(1f, 0.85f, 0.40f, 1f), TextAlignmentOptions.Left);
        RectTransform catRT = txtCategory.rectTransform;
        catRT.anchorMin = new Vector2(0f, 1f); catRT.anchorMax = new Vector2(1f, 1f);
        catRT.pivot     = new Vector2(0f, 1f);
        catRT.anchoredPosition = new Vector2(25f, -14f);
        catRT.sizeDelta = new Vector2(-50f, 22f);

        // Judul Langkah
        txtTitle = CreateTMP(panelRoot.transform, "TxtTitle", 20f, FontStyles.Bold,
            new Color(1.0f, 0.96f, 0.88f, 1f), TextAlignmentOptions.Left);
        RectTransform titleRT = txtTitle.rectTransform;
        titleRT.anchorMin = new Vector2(0f, 1f); titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot     = new Vector2(0f, 1f);
        titleRT.anchoredPosition = new Vector2(25f, -38f);
        titleRT.sizeDelta = new Vector2(-50f, 28f);

        // Tombol Badge Key (Kotak Highlight Key)
        GameObject badgeBox = new GameObject("KeyBadgeContainer");
        badgeBox.transform.SetParent(panelRoot.transform, false);
        RectTransform badgeBoxRT = badgeBox.AddComponent<RectTransform>();
        badgeBoxRT.anchorMin = new Vector2(0f, 1f); badgeBoxRT.anchorMax = new Vector2(0f, 1f);
        badgeBoxRT.pivot     = new Vector2(0f, 1f);
        badgeBoxRT.anchoredPosition = new Vector2(25f, -70f);
        badgeBoxRT.sizeDelta = new Vector2(220f, 38f);

        Image badgeBoxImg = badgeBox.AddComponent<Image>();
        badgeBoxImg.color = new Color(0.12f, 0.08f, 0.22f, 0.95f);
        if (btnFantasyNormal != null)
        {
            badgeBoxImg.sprite = btnFantasyNormal;
            badgeBoxImg.type = Image.Type.Sliced;
            badgeBoxImg.color = Color.white;
        }

        txtKeyBadge = CreateTMP(badgeBox.transform, "TxtKeyBadge", 18f, FontStyles.Bold,
            new Color(0.35f, 0.85f, 1.0f, 1f), TextAlignmentOptions.Center);
        RectTransform keyRT = txtKeyBadge.rectTransform;
        keyRT.anchorMin = Vector2.zero; keyRT.anchorMax = Vector2.one;
        keyRT.offsetMin = Vector2.zero; keyRT.offsetMax = Vector2.zero;

        // Deskripsi Arahan
        txtDescription = CreateTMP(panelRoot.transform, "TxtDescription", 13.5f, FontStyles.Normal,
            new Color(0.88f, 0.84f, 0.78f, 1f), TextAlignmentOptions.TopLeft);
        RectTransform descRT = txtDescription.rectTransform;
        descRT.anchorMin = new Vector2(0f, 1f); descRT.anchorMax = new Vector2(1f, 1f);
        descRT.pivot     = new Vector2(0f, 1f);
        descRT.anchoredPosition = new Vector2(25f, -118f);
        descRT.sizeDelta = new Vector2(-50f, 75f);

        // Progress Text di bawah (misal: "Langkah 1 dari 7")
        txtProgress = CreateTMP(panelRoot.transform, "TxtProgress", 12f, FontStyles.Normal,
            new Color(0.65f, 0.65f, 0.65f, 0.85f), TextAlignmentOptions.Left);
        RectTransform progRT = txtProgress.rectTransform;
        progRT.anchorMin = new Vector2(0f, 0f); progRT.anchorMax = new Vector2(0f, 0f);
        progRT.pivot     = new Vector2(0f, 0f);
        progRT.anchoredPosition = new Vector2(25f, 12f);
        progRT.sizeDelta = new Vector2(200f, 20f);

        // Tombol LEWATI (SKIP)
        GameObject skipBtnObj = new GameObject("BtnSkipTutorial");
        skipBtnObj.transform.SetParent(panelRoot.transform, false);
        RectTransform skipRT = skipBtnObj.AddComponent<RectTransform>();
        skipRT.anchorMin = new Vector2(1f, 0f); skipRT.anchorMax = new Vector2(1f, 0f);
        skipRT.pivot     = new Vector2(1f, 0f);
        skipRT.anchoredPosition = new Vector2(-22f, 10f);
        skipRT.sizeDelta = new Vector2(110f, 26f);

        Image skipImg = skipBtnObj.AddComponent<Image>();
        skipImg.color = new Color(0.18f, 0.12f, 0.28f, 0.85f);
        Button skipBtn = skipBtnObj.AddComponent<Button>();
        skipBtn.onClick.AddListener(OnSkipClicked);

        TMP_Text skipTxt = CreateTMP(skipBtnObj.transform, "TxtSkip", 11f, FontStyles.Bold,
            new Color(0.85f, 0.75f, 0.40f, 1f), TextAlignmentOptions.Center);
        skipTxt.text = "LEWATI [TAB]";
        skipTxt.rectTransform.anchorMin = Vector2.zero;
        skipTxt.rectTransform.anchorMax = Vector2.one;
        skipTxt.rectTransform.offsetMin = Vector2.zero;
        skipTxt.rectTransform.offsetMax = Vector2.zero;

        // Badge Sukses (Centang Hijau ✔)
        successBadge = new GameObject("SuccessBadge");
        successBadge.transform.SetParent(panelRoot.transform, false);
        RectTransform succRT = successBadge.AddComponent<RectTransform>();
        succRT.anchorMin = new Vector2(1f, 1f); succRT.anchorMax = new Vector2(1f, 1f);
        succRT.pivot     = new Vector2(1f, 1f);
        succRT.anchoredPosition = new Vector2(-25f, -12f);
        succRT.sizeDelta = new Vector2(120f, 32f);

        TMP_Text succTxt = CreateTMP(successBadge.transform, "TxtSucc", 14f, FontStyles.Bold,
            new Color(0.35f, 1.0f, 0.50f, 1f), TextAlignmentOptions.Right);
        succTxt.text = "✔ SELESAI!";
        succTxt.rectTransform.anchorMin = Vector2.zero;
        succTxt.rectTransform.anchorMax = Vector2.one;
        succTxt.rectTransform.offsetMin = Vector2.zero;
        succTxt.rectTransform.offsetMax = Vector2.zero;

        successBadge.SetActive(false);

        // Flash Image saat sukses
        GameObject flashObj = new GameObject("SuccessFlash");
        flashObj.transform.SetParent(panelRoot.transform, false);
        RectTransform flashRT = flashObj.AddComponent<RectTransform>();
        flashRT.anchorMin = Vector2.zero; flashRT.anchorMax = Vector2.one;
        flashRT.offsetMin = Vector2.zero; flashRT.offsetMax = Vector2.zero;

        successFlashImg = flashObj.AddComponent<Image>();
        successFlashImg.color = new Color(0.35f, 1f, 0.5f, 0f);
        successFlashImg.raycastTarget = false;
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

    private void DisplayCurrentStep()
    {
        if (currentStepIndex >= steps.Length)
        {
            StartCoroutine(ShowTutorialCompletionRoutine());
            return;
        }

        StepInfo info = steps[currentStepIndex];
        txtCategory.text = info.category;
        txtTitle.text = info.title;
        txtKeyBadge.text = info.keyBadge;
        txtKeyBadge.color = info.badgeColor;
        txtDescription.text = info.description;
        txtProgress.text = $"Kemajuan: {currentStepIndex + 1} / {steps.Length}";

        isStepCompleted = false;
        holdAccumulator = 0f;
        if (successBadge != null) successBadge.SetActive(false);

        // Jika langkah skill, pastikan player punya cukup mana untuk mencoba
        if (player != null && currentStepIndex >= 4)
        {
            if (player.GetCurrentMana() < 60)
            {
                player.RestoreMana(60);
            }
        }
    }

    private void Update()
    {
        if (isTutorialFinished) return;

        // Tekan TAB untuk melewati tutorial kapan saja
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            OnSkipClicked();
            return;
        }

        if (isStepCompleted || currentStepIndex >= steps.Length) return;

        // Auto-recharge mana jika player sedang di sesi skill tutorial
        if (currentStepIndex >= 4 && player != null && player.GetCurrentMana() < 40)
        {
            player.RestoreMana(50);
        }

        StepInfo current = steps[currentStepIndex];

        bool anyKeyPressed = false;
        bool anyKeyHeld = false;

        foreach (KeyCode k in current.triggerKeys)
        {
            if (Input.GetKeyDown(k)) anyKeyPressed = true;
            if (Input.GetKey(k)) anyKeyHeld = true;
        }

        if (current.requireHold)
        {
            if (anyKeyHeld)
            {
                holdAccumulator += Time.deltaTime;
                if (holdAccumulator >= current.holdTimeRequired)
                {
                    CompleteCurrentStep();
                }
            }
            else
            {
                holdAccumulator = 0f;
            }
        }
        else
        {
            if (anyKeyPressed)
            {
                CompleteCurrentStep();
            }
        }
    }

    private void CompleteCurrentStep()
    {
        if (isStepCompleted) return;
        isStepCompleted = true;

        if (audioSource != null && sfxSuccess != null)
        {
            audioSource.PlayOneShot(sfxSuccess, 0.85f);
        }

        if (successBadge != null) successBadge.SetActive(true);

        StartCoroutine(FlashAndAdvanceRoutine());
    }

    private IEnumerator FlashAndAdvanceRoutine()
    {
        // Flash effect warna hijau berkilau
        if (successFlashImg != null)
        {
            float elapsed = 0f;
            float dur = 0.35f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dur;
                float alpha = Mathf.Sin(t * Mathf.PI) * 0.45f;
                successFlashImg.color = new Color(0.35f, 1.0f, 0.5f, alpha);
                yield return null;
            }
            successFlashImg.color = new Color(0.35f, 1.0f, 0.5f, 0f);
        }

        yield return new WaitForSeconds(0.45f);

        currentStepIndex++;
        DisplayCurrentStep();
    }

    private void OnSkipClicked()
    {
        if (isTutorialFinished) return;
        if (audioSource != null && sfxClick != null) audioSource.PlayOneShot(sfxClick);
        StopAllCoroutines();
        StartCoroutine(ShowTutorialCompletionRoutine());
    }

    private IEnumerator ShowTutorialCompletionRoutine()
    {
        isTutorialFinished = true;

        if (txtCategory != null) txtCategory.text = "✦ PELATIHAN SELESAI ✦";
        if (txtTitle != null) txtTitle.text = "KAU SIAP BERTARUNG!";
        if (txtKeyBadge != null)
        {
            txtKeyBadge.text = "⚔  SALAM PENDEKAR  ⚔";
            txtKeyBadge.color = new Color(1.0f, 0.85f, 0.35f, 1f);
        }
        if (txtDescription != null)
        {
            txtDescription.text = "Selamat, Pejuang! Kamu telah menguasai semua gerakan dan jurus pedang sakti.\nHancurkan rintangan dan taklukkan Raja Monster Slime di ujung hutan!";
        }
        if (txtProgress != null) txtProgress.text = "Semua latihan selesai!";
        if (successBadge != null) successBadge.SetActive(true);

        yield return new WaitForSeconds(3.5f);

        // Fade out lembut panel
        if (panelBackground != null)
        {
            float elapsed = 0f;
            float dur = 0.6f;
            Color c = panelBackground.color;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Lerp(c.a, 0f, elapsed / dur);
                panelBackground.color = new Color(c.r, c.g, c.b, a);
                yield return null;
            }
        }

        if (panelRoot != null) panelRoot.SetActive(false);
    }
}
