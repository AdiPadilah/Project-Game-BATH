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

    void Start()
    {
        LoadFantasyAssets();
        BuildAtmosphere();
        FixMenuLayout();
        SpawnFloatingEmbers();
    }

    void LoadFantasyAssets()
    {
        btnNormal      = Resources.Load<Sprite>("UI/btn_fantasy_normal");
        btnHover       = Resources.Load<Sprite>("UI/btn_fantasy_hover");
        btnPressed     = Resources.Load<Sprite>("UI/btn_fantasy_pressed");
        leftVignette   = Resources.Load<Sprite>("UI/menu_left_vignette");
        screenVignette = Resources.Load<Sprite>("UI/screen_vignette");
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
            vigGO.transform.SetSiblingIndex(1); // Tepat di atas background

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

        // 2. Left menu shadow panel (agar tombol kontras dan terbaca jelas)
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

    void FixMenuLayout()
    {
        // Posisi container Main Menu di sisi kiri
        RectTransform me = GetComponent<RectTransform>();
        if (me != null)
        {
            me.anchorMin        = new Vector2(0f, 0.5f);
            me.anchorMax        = new Vector2(0f, 0.5f);
            me.pivot            = new Vector2(0f, 0.5f);
            me.anchoredPosition = new Vector2(140f, -60f);
            me.sizeDelta        = new Vector2(300f, 380f);
        }

        // Urutan tombol resmi: PLAY (atas) -> BIODATA -> ABOUT -> QUIT (bawah)
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

            // RectTransform
            RectTransform rt = btnT.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin        = new Vector2(0.5f, 0.5f);
                rt.anchorMax        = new Vector2(0.5f, 0.5f);
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, yPos);
                rt.sizeDelta        = new Vector2(260f, 52f);
            }

            // Image latar tombol bergaya Dark Fantasy
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
                // Fallback jika sprite belum di-import
                img.color = new Color(0.10f, 0.06f, 0.12f, 0.75f);
            }

            // Button transitions
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
            }

            // Hover effect (animasi skala halus)
            AddHoverEffect(btnT.gameObject);

            // Styling TMP Text
            foreach (var tmp in btnT.GetComponentsInChildren<TMP_Text>(true))
            {
                tmp.text               = label;
                tmp.enableWordWrapping = false;
                tmp.overflowMode       = TextOverflowModes.Overflow;
                tmp.alignment          = TextAlignmentOptions.Center;
                tmp.fontSize           = 28f;
                tmp.characterSpacing   = 3f;
                tmp.color              = new Color(1.0f, 0.95f, 0.82f, 1f); // emas berkilau
            }
        }

        Debug.Log("<color=gold>[MainMenu] Dark Fantasy theme diterapkan dengan rapi!</color>");
    }

    void AddHoverEffect(GameObject go)
    {
        EventTrigger trigger = go.GetComponent<EventTrigger>();
        if (trigger == null) trigger = go.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        // Pointer Enter: scale up ke 1.04x
        EventTrigger.Entry enter = new EventTrigger.Entry();
        enter.eventID = EventTriggerType.PointerEnter;
        enter.callback.AddListener((data) => {
            StopAllCoroutines();
            StartCoroutine(AnimateScale(go.transform, 1.04f, 0.12f));
        });
        trigger.triggers.Add(enter);

        // Pointer Exit: scale kembali ke 1.0x
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
        main.loop           = true;
        main.startLifetime  = 5f;
        main.startSpeed     = 0.6f;
        main.startSize      = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor     = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.75f, 0.25f, 0.8f),
            new Color(1f, 0.40f, 0.05f, 0.6f)
        );
        main.maxParticles   = 35;
        main.simulationSpace= ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 6f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(16f, 0.5f, 1f);
        shape.position  = new Vector3(0f, -4.5f, 0f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.y = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
        vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);

        var renderer = emberGO.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 5;

        Debug.Log("<color=orange>[MainMenu] Partikel ember api aktif!</color>");
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
        SceneManager.LoadScene("Main Menu");
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
