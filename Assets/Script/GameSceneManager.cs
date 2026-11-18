using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// GameSceneManager — Membangun scene gameplay dark fantasy secara prosedural.
/// Scene: pemakaman gelap dengan kabut ungu, lantai batu, pohon mati, musuh bergelombang.
/// HUD: portrait + HP/MP bar kiri-atas, tombol skill kanan, hint bawah.
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    // =====================================================================
    // KONSTANTA WARNA DARK FANTASY
    // =====================================================================
    static readonly Color COL_BG_DEEP       = new Color(0.04f, 0.02f, 0.08f, 1f);   // ungu gelap
    static readonly Color COL_BG_MID        = new Color(0.07f, 0.03f, 0.14f, 1f);   // ungu tengah
    static readonly Color COL_FLOOR         = new Color(0.13f, 0.09f, 0.20f, 1f);   // batu ungu
    static readonly Color COL_FLOOR_DETAIL  = new Color(0.17f, 0.12f, 0.26f, 1f);   // detail lantai
    static readonly Color COL_PURPLE_FOG    = new Color(0.38f, 0.10f, 0.85f, 0.12f);// kabut
    static readonly Color COL_GOLD          = new Color(0.98f, 0.78f, 0.30f, 1f);   // emas
    static readonly Color COL_TEAL_LEAF     = new Color(0.10f, 0.55f, 0.50f, 1f);   // daun teal
    static readonly Color COL_PURPLE_LEAF   = new Color(0.45f, 0.15f, 0.70f, 1f);   // daun ungu
    static readonly Color COL_HP_RED        = new Color(0.80f, 0.12f, 0.12f, 1f);   // HP merah
    static readonly Color COL_MP_CYAN       = new Color(0.10f, 0.65f, 0.90f, 1f);   // MP biru
    static readonly Color COL_PANEL_BG      = new Color(0.05f, 0.03f, 0.10f, 0.88f);// panel gelap
    static readonly Color COL_SKILL_BTN     = new Color(0.12f, 0.06f, 0.25f, 0.90f);// tombol skill
    static readonly Color COL_SKILL_RIM     = new Color(0.55f, 0.20f, 0.95f, 1f);   // rim skill

    // =====================================================================
    // STATE GAME
    // =====================================================================
    private int    hpMax    = 100;
    private int    hpCur    = 100;
    private int    mpMax    = 50;
    private int    mpCur    = 50;
    private int    level    = 1;
    private int    score    = 0;
    private float  waveTimer = 0f;
    private float  waveInterval = 18f;
    private int    waveCount = 0;
    private bool   gameOver  = false;
    private bool   paused    = false;

    // UI refs
    private Image  hpFill;
    private Image  mpFill;
    private TMP_Text hpText;
    private TMP_Text mpText;
    private TMP_Text scoreText;
    private TMP_Text waveText;
    private GameObject pausePanel;
    private GameObject gameOverPanel;

    // Player ref
    private GameObject player;
    private Camera     mainCam;
    private float      camFollowSpeed = 5f;

    // Font
    private TMP_FontAsset tmpFont;

    // Spawn musuh
    private List<GameObject> activeEnemies = new List<GameObject>();
    private float groundY = -4.5f;

    // =====================================================================
    // INISIALISASI
    // =====================================================================
    void Awake()
    {
        tmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    void Start()
    {
        BuildScene();
    }

    void BuildScene()
    {
        // Urutan build
        BuildBackground();
        BuildFloor();
        BuildEnvironment();
        BuildAtmosphere();
        SpawnPlayer();
        BuildHUD();
        BuildPausePanel();
        BuildGameOverPanel();
        SetupCamera();
        StartCoroutine(WaveSpawner());
        StartCoroutine(IntroText());
    }

    // =====================================================================
    // 1. BACKGROUND — langit ungu berbintang
    // =====================================================================
    void BuildBackground()
    {
        // Layer paling belakang: warna dasar
        CreateQuad("BG_Base", new Vector3(0, 0, 10f), new Vector2(60f, 30f), COL_BG_DEEP, -20);

        // Gradasi tengah
        CreateQuad("BG_Mid", new Vector3(0, 2f, 9.5f), new Vector2(60f, 16f), COL_BG_MID, -19);

        // Bulan besar atas tengah
        GameObject moon = CreateCircleSprite("Moon", new Vector3(4f, 7.5f, 9f), 2.6f, new Color(0.75f, 0.65f, 0.90f, 0.95f), -18);

        // Cahaya bulan (glow)
        CreateCircleSprite("MoonGlow", new Vector3(4f, 7.5f, 9.1f), 4.0f, new Color(0.55f, 0.30f, 0.80f, 0.18f), -19);

        // Bintang-bintang
        System.Random rng = new System.Random(42);
        for (int i = 0; i < 90; i++)
        {
            float sx = (float)(rng.NextDouble() * 50 - 25);
            float sy = (float)(rng.NextDouble() * 10 + 2);
            float sz = 8.5f;
            float ss = (float)(rng.NextDouble() * 0.08f + 0.03f);
            float sa = (float)(rng.NextDouble() * 0.6f + 0.3f);
            Color sc = new Color(1f, 1f, 1f, sa);
            CreateCircleSprite("Star_" + i, new Vector3(sx, sy, sz), ss, sc, -17);
        }

        // Kabut horison
        CreateQuad("HorizonFog", new Vector3(0, -1.5f, 8f), new Vector2(60f, 6f),
            new Color(0.20f, 0.06f, 0.40f, 0.35f), -16);
    }

    // =====================================================================
    // 2. LANTAI — batu ungu berulang
    // =====================================================================
    void BuildFloor()
    {
        float floorY = groundY;

        // Tanah utama dengan collider
        GameObject floor = new GameObject("Floor");
        floor.transform.position = new Vector3(0, floorY - 0.5f, 0);
        floor.layer = 6; // Ground

        SpriteRenderer floorSR = floor.AddComponent<SpriteRenderer>();
        floorSR.sprite = CreateSolidSprite(COL_FLOOR);
        floorSR.transform.localScale = new Vector3(80f, 3f, 1f);
        floorSR.sortingOrder = -10;
        floorSR.color = COL_FLOOR;

        BoxCollider2D floorCol = floor.AddComponent<BoxCollider2D>();
        floorCol.size = new Vector2(80f, 3f);

        // Detail ubin di atas lantai
        for (int i = -18; i <= 18; i++)
        {
            float tx = i * 2.2f;
            GameObject tile = CreateQuad("Tile_" + i, new Vector3(tx, floorY + 0.05f, 0), new Vector2(2.0f, 0.35f), COL_FLOOR_DETAIL, -9);
            // Variasi warna sedikit
            if (Mathf.Abs(i) % 3 == 0)
            {
                SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(0.20f, 0.14f, 0.30f, 1f);
            }
        }

        // Tepi lantai (garis emas tipis)
        CreateQuad("FloorEdge", new Vector3(0, floorY + 0.23f, 0), new Vector2(80f, 0.08f),
            new Color(0.98f, 0.78f, 0.30f, 0.4f), -8);

        // Platform melayang 1
        BuildPlatform(new Vector3(-8f, -1.8f, 0), new Vector2(5f, 0.4f));
        // Platform melayang 2
        BuildPlatform(new Vector3(2f, -0.5f, 0), new Vector2(4f, 0.4f));
        // Platform melayang 3
        BuildPlatform(new Vector3(11f, -1.5f, 0), new Vector2(5f, 0.4f));
    }

    void BuildPlatform(Vector3 pos, Vector2 size)
    {
        GameObject plat = new GameObject("Platform");
        plat.transform.position = pos;
        plat.layer = 6;

        SpriteRenderer sr = plat.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSolidSprite(COL_FLOOR);
        sr.sortingOrder = -8;
        sr.color = COL_FLOOR;
        plat.transform.localScale = new Vector3(size.x, size.y, 1f);

        BoxCollider2D bc = plat.AddComponent<BoxCollider2D>();
        bc.size = Vector2.one;

        // Detail tepi platform
        CreateQuad("PlatEdge_" + pos.x, new Vector3(pos.x, pos.y + size.y * 0.5f + 0.04f, 0),
            new Vector2(size.x, 0.08f), new Color(0.98f, 0.78f, 0.30f, 0.5f), -7);
    }

    // =====================================================================
    // 3. ENVIRONMENT — nisan, pohon mati, pilar
    // =====================================================================
    void BuildEnvironment()
    {
        // --- Pohon mati (kiri ke kanan) ---
        BuildDeadTree(new Vector3(-22f, groundY, -1f), 1.0f);
        BuildDeadTree(new Vector3(-15f, groundY, -1f), 1.3f);
        BuildDeadTree(new Vector3(-5f,  groundY, -1f), 0.9f);
        BuildDeadTree(new Vector3(16f,  groundY, -1f), 1.1f);
        BuildDeadTree(new Vector3(24f,  groundY, -1f), 1.4f);

        // --- Nisan ---
        BuildGravestone(new Vector3(-18f, groundY + 0.25f, -0.5f), 0.8f);
        BuildGravestone(new Vector3(-12f, groundY + 0.25f, -0.5f), 1.0f);
        BuildGravestone(new Vector3(-6f,  groundY + 0.25f, -0.5f), 0.7f);
        BuildGravestone(new Vector3(8f,   groundY + 0.25f, -0.5f), 0.9f);
        BuildGravestone(new Vector3(14f,  groundY + 0.25f, -0.5f), 0.75f);
        BuildGravestone(new Vector3(20f,  groundY + 0.25f, -0.5f), 1.1f);

        // --- Pilar reruntuhan ---
        BuildPillar(new Vector3(-20f, groundY + 1.5f, -0.8f), 0.9f);
        BuildPillar(new Vector3(18f,  groundY + 1.5f, -0.8f), 1.0f);

        // --- Puing/batu kecil ---
        BuildRubble(new Vector3(-9f,  groundY + 0.1f, -0.3f));
        BuildRubble(new Vector3(5f,   groundY + 0.1f, -0.3f));
        BuildRubble(new Vector3(12f,  groundY + 0.1f, -0.3f));
    }

    void BuildDeadTree(Vector3 pos, float scale)
    {
        GameObject tree = new GameObject("DeadTree");
        tree.transform.position = pos;

        // Batang
        GameObject trunk = CreateQuad("Trunk", pos + new Vector3(0, 1.5f * scale, 0),
            new Vector2(0.25f * scale, 3.0f * scale), new Color(0.18f, 0.10f, 0.14f, 1f), -5);

        // Cabang kiri
        GameObject branchL = CreateQuad("BranchL", pos + new Vector3(-0.6f * scale, 2.5f * scale, 0),
            new Vector2(1.2f * scale, 0.18f * scale), new Color(0.18f, 0.10f, 0.14f, 1f), -5);
        branchL.transform.rotation = Quaternion.Euler(0, 0, 25f);

        // Cabang kanan
        GameObject branchR = CreateQuad("BranchR", pos + new Vector3(0.6f * scale, 2.8f * scale, 0),
            new Vector2(1.0f * scale, 0.16f * scale), new Color(0.18f, 0.10f, 0.14f, 1f), -5);
        branchR.transform.rotation = Quaternion.Euler(0, 0, -20f);

        // Daun teal (kiri)
        CreateCircleSprite("LeafTeal_L_" + pos.x, pos + new Vector3(-1.0f * scale, 3.0f * scale, 0),
            0.6f * scale, new Color(COL_TEAL_LEAF.r, COL_TEAL_LEAF.g, COL_TEAL_LEAF.b, 0.80f), -4);
        // Daun ungu (kanan)
        CreateCircleSprite("LeafPurple_R_" + pos.x, pos + new Vector3(0.9f * scale, 3.2f * scale, 0),
            0.55f * scale, new Color(COL_PURPLE_LEAF.r, COL_PURPLE_LEAF.g, COL_PURPLE_LEAF.b, 0.75f), -4);
        // Daun campuran (atas)
        CreateCircleSprite("LeafTop_" + pos.x, pos + new Vector3(0f, 3.6f * scale, 0),
            0.45f * scale, new Color(0.30f, 0.40f, 0.60f, 0.70f), -4);
    }

    void BuildGravestone(Vector3 pos, float scale)
    {
        // Tubuh nisan
        CreateQuad("GS_Body_" + pos.x, pos + new Vector3(0, 0.6f * scale, 0),
            new Vector2(0.6f * scale, 1.1f * scale), new Color(0.22f, 0.15f, 0.30f, 1f), -6);
        // Kepala melengkung (simulasi dengan lingkaran gepeng)
        CreateCircleSprite("GS_Top_" + pos.x, pos + new Vector3(0, 1.2f * scale, 0.1f),
            0.32f * scale, new Color(0.25f, 0.17f, 0.35f, 1f), -6);
        // Garis salib
        CreateQuad("GS_Cross_H_" + pos.x, pos + new Vector3(0, 0.8f * scale, -0.1f),
            new Vector2(0.4f * scale, 0.07f * scale), new Color(0.65f, 0.45f, 0.80f, 0.6f), -5);
        CreateQuad("GS_Cross_V_" + pos.x, pos + new Vector3(0, 1.0f * scale, -0.1f),
            new Vector2(0.07f * scale, 0.4f * scale), new Color(0.65f, 0.45f, 0.80f, 0.6f), -5);
    }

    void BuildPillar(Vector3 pos, float scale)
    {
        // Tubuh pilar
        CreateQuad("Pillar_" + pos.x, pos, new Vector2(0.6f * scale, 3.0f * scale),
            new Color(0.20f, 0.13f, 0.28f, 1f), -6);
        // Kepala pilar
        CreateQuad("PillarTop_" + pos.x, pos + new Vector3(0, 1.6f * scale, 0),
            new Vector2(0.9f * scale, 0.3f * scale), new Color(0.26f, 0.17f, 0.35f, 1f), -5);
        // Retak
        CreateQuad("PillarCrack_" + pos.x, pos + new Vector3(0.1f * scale, 0.5f * scale, -0.1f),
            new Vector2(0.06f * scale, 1.0f * scale), new Color(0.08f, 0.04f, 0.12f, 0.7f), -4);
    }

    void BuildRubble(Vector3 pos)
    {
        for (int i = 0; i < 4; i++)
        {
            float rx = pos.x + (i - 1.5f) * 0.3f;
            float ry = pos.y + (i % 2) * 0.08f;
            float rs = 0.12f + (i % 3) * 0.06f;
            CreateQuad("Rubble_" + i + "_" + pos.x, new Vector3(rx, ry, pos.z),
                new Vector2(rs, rs * 0.7f), new Color(0.20f, 0.13f, 0.26f, 1f), -6);
        }
    }

    // =====================================================================
    // 4. ATMOSFER — partikel kabut ungu
    // =====================================================================
    void BuildAtmosphere()
    {
        // Kabut bawah
        BuildFogStrip("FogLow", new Vector3(0, groundY + 0.4f, -2f), new Color(0.30f, 0.08f, 0.60f, 0.18f));
        // Kabut tengah
        BuildFogStrip("FogMid", new Vector3(0, 0f, -1.5f), new Color(0.20f, 0.05f, 0.50f, 0.08f));

        // Partikel debu ungu mengambang
        BuildFogParticles();

        // Cahaya ungu di latar pohon
        for (int i = 0; i < 5; i++)
        {
            float gx = -20f + i * 10f;
            CreateCircleSprite("GlowOrb_" + i, new Vector3(gx, groundY + 2.5f, -1f),
                1.5f, new Color(0.50f, 0.15f, 0.90f, 0.07f), -3);
        }
    }

    void BuildFogStrip(string name, Vector3 pos, Color col)
    {
        GameObject fog = new GameObject(name);
        fog.transform.position = pos;

        SpriteRenderer sr = fog.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSolidSprite(col);
        sr.color = col;
        sr.sortingOrder = -3;
        fog.transform.localScale = new Vector3(80f, 2.5f, 1f);
    }

    void BuildFogParticles()
    {
        GameObject fogObj = new GameObject("FogParticles");
        ParticleSystem ps = fogObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.40f, 0.10f, 0.80f, 0.08f),
            new Color(0.60f, 0.20f, 0.95f, 0.18f)
        );
        main.maxParticles = 60;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;

        var emission = ps.emission;
        emission.rateOverTime = 4f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(50f, 6f, 1f);

        fogObj.transform.position = new Vector3(0, groundY + 1f, -2f);

        var psr = fogObj.GetComponent<ParticleSystemRenderer>();
        psr.sortingOrder = -2;
        psr.renderMode = ParticleSystemRenderMode.Billboard;

        ps.Play();
    }

    // =====================================================================
    // 5. PLAYER — spawn karakter utama
    // =====================================================================
    void SpawnPlayer()
    {
        // Cari player yang sudah ada
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) return;

        player = new GameObject("Aether_Player");
        player.transform.position = new Vector3(-8f, groundY + 0.5f, 0);
        player.tag = "Player";
        player.layer = 8; // Player

        // Sprite karakter (kotak placeholder)
        SpriteRenderer psr = player.AddComponent<SpriteRenderer>();
        psr.sprite = CreateSolidSprite(new Color(0.85f, 0.85f, 0.90f, 1f)); // putih abu
        psr.color = new Color(0.85f, 0.85f, 0.90f, 1f);
        psr.sortingOrder = 10;
        player.transform.localScale = new Vector3(0.65f, 1.1f, 1f);

        // Fisika
        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = 3f;

        CapsuleCollider2D cc = player.AddComponent<CapsuleCollider2D>();
        cc.size = new Vector2(0.9f, 1.8f);

        // Script player (kalau ada)
        Player playerScript = player.GetComponent<Player>();
        if (playerScript == null)
        {
            // Player script mungkin perlu komponen lain — tambah AudioSource
            AudioSource aud = player.AddComponent<AudioSource>();
            aud.playOnAwake = false;
        }
    }

    // =====================================================================
    // 6. HUD — antarmuka pengguna
    // =====================================================================
    void BuildHUD()
    {
        // Canvas utama HUD
        GameObject canvasObj = new GameObject("HUD_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();

        // --- PANEL KIRI ATAS: Portrait + Level + HP + MP ---
        BuildHUDTopLeft(canvasObj);

        // --- PANEL KANAN: Tombol Skill ---
        BuildHUDSkillButtons(canvasObj);

        // --- TEKS WAVE & SCORE ---
        BuildHUDWaveScore(canvasObj);

        // --- HINT BAWAH TENGAH ---
        BuildHUDHint(canvasObj);
    }

    void BuildHUDTopLeft(GameObject canvas)
    {
        // Panel latar
        GameObject panel = CreateUIPanel("HUD_TopLeft", canvas.transform,
            new Vector2(290, 100), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(10, -10));
        Image panelImg = panel.GetComponent<Image>();
        panelImg.color = COL_PANEL_BG;

        // Outline panel (emas)
        CreateUIOutline(panel, new Color(0.98f, 0.78f, 0.30f, 0.6f), 2f);

        // --- Portrait bulat ---
        GameObject portraitBG = CreateUIImage("Portrait_BG", panel.transform,
            new Vector2(68, 68), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(42, 0));
        portraitBG.GetComponent<Image>().color = new Color(0.08f, 0.04f, 0.18f, 1f);

        // Rim portrait
        CreateUIOutline(portraitBG, COL_GOLD, 2.5f);

        // Teks "Lv." di bawah portrait
        CreateUIText("LevelText", panel.transform, "Lv. " + level,
            new Vector2(68, 20), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(42, -40), 11f, COL_GOLD, TextAlignmentOptions.Center);

        // Nama karakter
        CreateUIText("NameText", panel.transform, "Aether",
            new Vector2(170, 22), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(100, 32), 12f, new Color(0.90f, 0.90f, 0.95f, 1f), TextAlignmentOptions.Left);

        // --- HP Bar ---
        GameObject hpBarBG = CreateUIImage("HP_BG", panel.transform,
            new Vector2(170, 16), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(100, 10));
        hpBarBG.GetComponent<Image>().color = new Color(0.12f, 0.05f, 0.05f, 1f);

        GameObject hpBarFill = CreateUIImage("HP_Fill", hpBarBG.transform,
            new Vector2(170, 16), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(0, 0));
        Image hpFillImg = hpBarFill.GetComponent<Image>();
        hpFillImg.color = COL_HP_RED;
        hpFillImg.type = Image.Type.Filled;
        hpFillImg.fillMethod = Image.FillMethod.Horizontal;
        hpFillImg.fillAmount = 1f;
        hpFill = hpFillImg;

        hpText = CreateUIText("HP_Text", hpBarBG.transform, "HP " + hpCur + " / " + hpMax,
            new Vector2(170, 16), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, 9.5f, Color.white, TextAlignmentOptions.Center);

        // Label HP
        CreateUIText("HP_Label", panel.transform, "HP",
            new Vector2(20, 16), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(84, 10), 9f, COL_HP_RED, TextAlignmentOptions.Left);

        // --- MP Bar ---
        GameObject mpBarBG = CreateUIImage("MP_BG", panel.transform,
            new Vector2(170, 14), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(100, -10));
        mpBarBG.GetComponent<Image>().color = new Color(0.03f, 0.08f, 0.15f, 1f);

        GameObject mpBarFill = CreateUIImage("MP_Fill", mpBarBG.transform,
            new Vector2(170, 14), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(0, 0));
        Image mpFillImg = mpBarFill.GetComponent<Image>();
        mpFillImg.color = COL_MP_CYAN;
        mpFillImg.type = Image.Type.Filled;
        mpFillImg.fillMethod = Image.FillMethod.Horizontal;
        mpFillImg.fillAmount = 1f;
        mpFill = mpFillImg;

        mpText = CreateUIText("MP_Text", mpBarBG.transform, "MP " + mpCur + " / " + mpMax,
            new Vector2(170, 14), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, 9f, Color.white, TextAlignmentOptions.Center);

        // Label MP
        CreateUIText("MP_Label", panel.transform, "MP",
            new Vector2(20, 14), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(84, -10), 9f, COL_MP_CYAN, TextAlignmentOptions.Left);
    }

    void BuildHUDSkillButtons(GameObject canvas)
    {
        // Container panel kanan
        GameObject panel = CreateUIPanel("HUD_Skills", canvas.transform,
            new Vector2(90, 280), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-10, -10));
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f); // transparan

        string[] skillNames  = { "Tebas", "Badai", "Ultimate" };
        string[] skillKeys   = { "[A]", "[S]", "[W]" };
        Color[]  skillColors = {
            new Color(0.90f, 0.30f, 0.30f, 1f),
            new Color(0.30f, 0.60f, 0.95f, 1f),
            new Color(0.90f, 0.65f, 0.10f, 1f)
        };

        for (int i = 0; i < 3; i++)
        {
            float btnY = -45 - i * 90f;

            // Lingkaran luar (rim)
            GameObject rim = CreateUIImage("Skill_Rim_" + i, panel.transform,
                new Vector2(78, 78), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, btnY + 4));
            rim.GetComponent<Image>().color = COL_SKILL_RIM;

            // Tombol utama
            GameObject btn = CreateUIImage("Skill_Btn_" + i, panel.transform,
                new Vector2(72, 72), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, btnY));
            Image btnImg = btn.GetComponent<Image>();
            btnImg.color = COL_SKILL_BTN;

            // Ikon (simbol ASCII)
            string icon = i == 0 ? "><" : i == 1 ? "~" : "***";
            CreateUIText("Skill_Icon_" + i, btn.transform, icon,
                new Vector2(72, 72), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, 20f, skillColors[i], TextAlignmentOptions.Center);

            // Label tombol
            CreateUIText("Skill_Label_" + i, panel.transform, skillNames[i] + " " + skillKeys[i],
                new Vector2(78, 18), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, btnY - 72), 8.5f, new Color(0.80f, 0.80f, 0.90f, 1f), TextAlignmentOptions.Center);
        }
    }

    void BuildHUDWaveScore(GameObject canvas)
    {
        // Teks wave atas tengah
        waveText = CreateUIText("WaveText", canvas.transform, "GELOMBANG 1",
            new Vector2(300, 36), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -12), 16f, new Color(0.90f, 0.80f, 1.0f, 0.90f), TextAlignmentOptions.Center);

        // Teks score kanan atas
        scoreText = CreateUIText("ScoreText", canvas.transform, "Skor: 0",
            new Vector2(200, 30), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-105, -12), 13f, COL_GOLD, TextAlignmentOptions.Right);
    }

    void BuildHUDHint(GameObject canvas)
    {
        // Panel hint bawah
        GameObject hintPanel = CreateUIPanel("HUD_Hint", canvas.transform,
            new Vector2(600, 32), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 10));
        hintPanel.GetComponent<Image>().color = new Color(0.03f, 0.01f, 0.08f, 0.70f);

        CreateUIText("HintText", hintPanel.transform,
            "[Panah] Gerak  [Spasi] Lompat  [A/S/W] Skill  [ESC] Jeda",
            new Vector2(590, 28), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, 10.5f, new Color(0.75f, 0.75f, 0.85f, 0.90f), TextAlignmentOptions.Center);
    }

    // =====================================================================
    // 7. PAUSE PANEL
    // =====================================================================
    void BuildPausePanel()
    {
        GameObject canvasObj = GameObject.Find("HUD_Canvas");
        if (canvasObj == null) return;

        pausePanel = CreateUIPanel("PausePanel", canvasObj.transform,
            new Vector2(400, 350), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero);
        pausePanel.GetComponent<Image>().color = new Color(0.04f, 0.02f, 0.10f, 0.95f);
        CreateUIOutline(pausePanel, COL_GOLD, 2f);

        CreateUIText("PauseTitle", pausePanel.transform, "- JEDA -",
            new Vector2(380, 50), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -20), 26f, COL_GOLD, TextAlignmentOptions.Center);

        // Tombol Lanjutkan
        BuildMenuButton("PauseResume", pausePanel.transform, "Lanjutkan [ESC]",
            new Vector2(0, -100), new Color(0.15f, 0.08f, 0.30f, 1f), () => TogglePause());

        // Tombol Keluar
        BuildMenuButton("PauseQuit", pausePanel.transform, "Keluar ke Menu",
            new Vector2(0, -170), new Color(0.25f, 0.05f, 0.08f, 1f), () => LoadMainMenu());

        pausePanel.SetActive(false);
    }

    // =====================================================================
    // 8. GAME OVER PANEL
    // =====================================================================
    void BuildGameOverPanel()
    {
        GameObject canvasObj = GameObject.Find("HUD_Canvas");
        if (canvasObj == null) return;

        gameOverPanel = CreateUIPanel("GameOverPanel", canvasObj.transform,
            new Vector2(500, 420), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero);
        gameOverPanel.GetComponent<Image>().color = new Color(0.08f, 0.01f, 0.04f, 0.97f);
        CreateUIOutline(gameOverPanel, new Color(0.80f, 0.10f, 0.10f, 1f), 3f);

        CreateUIText("GameOverTitle", gameOverPanel.transform, "GUGUR",
            new Vector2(480, 70), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -25), 42f, new Color(0.95f, 0.20f, 0.20f, 1f), TextAlignmentOptions.Center);

        CreateUIText("GameOverSub", gameOverPanel.transform,
            "\"Dunia tidak akan berhenti\nhanya karena kau terjatuh...\"",
            new Vector2(460, 80), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -110), 14f, new Color(0.75f, 0.65f, 0.85f, 0.9f), TextAlignmentOptions.Center);

        BuildMenuButton("GORetry", gameOverPanel.transform, "Coba Lagi",
            new Vector2(0, -210), new Color(0.15f, 0.08f, 0.30f, 1f), () => ReloadScene());

        BuildMenuButton("GOMenu", gameOverPanel.transform, "Menu Utama",
            new Vector2(0, -280), new Color(0.20f, 0.05f, 0.05f, 1f), () => LoadMainMenu());

        gameOverPanel.SetActive(false);
    }

    void BuildMenuButton(string name, Transform parent, string label, Vector2 anchoredPos, Color bgColor, System.Action onClick)
    {
        GameObject btn = CreateUIImage(name, parent,
            new Vector2(260, 50), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            anchoredPos);
        btn.GetComponent<Image>().color = bgColor;
        CreateUIOutline(btn, COL_GOLD, 1.5f);

        CreateUIText(name + "_Txt", btn.transform, label,
            new Vector2(250, 48), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, 15f, Color.white, TextAlignmentOptions.Center);

        Button btnComp = btn.AddComponent<Button>();
        btnComp.onClick.AddListener(() => onClick?.Invoke());
    }

    // =====================================================================
    // 9. KAMERA — ikuti player
    // =====================================================================
    void SetupCamera()
    {
        mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("MainCamera");
            camObj.tag = "MainCamera";
            mainCam = camObj.AddComponent<Camera>();
            mainCam.transform.position = new Vector3(0, 0, -10f);
        }

        mainCam.orthographic = true;
        mainCam.orthographicSize = 6.0f;
        mainCam.backgroundColor = COL_BG_DEEP;
        mainCam.clearFlags = CameraClearFlags.SolidColor;
    }

    void LateUpdate()
    {
        if (mainCam == null || player == null || gameOver) return;

        // Ikuti player secara smooth, batasi Y
        Vector3 target = new Vector3(player.transform.position.x, 0f, -10f);
        mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, target, camFollowSpeed * Time.deltaTime);
    }

    // =====================================================================
    // 10. INPUT & LOGIKA GAME
    // =====================================================================
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (gameOver || paused) return;

        // Update wave timer
        waveTimer += Time.deltaTime;

        // Bersihkan musuh yang sudah mati dari daftar
        activeEnemies.RemoveAll(e => e == null);
    }

    void TogglePause()
    {
        if (gameOver) return;
        paused = !paused;
        Time.timeScale = paused ? 0f : 1f;
        if (pausePanel != null) pausePanel.SetActive(paused);
    }

    public void TakeDamage(int dmg)
    {
        if (gameOver) return;
        hpCur = Mathf.Max(0, hpCur - dmg);
        RefreshHUD();
        if (hpCur <= 0) TriggerGameOver();
    }

    public void RestoreMP(int amount)
    {
        mpCur = Mathf.Min(mpMax, mpCur + amount);
        RefreshHUD();
    }

    public void AddScore(int pts)
    {
        score += pts;
        if (scoreText != null) scoreText.text = "Skor: " + score;
    }

    void RefreshHUD()
    {
        if (hpFill != null)  hpFill.fillAmount  = (float)hpCur / hpMax;
        if (hpText != null)  hpText.text = "HP " + hpCur + " / " + hpMax;
        if (mpFill != null)  mpFill.fillAmount  = (float)mpCur / mpMax;
        if (mpText != null)  mpText.text = "MP " + mpCur + " / " + mpMax;
    }

    void TriggerGameOver()
    {
        gameOver = true;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    void ReloadScene() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    void LoadMainMenu() { Time.timeScale = 1f; SceneManager.LoadScene("Main Menu"); }

    // =====================================================================
    // 11. COROUTINE — gelombang musuh
    // =====================================================================
    IEnumerator WaveSpawner()
    {
        yield return new WaitForSeconds(3f);

        while (!gameOver)
        {
            waveCount++;
            if (waveText != null) waveText.text = "GELOMBANG " + waveCount;

            int enemyCount = 2 + waveCount;
            for (int i = 0; i < enemyCount; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(1.5f);
            }

            yield return new WaitForSeconds(waveInterval);
        }
    }

    void SpawnEnemy()
    {
        // Spawn dari kanan atau kiri layar
        float side = Random.value > 0.5f ? 1f : -1f;
        float spawnX = side * (12f + Random.Range(0f, 4f));

        GameObject enemy = new GameObject("GraveWalker");
        enemy.transform.position = new Vector3(spawnX, groundY + 0.5f, 0);
        enemy.tag = "Enemy";
        enemy.layer = 7; // Enemy

        SpriteRenderer esr = enemy.AddComponent<SpriteRenderer>();
        esr.sprite = CreateSolidSprite(new Color(0.30f, 0.08f, 0.50f, 1f));
        esr.color = new Color(0.30f, 0.08f, 0.50f, 1f);
        esr.sortingOrder = 10;
        enemy.transform.localScale = new Vector3(0.55f, 0.95f, 1f);

        // Mata merah
        GameObject eyeL = CreateCircleSprite("EyeL", enemy.transform.position + new Vector3(-0.08f, 0.25f, -0.1f),
            0.07f, new Color(1f, 0.1f, 0.1f, 0.9f), 11);
        GameObject eyeR = CreateCircleSprite("EyeR", enemy.transform.position + new Vector3(0.08f, 0.25f, -0.1f),
            0.07f, new Color(1f, 0.1f, 0.1f, 0.9f), 11);
        eyeL.transform.SetParent(enemy.transform);
        eyeR.transform.SetParent(enemy.transform);

        Rigidbody2D erb = enemy.AddComponent<Rigidbody2D>();
        erb.freezeRotation = true;
        erb.gravityScale = 3f;

        CapsuleCollider2D ecc = enemy.AddComponent<CapsuleCollider2D>();
        ecc.size = new Vector2(0.9f, 1.8f);

        // AI simple
        SimpleEnemyAI ai = enemy.AddComponent<SimpleEnemyAI>();
        ai.Init(player, side, this);

        activeEnemies.Add(enemy);
    }

    IEnumerator IntroText()
    {
        // Teks intro singkat di awal scene
        GameObject canvasObj = GameObject.Find("HUD_Canvas");
        if (canvasObj == null) yield break;

        TMP_Text intro = CreateUIText("IntroMsg", canvasObj.transform,
            "Dunia ini adalah makam...\ndan kau adalah satu-satunya yang masih berjalan.",
            new Vector2(800, 100), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 80), 18f, new Color(0.80f, 0.70f, 0.95f, 0f), TextAlignmentOptions.Center);

        // Fade in
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 0.6f;
            intro.color = new Color(0.80f, 0.70f, 0.95f, t);
            yield return null;
        }
        yield return new WaitForSeconds(3.5f);

        // Fade out
        t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime * 0.8f;
            intro.color = new Color(0.80f, 0.70f, 0.95f, t);
            yield return null;
        }
        Destroy(intro.gameObject);
    }

    // =====================================================================
    // HELPER: Buat Sprite/GameObject Primitif
    // =====================================================================
    Sprite CreateSolidSprite(Color col)
    {
        Texture2D tex = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = col;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    Sprite CreateCircleSpriteTexture(int res = 64)
    {
        Texture2D tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;
        float r = res / 2f;
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dx = x - r, dy = y - r;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - (dist - (r - 1.5f)) / 1.5f);
                tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }

    GameObject CreateQuad(string name, Vector3 pos, Vector2 size, Color col, int sortOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSolidSprite(col);
        sr.color = col;
        sr.sortingOrder = sortOrder;
        return go;
    }

    GameObject CreateCircleSprite(string name, Vector3 pos, float radius, Color col, int sortOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSpriteTexture(64);
        sr.color = col;
        sr.sortingOrder = sortOrder;
        return go;
    }

    // =====================================================================
    // HELPER: UI
    // =====================================================================
    GameObject CreateUIPanel(string name, Transform parent, Vector2 size,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = COL_PANEL_BG;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        return go;
    }

    GameObject CreateUIImage(string name, Transform parent, Vector2 size,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        return go;
    }

    TMP_Text CreateUIText(string name, Transform parent, string text, Vector2 size,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos,
        float fontSize, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TMP_Text t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = align;
        t.enableAutoSizing = false;
        if (tmpFont != null) t.font = tmpFont;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        return t;
    }

    void CreateUIOutline(GameObject target, Color color, float width)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(width, -width);
    }
}

