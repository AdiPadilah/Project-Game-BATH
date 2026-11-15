using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MainMenu : MonoBehaviour
{
    private Sprite btnNormal;
    private Sprite btnHover;
    private Sprite btnPressed;
    private Sprite leftVignette;
    private Sprite screenVignette;
    private Sprite fireGlowSprite;
    private Sprite sunsetRaysSprite;
    private Sprite modalPanelSprite;

    private AudioClip sfxHover;
    private AudioClip sfxClick;
    private AudioSource audioSource;

    // Komponen Dynamic Background
    private RectTransform bgRect;
    private Image bgImage;
    private Vector2 bgBasePos;
    private Vector2 currentParallax;

    // Efek Visual
    private Image fireGlowImg;
    private RectTransform fireGlowRect;
    private Image sunHaloImg;
    private RectTransform sunHaloRect;
    private Image sunsetRaysImg;
    private RectTransform sunsetRaysRect;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void Start()
    {
        LoadFantasyAssets();
        BuildAtmosphere();
        SetupDynamicBackground();
        FixMenuLayout();
        FixBiodataLayout();
        SpawnFloatingEmbers();
        SpawnDragonMouthSparks();
        SpawnDesertSandWind();
    }

    void LoadFantasyAssets()
    {
        btnNormal        = Resources.Load<Sprite>("UI/btn_fantasy_normal");
        btnHover         = Resources.Load<Sprite>("UI/btn_fantasy_hover");
        btnPressed       = Resources.Load<Sprite>("UI/btn_fantasy_pressed");
        leftVignette     = Resources.Load<Sprite>("UI/menu_left_vignette");
        screenVignette   = Resources.Load<Sprite>("UI/screen_vignette");
        fireGlowSprite   = Resources.Load<Sprite>("UI/fire_glow");
        sunsetRaysSprite = Resources.Load<Sprite>("UI/sunset_rays");
        modalPanelSprite = Resources.Load<Sprite>("UI/panel_fantasy_modal");

        sfxHover         = Resources.Load<AudioClip>("Audio/ui_hover");
        sfxClick         = Resources.Load<AudioClip>("Audio/ui_click");
    }

    void BuildAtmosphere()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        // 1. Fullscreen cinematic vignette
        if (screenVignette != null && canvas.transform.Find("Atmosphere_Vignette") == null)
        {
            GameObject vigGO = new GameObject("Atmosphere_Vignette");
            vigGO.transform.SetParent(canvas.transform, false);
            vigGO.transform.SetSiblingIndex(1);

            RectTransform rt = vigGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = vigGO.AddComponent<Image>();
            img.sprite = screenVignette;
            img.color = new Color(1f, 1f, 1f, 0.7f);
            img.raycastTarget = false;
        }

        // 2. Left menu shadow panel
        if (leftVignette != null && canvas.transform.Find("Atmosphere_LeftPanel") == null)
        {
            GameObject panelGO = new GameObject("Atmosphere_LeftPanel");
            panelGO.transform.SetParent(canvas.transform, false);
            panelGO.transform.SetSiblingIndex(2);

            RectTransform rt = panelGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0.42f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = panelGO.AddComponent<Image>();
            img.sprite = leftVignette;
            img.color = new Color(1f, 1f, 1f, 0.85f);
            img.raycastTarget = false;
        }
    }

    void SetupDynamicBackground()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        Transform bgT = canvas.transform.Find("Background");
        if (bgT != null)
        {
            bgImage = bgT.GetComponent<Image>();
            bgRect  = bgT.GetComponent<RectTransform>();

            if (bgRect != null)
            {
                bgBasePos = bgRect.anchoredPosition;
                bgRect.localScale = new Vector3(1.10f, 1.10f, 1f);

                // 1. Cahaya api membara di mulut naga
                if (fireGlowSprite != null && bgT.Find("Dragon_Fire_Glow") == null)
                {
                    GameObject glowGO = new GameObject("Dragon_Fire_Glow");
                    glowGO.transform.SetParent(bgT, false);

                    fireGlowRect = glowGO.AddComponent<RectTransform>();
                    fireGlowRect.anchorMin = new Vector2(0.594f, 0.463f);
                    fireGlowRect.anchorMax = new Vector2(0.594f, 0.463f);
                    fireGlowRect.pivot     = new Vector2(0.5f, 0.5f);
                    fireGlowRect.sizeDelta = new Vector2(190f, 190f);
                    fireGlowRect.anchoredPosition = Vector2.zero;

                    fireGlowImg = glowGO.AddComponent<Image>();
                    fireGlowImg.sprite = fireGlowSprite;
                    fireGlowImg.color  = new Color(1f, 0.65f, 0.18f, 0.60f);
                    fireGlowImg.raycastTarget = false;
                }

                // 2. Denyut cahaya halo matahari di langit
                if (fireGlowSprite != null && bgT.Find("Sun_Halo_Glow") == null)
                {
                    GameObject haloGO = new GameObject("Sun_Halo_Glow");
                    haloGO.transform.SetParent(bgT, false);

                    sunHaloRect = haloGO.AddComponent<RectTransform>();
                    sunHaloRect.anchorMin = new Vector2(0.505f, 0.705f);
                    sunHaloRect.anchorMax = new Vector2(0.505f, 0.705f);
                    sunHaloRect.pivot     = new Vector2(0.5f, 0.5f);
                    sunHaloRect.sizeDelta = new Vector2(360f, 360f);
                    sunHaloRect.anchoredPosition = Vector2.zero;

                    sunHaloImg = haloGO.AddComponent<Image>();
                    sunHaloImg.sprite = fireGlowSprite;
                    sunHaloImg.color  = new Color(1f, 0.85f, 0.4f, 0.22f);
                    sunHaloImg.raycastTarget = false;
                }

                // 3. Berkas sinar matahari sore (Sunset Godrays)
                if (sunsetRaysSprite != null && bgT.Find("Sunset_Godrays") == null)
                {
                    GameObject raysGO = new GameObject("Sunset_Godrays");
                    raysGO.transform.SetParent(bgT, false);

                    sunsetRaysRect = raysGO.AddComponent<RectTransform>();
                    sunsetRaysRect.anchorMin = new Vector2(0.505f, 0.710f);
                    sunsetRaysRect.anchorMax = new Vector2(0.505f, 0.710f);
                    sunsetRaysRect.pivot     = new Vector2(0.5f, 1.0f);
                    sunsetRaysRect.sizeDelta = new Vector2(750f, 650f);
                    sunsetRaysRect.anchoredPosition = Vector2.zero;

                    sunsetRaysImg = raysGO.AddComponent<Image>();
                    sunsetRaysImg.sprite = sunsetRaysSprite;
                    sunsetRaysImg.color  = new Color(1f, 0.82f, 0.45f, 0.28f);
                    sunsetRaysImg.raycastTarget = false;
                }
            }
        }
    }

    void Update()
    {
        AnimateDynamicMotion();
        AnimateSkyAndFire();
    }

    void AnimateDynamicMotion()
    {
        if (bgRect == null) return;

        float t = Time.time;

        // Gerakan terbang dinamis naga
        float swayX = Mathf.Sin(t * 0.40f) * 26f + Mathf.Sin(t * 1.05f) * 6f;
        float bobY  = Mathf.Cos(t * 0.32f) * 16f + Mathf.Sin(t * 0.85f) * 4f;

        float zoom = 1.10f + Mathf.Sin(t * 0.16f) * 0.025f;
        bgRect.localScale = new Vector3(zoom, zoom, 1f);

        float rollAngle = Mathf.Sin(t * 0.32f) * 1.6f;
        bgRect.localEulerAngles = new Vector3(0f, 0f, rollAngle);

        Vector2 mouseNorm = Vector2.zero;
        if (Screen.width > 0 && Screen.height > 0)
        {
            mouseNorm = new Vector2(
                (Input.mousePosition.x / Screen.width) - 0.5f,
                (Input.mousePosition.y / Screen.height) - 0.5f
            );
        }
        Vector2 targetParallax = new Vector2(-mouseNorm.x * 38f, -mouseNorm.y * 24f);
        currentParallax = Vector2.Lerp(currentParallax, targetParallax, Time.deltaTime * 4.0f);

        bgRect.anchoredPosition = bgBasePos + new Vector2(swayX, bobY) + currentParallax;
    }

    void AnimateSkyAndFire()
    {
        float t = Time.time;

        if (fireGlowImg != null && fireGlowRect != null)
        {
            float flicker = Mathf.PerlinNoise(t * 6.5f, 0f);
            float scaleMul = 0.90f + flicker * 0.30f;
            fireGlowRect.localScale = new Vector3(scaleMul, scaleMul, 1f);

            float alpha = 0.45f + flicker * 0.40f;
            fireGlowImg.color = new Color(1f, 0.65f, 0.18f, alpha);
        }

        if (sunHaloImg != null && sunHaloRect != null)
        {
            float pulse = Mathf.Sin(t * 1.1f) * 0.5f + 0.5f;
            float s = 0.95f + pulse * 0.12f;
            sunHaloRect.localScale = new Vector3(s, s, 1f);
            sunHaloImg.color = new Color(1f, 0.85f, 0.45f, 0.18f + pulse * 0.16f);
        }

        if (sunsetRaysImg != null && sunsetRaysRect != null)
        {
            float rayPulse = Mathf.Sin(t * 0.75f) * 0.5f + 0.5f;
            sunsetRaysImg.color = new Color(1f, 0.85f, 0.45f, 0.18f + rayPulse * 0.18f);

            float rayAngle = Mathf.Sin(t * 0.30f) * 2.5f;
            sunsetRaysRect.localEulerAngles = new Vector3(0f, 0f, rayAngle);
        }

        if (bgImage != null)
        {
            float sunsetBreathe = Mathf.Sin(t * 0.45f) * 0.5f + 0.5f;
            bgImage.color = Color.Lerp(
                new Color(1.0f, 0.96f, 0.92f, 1f),
                new Color(1.0f, 0.88f, 0.82f, 1f),
                sunsetBreathe
            );
        }
    }

    void FixMenuLayout()
    {
        RectTransform me = GetComponent<RectTransform>();
        if (me != null)
        {
            me.anchorMin        = new Vector2(0f, 0.5f);
            me.anchorMax        = new Vector2(0f, 0.5f);
            me.pivot            = new Vector2(0f, 0.5f);
            me.anchoredPosition = new Vector2(140f, -60f);
            me.sizeDelta        = new Vector2(300f, 380f);
        }

        var buttons = new (string btn, string txtLabel, float yPos)[]
        {
            ("Play Button",      "PLAY",     105f),
            ("Options Button",   "BIODATA",   35f),
            ("Quit Button (1)",  "ABOUT",    -35f),
            ("Quit Button",      "QUIT",    -105f)
        };

        foreach (var (btnName, label, yPos) in buttons)
        {
            Transform btnT = transform.Find(btnName);
            if (btnT == null) continue;

            RectTransform rt = btnT.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin        = new Vector2(0.5f, 0.5f);
                rt.anchorMax        = new Vector2(0.5f, 0.5f);
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, yPos);
                rt.sizeDelta        = new Vector2(260f, 52f);
            }

            Image img = btnT.GetComponent<Image>();
            if (img == null) img = btnT.gameObject.AddComponent<Image>();

            if (btnNormal != null)
            {
                img.sprite = btnNormal;
                img.type   = Image.Type.Sliced;
                img.color  = Color.white;
            }
            else
            {
                img.color = new Color(0.10f, 0.06f, 0.12f, 0.75f);
            }

            Button btn = btnT.GetComponent<Button>();
            if (btn != null)
            {
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
                    cb.highlightedColor = new Color(1.3f, 1.15f, 0.7f, 1f);
                    cb.pressedColor     = new Color(1.5f, 0.9f, 0.3f, 1f);
                    btn.colors = cb;
                }

                btn.onClick.RemoveListener(PlayClickSound);
                btn.onClick.AddListener(PlayClickSound);
            }

            AddHoverEffect(btnT.gameObject);

            foreach (var tmp in btnT.GetComponentsInChildren<TMP_Text>(true))
            {
                tmp.text               = label;
                tmp.enableWordWrapping = false;
                tmp.overflowMode       = TextOverflowModes.Overflow;
                tmp.alignment          = TextAlignmentOptions.Center;
                tmp.fontSize           = 28f;
                tmp.characterSpacing   = 3f;
                tmp.color              = new Color(1.0f, 0.95f, 0.82f, 1f);
                tmp.margin             = Vector4.zero;

                RectTransform textRt = tmp.GetComponent<RectTransform>();
                if (textRt != null)
                {
                    textRt.anchorMin        = Vector2.zero;
                    textRt.anchorMax        = Vector2.one;
                    textRt.offsetMin        = Vector2.zero;
                    textRt.offsetMax        = Vector2.zero;
                    textRt.anchoredPosition = Vector2.zero;
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Penataan & Estetika Ornate pada Panel Biodata
    // ─────────────────────────────────────────────────────────────────────
    void FixBiodataLayout()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        Transform optMenu = canvas.transform.Find("Options Menu");
        if (optMenu == null) return;

        Transform panel = optMenu.Find("Panel");
        if (panel == null) return;

        // 1. Frame Panel Modal
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        if (panelRt != null)
        {
            panelRt.anchorMin        = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax        = new Vector2(0.5f, 0.5f);
            panelRt.pivot            = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta        = new Vector2(760f, 500f);
        }

        Image panelImg = panel.GetComponent<Image>();
        if (panelImg != null && modalPanelSprite != null)
        {
            panelImg.sprite = modalPanelSprite;
            panelImg.type   = Image.Type.Sliced;
            panelImg.color  = Color.white;
        }

        // 2. Tombol Kembali di dalam Biodata
        Transform backBtn = panel.Find("Back Button");
        if (backBtn != null)
        {
            RectTransform backRt = backBtn.GetComponent<RectTransform>();
            if (backRt != null)
            {
                backRt.anchorMin        = new Vector2(0.5f, 0f);
                backRt.anchorMax        = new Vector2(0.5f, 0f);
                backRt.pivot            = new Vector2(0.5f, 0f);
                backRt.anchoredPosition = new Vector2(0f, 35f);
                backRt.sizeDelta        = new Vector2(220f, 48f);
            }

            Image backImg = backBtn.GetComponent<Image>();
            if (backImg != null && btnNormal != null)
            {
                backImg.sprite = btnNormal;
                backImg.type   = Image.Type.Sliced;
                backImg.color  = Color.white;
            }

            Button btn = backBtn.GetComponent<Button>();
            if (btn != null)
            {
                if (btnHover != null && btnPressed != null)
                {
                    btn.transition = Selectable.Transition.SpriteSwap;
                    SpriteState ss = new SpriteState();
                    ss.highlightedSprite = btnHover;
                    ss.pressedSprite     = btnPressed;
                    ss.selectedSprite    = btnHover;
                    btn.spriteState      = ss;
                }

                btn.onClick.RemoveListener(PlayClickSound);
                btn.onClick.AddListener(PlayClickSound);

                btn.onClick.RemoveListener(kembali);
                btn.onClick.AddListener(kembali);
            }

            AddHoverEffect(backBtn.gameObject);
        }

        Debug.Log("<color=cyan>[MainMenu] Layout Biodata diperindah sempurna!</color>");
    }

    void PlayClickSound()
    {
        if (audioSource != null && sfxClick != null)
        {
            audioSource.PlayOneShot(sfxClick, 0.55f);
        }
    }

    void AddHoverEffect(GameObject go)
    {
        EventTrigger trigger = go.GetComponent<EventTrigger>();
        if (trigger == null) trigger = go.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        EventTrigger.Entry enter = new EventTrigger.Entry();
        enter.eventID = EventTriggerType.PointerEnter;
        enter.callback.AddListener((data) => {
            if (audioSource != null && sfxHover != null)
            {
                audioSource.PlayOneShot(sfxHover, 0.22f);
            }
            StopAllCoroutines();
            StartCoroutine(AnimateScale(go.transform, 1.04f, 0.12f));
        });
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry();
        exit.eventID = EventTriggerType.PointerExit;
        exit.callback.AddListener((data) => {
            StopAllCoroutines();
            StartCoroutine(AnimateScale(go.transform, 1.00f, 0.12f));
        });
        trigger.triggers.Add(exit);
    }

    IEnumerator AnimateScale(Transform tr, float targetScale, float duration)
    {
        if (tr == null) yield break;
        Vector3 start = tr.localScale;
        Vector3 end   = new Vector3(targetScale, targetScale, 1f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (tr == null) yield break;
            elapsed += Time.unscaledDeltaTime;
            tr.localScale = Vector3.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        if (tr != null) tr.localScale = end;
    }

    void SpawnFloatingEmbers()
    {
        if (GameObject.Find("Menu_Embers") != null) return;

        GameObject emberGO = new GameObject("Menu_Embers");
        ParticleSystem ps  = emberGO.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop            = true;
        main.startLifetime   = 6f;
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.8f, 0.25f, 0.85f),
            new Color(1f, 0.45f, 0.08f, 0.65f)
        );
        main.maxParticles    = 40;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.02f;

        var emission = ps.emission;
        emission.rateOverTime = 7f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(18f, 0.5f, 1f);
        shape.position  = new Vector3(0f, -5.2f, 0f);
        shape.rotation  = new Vector3(-90f, 0f, 0f);

        var renderer = emberGO.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 5;
    }

    void SpawnDragonMouthSparks()
    {
        if (GameObject.Find("Dragon_Mouth_Sparks") != null) return;

        GameObject sparkGO = new GameObject("Dragon_Mouth_Sparks");
        sparkGO.transform.position = new Vector3(1.8f, -0.6f, 0f);

        ParticleSystem ps = sparkGO.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop            = true;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(1.5f, 3.0f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.9f, 0.3f, 0.9f),
            new Color(1f, 0.35f, 0.05f, 0.7f)
        );
        main.maxParticles    = 25;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.015f;

        var emission = ps.emission;
        emission.rateOverTime = 8f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle     = 25f;
        shape.radius    = 0.15f;
        shape.rotation  = new Vector3(0f, 0f, 160f);

        var renderer = sparkGO.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 6;
    }

    void SpawnDesertSandWind()
    {
        if (GameObject.Find("Desert_Sand_Wind") != null) return;

        GameObject sandGO = new GameObject("Desert_Sand_Wind");
        sandGO.transform.position = new Vector3(10f, -1.0f, 0f);

        ParticleSystem ps = sandGO.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop            = true;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(2.8f, 4.5f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(4.5f, 8.5f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.02f, 0.07f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(0.92f, 0.74f, 0.42f, 0.70f),
            new Color(0.78f, 0.50f, 0.22f, 0.45f)
        );
        main.maxParticles    = 70;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.03f;

        var emission = ps.emission;
        emission.rateOverTime = 16f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(1f, 8.5f, 1f);
        shape.rotation  = new Vector3(0f, 0f, 172f);

        var renderer = sandGO.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 7;
    }

    // ─────────────────────────────────────────────────────────────────────
    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void kembali()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Transform opt = canvas.transform.Find("Options Menu");
            if (opt != null) opt.gameObject.SetActive(false);
            gameObject.SetActive(true);
        }
        else
        {
            SceneManager.LoadScene("Main Menu");
        }
    }

    public void Next()
    {
        SceneManager.LoadScene("Next");
    }

    public void About()
    {
        SceneManager.LoadScene("About");
    }
}
