using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// GameSceneManager — Scene gameplay dark fantasy "Born After The End"
/// Memuat sprite asset nyata: Aether, tileset environment, HUD pixel art
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    // ===== WARNA =====
    static readonly Color COL_BG        = new Color(0.04f, 0.02f, 0.08f, 1f);
    static readonly Color COL_GOLD      = new Color(0.98f, 0.78f, 0.30f, 1f);
    static readonly Color COL_HP_RED    = new Color(0.80f, 0.12f, 0.12f, 1f);
    static readonly Color COL_MP_CYAN   = new Color(0.10f, 0.65f, 0.90f, 1f);
    static readonly Color COL_PANEL     = new Color(0.05f, 0.02f, 0.12f, 0.92f);

    // ===== SPRITE ASSETS =====
    private Texture2D texAether;
    private Texture2D texTileset;
    private Texture2D texHUD;
    private Sprite[] aetherFrames;   // frame animasi Aether
    private Sprite   spriteTileset;
    private Sprite   spriteHUD;

    // ===== ANIMASI =====
    // Layout spritesheet Aether: 4 baris
    // Row 0 (y=top): Idle(4) + Walk(6) = 10 frame
    // Row 1       : Run(4) + Jump(5)   = 9 frame  
    // Row 2       : Attack(5) + Skill(4)= 9 frame
    // Row 3 (bot) : Hurt(2) + Die(4)   = 6 frame
    private const int FRAME_W = 10; // kolom per baris (perkiraan)
    private const int FRAME_H = 4;  // jumlah baris

    private int   animFrame      = 0;
    private float animTimer      = 0f;
    private float animSpeed      = 0.1f;
    private string currentAnim  = "idle";
    private int   animStart     = 0;
    private int   animLength    = 4;
    private SpriteRenderer playerSR;

    // ===== PLAYER =====
    private GameObject player;
    private Rigidbody2D playerRB;
    private bool isGrounded  = false;
    private bool facingRight = true;
    private float moveSpeed  = 5f;
    private float jumpForce  = 10f;
    private float groundY    = -4.2f;

    // ===== GAME STATE =====
    private int   hpMax = 100, hpCur = 100;
    private int   mpMax = 50,  mpCur = 50;
    private int   level = 1,   score = 0;
    private int   waveCount  = 0;
    private bool  gameOver   = false;
    private bool  paused     = false;
    private float invincibleTimer = 0f;

    // ===== UI REFS =====
    private Image    hpFill, mpFill;
    private TMP_Text hpText, mpText, scoreText, waveLabel;
    private GameObject pausePanel, gameOverPanel;
    private TMP_FontAsset font;

    // ===== ENEMY =====
    private List<GameObject> enemies = new List<GameObject>();
    private float waveDelay = 20f;

    // ===== KAMERA =====
    private Camera cam;

    // ================================================================
    void Awake()
    {
        font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        LoadAssets();
    }

    void Start()
    {
        BuildScene();
    }

    // ================================================================
    // LOAD SPRITE DARI RESOURCES
    // ================================================================
    void LoadAssets()
    {
        // Load Aether spritesheet
        texAether  = LoadTex("Stage1/aether_spritesheet");
        // Load environment tileset
        texTileset = LoadTex("Stage1/tileset_environment");
        // Load HUD
        texHUD     = LoadTex("Stage1/hud_ui");

        if (texAether != null) SliceAetherFrames();
        if (texTileset != null) spriteTileset = Sprite.Create(texTileset,
            new Rect(0, 0, texTileset.width, texTileset.height), new Vector2(0.5f, 0.5f), 100f);
        if (texHUD != null) spriteHUD = Sprite.Create(texHUD,
            new Rect(0, 0, texHUD.width, texHUD.height), new Vector2(0.5f, 0.5f), 100f);
    }

    Texture2D LoadTex(string path)
    {
        // Coba load sebagai Texture2D dari Resources
        Texture2D t = Resources.Load<Texture2D>(path);
        if (t != null) return t;

        // Fallback: buat texture solid gelap
        Texture2D fallback = new Texture2D(64, 64);
        Color[] px = new Color[64 * 64];
        for (int i = 0; i < px.Length; i++) px[i] = COL_BG;
        fallback.SetPixels(px);
        fallback.Apply();
        Debug.LogWarning("[GameSceneManager] Asset tidak ditemukan: " + path);
        return fallback;
    }

    /// <summary>
    /// Potong spritesheet Aether menjadi frame individual.
    /// Spritesheet punya 4 baris, estimasi ~10 kolom.
    /// Idle: baris 0 kol 0-3 | Walk: baris 0 kol 4-9
    /// Run: baris 1 kol 0-3  | Jump: baris 1 kol 4-8
    /// Attack: baris 2 kol 0-4 | Skill: baris 2 kol 5-8
    /// Hurt: baris 3 kol 0-1 | Die: baris 3 kol 2-5
    /// </summary>
    void SliceAetherFrames()
    {
        int tw = texAether.width;
        int th = texAether.height;
        int fw = tw / FRAME_W;  // lebar satu frame
        int fh = th / FRAME_H;  // tinggi satu frame

        List<Sprite> frames = new List<Sprite>();
        for (int row = 0; row < FRAME_H; row++)
        {
            for (int col = 0; col < FRAME_W; col++)
            {
                int x = col * fw;
                int y = th - (row + 1) * fh; // Unity Y dari bawah
                Rect rect = new Rect(x, y, fw, fh);
                Sprite s = Sprite.Create(texAether, rect, new Vector2(0.5f, 0.5f), (float)fh / 2f);
                frames.Add(s);
            }
        }
        aetherFrames = frames.ToArray();
    }

    // ================================================================
    // BUILD SCENE
    // ================================================================
    void BuildScene()
    {
        SetupCamera();
        BuildBackground();
        BuildFloor();
        BuildEnvironmentProps();
        BuildFog();
        SpawnPlayer();
        BuildHUD();
        BuildPausePanel();
        BuildGameOverPanel();
        StartCoroutine(WaveSpawner());
        StartCoroutine(IntroFade());
    }

    // ================================================================
    // KAMERA
    // ================================================================
    void SetupCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            GameObject co = new GameObject("MainCamera");
            co.tag = "MainCamera";
            cam = co.AddComponent<Camera>();
            co.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 5.5f;
        cam.backgroundColor = COL_BG;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0, 0, -10f);
    }

    void LateUpdate()
    {
        if (cam == null || player == null) return;
        Vector3 t = new Vector3(player.transform.position.x, 0.2f, -10f);
        cam.transform.position = Vector3.Lerp(cam.transform.position, t, 6f * Time.deltaTime);
    }

    // ================================================================
    // BACKGROUND — langit ungu berbintang + bulan
    // ================================================================
    void BuildBackground()
    {
        // Langit malam gradient — gunakan bagian kanan tileset (sky panel)
        // Buat dari warna solid dark
        CreateWorld("Sky", new Vector3(0, 2f, 15f), new Vector2(100f, 20f),
            new Color(0.04f, 0.02f, 0.09f, 1f), -20);

        // Gradasi horison ungu
        CreateWorld("Horizon", new Vector3(0, -1f, 14f), new Vector2(100f, 8f),
            new Color(0.09f, 0.03f, 0.18f, 1f), -19);

        // Bintang prosedural
        System.Random rng = new System.Random(7);
        for (int i = 0; i < 80; i++)
        {
            float sx = (float)(rng.NextDouble() * 60 - 30);
            float sy = (float)(rng.NextDouble() * 8 + 2);
            float ss = (float)(rng.NextDouble() * 0.07f + 0.025f);
            float sa = (float)(rng.NextDouble() * 0.65f + 0.25f);
            CreateCircle("Star" + i, new Vector3(sx, sy, 13f), ss,
                new Color(1f, 0.95f, 1f, sa), -18);
        }

        // Bulan besar
        CreateCircle("Moon", new Vector3(5f, 6.5f, 12f), 2.0f,
            new Color(0.88f, 0.82f, 1.0f, 1f), -17);
        CreateCircle("MoonGlow", new Vector3(5f, 6.5f, 12.1f), 3.5f,
            new Color(0.60f, 0.35f, 0.90f, 0.12f), -18);
    }

    // ================================================================
    // LANTAI — tilemap batu
    // ================================================================
    void BuildFloor()
    {
        // Platform dasar dengan collider
        GameObject floor = new GameObject("Floor");
        floor.transform.position = new Vector3(0, groundY - 0.6f, 0);
        floor.layer = 6;

        SpriteRenderer sr = floor.AddComponent<SpriteRenderer>();
        // Gunakan bagian tileset (ground tile) — potong dari texTileset kiri-bawah
        if (texTileset != null)
        {
            int tw = texTileset.width / 2; // setengah kiri = ground tile
            int th = texTileset.height / 3;
            Rect tileRect = new Rect(0, 0, tw, th);
            sr.sprite = Sprite.Create(texTileset, tileRect, new Vector2(0.5f, 0.5f), (float)th);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(80f, 2.5f);
        }
        else
        {
            sr.sprite = MakeSolidSprite(new Color(0.14f, 0.09f, 0.22f, 1f));
            floor.transform.localScale = new Vector3(80f, 2.5f, 1f);
        }
        sr.sortingOrder = -10;
        floor.AddComponent<BoxCollider2D>().size =
            new Vector2(80f, 2.5f);

        // Garis emas tipis di tepi atas lantai
        CreateWorld("FloorEdge", new Vector3(0, groundY + 0.09f, 0),
            new Vector2(80f, 0.07f), new Color(0.98f, 0.78f, 0.30f, 0.5f), -9);

        // Platform melayang 1 (kiri)
        BuildPlatform(new Vector3(-8f, -1.5f, 0f), new Vector2(5.5f, 0.4f));
        // Platform melayang 2 (tengah)
        BuildPlatform(new Vector3(2f,  -0.3f, 0f), new Vector2(4.0f, 0.4f));
        // Platform melayang 3 (kanan)
        BuildPlatform(new Vector3(11f, -1.2f, 0f), new Vector2(5.0f, 0.4f));
    }

    void BuildPlatform(Vector3 pos, Vector2 size)
    {
        GameObject p = new GameObject("Platform");
        p.transform.position = pos;
        p.layer = 6;

        SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
        if (texTileset != null)
        {
            int tw = texTileset.width / 2;
            int th = texTileset.height / 3;
            sr.sprite = Sprite.Create(texTileset, new Rect(0, 0, tw, th),
                new Vector2(0.5f, 0.5f), (float)th);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
        }
        else
        {
            sr.sprite = MakeSolidSprite(new Color(0.14f, 0.09f, 0.22f, 1f));
            p.transform.localScale = new Vector3(size.x, size.y, 1f);
        }
        sr.sortingOrder = -9;

        BoxCollider2D bc = p.AddComponent<BoxCollider2D>();
        bc.size = size;

        // Tepi emas
        CreateWorld("PlatEdge" + pos.x,
            new Vector3(pos.x, pos.y + size.y * 0.5f + 0.03f, 0),
            new Vector2(size.x, 0.06f),
            new Color(0.98f, 0.78f, 0.30f, 0.55f), -8);
    }

    // ================================================================
    // ENVIRONMENT PROPS — pohon, nisan, pilar dari tileset
    // ================================================================
    void BuildEnvironmentProps()
    {
        // Pohon teal (dari tileset bagian bawah-tengah)
        PlaceProp("TreeTeal1", new Vector3(-16f, groundY + 1.8f, -1f), 2.2f, GetTreeTealSprite());
        PlaceProp("TreeTeal2", new Vector3(18f,  groundY + 2.0f, -1f), 2.0f, GetTreeTealSprite());
        PlaceProp("TreeDark1", new Vector3(-24f, groundY + 1.5f, -1f), 1.8f, GetTreeDarkSprite());
        PlaceProp("TreeDark2", new Vector3(26f,  groundY + 1.5f, -1f), 1.9f, GetTreeDarkSprite());

        // Nisan (dari tileset)
        PlaceProp("Grave1", new Vector3(-12f, groundY + 0.6f, -0.5f), 1.0f, GetGraveSprite());
        PlaceProp("Grave2", new Vector3(-5f,  groundY + 0.6f, -0.5f), 0.8f, GetGraveSprite());
        PlaceProp("Grave3", new Vector3(8f,   groundY + 0.6f, -0.5f), 1.0f, GetGraveSprite());
        PlaceProp("Grave4", new Vector3(15f,  groundY + 0.6f, -0.5f), 0.9f, GetGraveSprite());

        // Pilar reruntuhan
        PlaceProp("Pillar1", new Vector3(-20f, groundY + 1.5f, -0.8f), 1.5f, GetPillarSprite());
        PlaceProp("Pillar2", new Vector3(22f,  groundY + 1.5f, -0.8f), 1.3f, GetPillarSprite());
    }

    // Ambil region sprite dari tileset
    Sprite GetTreeTealSprite()  => CropTileset(0.50f, 0.00f, 0.22f, 0.45f);
    Sprite GetTreeDarkSprite()  => CropTileset(0.72f, 0.00f, 0.15f, 0.45f);
    Sprite GetGraveSprite()     => CropTileset(0.50f, 0.45f, 0.12f, 0.28f);
    Sprite GetPillarSprite()    => CropTileset(0.63f, 0.45f, 0.10f, 0.35f);

    // Crop region dari texTileset (normalized 0-1)
    Sprite CropTileset(float nx, float ny, float nw, float nh)
    {
        if (texTileset == null) return MakeSolidSprite(new Color(0.2f, 0.1f, 0.3f, 1f));
        int tw = texTileset.width, th = texTileset.height;
        Rect r = new Rect(nx * tw, ny * th, nw * tw, nh * th);
        // Balik Y karena Unity dari bawah
        r.y = th - r.y - r.height;
        r.y = Mathf.Max(0, r.y);
        r.height = Mathf.Min(r.height, th - r.y);
        return Sprite.Create(texTileset, r, new Vector2(0.5f, 0f), r.height);
    }

    void PlaceProp(string name, Vector3 pos, float scale, Sprite spr)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr ?? MakeSolidSprite(new Color(0.2f, 0.1f, 0.3f));
        sr.sortingOrder = -5;
    }

    // ================================================================
    // KABUT UNGU
    // ================================================================
    void BuildFog()
    {
        // Strip kabut bawah dari tileset (baris paling bawah, bagian kanan)
        Sprite fogSprite = CropTileset(0.50f, 0.75f, 0.50f, 0.25f);

        CreateFogStrip("FogLow",  new Vector3(0, groundY + 0.3f, -2f), fogSprite);
        CreateFogStrip("FogMid",  new Vector3(0, 0.5f, -1.5f),         fogSprite);

        // Partikel kabut
        GameObject fogObj = new GameObject("FogParticles");
        fogObj.transform.position = new Vector3(0, groundY + 0.5f, -2f);
        ParticleSystem ps = fogObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(5f, 10f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.05f, 0.20f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.5f, 1.8f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(0.35f, 0.08f, 0.70f, 0.07f),
            new Color(0.55f, 0.18f, 0.90f, 0.16f));
        main.maxParticles    = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;
        var em = ps.emission; em.rateOverTime = 3f;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(60f, 4f, 1f);
        fogObj.GetComponent<ParticleSystemRenderer>().sortingOrder = -2;
        ps.Play();
    }

    void CreateFogStrip(string name, Vector3 pos, Sprite spr)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr ?? MakeSolidSprite(new Color(0.3f, 0.05f, 0.6f, 0.15f));
        sr.color = new Color(1f, 1f, 1f, 0.35f);
        sr.sortingOrder = -3;
        go.transform.localScale = new Vector3(10f, 1f, 1f);
    }

    // ================================================================
    // PLAYER SPAWN
    // ================================================================
    void SpawnPlayer()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = new GameObject("Aether");
            player.tag   = "Player";
            player.layer = 8;
        }

        player.transform.position = new Vector3(-7f, groundY + 0.6f, 0f);

        // Sprite Renderer
        playerSR = player.GetComponent<SpriteRenderer>();
        if (playerSR == null) playerSR = player.AddComponent<SpriteRenderer>();

        if (aetherFrames != null && aetherFrames.Length > 0)
            playerSR.sprite = aetherFrames[0];
        else
            playerSR.sprite = MakeSolidSprite(new Color(0.7f, 0.7f, 0.9f, 1f));

        playerSR.sortingOrder = 10;
        player.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

        // Fisika
        playerRB = player.GetComponent<Rigidbody2D>();
        if (playerRB == null)
        {
            playerRB = player.AddComponent<Rigidbody2D>();
            playerRB.freezeRotation = true;
            playerRB.gravityScale   = 4f;
            playerRB.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        CapsuleCollider2D cc = player.GetComponent<CapsuleCollider2D>();
        if (cc == null)
        {
            cc = player.AddComponent<CapsuleCollider2D>();
            cc.size = new Vector2(0.6f, 1.0f);
        }

        // Audio
        if (player.GetComponent<AudioSource>() == null)
            player.AddComponent<AudioSource>().playOnAwake = false;

        // Ground check child
        GameObject gc = new GameObject("GroundCheck");
        gc.transform.SetParent(player.transform);
        gc.transform.localPosition = new Vector3(0, -0.55f, 0);
    }

    // ================================================================
    // UPDATE — input + animasi player
    // ================================================================
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        if (gameOver || paused) return;

        HandlePlayerInput();
        UpdateAnimation();
        CheckGround();

        if (invincibleTimer > 0f) invincibleTimer -= Time.deltaTime;
        enemies.RemoveAll(e => e == null);
    }

    void HandlePlayerInput()
    {
        if (player == null || playerRB == null) return;

        float h = Input.GetAxisRaw("Horizontal");
        playerRB.velocity = new Vector2(h * moveSpeed, playerRB.velocity.y);

        // Flip
        if (h > 0 && !facingRight) { facingRight = true;  player.transform.localScale = new Vector3( 1.2f, 1.2f, 1f); }
        if (h < 0 &&  facingRight) { facingRight = false; player.transform.localScale = new Vector3(-1.2f, 1.2f, 1f); }

        // Lompat
        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W)) && isGrounded)
        {
            playerRB.velocity = new Vector2(playerRB.velocity.x, jumpForce);
            SetAnim("jump", 20, 3);
        }

        // Serangan A
        if (Input.GetKeyDown(KeyCode.A)) StartCoroutine(AttackAnim());
        // Skill S
        if (Input.GetKeyDown(KeyCode.S) && mpCur >= 10) StartCoroutine(SkillAnim());

        // Anim berjalan
        if (isGrounded && currentAnim != "attack" && currentAnim != "skill")
        {
            if (Mathf.Abs(h) > 0.1f)
                SetAnim("walk", 4, 6);
            else
                SetAnim("idle", 0, 4);
        }
    }

    void CheckGround()
    {
        if (player == null) return;
        Transform gc = player.transform.Find("GroundCheck");
        if (gc == null) return;
        isGrounded = Physics2D.OverlapCircle(gc.position, 0.15f,
            LayerMask.GetMask("Ground"));
    }

    // ================================================================
    // ANIMASI
    // ================================================================
    // Mapping frame ke indeks spritesheet (4 baris x 10 col):
    // idle    : baris 0, kol 0-3  → idx 0-3
    // walk    : baris 0, kol 4-9  → idx 4-9
    // run     : baris 1, kol 0-3  → idx 10-13
    // jump    : baris 1, kol 4-8  → idx 14-18
    // attack  : baris 2, kol 0-4  → idx 20-24
    // skill   : baris 2, kol 5-8  → idx 25-28
    // hurt    : baris 3, kol 0-1  → idx 30-31
    // die     : baris 3, kol 2-5  → idx 32-35

    void SetAnim(string name, int startIdx, int length)
    {
        if (currentAnim == name) return;
        currentAnim = name;
        animStart   = startIdx;
        animLength  = length;
        animFrame   = 0;
        animTimer   = 0f;
    }

    void UpdateAnimation()
    {
        if (aetherFrames == null || aetherFrames.Length == 0) return;
        animTimer += Time.deltaTime;
        if (animTimer >= animSpeed)
        {
            animTimer = 0f;
            animFrame = (animFrame + 1) % animLength;
            int idx = animStart + animFrame;
            if (idx < aetherFrames.Length && playerSR != null)
                playerSR.sprite = aetherFrames[idx];
        }
    }

    IEnumerator AttackAnim()
    {
        SetAnim("attack", 20, 5);
        DealDamageToNearbyEnemies(2.0f, 15);
        yield return new WaitForSeconds(0.5f);
        if (isGrounded) SetAnim("idle", 0, 4);
    }

    IEnumerator SkillAnim()
    {
        if (mpCur < 10) yield break;
        mpCur -= 10;
        RefreshHUD();
        SetAnim("skill", 25, 4);
        DealDamageToNearbyEnemies(4.0f, 25);
        SpawnSkillFX(player.transform.position);
        yield return new WaitForSeconds(0.6f);
        if (isGrounded) SetAnim("idle", 0, 4);
    }

    void DealDamageToNearbyEnemies(float radius, int dmg)
    {
        foreach (var e in enemies)
        {
            if (e == null) continue;
            if (Vector2.Distance(e.transform.position, player.transform.position) <= radius)
            {
                GraveWalkerAI ai = e.GetComponent<GraveWalkerAI>();
                if (ai != null) ai.TakeDamage(dmg);
            }
        }
    }

    void SpawnSkillFX(Vector3 pos)
    {
        for (int i = 0; i < 8; i++)
        {
            GameObject fx = new GameObject("SkillFX");
            fx.transform.position = pos;
            SpriteRenderer sr = fx.AddComponent<SpriteRenderer>();
            sr.sprite = MakeCircleSprite(32);
            float angle = i * 45f * Mathf.Deg2Rad;
            sr.color = new Color(0.6f, 0.1f, 1.0f, 0.8f);
            sr.sortingOrder = 20;
            fx.transform.localScale = Vector3.one * 0.3f;
            StartCoroutine(MoveFX(fx, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 3f));
        }
    }

    IEnumerator MoveFX(GameObject fx, Vector2 dir)
    {
        float t = 0f;
        Vector3 start = fx.transform.position;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            if (fx == null) yield break;
            fx.transform.position = start + new Vector3(dir.x, dir.y, 0) * (t / 0.4f) * 2f;
            SpriteRenderer sr = fx.GetComponent<SpriteRenderer>();
            if (sr) sr.color = new Color(0.6f, 0.1f, 1.0f, 1f - t / 0.4f);
            yield return null;
        }
        if (fx != null) Destroy(fx);
    }

    // ================================================================
    // HUD — Pakai gambar hud_ui sebagai background + TMP overlay
    // ================================================================
    void BuildHUD()
    {
        GameObject canvasObj = new GameObject("HUD_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // ===== HUD IMAGE kiri-atas =====
        // Render gambar hud_ui.jpg sebagai background (dipotong sepertiga kiri = portrait+bar)
        GameObject hudBG = MakeRawUIImg("HUD_BG", canvasObj.transform,
            new Vector2(420, 110), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(10, -10));

        if (texHUD != null)
        {
            RawImage ri = hudBG.AddComponent<RawImage>();
            // Ambil 60% kiri gambar HUD (portrait + HP/MP)
            ri.texture = texHUD;
            ri.uvRect = new Rect(0f, 0f, 0.60f, 1f);
            ri.color = Color.white;
        }
        else
        {
            Image img = hudBG.AddComponent<Image>();
            img.color = COL_PANEL;
        }

        // Overlay teks HP/MP agar selalu akurat
        hpText = MakeText("HP_Val", canvasObj.transform,
            "HP " + hpCur + "/" + hpMax,
            new Vector2(160, 26), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(165, -42), 13f, Color.white, TextAlignmentOptions.Left);

        mpText = MakeText("MP_Val", canvasObj.transform,
            "MP " + mpCur + "/" + mpMax,
            new Vector2(160, 26), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(165, -72), 13f, Color.white, TextAlignmentOptions.Left);

        // ===== SKILL BUTTONS — render bagian kanan dari hud_ui =====
        if (texHUD != null)
        {
            GameObject skillBG = MakeRawUIImg("SkillBG", canvasObj.transform,
                new Vector2(290, 110), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-10, -10));
            RawImage ri = skillBG.AddComponent<RawImage>();
            ri.texture = texHUD;
            ri.uvRect = new Rect(0.40f, 0f, 0.60f, 1f); // bagian kanan HUD = skill buttons
            ri.color = Color.white;
        }

        // Label tombol skill
        string[] labels = { "[A] Tebas", "[S] Badai", "[W] Lompat" };
        for (int i = 0; i < 3; i++)
        {
            MakeText("SkillLbl" + i, canvasObj.transform, labels[i],
                new Vector2(90, 22), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-130 + i * 95f, -120), 9.5f,
                new Color(0.9f, 0.9f, 1f, 0.9f), TextAlignmentOptions.Center);
        }

        // ===== WAVE & SCORE =====
        waveLabel = MakeText("WaveLbl", canvasObj.transform, "GELOMBANG 1",
            new Vector2(300, 36), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -12), 16f, new Color(0.85f, 0.75f, 1f, 0.9f), TextAlignmentOptions.Center);

        scoreText = MakeText("ScoreLbl", canvasObj.transform, "Skor: 0",
            new Vector2(200, 30), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-10, -50), 13f, COL_GOLD, TextAlignmentOptions.Right);

        // ===== HINT BAWAH =====
        GameObject hint = MakeRawUIImg("HintBG", canvasObj.transform,
            new Vector2(680, 34), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 8));
        Image hintImg = hint.AddComponent<Image>();
        hintImg.color = new Color(0.03f, 0.01f, 0.08f, 0.75f);
        MakeText("HintTxt", hint.transform,
            "[Panah] Gerak  [Spasi/W] Lompat  [A] Tebas  [S] Skill  [ESC] Jeda",
            new Vector2(670, 30), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, 10.5f, new Color(0.75f, 0.75f, 0.9f, 0.9f), TextAlignmentOptions.Center);
    }

    // ================================================================
    // PAUSE & GAME OVER
    // ================================================================
    void BuildPausePanel()
    {
        GameObject c = GameObject.Find("HUD_Canvas");
        if (!c) return;
        pausePanel = MakePanel("PausePanel", c.transform,
            new Vector2(380, 320), new Vector2(0.5f, 0.5f), Vector2.zero);

        MakeText("PT", pausePanel.transform, "- JEDA -",
            new Vector2(360, 50), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -20), 28f, COL_GOLD, TextAlignmentOptions.Center);

        MakeMenuBtn("BtnResume", pausePanel.transform, "Lanjutkan [ESC]",
            new Vector2(0, -100), () => TogglePause());
        MakeMenuBtn("BtnQuit", pausePanel.transform, "Menu Utama",
            new Vector2(0, -165), () => GoMainMenu());
        pausePanel.SetActive(false);
    }

    void BuildGameOverPanel()
    {
        GameObject c = GameObject.Find("HUD_Canvas");
        if (!c) return;
        gameOverPanel = MakePanel("GOPanel", c.transform,
            new Vector2(480, 400), new Vector2(0.5f, 0.5f), Vector2.zero);

        MakeText("GOT", gameOverPanel.transform, "GUGUR",
            new Vector2(460, 70), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -25), 44f, new Color(0.95f, 0.15f, 0.15f, 1f), TextAlignmentOptions.Center);

        MakeText("GOS", gameOverPanel.transform,
            "\"Dunia tidak akan berhenti\nhanya karena kau terjatuh...\"",
            new Vector2(450, 80), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -110), 14f, new Color(0.75f, 0.65f, 0.9f, 0.9f), TextAlignmentOptions.Center);

        MakeMenuBtn("BtnRetry", gameOverPanel.transform, "Coba Lagi",
            new Vector2(0, -210), () => { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); });
        MakeMenuBtn("BtnMenu2", gameOverPanel.transform, "Menu Utama",
            new Vector2(0, -278), () => GoMainMenu());
        gameOverPanel.SetActive(false);
    }

    // ================================================================
    // WAVE SPAWNER MUSUH
    // ================================================================
    IEnumerator WaveSpawner()
    {
        yield return new WaitForSeconds(3f);
        while (!gameOver)
        {
            waveCount++;
            if (waveLabel != null) waveLabel.text = "GELOMBANG " + waveCount;
            int count = 2 + waveCount;
            for (int i = 0; i < count; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(1.4f);
            }
            yield return new WaitForSeconds(waveDelay);
        }
    }

    void SpawnEnemy()
    {
        float side  = Random.value > 0.5f ? 1f : -1f;
        float spawnX = side * (11f + Random.Range(0f, 5f));

        GameObject e = new GameObject("GraveWalker");
        e.transform.position = new Vector3(spawnX, groundY + 0.5f, 0);
        e.tag   = "Enemy";
        e.layer = 7;

        SpriteRenderer sr = e.AddComponent<SpriteRenderer>();
        // Pakai frame Hurt Aether sebagai placeholder musuh (terbalik warna)
        if (aetherFrames != null && aetherFrames.Length > 30)
            sr.sprite = aetherFrames[30];
        else
            sr.sprite = MakeSolidSprite(new Color(0.35f, 0.05f, 0.55f, 1f));
        sr.color = new Color(0.80f, 0.30f, 1.0f, 1f); // tint ungu
        sr.sortingOrder = 10;
        e.transform.localScale = new Vector3(-1.0f, 1.0f, 1f); // flip

        Rigidbody2D rb = e.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale   = 4f;

        CapsuleCollider2D cc = e.AddComponent<CapsuleCollider2D>();
        cc.size = new Vector2(0.6f, 1.0f);

        GraveWalkerAI ai = e.AddComponent<GraveWalkerAI>();
        ai.Init(player, this, aetherFrames);

        enemies.Add(e);
    }

    // ================================================================
    // INTRO FADE
    // ================================================================
    IEnumerator IntroFade()
    {
        GameObject c = GameObject.Find("HUD_Canvas");
        if (!c) yield break;
        TMP_Text t = MakeText("Intro", c.transform,
            "Dunia ini adalah makam...\ndan kau adalah satu-satunya yang masih berjalan.",
            new Vector2(800, 100), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 80), 18f, new Color(0.8f, 0.7f, 1f, 0f), TextAlignmentOptions.Center);

        for (float f = 0f; f < 1f; f += Time.deltaTime * 0.5f) { t.color = new Color(0.8f, 0.7f, 1f, f); yield return null; }
        yield return new WaitForSeconds(3.5f);
        for (float f = 1f; f > 0f; f -= Time.deltaTime * 0.8f) { t.color = new Color(0.8f, 0.7f, 1f, f); yield return null; }
        Destroy(t.gameObject);
    }

    // ================================================================
    // PUBLIC — dipanggil dari AI musuh
    // ================================================================
    public void PlayerTakeDamage(int dmg)
    {
        if (invincibleTimer > 0f || gameOver) return;
        hpCur = Mathf.Max(0, hpCur - dmg);
        invincibleTimer = 1.0f;
        RefreshHUD();
        if (hpCur <= 0) TriggerGameOver();
    }

    public void EnemyKilled()
    {
        score += 100;
        mpCur = Mathf.Min(mpMax, mpCur + 5);
        RefreshHUD();
        if (scoreText != null) scoreText.text = "Skor: " + score;
    }

    // ================================================================
    // HELPER LOGIKA
    // ================================================================
    void RefreshHUD()
    {
        if (hpText != null) hpText.text = "HP " + hpCur + "/" + hpMax;
        if (mpText != null) mpText.text = "MP " + mpCur + "/" + mpMax;
    }

    void TriggerGameOver()
    {
        gameOver = true;
        SetAnim("die", 32, 4);
        if (gameOverPanel != null) { gameOverPanel.SetActive(true); }
    }

    void TogglePause()
    {
        if (gameOver) return;
        paused = !paused;
        Time.timeScale = paused ? 0f : 1f;
        if (pausePanel != null) pausePanel.SetActive(paused);
    }

    void GoMainMenu() { Time.timeScale = 1f; SceneManager.LoadScene("Main Menu"); }

    // ================================================================
    // HELPER UI
    // ================================================================
    GameObject MakePanel(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.04f, 0.02f, 0.10f, 0.95f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return go;
    }

    GameObject MakeRawUIImg(string name, Transform parent, Vector2 size, Vector2 aMin, Vector2 aMax, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        return go;
    }

    TMP_Text MakeText(string name, Transform parent, string text, Vector2 size,
        Vector2 aMin, Vector2 aMax, Vector2 pos, float fs, Color col, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = fs; t.color = col; t.alignment = align;
        if (font != null) t.font = font;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        return t;
    }

    void MakeMenuBtn(string name, Transform parent, string label, Vector2 pos, System.Action cb)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.06f, 0.25f, 1f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(260, 48);
        rt.anchoredPosition = pos;
        Button btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() => cb?.Invoke());
        MakeText(name + "Txt", go.transform, label,
            new Vector2(250, 44), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, 15f, Color.white, TextAlignmentOptions.Center);
    }

    // ================================================================
    // HELPER SPRITE PRIMITIF (fallback)
    // ================================================================
    Sprite MakeSolidSprite(Color col)
    {
        Texture2D t = new Texture2D(4, 4);
        Color[] px = new Color[16]; for (int i = 0; i < 16; i++) px[i] = col;
        t.SetPixels(px); t.Apply();
        return Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    Sprite MakeCircleSprite(int res)
    {
        Texture2D t = new Texture2D(res, res); t.filterMode = FilterMode.Bilinear;
        float r = res / 2f;
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float d = Mathf.Sqrt((x - r) * (x - r) + (y - r) * (y - r));
                float a = Mathf.Clamp01(1f - (d - (r - 1.5f)) / 1.5f);
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }

    // ================================================================
    // HELPER WORLD OBJECT
    // ================================================================
    GameObject CreateWorld(string name, Vector3 pos, Vector2 size, Color col, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSolidSprite(col); sr.color = col; sr.sortingOrder = order;
        return go;
    }

    GameObject CreateCircle(string name, Vector3 pos, float radius, Color col, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * (radius * 2f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeCircleSprite(64); sr.color = col; sr.sortingOrder = order;
        return go;
    }
}

// ================================================================
// GraveWalkerAI — AI musuh GraveWalker
// ================================================================
public class GraveWalkerAI : MonoBehaviour
{
    private GameObject target;
    private GameSceneManager manager;
    private Sprite[] frames;
    private SpriteRenderer sr;
    private int hp = 30;
    private bool dead = false;
    private float speed;
    private float attackTimer = 0f;
    private int animF = 0;
    private float animT = 0f;

    public void Init(GameObject player, GameSceneManager mgr, Sprite[] aetherFrames)
    {
        target  = player;
        manager = mgr;
        frames  = aetherFrames;
        speed   = Random.Range(1.8f, 3.2f);
        sr      = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (dead || target == null) return;

        Vector3 dir = target.transform.position - transform.position;

        // Gerak ke arah player
        transform.position += new Vector3(Mathf.Sign(dir.x) * speed * Time.deltaTime, 0, 0);

        // Flip
        transform.localScale = new Vector3(dir.x > 0 ? -1f : 1f, 1f, 1f);

        // Animasi walk (frame baris 0, kol 4-9)
        animT += Time.deltaTime;
        if (animT > 0.12f)
        {
            animT = 0f;
            animF = (animF + 1) % 6;
            if (frames != null && frames.Length > 9 && sr != null)
                sr.sprite = frames[4 + animF];
        }

        // Serang jika dekat
        attackTimer -= Time.deltaTime;
        if (Mathf.Abs(dir.x) < 1.0f && attackTimer <= 0f)
        {
            attackTimer = 1.5f;
            if (manager != null) manager.PlayerTakeDamage(8);
        }
    }

    public void TakeDamage(int dmg)
    {
        if (dead) return;
        hp -= dmg;
        if (sr) sr.color = new Color(1f, 0.3f, 0.3f, 1f);
        StartCoroutine(ResetColor());
        if (hp <= 0) Die();
    }

    IEnumerator ResetColor()
    {
        yield return new WaitForSeconds(0.12f);
        if (sr) sr.color = new Color(0.80f, 0.30f, 1.0f, 1f);
    }

    void Die()
    {
        dead = true;
        if (manager != null) manager.EnemyKilled();
        StartCoroutine(DeathAnim());
    }

    IEnumerator DeathAnim()
    {
        // Flash merah lalu fade
        for (float f = 1f; f > 0f; f -= Time.deltaTime * 3f)
        {
            if (sr) sr.color = new Color(1f, 0.1f, 0.1f, f);
            yield return null;
        }
        Destroy(gameObject);
    }
}
