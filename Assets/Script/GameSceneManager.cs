using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// GameSceneManager v4 — 100% Sesuai Gambar Referensi #2 "Born After The End":
/// - Perspektif: Top-Down / 2.5D Action RPG (jalan bebas di jalan batu halaman reruntuhan kuno)
/// - Karakter: Aether (rambut perak, mantel hitam, pedang ungu, tebasan sabit energi)
/// - Map: Reruntuhan halaman batu dengan pohon wisteria ungu, pilar batu, & atmosfer kabut
/// - UI: Portrait Aether, HP/MP bar bersudut, Minimap kanan-atas, tombol skill melingkar kanan-bawah
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    // =========================================================================
    // WARNA TEMA DARK FANTASY
    // =========================================================================
    static readonly Color C_DARK_BG   = new Color(0.04f, 0.02f, 0.08f, 1f);
    static readonly Color C_GOLD      = new Color(0.98f, 0.78f, 0.30f, 1f);
    static readonly Color C_HP_RED    = new Color(0.85f, 0.15f, 0.18f, 1f);
    static readonly Color C_MP_BLUE   = new Color(0.12f, 0.65f, 0.95f, 1f);
    static readonly Color C_PURPLE_FX = new Color(0.68f, 0.22f, 0.98f, 1f);
    static readonly Color C_PANEL     = new Color(0.05f, 0.03f, 0.12f, 0.94f);

    // =========================================================================
    // SPRITE ASSETS
    // =========================================================================
    private Sprite sprMapCourtyard;
    private Sprite sprAetherIdle;
    private Sprite sprAetherWalk1;
    private Sprite sprAetherWalk2;
    private Sprite sprAetherRun;
    private Sprite sprAetherAttack;
    private Sprite sprAetherSkill;
    private Sprite sprAetherHurt;
    private Sprite sprAetherDie;

    private Sprite sprDemonGuard;
    private Sprite sprDemonWarden;

    private Sprite sprHudTopLeft;
    private Sprite sprHudMinimap;
    private Sprite sprHudActionPanel;
    private Sprite sprBtnAttack;
    private Sprite sprBtnSkill1;
    private Sprite sprBtnSkill2;
    private Sprite sprBtnSkill3;
    private Sprite sprPortrait;

    // =========================================================================
    // STATE PEMAIN (AETHER)
    // =========================================================================
    private GameObject playerObj;
    private SpriteRenderer playerSR;
    private Rigidbody2D playerRB;

    private int hpMax = 100;
    private int hpCur = 100;
    private int mpMax = 50;
    private int mpCur = 50;
    private int score = 0;
    private int wave  = 1;

    private float moveSpeed = 4.2f;
    private bool  isAttacking = false;
    private bool  isDead      = false;
    private bool  isPaused    = false;
    private float invincibilityTimer = 0f;

    // Animasi
    private float walkAnimTimer = 0f;
    private int   walkFrameIndex = 0;
    private bool  facingRight   = true;

    // =========================================================================
    // UI REFS
    // =========================================================================
    private Image hpBarFill;
    private Image mpBarFill;
    private TMP_Text hpValueText;
    private TMP_Text mpValueText;
    private TMP_Text scoreValueText;
    private TMP_Text waveValueText;
    private RectTransform minimapPlayerMarker;
    private GameObject pauseMenuObj;
    private GameObject gameOverMenuObj;
    private TMP_FontAsset mainFont;

    // =========================================================================
    // SISTEM MUSUH
    // =========================================================================
    private List<GameObject> activeEnemies = new List<GameObject>();
    private float enemySpawnInterval = 14f;

    // Batas Map Courtyard (Clamping Kamera & Karakter)
    private readonly Vector2 mapMin = new Vector2(-5.5f, -3.2f);
    private readonly Vector2 mapMax = new Vector2( 5.5f,  3.2f);

    // =========================================================================
    // AWAKE & START
    // =========================================================================
    void Awake()
    {
        mainFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        LoadAllSprites();
    }

    void Start()
    {
        SetupCamera();
        BuildCourtyardEnvironment();
        SpawnAether();
        BuildHUD();
        BuildPauseMenu();
        BuildGameOverMenu();

        StartCoroutine(EnemyWaveRoutine());
        StartCoroutine(PassiveManaRegen());
    }

    // =========================================================================
    // 1. LOAD SPRITES DENGAN TRIPLE FALLBACK (Resources / Texture / File Langsung)
    // =========================================================================
    void LoadAllSprites()
    {
        sprMapCourtyard = SafeLoadSprite("Stage1/map_courtyard_hd");
        sprAetherIdle   = SafeLoadSprite("Stage1/aether_idle");
        sprAetherWalk1  = SafeLoadSprite("Stage1/aether_walk1");
        sprAetherWalk2  = SafeLoadSprite("Stage1/aether_walk2");
        sprAetherRun    = SafeLoadSprite("Stage1/aether_run");
        sprAetherAttack = SafeLoadSprite("Stage1/aether_attack");
        sprAetherSkill  = SafeLoadSprite("Stage1/aether_skill");
        sprAetherHurt   = SafeLoadSprite("Stage1/aether_hurt");
        sprAetherDie    = SafeLoadSprite("Stage1/aether_die");

        sprDemonGuard   = SafeLoadSprite("Stage1/demon_guard_pixel");
        sprDemonWarden  = SafeLoadSprite("Stage1/demon_warden_pixel");

        sprHudTopLeft     = SafeLoadSprite("Stage1/hud_top_left");
        sprHudMinimap     = SafeLoadSprite("Stage1/hud_minimap");
        sprHudActionPanel = SafeLoadSprite("Stage1/hud_action_panel");
        sprBtnAttack      = SafeLoadSprite("Stage1/ui_btn_attack");
        sprBtnSkill1      = SafeLoadSprite("Stage1/ui_btn_skill1");
        sprBtnSkill2      = SafeLoadSprite("Stage1/ui_btn_skill2");
        sprBtnSkill3      = SafeLoadSprite("Stage1/ui_btn_skill3");
        sprPortrait       = SafeLoadSprite("Stage1/ui_portrait");
    }

    Sprite SafeLoadSprite(string resourceSubPath)
    {
        // Cara 1: Resources.Load<Sprite>
        Sprite s = Resources.Load<Sprite>(resourceSubPath);
        if (s != null) return s;

        // Cara 2: Resources.Load<Texture2D>
        Texture2D t = Resources.Load<Texture2D>(resourceSubPath);
        if (t != null)
        {
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
        }

        // Cara 3: Baca file mentah dari disk (Application.dataPath)
        string basePath = Path.Combine(Application.dataPath, "Resources", resourceSubPath);
        string[] candidates = new string[] { basePath + ".png", basePath + ".jpg", basePath + ".jpeg" };

        foreach (string path in candidates)
        {
            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D diskTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                diskTex.filterMode = FilterMode.Bilinear;
                if (diskTex.LoadImage(bytes))
                {
                    return Sprite.Create(diskTex, new Rect(0, 0, diskTex.width, diskTex.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
        }

        Debug.LogWarning("[GameSceneManager] Sprite tidak ditemukan di: " + resourceSubPath);
        return null;
    }

    // =========================================================================
    // 2. KAMERA TOP-DOWN
    // =========================================================================
    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGO = new GameObject("MainCamera");
            camGO.tag = "MainCamera";
            cam = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
        }

        cam.orthographic = true;
        cam.orthographicSize = 3.6f; // Zoom sempurna agar halaman terlihat pas seperti gambar 2
        cam.backgroundColor = C_DARK_BG;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0, 0, -10f);
    }

    void LateUpdate()
    {
        if (playerObj == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        // Kamera mengikuti posisi player dengan smooth damping & batas (clamping)
        Vector3 targetPos = new Vector3(
            Mathf.Clamp(playerObj.transform.position.x, mapMin.x * 0.45f, mapMax.x * 0.45f),
            Mathf.Clamp(playerObj.transform.position.y, mapMin.y * 0.45f, mapMax.y * 0.45f),
            -10f
        );

        cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, 6.5f * Time.deltaTime);

        // Update minimap player marker
        if (minimapPlayerMarker != null)
        {
            float normX = Mathf.InverseLerp(mapMin.x, mapMax.x, playerObj.transform.position.x);
            float normY = Mathf.InverseLerp(mapMin.y, mapMax.y, playerObj.transform.position.y);
            minimapPlayerMarker.anchoredPosition = new Vector2((normX - 0.5f) * 70f, (normY - 0.5f) * 60f);
        }
    }

    // =========================================================================
    // 3. LINGKUNGAN MAP COURTYARD (Top-Down ARPG)
    // =========================================================================
    void BuildCourtyardEnvironment()
    {
        // Map Utama (Halaman Reruntuhan Kuno Berbatu)
        GameObject mapGO = new GameObject("CourtyardMap");
        mapGO.transform.position = Vector3.zero;

        SpriteRenderer sr = mapGO.AddComponent<SpriteRenderer>();
        sr.sprite = sprMapCourtyard;
        sr.sortingOrder = -10;
        mapGO.transform.localScale = Vector3.one * 1.0f;

        // Pembatas Tembok Luar (Invisible Wall Colliders)
        AddWallCollider("Wall_North", new Vector2(0f, 3.5f),  new Vector2(14f, 1f));
        AddWallCollider("Wall_South", new Vector2(0f, -3.5f), new Vector2(14f, 1f));
        AddWallCollider("Wall_West",  new Vector2(-6.2f, 0f), new Vector2(1f, 8f));
        AddWallCollider("Wall_East",  new Vector2(6.2f, 0f),  new Vector2(1f, 8f));

        // Reruntuhan Pilar Kiri & Kanan (Pilar Colliders agar Aether bisa kelilingi)
        AddObstacle("PillarArch_Left",  new Vector2(-2.8f, 1.2f), new Vector2(1.2f, 1.5f));
        AddObstacle("PillarArch_Right", new Vector2( 2.8f, 1.2f), new Vector2(1.2f, 1.5f));
        AddObstacle("Ruins_BottomLeft", new Vector2(-3.0f, -1.8f), new Vector2(1.5f, 1.0f));
        AddObstacle("Ruins_BottomRight",new Vector2( 3.0f, -1.8f), new Vector2(1.5f, 1.0f));

        // Partikel Kabut Ungu Melayang di Courtyard
        BuildAtmosphericMist();
    }

    void AddWallCollider(string name, Vector2 pos, Vector2 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.position = pos;
        wall.layer = 6; // Ground/Obstacle
        BoxCollider2D bc = wall.AddComponent<BoxCollider2D>();
        bc.size = size;
    }

    void AddObstacle(string name, Vector2 pos, Vector2 size)
    {
        GameObject obs = new GameObject(name);
        obs.transform.position = pos;
        obs.layer = 6;
        BoxCollider2D bc = obs.AddComponent<BoxCollider2D>();
        bc.size = size;
    }

    void BuildAtmosphericMist()
    {
        GameObject mistGO = new GameObject("CourtyardMistParticles");
        mistGO.transform.position = new Vector3(0, 0, -1f);

        ParticleSystem ps = mistGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 11f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.15f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.40f, 0.10f, 0.75f, 0.08f),
            new Color(0.60f, 0.20f, 0.95f, 0.16f)
        );
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;

        var em = ps.emission;
        em.rateOverTime = 3.5f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(12f, 7f, 1f);

        var psr = mistGO.GetComponent<ParticleSystemRenderer>();
        psr.sortingOrder = 5;
        psr.renderMode = ParticleSystemRenderMode.Billboard;

        ps.Play();
    }

    // =========================================================================
    // 4. SPAWN PEMAIN (AETHER)
    // =========================================================================
    void SpawnAether()
    {
        playerObj = new GameObject("Aether_Hero");
        playerObj.tag = "Player";
        playerObj.layer = 8; // Player
        playerObj.transform.position = new Vector3(0f, -0.6f, 0f); // Berdiri di tengah jalan batu

        playerSR = playerObj.AddComponent<SpriteRenderer>();
        playerSR.sprite = sprAetherIdle;
        playerSR.sortingOrder = 10;
        playerObj.transform.localScale = Vector3.one * 0.55f; // Skala pas proporsional dengan pilar

        playerRB = playerObj.AddComponent<Rigidbody2D>();
        playerRB.bodyType = RigidbodyType2D.Dynamic;
        playerRB.gravityScale = 0f; // TOP-DOWN (tidak jatuh ke bawah!)
        playerRB.freezeRotation = true;
        playerRB.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Kaki collider untuk navigasi top-down
        CircleCollider2D cc = playerObj.AddComponent<CircleCollider2D>();
        cc.radius = 0.45f;
        cc.offset = new Vector2(0f, -0.4f);

        // Audio Source untuk sfx serangan
        AudioSource aud = playerObj.AddComponent<AudioSource>();
        aud.playOnAwake = false;

        // Efek aura pedang ungu halus
        GameObject swordAura = new GameObject("SwordGlow");
        swordAura.transform.SetParent(playerObj.transform, false);
        swordAura.transform.localPosition = new Vector2(0.35f, -0.2f);
        SpriteRenderer auraSR = swordAura.AddComponent<SpriteRenderer>();
        auraSR.sprite = MakeGlowOrbSprite(32);
        auraSR.color = new Color(0.65f, 0.20f, 1.0f, 0.35f);
        auraSR.sortingOrder = 9;
        swordAura.transform.localScale = Vector3.one * 0.8f;
    }

    // =========================================================================
    // 5. UPDATE: KONTROL 8-ARAH & KOMBAK TOP-DOWN
    // =========================================================================
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (isDead || isPaused) return;

        if (invincibilityTimer > 0f)
        {
            invincibilityTimer -= Time.deltaTime;
        }

        HandleMovementInput();
        HandleCombatInput();

        // Bersihkan musuh mati
        activeEnemies.RemoveAll(e => e == null);
    }

    void HandleMovementInput()
    {
        if (playerObj == null || playerRB == null || isAttacking) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector2 inputDir = new Vector2(h, v).normalized;
        playerRB.velocity = inputDir * moveSpeed;

        // Flip arah pandang horizontal
        if (h > 0.05f && !facingRight)
        {
            facingRight = true;
            playerObj.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
        }
        else if (h < -0.05f && facingRight)
        {
            facingRight = false;
            playerObj.transform.localScale = new Vector3(-0.55f, 0.55f, 1f);
        }

        // Animasi Gerak
        if (inputDir.sqrMagnitude > 0.01f)
        {
            walkAnimTimer += Time.deltaTime;
            if (walkAnimTimer >= 0.14f)
            {
                walkAnimTimer = 0f;
                walkFrameIndex = (walkFrameIndex + 1) % 2;
                playerSR.sprite = (walkFrameIndex == 0) ? sprAetherWalk1 : sprAetherWalk2;
            }
        }
        else
        {
            walkAnimTimer = 0f;
            playerSR.sprite = sprAetherIdle;
        }
    }

    void HandleCombatInput()
    {
        if (isAttacking) return;

        // Tombol Serang Biasa: J, Z, Space, atau Klik Kiri
        if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            PerformBasicAttack();
        }

        // Skill 1: K atau 1 (Tebasan Sabit Cepat)
        if (Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Alpha1))
        {
            PerformSkill1();
        }

        // Skill 2: L atau 2 (Badai Energi Abyssal)
        if (Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.Alpha2))
        {
            PerformSkill2();
        }

        // Skill 3: I atau 3 (Kiamat Bayangan Gelap)
        if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Alpha3))
        {
            PerformSkill3();
        }
    }

    // =========================================================================
    // SERANGAN & SKILL AETHER
    // =========================================================================
    public void PerformBasicAttack()
    {
        if (isAttacking || isDead) return;
        StartCoroutine(BasicAttackRoutine());
    }

    IEnumerator BasicAttackRoutine()
    {
        isAttacking = true;
        playerRB.velocity = Vector2.zero;

        // Tampilkan pose tebasan Aether dengan sabit ungu
        if (sprAetherAttack != null) playerSR.sprite = sprAetherAttack;

        // Spawn gelombang sabit ungu meluncur
        Vector2 slashDir = facingRight ? Vector2.right : Vector2.left;
        SpawnSlashWave(playerObj.transform.position + new Vector3(slashDir.x * 0.7f, 0.1f, 0f), slashDir, 25);

        // Beri damage ke musuh di depan
        HitEnemiesInCone(playerObj.transform.position, slashDir, 2.2f, 25);

        yield return new WaitForSeconds(0.32f);

        isAttacking = false;
        playerSR.sprite = sprAetherIdle;
    }

    public void PerformSkill1()
    {
        if (isAttacking || isDead || mpCur < 12) return;
        mpCur -= 12;
        UpdateHUDValues();
        StartCoroutine(Skill1Routine());
    }

    IEnumerator Skill1Routine()
    {
        isAttacking = true;
        playerSR.sprite = sprAetherAttack;

        // Dash cepat ke depan dengan jejak bayangan ungu
        Vector2 dashDir = facingRight ? Vector2.right : Vector2.left;
        playerRB.velocity = dashDir * 12f;

        // Efek tebasan ganda
        SpawnSlashWave(playerObj.transform.position + new Vector3(dashDir.x * 0.9f, 0.2f, 0f), dashDir, 40);
        HitEnemiesInCone(playerObj.transform.position, dashDir, 3.2f, 40);

        yield return new WaitForSeconds(0.20f);
        playerRB.velocity = Vector2.zero;

        yield return new WaitForSeconds(0.18f);
        isAttacking = false;
        playerSR.sprite = sprAetherIdle;
    }

    public void PerformSkill2()
    {
        if (isAttacking || isDead || mpCur < 20) return;
        mpCur -= 20;
        UpdateHUDValues();
        StartCoroutine(Skill2Routine());
    }

    IEnumerator Skill2Routine()
    {
        isAttacking = true;
        playerRB.velocity = Vector2.zero;
        playerSR.sprite = sprAetherSkill;

        // Ledakan radial 360 derajat
        SpawnRadialSkillBurst(playerObj.transform.position, 3.5f, 50);

        yield return new WaitForSeconds(0.45f);
        isAttacking = false;
        playerSR.sprite = sprAetherIdle;
    }

    public void PerformSkill3()
    {
        if (isAttacking || isDead || mpCur < 35) return;
        mpCur -= 35;
        UpdateHUDValues();
        StartCoroutine(Skill3Routine());
    }

    IEnumerator Skill3Routine()
    {
        isAttacking = true;
        playerRB.velocity = Vector2.zero;
        playerSR.sprite = sprAetherSkill;

        // Ultimate: Hantaman Pilar Cahaya Ungu ke Semua Musuh di Courtyard
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                SpawnPillarOfLight(enemy.transform.position);
                enemy.GetComponent<DemonCourtyardAI>()?.TakeDamage(80);
            }
        }

        SpawnRadialSkillBurst(playerObj.transform.position, 5.0f, 60);

        yield return new WaitForSeconds(0.60f);
        isAttacking = false;
        playerSR.sprite = sprAetherIdle;
    }

    void SpawnSlashWave(Vector3 pos, Vector2 dir, int damage)
    {
        GameObject slash = new GameObject("PurpleSlashWave");
        slash.transform.position = pos;
        slash.transform.localScale = new Vector3(facingRight ? 1.4f : -1.4f, 1.4f, 1f);

        SpriteRenderer sr = slash.AddComponent<SpriteRenderer>();
        sr.sprite = MakeCrescentSlashSprite(64);
        sr.color = new Color(0.85f, 0.35f, 1.0f, 0.95f);
        sr.sortingOrder = 15;

        StartCoroutine(AnimateSlash(slash, dir));
    }

    IEnumerator AnimateSlash(GameObject slash, Vector2 dir)
    {
        float timer = 0f;
        Vector3 start = slash.transform.position;
        while (timer < 0.28f)
        {
            timer += Time.deltaTime;
            if (slash == null) yield break;
            slash.transform.position = start + (Vector3)(dir * timer * 6f);
            SpriteRenderer sr = slash.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(0.85f, 0.35f, 1.0f, 1f - (timer / 0.28f));
            yield return null;
        }
        if (slash != null) Destroy(slash);
    }

    void SpawnRadialSkillBurst(Vector3 center, float radius, int damage)
    {
        // 8 proyektil melingkar
        for (int i = 0; i < 8; i++)
        {
            float ang = i * 45f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

            GameObject orb = new GameObject("BurstOrb");
            orb.transform.position = center + (Vector3)(dir * 0.4f);
            SpriteRenderer sr = orb.AddComponent<SpriteRenderer>();
            sr.sprite = MakeGlowOrbSprite(32);
            sr.color = C_PURPLE_FX;
            sr.sortingOrder = 16;
            orb.transform.localScale = Vector3.one * 0.6f;

            StartCoroutine(AnimateSlash(orb, dir));
        }

        // Damage melingkar
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null && Vector2.Distance(center, enemy.transform.position) <= radius)
            {
                enemy.GetComponent<DemonCourtyardAI>()?.TakeDamage(damage);
            }
        }
    }

    void SpawnPillarOfLight(Vector3 pos)
    {
        GameObject pillar = new GameObject("PurplePillarFX");
        pillar.transform.position = pos + new Vector3(0, 1.2f, 0);
        pillar.transform.localScale = new Vector3(0.6f, 3.5f, 1f);

        SpriteRenderer sr = pillar.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSolidQuadSprite(new Color(0.85f, 0.3f, 1.0f, 0.85f));
        sr.sortingOrder = 18;

        StartCoroutine(FadeAndDestroy(pillar, 0.35f));
    }

    IEnumerator FadeAndDestroy(GameObject go, float duration)
    {
        float t = 0f;
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        Color startCol = sr ? sr.color : Color.white;
        while (t < duration)
        {
            t += Time.deltaTime;
            if (go == null) yield break;
            if (sr != null) sr.color = new Color(startCol.r, startCol.g, startCol.b, startCol.a * (1f - t / duration));
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    void HitEnemiesInCone(Vector3 origin, Vector2 dir, float range, int dmg)
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy == null) continue;
            Vector2 toEnemy = (enemy.transform.position - origin);
            if (toEnemy.magnitude <= range && Vector2.Dot(dir, toEnemy.normalized) > 0.35f)
            {
                enemy.GetComponent<DemonCourtyardAI>()?.TakeDamage(dmg);
            }
        }
    }

    // =========================================================================
    // 6. HUD BERSIH & PRESISI (Sesuai 100% dengan Gambar #2)
    // =========================================================================
    void BuildHUD()
    {
        GameObject canvasGO = new GameObject("HUD_Canvas");
        Canvas cv = canvasGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 100;

        CanvasScaler cs = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // -------------------------------------------------------------
        // A. PANEL KIRI ATAS: PORTRAIT + LV.1 + HP/MP BARS
        // -------------------------------------------------------------
        GameObject topLeftPanel = new GameObject("HUD_TopLeft_Panel");
        topLeftPanel.transform.SetParent(canvasGO.transform, false);
        RectTransform rtTL = topLeftPanel.AddComponent<RectTransform>();
        rtTL.anchorMin = rtTL.anchorMax = new Vector2(0f, 1f);
        rtTL.pivot = new Vector2(0f, 1f);
        rtTL.anchoredPosition = new Vector2(30f, -25f);
        rtTL.sizeDelta = new Vector2(380f, 110f);

        // Portrait Frame Bulat
        GameObject portFrame = CreateUIBox("PortraitFrame", topLeftPanel.transform,
            new Vector2(88f, 88f), new Vector2(0f, 0.5f), new Vector2(48f, 0f),
            new Color(0.18f, 0.12f, 0.28f, 0.95f));
        CreateUIBorder(portFrame, C_GOLD, 2.5f);

        // Wajah Aether di Portrait
        GameObject portImg = new GameObject("PortraitFace");
        portImg.transform.SetParent(portFrame.transform, false);
        Image pFace = portImg.AddComponent<Image>();
        pFace.sprite = (sprPortrait != null) ? sprPortrait : sprAetherIdle;
        pFace.preserveAspect = true;
        RectTransform rtFace = portImg.GetComponent<RectTransform>();
        rtFace.sizeDelta = new Vector2(74f, 74f);

        // Badge Level "Lv. 1"
        CreateUIText("LvText", topLeftPanel.transform, "Lv. 1",
            new Vector2(90f, 24f), new Vector2(0f, 1f), new Vector2(150f, -18f),
            14f, C_GOLD, TextAlignmentOptions.Left);

        // HP BAR (Merah)
        GameObject hpBG = CreateUIBox("HP_Bar_BG", topLeftPanel.transform,
            new Vector2(230f, 22f), new Vector2(0f, 1f), new Vector2(235f, -48f),
            new Color(0.16f, 0.05f, 0.05f, 0.95f));
        CreateUIBorder(hpBG, new Color(0.5f, 0.15f, 0.15f, 1f), 1.5f);

        GameObject hpFillGO = CreateUIBox("HP_Bar_Fill", hpBG.transform,
            new Vector2(226f, 18f), new Vector2(0f, 0.5f), new Vector2(113f, 0f),
            C_HP_RED);
        hpBarFill = hpFillGO.GetComponent<Image>();
        hpBarFill.type = Image.Type.Filled;
        hpBarFill.fillMethod = Image.FillMethod.Horizontal;
        hpBarFill.fillAmount = 1f;

        hpValueText = CreateUIText("HP_Text", hpBG.transform, "HP  100 / 100",
            new Vector2(220f, 20f), new Vector2(0.5f, 0.5f), Vector2.zero,
            12f, Color.white, TextAlignmentOptions.Center);

        // MP BAR (Biru Cyan)
        GameObject mpBG = CreateUIBox("MP_Bar_BG", topLeftPanel.transform,
            new Vector2(230f, 20f), new Vector2(0f, 1f), new Vector2(235f, -76f),
            new Color(0.04f, 0.08f, 0.18f, 0.95f));
        CreateUIBorder(mpBG, new Color(0.15f, 0.35f, 0.65f, 1f), 1.5f);

        GameObject mpFillGO = CreateUIBox("MP_Bar_Fill", mpBG.transform,
            new Vector2(226f, 16f), new Vector2(0f, 0.5f), new Vector2(113f, 0f),
            C_MP_BLUE);
        mpBarFill = mpFillGO.GetComponent<Image>();
        mpBarFill.type = Image.Type.Filled;
        mpBarFill.fillMethod = Image.FillMethod.Horizontal;
        mpBarFill.fillAmount = 1f;

        mpValueText = CreateUIText("MP_Text", mpBG.transform, "MP  50 / 50",
            new Vector2(220f, 18f), new Vector2(0.5f, 0.5f), Vector2.zero,
            11.5f, Color.white, TextAlignmentOptions.Center);

        // -------------------------------------------------------------
        // B. PANEL KANAN ATAS: MINIMAP DUNGEON
        // -------------------------------------------------------------
        GameObject minimapCard = CreateUIBox("HUD_Minimap_Card", canvasGO.transform,
            new Vector2(140f, 110f), new Vector2(1f, 1f), new Vector2(-95f, -75f),
            new Color(0.06f, 0.04f, 0.14f, 0.92f));
        CreateUIBorder(minimapCard, new Color(0.55f, 0.30f, 0.85f, 0.8f), 2f);

        if (sprHudMinimap != null)
        {
            GameObject mBg = new GameObject("MinimapBGImg");
            mBg.transform.SetParent(minimapCard.transform, false);
            Image mImg = mBg.AddComponent<Image>();
            mImg.sprite = sprHudMinimap;
            mImg.preserveAspect = true;
            mBg.GetComponent<RectTransform>().sizeDelta = new Vector2(130f, 100f);
        }

        // Marker Posisi Pemain (Diamond Putih Berkedip)
        GameObject markerGO = CreateUIBox("PlayerMinimapDiamond", minimapCard.transform,
            new Vector2(10f, 10f), new Vector2(0.5f, 0.5f), Vector2.zero,
            Color.white);
        markerGO.transform.rotation = Quaternion.Euler(0, 0, 45f);
        minimapPlayerMarker = markerGO.GetComponent<RectTransform>();

        // -------------------------------------------------------------
        // C. PANEL KANAN BAWAH: TOMBOL AKSI MELINGKAR (GAMBAR 2)
        // -------------------------------------------------------------
        // 1. Tombol Serang Utama (Pedang Besar)
        CreateActionCircleButton("Btn_Attack", canvasGO.transform,
            new Vector2(-110f, 110f), 105f, sprBtnAttack, "><", "[J/Space]", () => PerformBasicAttack());

        // 2. Tiga Tombol Skill dalam Lengkungan
        CreateActionCircleButton("Btn_Skill1", canvasGO.transform,
            new Vector2(-215f, 75f), 74f, sprBtnSkill1, "~", "[K/1]", () => PerformSkill1());

        CreateActionCircleButton("Btn_Skill2", canvasGO.transform,
            new Vector2(-195f, 175f), 74f, sprBtnSkill2, "*", "[L/2]", () => PerformSkill2());

        CreateActionCircleButton("Btn_Skill3", canvasGO.transform,
            new Vector2(-105f, 235f), 74f, sprBtnSkill3, "!", "[I/3]", () => PerformSkill3());

        // -------------------------------------------------------------
        // D. TEKS GELOMBANG & SKOR (ATAS TENGAH)
        // -------------------------------------------------------------
        waveValueText = CreateUIText("WaveLabel", canvasGO.transform, "GELOMBANG 1",
            new Vector2(360f, 36f), new Vector2(0.5f, 1f), new Vector2(0f, -22f),
            18f, new Color(0.9f, 0.8f, 1.0f, 0.95f), TextAlignmentOptions.Center);

        scoreValueText = CreateUIText("ScoreLabel", canvasGO.transform, "Skor: 0",
            new Vector2(250f, 30f), new Vector2(0.5f, 1f), new Vector2(0f, -54f),
            14f, C_GOLD, TextAlignmentOptions.Center);

        // -------------------------------------------------------------
        // E. HINT KONTROL BAWAH TENGAH
        // -------------------------------------------------------------
        GameObject hintBox = CreateUIBox("ControlHint", canvasGO.transform,
            new Vector2(720f, 32f), new Vector2(0.5f, 0f), new Vector2(0f, 16f),
            new Color(0.04f, 0.02f, 0.10f, 0.78f));
        CreateUIText("HintText", hintBox.transform,
            "[WASD / Panah] Gerak Bebas  |  [J / Space] Serang  |  [K / L / I] Skill  |  [ESC] Jeda",
            new Vector2(710f, 28f), new Vector2(0.5f, 0.5f), Vector2.zero,
            11f, new Color(0.85f, 0.85f, 0.95f, 0.9f), TextAlignmentOptions.Center);
    }

    void CreateActionCircleButton(string name, Transform parent, Vector2 pos, float size,
        Sprite iconSpr, string fallbackIco, string keyHint, System.Action onClick)
    {
        GameObject btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);

        RectTransform rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(size, size);

        // Latar tombol lingkaran
        Image bgImg = btnGO.AddComponent<Image>();
        bgImg.color = new Color(0.10f, 0.05f, 0.22f, 0.95f);
        CreateUIBorder(btnGO, C_PURPLE_FX, 2.5f);

        // Icon tombol
        if (iconSpr != null)
        {
            GameObject icoGO = new GameObject("Icon");
            icoGO.transform.SetParent(btnGO.transform, false);
            Image icoImg = icoGO.AddComponent<Image>();
            icoImg.sprite = iconSpr;
            icoImg.preserveAspect = true;
            icoGO.GetComponent<RectTransform>().sizeDelta = new Vector2(size * 0.7f, size * 0.7f);
        }
        else
        {
            CreateUIText("FallbackIco", btnGO.transform, fallbackIco,
                new Vector2(size, size), new Vector2(0.5f, 0.5f), Vector2.zero,
                size * 0.35f, Color.white, TextAlignmentOptions.Center);
        }

        // Teks petunjuk tombol di bawah
        CreateUIText("KeyHint", btnGO.transform, keyHint,
            new Vector2(size + 20f, 18f), new Vector2(0.5f, 0f), new Vector2(0f, -14f),
            9.5f, C_GOLD, TextAlignmentOptions.Center);

        // Tombol Interaktif
        Button btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick?.Invoke());
    }

    // =========================================================================
    // 7. MENU JEDA & GAME OVER
    // =========================================================================
    void BuildPauseMenu()
    {
        GameObject c = GameObject.Find("HUD_Canvas");
        if (!c) return;

        pauseMenuObj = CreateUIBox("PauseMenuModal", c.transform,
            new Vector2(400f, 320f), new Vector2(0.5f, 0.5f), Vector2.zero,
            C_PANEL);
        CreateUIBorder(pauseMenuObj, C_GOLD, 2f);

        CreateUIText("PauseTitle", pauseMenuObj.transform, "- JEDA -",
            new Vector2(360f, 50f), new Vector2(0.5f, 1f), new Vector2(0f, -24f),
            28f, C_GOLD, TextAlignmentOptions.Center);

        CreateModalButton("BtnResume", pauseMenuObj.transform, "Lanjutkan [ESC]",
            new Vector2(0f, -100f), () => TogglePause());

        CreateModalButton("BtnMenu", pauseMenuObj.transform, "Menu Utama",
            new Vector2(0f, -170f), () => ExitToMainMenu());

        pauseMenuObj.SetActive(false);
    }

    void BuildGameOverMenu()
    {
        GameObject c = GameObject.Find("HUD_Canvas");
        if (!c) return;

        gameOverMenuObj = CreateUIBox("GameOverModal", c.transform,
            new Vector2(480f, 390f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Color(0.08f, 0.02f, 0.05f, 0.98f));
        CreateUIBorder(gameOverMenuObj, new Color(0.85f, 0.15f, 0.15f, 1f), 3f);

        CreateUIText("GOTitle", gameOverMenuObj.transform, "GUGUR",
            new Vector2(440f, 65f), new Vector2(0.5f, 1f), new Vector2(0f, -25f),
            46f, new Color(0.95f, 0.2f, 0.2f, 1f), TextAlignmentOptions.Center);

        CreateUIText("GOSub", gameOverMenuObj.transform,
            "\"Selama aku masih hidup,\naku tidak akan menyerah.\"",
            new Vector2(440f, 75f), new Vector2(0.5f, 1f), new Vector2(0f, -105f),
            14f, new Color(0.85f, 0.75f, 0.95f, 0.95f), TextAlignmentOptions.Center);

        CreateModalButton("BtnRetry", gameOverMenuObj.transform, "Coba Lagi",
            new Vector2(0f, -205f), () => RestartScene());

        CreateModalButton("BtnExit", gameOverMenuObj.transform, "Menu Utama",
            new Vector2(0f, -275f), () => ExitToMainMenu());

        gameOverMenuObj.SetActive(false);
    }

    void CreateModalButton(string name, Transform parent, string label, Vector2 pos, System.Action onClick)
    {
        GameObject btnGO = CreateUIBox(name, parent, new Vector2(260f, 48f),
            new Vector2(0.5f, 0.5f), pos, new Color(0.14f, 0.08f, 0.26f, 1f));
        CreateUIBorder(btnGO, C_GOLD, 1.5f);

        CreateUIText(name + "_Txt", btnGO.transform, label,
            new Vector2(250f, 44f), new Vector2(0.5f, 0.5f), Vector2.zero,
            14f, Color.white, TextAlignmentOptions.Center);

        Button btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick?.Invoke());
    }

    // =========================================================================
    // 8. LOGIKA GAMEPLAY & DAMAGE
    // =========================================================================
    public void PlayerTakeDamage(int dmg)
    {
        if (invincibilityTimer > 0f || isDead) return;

        hpCur = Mathf.Max(0, hpCur - dmg);
        invincibilityTimer = 0.8f;
        UpdateHUDValues();

        // Flash merah & pose hurt
        if (playerSR != null)
        {
            StartCoroutine(PlayerHurtRoutine());
        }

        if (hpCur <= 0)
        {
            TriggerGameOver();
        }
    }

    IEnumerator PlayerHurtRoutine()
    {
        if (sprAetherHurt != null) playerSR.sprite = sprAetherHurt;
        playerSR.color = new Color(1f, 0.4f, 0.4f, 1f);

        yield return new WaitForSeconds(0.18f);

        playerSR.color = Color.white;
        if (!isAttacking && !isDead) playerSR.sprite = sprAetherIdle;
    }

    public void OnEnemyKilled(int points)
    {
        score += points;
        mpCur = Mathf.Min(mpMax, mpCur + 8);
        UpdateHUDValues();
    }

    void UpdateHUDValues()
    {
        if (hpBarFill != null) hpBarFill.fillAmount = (float)hpCur / hpMax;
        if (hpValueText != null) hpValueText.text = "HP  " + hpCur + " / " + hpMax;

        if (mpBarFill != null) mpBarFill.fillAmount = (float)mpCur / mpMax;
        if (mpValueText != null) mpValueText.text = "MP  " + mpCur + " / " + mpMax;

        if (scoreValueText != null) scoreValueText.text = "Skor: " + score;
        if (waveValueText != null) waveValueText.text = "GELOMBANG " + wave;
    }

    IEnumerator PassiveManaRegen()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(1.5f);
            if (mpCur < mpMax)
            {
                mpCur = Mathf.Min(mpMax, mpCur + 2);
                UpdateHUDValues();
            }
        }
    }

    void TriggerGameOver()
    {
        isDead = true;
        if (sprAetherDie != null && playerSR != null) playerSR.sprite = sprAetherDie;
        if (gameOverMenuObj != null) gameOverMenuObj.SetActive(true);
    }

    void TogglePause()
    {
        if (isDead) return;
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        if (pauseMenuObj != null) pauseMenuObj.SetActive(isPaused);
    }

    void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void ExitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main Menu");
    }

    // =========================================================================
    // 9. WAVE SPAWNER MUSUH (Demon Guards & Warden)
    // =========================================================================
    IEnumerator EnemyWaveRoutine()
    {
        yield return new WaitForSeconds(2.5f);

        while (!isDead)
        {
            int count = 2 + wave;
            for (int i = 0; i < count; i++)
            {
                SpawnDemonEnemy(i == count - 1 && wave % 2 == 0); // Mini-boss tiap gelombang genap
                yield return new WaitForSeconds(1.2f);
            }

            yield return new WaitForSeconds(enemySpawnInterval);
            wave++;
            UpdateHUDValues();
        }
    }

    void SpawnDemonEnemy(bool isWarden)
    {
        // Titik spawn dari reruntuhan luar courtyard
        Vector2[] spawnPoints = new Vector2[]
        {
            new Vector2(-4.5f,  2.2f),
            new Vector2( 4.5f,  2.2f),
            new Vector2(-4.5f, -2.2f),
            new Vector2( 4.5f, -2.2f),
            new Vector2( 0.0f,  3.2f)
        };

        Vector2 pt = spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject demon = new GameObject(isWarden ? "DemonWarden" : "DemonGuard");
        demon.transform.position = pt;
        demon.tag = "Enemy";
        demon.layer = 7; // Enemy

        SpriteRenderer dsr = demon.AddComponent<SpriteRenderer>();
        dsr.sprite = isWarden ? (sprDemonWarden ?? sprDemonGuard) : (sprDemonGuard ?? sprDemonWarden);
        dsr.sortingOrder = 9;
        demon.transform.localScale = Vector3.one * (isWarden ? 0.75f : 0.55f);

        Rigidbody2D drb = demon.AddComponent<Rigidbody2D>();
        drb.gravityScale = 0f; // TOP-DOWN
        drb.freezeRotation = true;

        CircleCollider2D dcc = demon.AddComponent<CircleCollider2D>();
        dcc.radius = 0.45f;

        DemonCourtyardAI ai = demon.AddComponent<DemonCourtyardAI>();
        ai.Init(playerObj, this, isWarden ? 120 : 45, isWarden ? 2.0f : 2.6f, isWarden ? 18 : 8);

        activeEnemies.Add(demon);
    }

    // =========================================================================
    // HELPER: PEMBUATAN UI PRIMITIF
    // =========================================================================
    GameObject CreateUIBox(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 pos, Color col)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image img = go.AddComponent<Image>();
        img.color = col;
        return go;
    }

    TMP_Text CreateUIText(string name, Transform parent, string text, Vector2 size,
        Vector2 anchor, Vector2 pos, float fontSize, Color col, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = fontSize;
        t.color = col;
        t.alignment = align;
        if (mainFont != null) t.font = mainFont;
        return t;
    }

    void CreateUIBorder(GameObject target, Color color, float thickness)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(thickness, -thickness);
    }

    // =========================================================================
    // HELPER: PROSEDURAL SPRITES (SABIT, ORB, QUAD)
    // =========================================================================
    Sprite MakeCrescentSlashSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float rOuter = size * 0.48f;
        float rInner = size * 0.32f;
        Vector2 centerOuter = new Vector2(size * 0.45f, size * 0.5f);
        Vector2 centerInner = new Vector2(size * 0.58f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x, y);
                float dOut = Vector2.Distance(p, centerOuter);
                float dIn  = Vector2.Distance(p, centerInner);

                if (dOut <= rOuter && dIn >= rInner && x > size * 0.25f)
                {
                    float alpha = Mathf.Clamp01((rOuter - dOut) / 4f) * Mathf.Clamp01((dIn - rInner) / 4f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    Sprite MakeGlowOrbSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(1f - (d / center));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    Sprite MakeSolidQuadSprite(Color c)
    {
        Texture2D t = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = c;
        t.SetPixels(px);
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }
}

// =============================================================================
// DemonCourtyardAI — AI Musuh Top-Down Courtyard
// =============================================================================
public class DemonCourtyardAI : MonoBehaviour
{
    private GameObject player;
    private GameSceneManager manager;
    private SpriteRenderer sr;
    private int maxHP;
    private int currentHP;
    private float moveSpeed;
    private int attackDamage;

    private float attackCooldown = 1.3f;
    private float lastAttackTime = -999f;
    private bool isDead = false;

    public void Init(GameObject targetPlayer, GameSceneManager mgr, int hp, float speed, int dmg)
    {
        player = targetPlayer;
        manager = mgr;
        maxHP = currentHP = hp;
        moveSpeed = speed;
        attackDamage = dmg;
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (isDead || player == null) return;

        Vector2 dir = (player.transform.position - transform.position);
        float dist = dir.magnitude;

        // Hadap ke arah player
        if (dir.x > 0.05f) transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, 1f);
        else if (dir.x < -0.05f) transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, 1f);

        // Kejar player jika dalam jangkauan
        if (dist > 0.95f)
        {
            transform.position += (Vector3)(dir.normalized * moveSpeed * Time.deltaTime);
        }
        else
        {
            // Serang jika dalam jangkauan dekat
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                manager.PlayerTakeDamage(attackDamage);
            }
        }
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        currentHP -= dmg;
        StartCoroutine(HitFlashRoutine());

        if (currentHP <= 0)
        {
            Die();
        }
    }

    IEnumerator HitFlashRoutine()
    {
        if (sr != null) sr.color = new Color(1f, 0.3f, 0.3f, 1f);
        yield return new WaitForSeconds(0.12f);
        if (sr != null) sr.color = Color.white;
    }

    void Die()
    {
        isDead = true;
        manager.OnEnemyKilled(100);
        StartCoroutine(DeathFadeRoutine());
    }

    IEnumerator DeathFadeRoutine()
    {
        float timer = 0f;
        while (timer < 0.35f)
        {
            timer += Time.deltaTime;
            if (sr != null) sr.color = new Color(0.8f, 0.2f, 1.0f, 1f - (timer / 0.35f));
            yield return null;
        }
        Destroy(gameObject);
    }
}