// =========================================================================
// SimpleEnemyAI — AI musuh dasar yang mengejar player
// =========================================================================
public class SimpleEnemyAI : MonoBehaviour
{
    private GameObject target;
    private float side;
    private GameSceneManager manager;
    private float speed;
    private int hp = 3;
    private Rigidbody2D rb;
    private bool isDead = false;

    public void Init(GameObject player, float spawnSide, GameSceneManager mgr)
    {
        target  = player;
        side    = spawnSide;
        manager = mgr;
        speed   = Random.Range(1.5f, 3.0f);
        rb      = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (isDead || target == null) return;

        Vector3 dir = (target.transform.position - transform.position).normalized;
        transform.position += new Vector3(dir.x * speed * Time.deltaTime, 0, 0);

        // Flip sprite
        if (dir.x < 0) transform.localScale = new Vector3(-0.55f, 0.95f, 1f);
        else            transform.localScale = new Vector3( 0.55f, 0.95f, 1f);

        // Serang jika dekat
        float dist = Vector2.Distance(transform.position, target.transform.position);
        if (dist < 0.8f)
        {
            if (manager != null) manager.TakeDamage(1);
            Destroy(gameObject);
        }
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;
        hp -= dmg;
        if (hp <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        if (manager != null) manager.AddScore(100);
        Destroy(gameObject);
    }
}
