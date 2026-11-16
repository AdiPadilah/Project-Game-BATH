using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// TutorialOverlay — Sistem panduan interaktif Full-Modal Freeze-Frame untuk scene GamePlay (Tutorial).
/// - Game FREEZE (Time.timeScale = 0) saat arahan muncul.
/// - Menampilkan Panel Besar di tengah layar dengan highlight tombol yang harus ditekan.
/// - Saat tombol ditekan, game MULAI LAGI (Time.timeScale = 1) dan jurus/gerakan langsung dieksekusi.
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
    private Sprite btnFantasyHover;
    private TMP_FontAsset tmpFont;
    private AudioClip sfxSuccess;
    private AudioClip sfxClick;
    private AudioSource audioSource;

    // ── UI References ────────────────────────────────────────────────────
    private Canvas targetCanvas;
    private GameObject dimmerRoot;
    private Image dimmerImage;
    private GameObject modalRoot;
    private RectTransform modalRT;
    private Image modalBackground;

    private TMP_Text txtCategory;
    private TMP_Text txtTitle;
    private GameObject keyBadgeContainer;
    private Image keyBadgeBg;
    private TMP_Text txtKeyBadge;
    private TMP_Text txtPressPrompt;
    private TMP_Text txtDescription;
    private TMP_Text txtStepCounter;

    // Compact In-Action Indicator (saat unfreeze)
    private GameObject liveIndicatorRoot;
    private TMP_Text txtLiveIndicator;

    // ── Player References ────────────────────────────────────────────────
    private Player player;
    private PlayerSkills playerSkills;

    // ── Tutorial Step Data ───────────────────────────────────────────────
    private enum ActionType
    {
        Walk,
        Jump,
        Attack,
        Dash,
        Skill1,
        Skill2,
        Skill3
    }

    private class StepInfo
    {
        public ActionType action;
        public string category;
        public string title;
        public string keyBadge;
        public string description;
        public KeyCode[] triggerKeys;
        public Color badgeColor;
        public float practiceDuration; // Waktu gerak aktif sebelum step berikutnya
    }

    private StepInfo[] steps;
    private int currentStepIndex = 0;
    private bool isWaitingForPlayerInput = false;
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
        btnFantasyHover  = Resources.Load<Sprite>("UI/btn_fantasy_hover");
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
                action = ActionType.Walk,
                category = "✦ TAHAP 1 DARI 7 — GERAKAN DASAR ✦",
                title = "BERJALAN MENJELAJAH HUTAN",
                keyBadge = "[ ← ]   atau   [ → ]",
                description = "Gunakan tombol PANAH KIRI atau PANAH KANAN untuk menggerakkan ksatria menjelajahi reruntuhan dan menghindari jebakan.",
                triggerKeys = new KeyCode[] { KeyCode.LeftArrow, KeyCode.RightArrow },
                badgeColor = new Color(0.35f, 0.85f, 1.0f, 1f),
                practiceDuration = 2.4f
            },

            // Step 2: Melompat
            new StepInfo
            {
                action = ActionType.Jump,
                category = "✦ TAHAP 2 DARI 7 — AKROBATIK ✦",
                title = "MELOMPATI PLATFORM & RINTANGAN",
                keyBadge = "[ SPASI ]",
                description = "Tekan tombol SPASI untuk melompat tinggi melewati platform berundak atau melompati tebasan bawah musuh.",
                triggerKeys = new KeyCode[] { KeyCode.Space },
                badgeColor = new Color(0.35f, 1.0f, 0.65f, 1f),
                practiceDuration = 2.0f
            },

            // Step 3: Serangan Dasar & Kombo
            new StepInfo
            {
                action = ActionType.Attack,
                category = "✦ TAHAP 3 DARI 7 — SENI PEDANG ✦",
                title = "TEBASAN PEDANG & KOMBO BERUNTUN",
                keyBadge = "[ ↑ ]  (Panah Atas)",
                description = "Tekan PANAH ATAS untuk menebaskan pedang ksatria! Tekan berkali-kali secara berirama untuk menghasilkan KOMBO 4 TEBASAN!",
                triggerKeys = new KeyCode[] { KeyCode.UpArrow },
                badgeColor = new Color(1.0f, 0.85f, 0.25f, 1f),
                practiceDuration = 2.2f
            },

            // Step 4: Dash Menghindar
            new StepInfo
            {
                action = ActionType.Dash,
                category = "✦ TAHAP 4 DARI 7 — KELINCAHAN ✦",
                title = "DASH KILAT MENGHINDAR",
                keyBadge = "[ D ]",
                description = "Tekan tombol D untuk melakukan DASH kilat ke depan! Ksatria menjadi kebal sesaat dan dapat menembus serangan lawan.",
                triggerKeys = new KeyCode[] { KeyCode.D },
                badgeColor = new Color(0.40f, 0.95f, 0.50f, 1f),
                practiceDuration = 2.0f
            },

            // Step 5: Skill 1 (Phantom Thrust)
            new StepInfo
            {
                action = ActionType.Skill1,
                category = "✦ TAHAP 5 DARI 7 — JURUS PEDANG I ✦",
                title = "SKILL 1: PHANTOM THRUST",
                keyBadge = "[ A ]",
                description = "Tekan A untuk melancarkan PHANTOM THRUST! Ksatria menerobos cepat dengan kilatan pedang emas berapi yang menembus barisan musuh.",
                triggerKeys = new KeyCode[] { KeyCode.A },
                badgeColor = new Color(1.0f, 0.55f, 0.15f, 1f),
                practiceDuration = 2.5f
            },

            // Step 6: Skill 2 (Cyclone Crescent)
            new StepInfo
            {
                action = ActionType.Skill2,
                category = "✦ TAHAP 6 DARI 7 — JURUS PEDANG II ✦",
                title = "SKILL 2: CYCLONE CRESCENT",
                keyBadge = "[ S ]",
                description = "Tekan S untuk memutar pedang 360 derajat menciptakan BADAI SABIT EMAS yang melibas dan memukul mundur seluruh musuh di sekitarmu!",
                triggerKeys = new KeyCode[] { KeyCode.S },
                badgeColor = new Color(1.0f, 0.40f, 0.20f, 1f),
                practiceDuration = 2.5f
            },

            // Step 7: Skill 3 (Judgement of the End - Ultimate)
            new StepInfo
            {
                action = ActionType.Skill3,
                category = "✦ TAHAP 7 DARI 7 — JURUS PAMUNGKAS (ULTIMATE) ✦",
                title = "ULTIMATE: JUDGEMENT OF THE END",
                keyBadge = "[ W ]",
                description = "Tekan W untuk membangkitkan jurus terkuat: Menghentikan waktu sesaat dan menghujani seluruh arena dengan Badai Seribu Pedang Dimensi!",
                triggerKeys = new KeyCode[] { KeyCode.W },
                badgeColor = new Color(1.0f, 0.25f, 0.25f, 1f),
                practiceDuration = 3.8f
            }
        };
    }

    private void Start()
    {
        FindCanvas();
        BuildUI();
        player = FindFirstObjectByType<Player>();
        playerSkills = FindFirstObjectByType<PlayerSkills>();

        // Beri jeda 0.8 detik di awal game sebelum freeze step 1 agar player melihat arena
        StartCoroutine(InitialDelayRoutine());
    }

    private IEnumerator InitialDelayRoutine()
    {
        yield return new WaitForSeconds(0.8f);
        StartTutorialStep(0);
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

        // 1. Fullscreen Dimmer Backdrop (Cinema Freeze)
        dimmerRoot = new GameObject("Tutorial_Dimmer");
        dimmerRoot.transform.SetParent(targetCanvas.transform, false);
        dimmerRoot.transform.SetAsLastSibling();

        RectTransform dimmerRT = dimmerRoot.AddComponent<RectTransform>();
        dimmerRT.anchorMin = Vector2.zero;
        dimmerRT.anchorMax = Vector2.one;
        dimmerRT.offsetMin = Vector2.zero;
        dimmerRT.offsetMax = Vector2.zero;

        dimmerImage = dimmerRoot.AddComponent<Image>();
        dimmerImage.color = new Color(0f, 0f, 0f, 0.72f);
        dimmerImage.raycastTarget = true;

        // 2. Large Central Modal Window (760 x 480)
        modalRoot = new GameObject("Tutorial_Modal");
        modalRoot.transform.SetParent(dimmerRoot.transform, false);

        modalRT = modalRoot.AddComponent<RectTransform>();
        modalRT.anchorMin = new Vector2(0.5f, 0.5f);
        modalRT.anchorMax = new Vector2(0.5f, 0.5f);
        modalRT.pivot     = new Vector2(0.5f, 0.5f);
        modalRT.sizeDelta = new Vector2(760f, 480f);
        modalRT.anchoredPosition = new Vector2(0f, 0f);

        modalBackground = modalRoot.AddComponent<Image>();
        if (modalPanelSprite != null)
        {
            modalBackground.sprite = modalPanelSprite;
            modalBackground.type   = Image.Type.Sliced;
            modalBackground.color  = new Color(1f, 1f, 1f, 0.98f);
        }
        else
        {
            modalBackground.color = new Color(0.08f, 0.05f, 0.16f, 0.96f);
        }

        // Gold Lines (Top & Bottom of Modal)
        CreateGoldDivider(modalRoot.transform, 1f, 0f);
        CreateGoldDivider(modalRoot.transform, 0f, 0f);

        // Category Tag (Atas)
        txtCategory = CreateTMP(modalRoot.transform, "TxtCategory", 15f, FontStyles.Bold,
            new Color(1f, 0.85f, 0.35f, 1f), TextAlignmentOptions.Center);
        RectTransform catRT = txtCategory.rectTransform;
        catRT.anchorMin = new Vector2(0f, 1f); catRT.anchorMax = new Vector2(1f, 1f);
        catRT.pivot     = new Vector2(0.5f, 1f);
        catRT.anchoredPosition = new Vector2(0f, -22f);
        catRT.sizeDelta = new Vector2(0f, 26f);

        // Title (Besar & Tegas)
        txtTitle = CreateTMP(modalRoot.transform, "TxtTitle", 28f, FontStyles.Bold,
            new Color(1f, 0.96f, 0.88f, 1f), TextAlignmentOptions.Center);
        RectTransform titleRT = txtTitle.rectTransform;
        titleRT.anchorMin = new Vector2(0f, 1f); titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot     = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -54f);
        titleRT.sizeDelta = new Vector2(-60f, 40f);

        // ── GIANT GLOWING KEY BADGE (Tengah) ──────────────────────────────────
        keyBadgeContainer = new GameObject("KeyBadgeContainer");
        keyBadgeContainer.transform.SetParent(modalRoot.transform, false);

        RectTransform badgeRT = keyBadgeContainer.AddComponent<RectTransform>();
        badgeRT.anchorMin = new Vector2(0.5f, 0.5f);
        badgeRT.anchorMax = new Vector2(0.5f, 0.5f);
        badgeRT.pivot     = new Vector2(0.5f, 0.5f);
        badgeRT.anchoredPosition = new Vector2(0f, 35f);
        badgeRT.sizeDelta = new Vector2(360f, 96f);

        keyBadgeBg = keyBadgeContainer.AddComponent<Image>();
        if (btnFantasyNormal != null)
        {
            keyBadgeBg.sprite = btnFantasyNormal;
            keyBadgeBg.type   = Image.Type.Sliced;
            keyBadgeBg.color  = Color.white;
        }
        else
        {
            keyBadgeBg.color = new Color(0.14f, 0.10f, 0.25f, 0.95f);
        }

        // Teks Tombol (Ukuran 38pt, sangat jelas!)
        txtKeyBadge = CreateTMP(keyBadgeContainer.transform, "TxtKeyBadge", 38f, FontStyles.Bold,
            new Color(0.35f, 0.90f, 1.0f, 1f), TextAlignmentOptions.Center);
        txtKeyBadge.characterSpacing = 3f;
        RectTransform keyRT = txtKeyBadge.rectTransform;
        keyRT.anchorMin = Vector2.zero; keyRT.anchorMax = Vector2.one;
        keyRT.offsetMin = Vector2.zero; keyRT.offsetMax = Vector2.zero;

        // Button Component di badge agar bisa juga diklik
        Button badgeBtn = keyBadgeContainer.AddComponent<Button>();
        badgeBtn.onClick.AddListener(OnBadgeOrKeyTriggered);

        // Prompt teks berkedip di bawah tombol
        txtPressPrompt = CreateTMP(modalRoot.transform, "TxtPressPrompt", 15f, FontStyles.Bold,
            new Color(1f, 0.88f, 0.40f, 1f), TextAlignmentOptions.Center);
        RectTransform promptRT = txtPressPrompt.rectTransform;
        promptRT.anchorMin = new Vector2(0f, 0.5f); promptRT.anchorMax = new Vector2(1f, 0.5f);
        promptRT.pivot     = new Vector2(0.5f, 0.5f);
        promptRT.anchoredPosition = new Vector2(0f, -32f);
        promptRT.sizeDelta = new Vector2(0f, 26f);
        txtPressPrompt.text = "⚡ TEKAN TOMBOL DI ATAS UNTUK MEMULAI KEMBALI ⚡";

        // Deskripsi Arahan (Kotak Teks di bagian bawah)
        txtDescription = CreateTMP(modalRoot.transform, "TxtDescription", 17f, FontStyles.Normal,
            new Color(0.90f, 0.86f, 0.80f, 1f), TextAlignmentOptions.Center);
        RectTransform descRT = txtDescription.rectTransform;
        descRT.anchorMin = new Vector2(0f, 0f); descRT.anchorMax = new Vector2(1f, 0f);
        descRT.pivot     = new Vector2(0.5f, 0f);
        descRT.anchoredPosition = new Vector2(0f, 55f);
        descRT.sizeDelta = new Vector2(660f, 75f);

        // Step Counter / Hint
        txtStepCounter = CreateTMP(modalRoot.transform, "TxtStepCounter", 13f, FontStyles.Normal,
            new Color(0.65f, 0.65f, 0.65f, 0.85f), TextAlignmentOptions.Left);
        RectTransform stepRT = txtStepCounter.rectTransform;
        stepRT.anchorMin = new Vector2(0f, 0f); stepRT.anchorMax = new Vector2(0f, 0f);
        stepRT.pivot     = new Vector2(0f, 0f);
        stepRT.anchoredPosition = new Vector2(30f, 16f);
        stepRT.sizeDelta = new Vector2(300f, 24f);

        // Tombol LEWATI (TAB) di pojok kanan bawah modal
        GameObject skipBtnObj = new GameObject("BtnSkip");
        skipBtnObj.transform.SetParent(modalRoot.transform, false);
        RectTransform skipRT = skipBtnObj.AddComponent<RectTransform>();
        skipRT.anchorMin = new Vector2(1f, 0f); skipRT.anchorMax = new Vector2(1f, 0f);
        skipRT.pivot     = new Vector2(1f, 0f);
        skipRT.anchoredPosition = new Vector2(-28f, 15f);
        skipRT.sizeDelta = new Vector2(140f, 32f);

        Image skipImg = skipBtnObj.AddComponent<Image>();
        skipImg.color = new Color(0.18f, 0.12f, 0.28f, 0.90f);
        if (btnFantasyNormal != null) { skipImg.sprite = btnFantasyNormal; skipImg.type = Image.Type.Sliced; }

        Button skipBtn = skipBtnObj.AddComponent<Button>();
        skipBtn.onClick.AddListener(OnSkipTutorialClicked);

        TMP_Text skipTxt = CreateTMP(skipBtnObj.transform, "TxtSkip", 12f, FontStyles.Bold,
            new Color(0.90f, 0.80f, 0.45f, 1f), TextAlignmentOptions.Center);
        skipTxt.text = "LEWATI [TAB]";
        skipTxt.rectTransform.anchorMin = Vector2.zero;
        skipTxt.rectTransform.anchorMax = Vector2.one;
        skipTxt.rectTransform.offsetMin = Vector2.zero;
        skipTxt.rectTransform.offsetMax = Vector2.zero;

        // 3. Compact Floating Indicator (aktif saat unfreeze latihan)
        liveIndicatorRoot = new GameObject("Tutorial_LiveIndicator");
        liveIndicatorRoot.transform.SetParent(targetCanvas.transform, false);
        liveIndicatorRoot.transform.SetAsLastSibling();

        RectTransform liveRT = liveIndicatorRoot.AddComponent<RectTransform>();
        liveRT.anchorMin = new Vector2(0.5f, 1f);
        liveRT.anchorMax = new Vector2(0.5f, 1f);
        liveRT.pivot     = new Vector2(0.5f, 1f);
        liveRT.anchoredPosition = new Vector2(0f, -25f);
        liveRT.sizeDelta = new Vector2(480f, 46f);

        Image liveBg = liveIndicatorRoot.AddComponent<Image>();
        liveBg.color = new Color(0.08f, 0.05f, 0.16f, 0.88f);
        if (btnFantasyNormal != null) { liveBg.sprite = btnFantasyNormal; liveBg.type = Image.Type.Sliced; }

        txtLiveIndicator = CreateTMP(liveIndicatorRoot.transform, "TxtLive", 16f, FontStyles.Bold,
            new Color(0.35f, 1.0f, 0.65f, 1f), TextAlignmentOptions.Center);
        RectTransform liveTxtRT = txtLiveIndicator.rectTransform;
        liveTxtRT.anchorMin = Vector2.zero; liveTxtRT.anchorMax = Vector2.one;
        liveTxtRT.offsetMin = Vector2.zero; liveTxtRT.offsetMax = Vector2.zero;

        liveIndicatorRoot.SetActive(false);
        dimmerRoot.SetActive(false);
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
    // FREEZE-FRAME STEP LOGIC
    // ─────────────────────────────────────────────────────────────────────
    private void StartTutorialStep(int idx)
    {
        if (idx >= steps.Length)
        {
            StartCoroutine(ShowTutorialCompleteRoutine());
            return;
        }

        currentStepIndex = idx;
        StepInfo s = steps[idx];

        // 1. BEKUKAN GAME! (TIME FREEZE)
        Time.timeScale = 0f;
        isWaitingForPlayerInput = true;

        // Auto-recharge mana jika tahap skill
        if (player != null && (s.action == ActionType.Skill1 || s.action == ActionType.Skill2 || s.action == ActionType.Skill3))
        {
            player.RestoreMana(100);
        }

        // 2. Isi konten modal
        txtCategory.text = s.category;
        txtTitle.text = s.title;
        txtKeyBadge.text = s.keyBadge;
        txtKeyBadge.color = s.badgeColor;
        txtDescription.text = s.description;
        txtStepCounter.text = $"Langkah {idx + 1} dari {steps.Length}";

        // Tampilkan modal dengan animasi pop-in menggunakan unscaled time
        if (liveIndicatorRoot != null) liveIndicatorRoot.SetActive(false);
        if (dimmerRoot != null) dimmerRoot.SetActive(true);

        StartCoroutine(ModalPopInRoutine());
    }

    private IEnumerator ModalPopInRoutine()
    {
        if (modalRT == null) yield break;

        modalRT.localScale = Vector3.one * 0.7f;
        float elapsed = 0f;
        float dur = 0.22f;

        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / dur;
            float scale = Mathf.SmoothStep(0.7f, 1f, t);
            modalRT.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        modalRT.localScale = Vector3.one;
    }

    private void Update()
    {
        if (isTutorialFinished) return;

        // Skip Tutorial kapan saja dengan tombol TAB
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            OnSkipTutorialClicked();
            return;
        }

        // Saat modal freeze aktif: deteksi penekanan tombol
        if (isWaitingForPlayerInput && currentStepIndex < steps.Length)
        {
            // Efek berkedip lembut teks prompt
            if (txtPressPrompt != null)
            {
                float pulse = Mathf.Sin(Time.unscaledTime * 5f) * 0.5f + 0.5f;
                txtPressPrompt.color = new Color(1f, 0.88f, 0.40f, 0.55f + pulse * 0.45f);
            }

            // Deteksi apakah tombol yang diminta ditekan
            StepInfo s = steps[currentStepIndex];
            bool match = false;

            foreach (KeyCode k in s.triggerKeys)
            {
                if (Input.GetKeyDown(k))
                {
                    match = true;
                    break;
                }
            }

            // Fallback: tombol Enter atau Space (jika bukan step jump)
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                match = true;
            }

            if (match)
            {
                OnBadgeOrKeyTriggered();
            }
        }
    }

    // Dipanggil saat tombol target ditekan (atau badge diklik)
    private void OnBadgeOrKeyTriggered()
    {
        if (!isWaitingForPlayerInput) return;
        isWaitingForPlayerInput = false;

        if (audioSource != null && sfxSuccess != null)
        {
            audioSource.PlayOneShot(sfxSuccess, 0.95f);
        }

        // 1. HIDUPKAN KEMBALI GAME! (UNFREEZE)
        Time.timeScale = 1.0f;

        // 2. Tutup Modal Besar
        if (dimmerRoot != null) dimmerRoot.SetActive(false);

        // 3. Eksekusi Aksi Langsung pada Player agar terasa responsif instan
        ExecutePlayerAction(steps[currentStepIndex].action);

        // 4. Munculkan Live Indicator di atas layar selama waktu latihan
        StartCoroutine(PracticeWindowRoutine(steps[currentStepIndex]));
    }

    private void ExecutePlayerAction(ActionType act)
    {
        if (player == null) player = FindFirstObjectByType<Player>();
        if (playerSkills == null) playerSkills = FindFirstObjectByType<PlayerSkills>();

        switch (act)
        {
            case ActionType.Jump:
                if (player != null) player.PerformJump();
                break;
            case ActionType.Attack:
                if (player != null) player.TryAttack();
                break;
            case ActionType.Dash:
                if (player != null) player.PerformDash();
                break;
            case ActionType.Skill1:
                if (playerSkills != null) playerSkills.TryCastSkill1();
                break;
            case ActionType.Skill2:
                if (playerSkills != null) playerSkills.TryCastSkill2();
                break;
            case ActionType.Skill3:
                if (playerSkills != null) playerSkills.TryCastSkill3();
                break;
            case ActionType.Walk:
                // Berjalan dikontrol bebas oleh player
                break;
        }
    }

    private IEnumerator PracticeWindowRoutine(StepInfo s)
    {
        // Tampilkan floating banner di atas layar
        if (liveIndicatorRoot != null && txtLiveIndicator != null)
        {
            txtLiveIndicator.text = $"✔ BERHASIL! Silakan coba lagi: {s.keyBadge}";
            liveIndicatorRoot.SetActive(true);
        }

        // Beri waktu bagi player untuk bergerak / melihat animasi skill
        yield return new WaitForSeconds(s.practiceDuration);

        if (liveIndicatorRoot != null) liveIndicatorRoot.SetActive(false);

        // Masuk ke step berikutnya (yang akan freeze game lagi)
        StartTutorialStep(currentStepIndex + 1);
    }

    private void OnSkipTutorialClicked()
    {
        if (isTutorialFinished) return;
        if (audioSource != null && sfxClick != null) audioSource.PlayOneShot(sfxClick);

        StopAllCoroutines();
        Time.timeScale = 1.0f;
        StartCoroutine(ShowTutorialCompleteRoutine());
    }

    private IEnumerator ShowTutorialCompleteRoutine()
    {
        isTutorialFinished = true;
        Time.timeScale = 1.0f;

        if (liveIndicatorRoot != null) liveIndicatorRoot.SetActive(false);

        if (dimmerRoot != null) dimmerRoot.SetActive(true);
        if (keyBadgeContainer != null) keyBadgeContainer.SetActive(false);
        if (txtPressPrompt != null) txtPressPrompt.gameObject.SetActive(false);

        txtCategory.text = "✦ PELATIHAN SELESAI ✦";
        txtTitle.text = "KAU SIAP BERTARUNG!";
        txtTitle.color = new Color(1f, 0.88f, 0.35f, 1f);

        txtDescription.text = "Selamat, Pejuang! Kamu telah menguasai seluruh jurus pedang dan manuver tempur.\n\nSekarang hancurkan monster penghalang dan taklukkan Raja Monster Slime di ujung hutan!";
        txtStepCounter.text = "Semua latihan tuntas.";

        yield return new WaitForSeconds(3.2f);

        // Fade out lembut
        if (dimmerImage != null)
        {
            float elapsed = 0f;
            float dur = 0.5f;
            Color dc = dimmerImage.color;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Lerp(dc.a, 0f, elapsed / dur);
                dimmerImage.color = new Color(dc.r, dc.g, dc.b, a);
                if (modalBackground != null) modalBackground.color = new Color(1f, 1f, 1f, a);
                yield return null;
            }
        }

        if (dimmerRoot != null) dimmerRoot.SetActive(false);
    }
}
