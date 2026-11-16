using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Chapter1Manager — Manajer Stage 1: Lembah Reruntuhan Kuno (Ancient Ruins Valley)
/// 
/// ALUR CHAPTER 1:
/// 1. Pemain memasuki Lembah Reruntuhan yang gelap dan berbahaya.
/// 2. Rintangan: Duri beracun di lantai, lubang jebakan, platform berjalan.
/// 3. Musuh: Para Iblis Penjaga berpatroli. Harus dibantai semua.
/// 4. Komandan Iblis (Warden) menjaga sel — tewaskan dia untuk merebut Kunci Sel!
/// 5. Buka sel dan selamatkan gadis penyintas (Lyra) — dia adalah kunci rahasia menemukan Elora!
/// 6. Semua terbunuh + Lyra selamat = Misi sukses! Transisi ke layar Chapter Clear.
/// </summary>
public class Chapter1Manager : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        Scene s = SceneManager.GetActiveScene();
        if (s.name == "Next")
        {
            if (FindFirstObjectByType<Chapter1Manager>() == null)
            {
                new GameObject("Chapter1Manager").AddComponent<Chapter1Manager>();
            }
        }

        SceneManager.sceneLoaded += (scene, mode) =>
        {
            if (scene.name == "Next")
            {
                if (FindFirstObjectByType<Chapter1Manager>() == null)
                    new GameObject("Chapter1Manager").AddComponent<Chapter1Manager>();
            }
        };
    }

    // ── Sprites
    private Sprite spriteDemonGuard;
    private Sprite spriteDemonWarden;
    private Sprite spriteFemaleSurvivor;
    private Sprite spriteSurvivorRescued;
    private Sprite spriteAbyssalCage;
    private Sprite spriteCageOpen;
    private Sprite spriteSurvivorPortrait;
    private Sprite spriteCellKey;
    private Sprite spriteSpikes;

    // ── UI Assets
    private Sprite modalPanelSprite;
    private Sprite btnFantasyNormal;
    private Sprite btnFantasyHover;
    private TMP_FontAsset tmpFont;
    private AudioClip sfxClick;
    private AudioClip sfxVictory;
    private AudioClip sfxDialogue;
    private AudioSource audioSource;

    // ── World Objects
    private List<DemonGuard> registeredDemons = new List<DemonGuard>();
    private int totalDemonsToKill = 0;
    private int demonsKilled = 0;
    private bool wardenKilled = false;
    private bool survivorRescued = false;
    private bool missionComplete = false;
    private bool cellKeyObtained = false;

    // ── World NPCs
    private GameObject femaleSurvivorGO;
    private GameObject abyssalCageGO;
    private GameObject cellKeyGO;
    private SpriteRenderer cageSR;
    private SpriteRenderer survivorSR;

    // ── HUD
    private Canvas overlayCanvas;
    private TMP_Text txtKillCount;
    private TMP_Text txtObjectiveStatus;
    private TMP_Text txtDialogueBox;
    private Image imgDialoguePortrait;
    private GameObject dialoguePanel;
    private bool dialoguePanelVisible = false;

    // ── Victory Screen
    private GameObject victoryPanel;

    // ── State Machine
    private enum Chapter1Phase
    {
        Fighting,
        WardenKilled,
        CellKeyObtained,
        SurvivorFreed,
        MissionSuccess
    }
    private Chapter1Phase phase = Chapter1Phase.Fighting;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        LoadAssets();
    }

    private void LoadAssets()
    {
        spriteDemonGuard    = Resources.Load<Sprite>("Stage1/demon_guard_pixel");
        spriteDemonWarden   = Resources.Load<Sprite>("Stage1/demon_warden_pixel");
        spriteFemaleSurvivor= Resources.Load<Sprite>("Stage1/female_survivor_pixel");
        spriteSurvivorRescued = Resources.Load<Sprite>("Stage1/female_survivor_rescued");
        spriteAbyssalCage   = Resources.Load<Sprite>("Stage1/abyssal_cage_pixel");
        spriteCageOpen      = Resources.Load<Sprite>("Stage1/abyssal_cage_open");
        spriteSurvivorPortrait = Resources.Load<Sprite>("Stage1/female_survivor_portrait");
        spriteCellKey       = Resources.Load<Sprite>("Stage1/item_cell_key");
        spriteSpikes        = Resources.Load<Sprite>("Stage1/obstacle_spikes_pixel");

        modalPanelSprite    = Resources.Load<Sprite>("UI/panel_fantasy_modal");
        btnFantasyNormal    = Resources.Load<Sprite>("UI/btn_fantasy_normal");
        btnFantasyHover     = Resources.Load<Sprite>("UI/btn_fantasy_hover");
        tmpFont             = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        sfxClick    = Resources.Load<AudioClip>("Audio/ui_click");
        sfxVictory  = Resources.Load<AudioClip>("Audio/star_all");
        sfxDialogue = Resources.Load<AudioClip>("Audio/star_1");

        if (sfxVictory == null) sfxVictory = sfxClick;
        if (sfxDialogue == null) sfxDialogue = sfxClick;
    }

    private void Start()
    {
        // Pastikan EventSystem ada agar UI bisa diklik
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        BuildOverlayCanvas();
        BuildChapter1World();
        StartCoroutine(OpeningDialogueSequence());
    }

    // ─────────────────────────────────────────────────────────────────────
    // BANGUN DUNIA CHAPTER 1
    // ─────────────────────────────────────────────────────────────────────
    private void BuildChapter1World()
    {
        // ── 0. Bangun Lingkungan Stage (Lantai, Dinding, Platform) ───────────
        BuildStageEnvironment();

        // ── 1. Pastikan Player dan Kamera Siap ───────────────────────────────
        EnsurePlayer();

        // ── 2. Spawn Iblis Penjaga (3 unit patroli) ──────────────────────────
        SpawnDemonGuard(new Vector3(-4.0f, -4.2f, 0f), false);
        SpawnDemonGuard(new Vector3( 2.5f, -4.2f, 0f), false);
        SpawnDemonGuard(new Vector3( 7.0f, -4.2f, 0f), false);

        // ── 3. Spawn Komandan Iblis Warden (1 unit, penjaga sel) ────────────
        SpawnDemonGuard(new Vector3(11.5f, -4.2f, 0f), true);

        totalDemonsToKill = registeredDemons.Count;

        // ── 4. Bangun Rintangan Duri di Jalur ───────────────────────────────
        SpawnSpikeTrap(new Vector3(-7.0f, -4.8f, 0f));
        SpawnSpikeTrap(new Vector3(-0.5f, -4.8f, 0f));
        SpawnSpikeTrap(new Vector3( 5.0f, -4.8f, 0f));

        // ── 5. Bangun Sel Penjara (Abyssal Iron Cage) di ujung kanan ─────────
        BuildAbyssalCage(new Vector3(15.5f, -3.8f, 0f));

        // ── 6. Tambah Kunci Sel (tersembunyi, spawn saat Warden mati) ───────
        BuildCellKey(new Vector3(11.5f, -4.0f, 0f));

        UpdateHUD();
    }

    private void BuildStageEnvironment()
    {
        // Sembunyikan tombol UI "Button" bawaan scene Next jika ada
        GameObject oldBtn = GameObject.Find("Button");
        if (oldBtn != null) oldBtn.SetActive(false);

        // Lantai Dasar (Solid Floor)
        GameObject floor = new GameObject("GroundFloor");
        floor.transform.position = new Vector3(0f, -5.5f, 0f);
        floor.layer = LayerMask.NameToLayer("Ground");
        BoxCollider2D bcFloor = floor.AddComponent<BoxCollider2D>();
        bcFloor.size = new Vector2(50f, 1.2f);

        SpriteRenderer floorSR = floor.AddComponent<SpriteRenderer>();
        if (modalPanelSprite != null) floorSR.sprite = modalPanelSprite;
        floorSR.drawMode = SpriteDrawMode.Tiled;
        floorSR.size = new Vector2(50f, 1.2f);
        floorSR.color = new Color(0.12f, 0.08f, 0.18f, 1f);
        floorSR.sortingOrder = 3;

        // Dinding Kiri (Batas Level)
        GameObject leftWall = new GameObject("LeftWall");
        leftWall.transform.position = new Vector3(-18f, 0f, 0f);
        leftWall.layer = LayerMask.NameToLayer("Ground");
        BoxCollider2D bcLeft = leftWall.AddComponent<BoxCollider2D>();
        bcLeft.size = new Vector2(1.5f, 25f);

        // Dinding Kanan (Batas Level)
        GameObject rightWall = new GameObject("RightWall");
        rightWall.transform.position = new Vector3(22f, 0f, 0f);
        rightWall.layer = LayerMask.NameToLayer("Ground");
        BoxCollider2D bcRight = rightWall.AddComponent<BoxCollider2D>();
        bcRight.size = new Vector2(1.5f, 25f);

        // Platform Melayang 1 (Kiri)
        BuildFloatingPlatform(new Vector3(-4.5f, -2.6f, 0f), 4.0f);

        // Platform Melayang 2 (Tengah)
        BuildFloatingPlatform(new Vector3(1.5f, -1.8f, 0f), 3.5f);

        // Platform Melayang 3 (Kanan)
        BuildFloatingPlatform(new Vector3(7.5f, -2.4f, 0f), 4.2f);

        // Background Dekoratif (Reruntuhan Kuno)
        GameObject bgObj = GameObject.Find("2f082b7a5592f322ef585a2083cf85f5");
        if (bgObj != null)
        {
            bgObj.transform.localScale = new Vector3(3.5f, 3.5f, 1f);
            bgObj.transform.position = new Vector3(0f, 0f, 10f);
            SpriteRenderer bgsr = bgObj.GetComponent<SpriteRenderer>();
            if (bgsr != null)
            {
                bgsr.color = new Color(0.35f, 0.28f, 0.45f, 1f); // Gelap misterius
                bgsr.sortingOrder = -20;
            }
        }
    }

    private void BuildFloatingPlatform(Vector3 pos, float width)
    {
        GameObject plat = new GameObject("Platform_" + pos.x);
        plat.transform.position = pos;
        plat.layer = LayerMask.NameToLayer("Ground");

        BoxCollider2D bc = plat.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(width, 0.35f);

        SpriteRenderer sr = plat.AddComponent<SpriteRenderer>();
        if (modalPanelSprite != null)
        {
            sr.sprite = modalPanelSprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(width, 0.35f);
        }
        sr.color = new Color(0.30f, 0.22f, 0.40f, 1f);
        sr.sortingOrder = 4;
    }

    private void EnsurePlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            playerObj = new GameObject("Player");
            playerObj.tag = "Player";
            playerObj.layer = LayerMask.NameToLayer("Player");
            playerObj.transform.position = new Vector3(-12f, -4.2f, 0f);

            // Physics
            Rigidbody2D rb = playerObj.AddComponent<Rigidbody2D>();
            rb.mass = 1f;
            rb.gravityScale = 3.5f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            CapsuleCollider2D cc = playerObj.AddComponent<CapsuleCollider2D>();
            cc.size = new Vector2(0.85f, 1.8f);
            cc.offset = new Vector2(0f, 0f);

            // Audio
            AudioSource pAudio = playerObj.AddComponent<AudioSource>();

            // Attack Point Child
            GameObject apGO = new GameObject("AttackPoint");
            apGO.transform.SetParent(playerObj.transform, false);
            apGO.transform.localPosition = new Vector3(1.4f, -0.3f, 0f);

            // Animator & Visual Child
            GameObject animGO = new GameObject("Animator");
            animGO.transform.SetParent(playerObj.transform, false);
            animGO.transform.localPosition = new Vector3(0.3f, 0f, 0f);

            SpriteRenderer sr = animGO.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;
            sr.sprite = Resources.Load<Sprite>("UI/hud_player_portrait");

            Animator anim = animGO.AddComponent<Animator>();
            RuntimeAnimatorController ctrl = Resources.Load<RuntimeAnimatorController>("Animations/Player");
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;

            animGO.AddComponent<EntityAnimationsEvent>();

            // Player Components
            playerObj.AddComponent<Player>();
            playerObj.AddComponent<PlayerHealthUI>();
            playerObj.AddComponent<PlayerSkills>();
        }

        // Setup Camera Follow
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            CameraFollow cf = mainCam.GetComponent<CameraFollow>();
            if (cf == null) cf = mainCam.gameObject.AddComponent<CameraFollow>();
            cf.target = playerObj.transform;
            cf.smoothSpeed = 5.0f;
            mainCam.orthographicSize = 6.5f;
        }

        // Pastikan PlayerHUD ada untuk HP dan Mana Bar
        if (PlayerHUD.instance == null)
        {
            new GameObject("PlayerHUD_Controller").AddComponent<PlayerHUD>();
        }
    }

    private void SpawnDemonGuard(Vector3 pos, bool isWarden)
    {
        GameObject go = new GameObject(isWarden ? "DemonWarden" : "DemonGuard");
        go.transform.position = pos;
        go.layer = LayerMask.NameToLayer("Enemy");
        go.tag = "Enemy";

        // Visuals
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = isWarden ? spriteDemonWarden : spriteDemonGuard;
        if (sr.sprite == null)
        {
            sr.color = isWarden ? new Color(0.9f, 0.1f, 0.3f, 1f) : new Color(0.7f, 0.2f, 0.2f, 1f);
        }
        sr.sortingOrder = 10;

        // Physics
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CapsuleCollider2D cc = go.AddComponent<CapsuleCollider2D>();
        cc.size = isWarden ? new Vector2(0.6f, 1.0f) : new Vector2(0.5f, 0.9f);

        // Scale
        go.transform.localScale = isWarden ? new Vector3(1.2f, 1.2f, 1f) : new Vector3(0.9f, 0.9f, 1f);

        // Demon Guard Script
        DemonGuard demon = go.AddComponent<DemonGuard>();
        demon.isWarden = isWarden;
    }

    public void RegisterDemon(DemonGuard demon)
    {
        if (!registeredDemons.Contains(demon))
        {
            registeredDemons.Add(demon);
        }
    }

    private void SpawnSpikeTrap(Vector3 pos)
    {
        GameObject go = new GameObject("SpikeTrap");
        go.transform.position = pos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spriteSpikes;
        if (sr.sprite == null)
        {
            sr.color = new Color(0.5f, 0.4f, 0.5f, 1f);
        }
        sr.sortingOrder = 5;

        PolygonCollider2D col = go.AddComponent<PolygonCollider2D>();
        col.SetPath(0, new Vector2[]{
            new Vector2(-0.6f, -0.1f),
            new Vector2( 0.6f, -0.1f),
            new Vector2( 0.6f,  0.1f),
            new Vector2(-0.6f,  0.1f)
        });
        col.isTrigger = true;

        SpikeTrap trap = go.AddComponent<SpikeTrap>();
        trap.damage = 20;
    }

    private void BuildAbyssalCage(Vector3 pos)
    {
        abyssalCageGO = new GameObject("AbyssalCage");
        abyssalCageGO.transform.position = pos;

        cageSR = abyssalCageGO.AddComponent<SpriteRenderer>();
        cageSR.sprite = spriteAbyssalCage;
        if (cageSR.sprite == null)
        {
            cageSR.color = new Color(0.3f, 0.2f, 0.4f, 1f);
        }
        cageSR.sortingOrder = 8;
        abyssalCageGO.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

        // Gadis Penyintas di dalam sel (default tersembunyi/sedang dikurung)
        femaleSurvivorGO = new GameObject("FemaleSurvivor_Lyra");
        femaleSurvivorGO.transform.position = pos + new Vector3(0f, -0.1f, -0.1f);
        survivorSR = femaleSurvivorGO.AddComponent<SpriteRenderer>();
        survivorSR.sprite = spriteFemaleSurvivor;
        survivorSR.sortingOrder = 9;
        survivorSR.color = new Color(1f, 1f, 1f, 0.9f);
        femaleSurvivorGO.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

        // Efek cahaya unggu magis di sekitar sel
        BuildCageGlow(pos);
    }

    private void BuildCageGlow(Vector3 pos)
    {
        // Lingkaran ritual kegelapan di bawah sel
        GameObject glow = new GameObject("RitualGlow");
        glow.transform.position = pos + new Vector3(0f, -0.5f, 0.1f);
        SpriteRenderer glowSR = glow.AddComponent<SpriteRenderer>();
        glowSR.sortingOrder = 4;

        // Jika tidak ada sprite glow, buat lingkaran warna ungu solid dengan Sprite
        glowSR.color = new Color(0.6f, 0.1f, 0.9f, 0.35f);
        glow.transform.localScale = new Vector3(2.5f, 0.5f, 1f);
    }

    private void BuildCellKey(Vector3 pos)
    {
        cellKeyGO = new GameObject("CellKey_Skull");
        cellKeyGO.transform.position = pos + new Vector3(0f, 0.5f, 0f);

        SpriteRenderer sr = cellKeyGO.AddComponent<SpriteRenderer>();
        sr.sprite = spriteCellKey;
        sr.sortingOrder = 15;
        sr.color = new Color(0.9f, 0.5f, 1f, 1f);

        // Key berputar lambat (menarik perhatian)
        cellKeyGO.AddComponent<RotatingPickup>();
        cellKeyGO.SetActive(false); // Muncul setelah Warden mati!

        // Trigger zone
        CircleCollider2D cc = cellKeyGO.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 1.0f;

        KeyPickupTrigger kpt = cellKeyGO.AddComponent<KeyPickupTrigger>();
        kpt.manager = this;
    }

    // ─────────────────────────────────────────────────────────────────────
    // OVERLAY CANVAS (HUD dan Dialog)
    // ─────────────────────────────────────────────────────────────────────
    private void BuildOverlayCanvas()
    {
        Canvas existingCanvas = FindObjectOfType<Canvas>();
        GameObject cgo = new GameObject("Chapter1HUD_Canvas");
        overlayCanvas = cgo.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 95;

        CanvasScaler sc = cgo.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;

        cgo.AddComponent<GraphicRaycaster>();

        // ── Kill Progress Tracker (Kiri Atas) ──────────────────────────────
        GameObject killTrack = new GameObject("KillTracker");
        killTrack.transform.SetParent(overlayCanvas.transform, false);
        RectTransform ktRT = killTrack.AddComponent<RectTransform>();
        ktRT.anchorMin = new Vector2(0f, 1f); ktRT.anchorMax = new Vector2(0f, 1f);
        ktRT.pivot     = new Vector2(0f, 1f);
        ktRT.anchoredPosition = new Vector2(20f, -20f);
        ktRT.sizeDelta = new Vector2(420f, 65f);

        Image ktBg = killTrack.AddComponent<Image>();
        if (modalPanelSprite != null) { ktBg.sprite = modalPanelSprite; ktBg.type = Image.Type.Sliced; }
        ktBg.color = new Color(0.07f, 0.04f, 0.14f, 0.90f);

        txtKillCount = CreateTMP(killTrack.transform, "TxtKill", 15f, FontStyles.Bold,
            new Color(1f, 0.88f, 0.45f, 1f), TextAlignmentOptions.Center);
        txtKillCount.rectTransform.anchorMin = Vector2.zero; txtKillCount.rectTransform.anchorMax = Vector2.one;
        txtKillCount.rectTransform.offsetMin = new Vector2(10f, 4f); txtKillCount.rectTransform.offsetMax = new Vector2(-10f, -4f);
        txtKillCount.enableWordWrapping = true;
        txtKillCount.text = "[IBLIS] Dibantai: 0 / 0\n[KUNCI] Status: Belum Ditemukan";

        // ── Objective Status (Kanan Atas) ──────────────────────────────────
        GameObject objStatus = new GameObject("ObjStatus");
        objStatus.transform.SetParent(overlayCanvas.transform, false);
        RectTransform osRT = objStatus.AddComponent<RectTransform>();
        osRT.anchorMin = new Vector2(1f, 1f); osRT.anchorMax = new Vector2(1f, 1f);
        osRT.pivot     = new Vector2(1f, 1f);
        osRT.anchoredPosition = new Vector2(-20f, -20f);
        osRT.sizeDelta = new Vector2(430f, 85f);

        Image osBg = objStatus.AddComponent<Image>();
        if (modalPanelSprite != null) { osBg.sprite = modalPanelSprite; osBg.type = Image.Type.Sliced; }
        osBg.color = new Color(0.07f, 0.04f, 0.14f, 0.90f);

        txtObjectiveStatus = CreateTMP(objStatus.transform, "TxtObj", 13f, FontStyles.Normal,
            new Color(0.90f, 0.86f, 0.80f, 1f), TextAlignmentOptions.Left);
        txtObjectiveStatus.rectTransform.anchorMin = Vector2.zero; txtObjectiveStatus.rectTransform.anchorMax = Vector2.one;
        txtObjectiveStatus.rectTransform.offsetMin = new Vector2(12f, 6f); txtObjectiveStatus.rectTransform.offsetMax = new Vector2(-12f, -6f);
        txtObjectiveStatus.enableWordWrapping = true;
        txtObjectiveStatus.text = "[ ] Bantai semua Iblis Penjaga\n[ ] Rebut Kunci Sel dari Warden\n[ ] Selamatkan Lyra dari Sel";

        // ── Dialogue Panel (Bawah Tengah) ─────────────────────────────────
        BuildDialoguePanel();
    }

    private void BuildDialoguePanel()
    {
        dialoguePanel = new GameObject("Chapter1_DialoguePanel");
        dialoguePanel.transform.SetParent(overlayCanvas.transform, false);

        RectTransform dRT = dialoguePanel.AddComponent<RectTransform>();
        dRT.anchorMin = new Vector2(0.5f, 0f); dRT.anchorMax = new Vector2(0.5f, 0f);
        dRT.pivot     = new Vector2(0.5f, 0f);
        dRT.sizeDelta = new Vector2(1100f, 165f);
        dRT.anchoredPosition = new Vector2(0f, 15f);

        Image dbg = dialoguePanel.AddComponent<Image>();
        if (modalPanelSprite != null) { dbg.sprite = modalPanelSprite; dbg.type = Image.Type.Sliced; }
        dbg.color = new Color(0.06f, 0.04f, 0.12f, 0.97f);

        // Gold dividers
        CreateGoldDiv(dialoguePanel.transform, 1f); CreateGoldDiv(dialoguePanel.transform, 0f);

        // Portrait of Lyra (female survivor)
        GameObject portGO = new GameObject("PortraitLyra");
        portGO.transform.SetParent(dialoguePanel.transform, false);
        RectTransform portRT = portGO.AddComponent<RectTransform>();
        portRT.anchorMin = new Vector2(0f, 0.5f); portRT.anchorMax = new Vector2(0f, 0.5f);
        portRT.pivot     = new Vector2(0f, 0.5f);
        portRT.anchoredPosition = new Vector2(10f, 0f);
        portRT.sizeDelta = new Vector2(130f, 130f);

        imgDialoguePortrait = portGO.AddComponent<Image>();
        imgDialoguePortrait.sprite = spriteSurvivorPortrait;
        imgDialoguePortrait.color = Color.white;
        imgDialoguePortrait.preserveAspect = true;

        // Dialogue text
        GameObject textGO = new GameObject("TxtDialogue");
        textGO.transform.SetParent(dialoguePanel.transform, false);
        RectTransform txtRT = textGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = new Vector2(155f, 15f);
        txtRT.offsetMax = new Vector2(-20f, -15f);

        txtDialogueBox = textGO.AddComponent<TextMeshProUGUI>();
        txtDialogueBox.font = tmpFont;
        txtDialogueBox.fontSize = 17f;
        txtDialogueBox.color = new Color(1f, 0.95f, 0.88f, 1f);
        txtDialogueBox.enableWordWrapping = true;
        txtDialogueBox.overflowMode = TextOverflowModes.Overflow;
        txtDialogueBox.richText = true;
        txtDialogueBox.lineSpacing = 4f;
        txtDialogueBox.text = "";

        dialoguePanel.SetActive(false);
    }

    private void CreateGoldDiv(Transform parent, float anchorY)
    {
        GameObject line = new GameObject("GoldDiv");
        line.transform.SetParent(parent, false);
        RectTransform rt = line.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, anchorY); rt.anchorMax = new Vector2(1f, anchorY);
        rt.pivot     = new Vector2(0.5f, anchorY);
        rt.sizeDelta = new Vector2(0f, 2.5f);
        rt.anchoredPosition = new Vector2(0f, 0f);
        line.AddComponent<Image>().color = new Color(0.98f, 0.78f, 0.30f, 0.85f);
    }

    // ─────────────────────────────────────────────────────────────────────
    // LOGIKA MISI
    // ─────────────────────────────────────────────────────────────────────
    public void OnDemonKilled(DemonGuard demon, bool wasWarden)
    {
        demonsKilled++;

        if (wasWarden)
        {
            wardenKilled = true;
            phase = Chapter1Phase.WardenKilled;
            StartCoroutine(SpawnCellKeySequence());
        }

        UpdateHUD();
        CheckMissionCompletion();
    }

    private IEnumerator SpawnCellKeySequence()
    {
        yield return new WaitForSeconds(0.6f);

        // Tampilkan Kunci Sel yang jatuh dari Warden!
        if (cellKeyGO != null) cellKeyGO.SetActive(true);
        if (audioSource != null && sfxDialogue != null)
            audioSource.PlayOneShot(sfxDialogue, 1.0f);

        yield return new WaitForSeconds(0.5f);
        ShowDialogue("KAELEN", null,
            $"\"Kunci Sel Penjara! Sekarang aku bisa membebaskan orang yang terkurung di dalam sel itu.\"", 3.5f);
    }

    public void OnCellKeyPickedUp()
    {
        if (cellKeyObtained) return;
        cellKeyObtained = true;
        phase = Chapter1Phase.CellKeyObtained;

        if (cellKeyGO != null) cellKeyGO.SetActive(false);

        if (audioSource != null && sfxDialogue != null)
            audioSource.PlayOneShot(sfxDialogue, 0.9f);

        UpdateHUD();
        StartCoroutine(OpenCageCutscene());
    }

    private IEnumerator OpenCageCutscene()
    {
        yield return new WaitForSeconds(0.3f);

        // Open the cage!
        if (cageSR != null && spriteCageOpen != null)
            cageSR.sprite = spriteCageOpen;

        // Show survivor happy/rescued sprite
        if (survivorSR != null && spriteSurvivorRescued != null)
            survivorSR.sprite = spriteSurvivorRescued;

        yield return new WaitForSeconds(0.2f);

        survivorRescued = true;
        phase = Chapter1Phase.SurvivorFreed;

        // Dialogue cutscene: Lyra berbicara dengan Kaelen
        yield return ShowDialogueCutscene();

        UpdateHUD();
        CheckMissionCompletion();
    }

    private IEnumerator ShowDialogueCutscene()
    {
        yield return new WaitForSeconds(0.3f);

        string playerName = PlayerPrefs.GetString("SelectedCharacterName", "Kaelen");

        ShowDialogue("LYRA — GADIS PENYINTAS",   null,
            $"<color=#FFD6A5>\"...K-kamu... kamu benar-benar datang untuk menyelamatkanku...?\"</color>",
            3.5f);
        yield return new WaitForSeconds(3.8f);

        ShowDialogue(playerName.ToUpper(), null,
            $"\"Namamu Lyra? Aku mendengar kamu tahu di mana Raja Iblis menyembunyikan tawanan spesialnya. Ceritakan semuanya.\"",
            4.0f);
        yield return new WaitForSeconds(4.3f);

        ShowDialogue("LYRA — GADIS PENYINTAS", null,
            $"<color=#FFD6A5>\"Aku melihat mereka membawa seorang gadis dengan rantai abyssal... ke <b>Kuil Kegelapan Baratdaya</b>... Mereka menyebutnya... Sang Persembahan Utama—\"</color>",
            5.0f);
        yield return new WaitForSeconds(5.3f);

        ShowDialogue(playerName.ToUpper(), null,
            $"\"<b>ELORA!</b> Kuil Kegelapan... aku harus ke sana sebelum ritual persembahan dimulai!\"",
            4.0f);
        yield return new WaitForSeconds(4.3f);

        // Sembunyikan dialog, lanjut ke misi selesai
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    private void ShowDialogue(string speaker, Sprite portrait, string text, float duration)
    {
        if (dialoguePanel == null) return;
        dialoguePanel.SetActive(true);

        if (txtDialogueBox != null)
        {
            txtDialogueBox.text = $"<color=#FFE08A><b>[ {speaker} ]</b></color>\n{text}";
        }

        if (imgDialoguePortrait != null && portrait != null)
        {
            imgDialoguePortrait.sprite = portrait;
            imgDialoguePortrait.color = Color.white;
        }

        if (audioSource != null && sfxDialogue != null)
            audioSource.PlayOneShot(sfxDialogue, 0.5f);

        StartCoroutine(HideDialogueAfter(duration));
    }

    private IEnumerator HideDialogueAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    private void CheckMissionCompletion()
    {
        if (missionComplete) return;

        bool allDemonsKilled = (demonsKilled >= totalDemonsToKill) || (totalDemonsToKill == 0);
        bool survivorFree = survivorRescued;

        if (allDemonsKilled && survivorFree)
        {
            missionComplete = true;
            phase = Chapter1Phase.MissionSuccess;
            StartCoroutine(MissionSuccessRoutine());
        }
    }

    private IEnumerator MissionSuccessRoutine()
    {
        yield return new WaitForSeconds(1.5f);

        if (audioSource != null && sfxVictory != null)
            audioSource.PlayOneShot(sfxVictory, 1.0f);

        // Update HUD ke status selesai
        UpdateHUD();

        yield return new WaitForSeconds(0.5f);

        // Tampilkan Victory Panel Chapter 1
        BuildVictoryPanel();
    }

    private void BuildVictoryPanel()
    {
        victoryPanel = new GameObject("Chapter1_VictoryScreen");
        victoryPanel.transform.SetParent(overlayCanvas.transform, false);

        RectTransform vRT = victoryPanel.AddComponent<RectTransform>();
        vRT.anchorMin = Vector2.zero; vRT.anchorMax = Vector2.one;
        vRT.offsetMin = Vector2.zero; vRT.offsetMax = Vector2.zero;

        Image vBg = victoryPanel.AddComponent<Image>();
        vBg.color = new Color(0f, 0f, 0f, 0.78f);

        // Panel Tengah
        GameObject vModal = new GameObject("VModal");
        vModal.transform.SetParent(victoryPanel.transform, false);
        RectTransform vmRT = vModal.AddComponent<RectTransform>();
        vmRT.anchorMin = new Vector2(0.5f, 0.5f); vmRT.anchorMax = new Vector2(0.5f, 0.5f);
        vmRT.pivot     = new Vector2(0.5f, 0.5f);
        vmRT.sizeDelta = new Vector2(820f, 480f);
        vmRT.anchoredPosition = Vector2.zero;

        Image vmBg = vModal.AddComponent<Image>();
        if (modalPanelSprite != null) { vmBg.sprite = modalPanelSprite; vmBg.type = Image.Type.Sliced; }
        vmBg.color = new Color(0.07f, 0.04f, 0.14f, 0.98f);

        CreateGoldDiv(vModal.transform, 1f); CreateGoldDiv(vModal.transform, 0f);

        // Chapter Clear Tag
        TMP_Text tag1 = CreateTMP(vModal.transform, "TagChapter", 16f, FontStyles.Bold,
            new Color(1f, 0.85f, 0.3f, 1f), TextAlignmentOptions.Center);
        tag1.rectTransform.anchorMin = new Vector2(0f, 1f); tag1.rectTransform.anchorMax = new Vector2(1f, 1f);
        tag1.rectTransform.pivot     = new Vector2(0.5f, 1f);
        tag1.rectTransform.anchoredPosition = new Vector2(0f, -26f);
        tag1.rectTransform.sizeDelta = new Vector2(0f, 28f);
        tag1.text = "CHAPTER 1 CLEAR -- LEMBAH RERUNTUHAN KUNO";

        // Big Title
        TMP_Text titleTxt = CreateTMP(vModal.transform, "TitleV", 32f, FontStyles.Bold,
            new Color(1f, 0.95f, 0.80f, 1f), TextAlignmentOptions.Center);
        titleTxt.rectTransform.anchorMin = new Vector2(0f, 1f); titleTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
        titleTxt.rectTransform.pivot     = new Vector2(0.5f, 1f);
        titleTxt.rectTransform.anchoredPosition = new Vector2(0f, -60f);
        titleTxt.rectTransform.sizeDelta = new Vector2(-60f, 44f);
        titleTxt.text = "MISI PENYELAMATAN BERHASIL!";

        // Story Summary
        string playerName = PlayerPrefs.GetString("SelectedCharacterName", "Kaelen");
        TMP_Text summaryTxt = CreateTMP(vModal.transform, "Summary", 16f, FontStyles.Normal,
            new Color(0.90f, 0.85f, 0.78f, 1f), TextAlignmentOptions.Center);
        summaryTxt.rectTransform.anchorMin = new Vector2(0.5f, 0.5f); summaryTxt.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        summaryTxt.rectTransform.pivot     = new Vector2(0.5f, 0.5f);
        summaryTxt.rectTransform.anchoredPosition = new Vector2(0f, 55f);
        summaryTxt.rectTransform.sizeDelta = new Vector2(700f, 120f);
        summaryTxt.enableWordWrapping = true;
        summaryTxt.text = $"<b>{playerName}</b> berhasil membantai semua Iblis Penjaga\ndan membebaskan <color=#FFD6A5><b>Lyra</b></color> dari Sel Tumbal Abyssal!\n\n<color=#FFD6A5>Lyra</color> mengungkap bahwa <color=#FFD6A5><b>Elora</b></color> dibawa ke <b>Kuil Kegelapan Baratdaya</b>.\nPerjalanan ke Chapter 2 menanti!";

        // Result rows
        BuildResultRow(vModal.transform, "Iblis Penjaga Dibantai", $"{demonsKilled} / {totalDemonsToKill}", new Vector2(0f, -60f), new Color(1f, 0.65f, 0.3f, 1f));
        BuildResultRow(vModal.transform, "Kunci Sel Ditemukan", cellKeyObtained ? "[V] YA" : "[X] Tidak", new Vector2(0f, -100f), new Color(0.9f, 0.5f, 1f, 1f));
        BuildResultRow(vModal.transform, "Lyra Diselamatkan", survivorRescued ? "[V] BERHASIL" : "[X] Gagal", new Vector2(0f, -140f), survivorRescued ? new Color(0.4f, 1f, 0.5f, 1f) : new Color(1f, 0.3f, 0.3f, 1f));

        // Continue button
        BuildVictoryButton(vModal.transform, ">  LANJUT KE CHAPTER 2", new Vector2(0f, 25f), OnContinueToChapter2);
        BuildVictoryButton(vModal.transform, "<  KEMBALI KE MENU", new Vector2(0f, -32f), OnBackToMenu, true);
    }

    private void BuildResultRow(Transform parent, string label, string value, Vector2 pos, Color valColor)
    {
        GameObject row = new GameObject("Row_" + label);
        row.transform.SetParent(parent, false);
        RectTransform rRT = row.AddComponent<RectTransform>();
        rRT.anchorMin = new Vector2(0.5f, 0f); rRT.anchorMax = new Vector2(0.5f, 0f);
        rRT.pivot     = new Vector2(0.5f, 0f);
        rRT.anchoredPosition = pos;
        rRT.sizeDelta = new Vector2(680f, 34f);

        Image rBg = row.AddComponent<Image>();
        rBg.color = new Color(0.12f, 0.08f, 0.22f, 0.75f);

        TMP_Text lTxt = CreateTMP(row.transform, "Label", 14f, FontStyles.Normal,
            new Color(0.88f, 0.84f, 0.78f, 1f), TextAlignmentOptions.Left);
        lTxt.rectTransform.anchorMin = new Vector2(0f, 0f); lTxt.rectTransform.anchorMax = new Vector2(0.6f, 1f);
        lTxt.rectTransform.offsetMin = new Vector2(16f, 0f); lTxt.rectTransform.offsetMax = Vector2.zero;
        lTxt.text = label;

        TMP_Text vTxt = CreateTMP(row.transform, "Value", 14f, FontStyles.Bold,
            valColor, TextAlignmentOptions.Right);
        vTxt.rectTransform.anchorMin = new Vector2(0.6f, 0f); vTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
        vTxt.rectTransform.offsetMin = Vector2.zero; vTxt.rectTransform.offsetMax = new Vector2(-16f, 0f);
        vTxt.text = value;
    }

    private void BuildVictoryButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action, bool isSecondary = false)
    {
        GameObject btnGO = new GameObject("VBtn_" + label);
        btnGO.transform.SetParent(parent, false);
        RectTransform bRT = btnGO.AddComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.5f, 0f); bRT.anchorMax = new Vector2(0.5f, 0f);
        bRT.pivot     = new Vector2(0.5f, 0f);
        bRT.anchoredPosition = pos;
        bRT.sizeDelta = new Vector2(isSecondary ? 260f : 300f, isSecondary ? 40f : 50f);

        Image bImg = btnGO.AddComponent<Image>();
        if (btnFantasyNormal != null) { bImg.sprite = btnFantasyNormal; bImg.type = Image.Type.Sliced; }
        bImg.color = isSecondary ? new Color(0.25f, 0.18f, 0.35f, 0.90f) : Color.white;

        Button btn = btnGO.AddComponent<Button>();
        if (btnFantasyHover != null)
        {
            btn.transition = Selectable.Transition.SpriteSwap;
            SpriteState ss = new SpriteState();
            ss.highlightedSprite = btnFantasyHover;
            ss.pressedSprite     = btnFantasyHover;
            btn.spriteState = ss;
        }
        btn.onClick.AddListener(action);

        TMP_Text bTxt = CreateTMP(btnGO.transform, "BtnLbl", isSecondary ? 14f : 18f, FontStyles.Bold,
            isSecondary ? new Color(0.80f, 0.76f, 0.70f, 1f) : new Color(1f, 0.92f, 0.45f, 1f),
            TextAlignmentOptions.Center);
        bTxt.rectTransform.anchorMin = Vector2.zero; bTxt.rectTransform.anchorMax = Vector2.one;
        bTxt.rectTransform.offsetMin = Vector2.zero; bTxt.rectTransform.offsetMax = Vector2.zero;
        bTxt.characterSpacing = 1.5f;
        bTxt.text = label;
    }

    private void OnContinueToChapter2()
    {
        if (audioSource != null && sfxClick != null) audioSource.PlayOneShot(sfxClick);
        // Untuk sementara kembali ke Main Menu karena Chapter 2 belum ada
        StartCoroutine(FadeAndLoadScene("Main Menu"));
    }

    private void OnBackToMenu()
    {
        if (audioSource != null && sfxClick != null) audioSource.PlayOneShot(sfxClick);
        StartCoroutine(FadeAndLoadScene("Main Menu"));
    }

    private IEnumerator FadeAndLoadScene(string sceneName)
    {
        // Simple fade keluar
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene(sceneName);
    }

    private void UpdateHUD()
    {
        if (txtKillCount != null)
        {
            string keyStatus = cellKeyObtained ? "<color=#FFD6A5>[V] DITEMUKAN</color>" : "<color=#FF8888>[X] Belum Ditemukan</color>";
            txtKillCount.text = $"[IBLIS] Dibantai: <color=#FF8844><b>{demonsKilled}</b></color> / {totalDemonsToKill}\n[KUNCI] Status: {keyStatus}";
        }

        if (txtObjectiveStatus != null)
        {
            bool allKilled = (demonsKilled >= totalDemonsToKill && totalDemonsToKill > 0);
            string o1 = allKilled ? "<color=#66FF88>[V] Bantai semua Iblis Penjaga</color>" : "[ ] Bantai semua Iblis Penjaga";
            string o2 = cellKeyObtained ? "<color=#66FF88>[V] Rebut Kunci Sel dari Warden</color>" : "[ ] Rebut Kunci Sel dari Warden";
            string o3 = survivorRescued ? "<color=#66FF88>[V] Selamatkan Lyra dari Sel</color>" : "[ ] Selamatkan Lyra dari Sel";
            txtObjectiveStatus.text = $"{o1}\n{o2}\n{o3}";
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // DIALOGUE CUTSCENE PEMBUKA
    // ─────────────────────────────────────────────────────────────────────
    private IEnumerator OpeningDialogueSequence()
    {
        yield return new WaitForSeconds(1.5f);

        string playerName = PlayerPrefs.GetString("SelectedCharacterName", "Kaelen");
        ShowDialogue("NARASI", null,
            $"<i>Lembah Reruntuhan Kuno... tempat ini berbau darah dan kegelapan Abyssal. Para iblis penjaga berpatroli di setiap sudut...</i>",
            4.0f);
        yield return new WaitForSeconds(4.5f);

        ShowDialogue(playerName.ToUpper(), null,
            $"\"Aku mendengar tangisan seseorang dari dalam sel itu. <color=#FFD6A5>Bertahanlah!</color> Aku akan membantai semua makhluk terkutuk ini dan membebaskanmu!\"",
            4.5f);
        yield return new WaitForSeconds(4.8f);

        // Setelah pembuka, tampilkan HUD kembali
        UpdateHUD();
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
}

// ─────────────────────────────────────────────────────────────────────
// HELPER COMPONENTS
// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// SpikeTrap — Merusak pemain yang terkena duri beracun di lantai.
/// </summary>
public class SpikeTrap : MonoBehaviour
{
    public int damage = 20;
    private float lastDamageTime = -999f;
    private float damageCooldown = 1.0f;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && Time.time >= lastDamageTime + damageCooldown)
        {
            lastDamageTime = Time.time;
            Entity ent = other.GetComponent<Entity>();
            if (ent != null)
            {
                ent.TakeDamage(damage);
            }
        }
    }
}

/// <summary>
/// RotatingPickup — Komponen agar objek item (kunci, permata) berputar lambat di dunia.
/// </summary>
public class RotatingPickup : MonoBehaviour
{
    public float rotateSpeed = 90f;

    private void Update()
    {
        transform.Rotate(Vector3.forward, rotateSpeed * Time.deltaTime, Space.World);
    }
}

/// <summary>
/// KeyPickupTrigger — Mendeteksi pemain menyentuh Kunci Sel dan memberitahu Manager.
/// </summary>
public class KeyPickupTrigger : MonoBehaviour
{
    public Chapter1Manager manager;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (manager != null) manager.OnCellKeyPickedUp();
        }
    }
}
