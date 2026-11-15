using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PlayerHUD - Mengelola tampilan Frame Avatar, Bar HP (Ruby Red),
/// dan Bar Mana (Sapphire Blue) di pojok kiri atas bergaya Dark Fantasy.
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    public static PlayerHUD instance;

    [Header("Player Reference")]
    [SerializeField] private Player player;

    [Header("UI Elements (Auto-Generated if null)")]
    [SerializeField] private Image hpFillImage;
    [SerializeField] private Image manaFillImage;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI manaText;

    // Internal references
    private GameObject hudRoot;
    private float targetHpPercent = 1f;
    private float currentHpPercent = 1f;
    private float targetManaPercent = 1f;
    private float currentManaPercent = 1f;

    // Dark Fantasy Sprites
    private Sprite avatarFrameSprite;
    private Sprite playerPortraitSprite;
    private Sprite barBgSprite;
    private Sprite hpFillSprite;
    private Sprite manaFillSprite;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        LoadSprites();
    }

    private void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<Player>();

        HideOldRawHealthSlider();
        BuildDarkFantasyHUD();
    }

    private void LoadSprites()
    {
        avatarFrameSprite   = Resources.Load<Sprite>("UI/hud_avatar_frame");
        playerPortraitSprite= Resources.Load<Sprite>("UI/hud_player_portrait");
        barBgSprite         = Resources.Load<Sprite>("UI/hud_bar_bg");
        hpFillSprite        = Resources.Load<Sprite>("UI/hud_hp_fill");
        manaFillSprite      = Resources.Load<Sprite>("UI/hud_mana_fill");
    }

    private void HideOldRawHealthSlider()
    {
        // Cari slider lama bawaan Unity (Slider hijau di canvas terpisah)
        PlayerHealthUI phUI = FindFirstObjectByType<PlayerHealthUI>();
        if (phUI != null && phUI.slider != null)
        {
            phUI.slider.gameObject.SetActive(false);
        }

        GameObject oldPlayerSlider = GameObject.Find("Canvas/Player");
        if (oldPlayerSlider != null)
        {
            Slider s = oldPlayerSlider.GetComponent<Slider>();
            if (s != null)
            {
                oldPlayerSlider.SetActive(false);
            }
        }
    }

    private void BuildDarkFantasyHUD()
    {
        // Pastikan kita menaruh HUD di dalam Canvas InGame_UI yang sudah ScaleWithScreenSize (1920x1080)
        Canvas targetCanvas = GetComponent<Canvas>();
        if (targetCanvas == null)
        {
            GameObject inGameCanvas = GameObject.Find("InGame_UI");
            if (inGameCanvas != null)
                targetCanvas = inGameCanvas.GetComponent<Canvas>();
            if (targetCanvas == null)
                targetCanvas = FindFirstObjectByType<Canvas>();
        }

        if (targetCanvas == null)
        {
            Debug.LogError("[PlayerHUD] Tidak menemukan Canvas untuk menempatkan HUD!");
            return;
        }

        Transform existingRoot = targetCanvas.transform.Find("Player_HUD_Frame");
        if (existingRoot != null)
        {
            hudRoot = existingRoot.gameObject;
            return;
        }

        // ── 1. Root Frame Container (Top-Left Anchor) ─────────────────────────
        hudRoot = new GameObject("Player_HUD_Frame");
        hudRoot.transform.SetParent(targetCanvas.transform, false);

        RectTransform rootRT = hudRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0f, 1f);
        rootRT.anchorMax = new Vector2(0f, 1f);
        rootRT.pivot     = new Vector2(0f, 1f);
        rootRT.anchoredPosition = new Vector2(24f, -20f);
        rootRT.sizeDelta        = new Vector2(400f, 110f);

        // ── 2. Avatar Container & Frame (Naik ke atas rata atas) ─────────────
        GameObject avatarGO = new GameObject("Avatar_Container");
        avatarGO.transform.SetParent(hudRoot.transform, false);

        RectTransform avatarRT = avatarGO.AddComponent<RectTransform>();
        avatarRT.anchorMin = new Vector2(0f, 1f);
        avatarRT.anchorMax = new Vector2(0f, 1f);
        avatarRT.pivot     = new Vector2(0f, 1f);
        avatarRT.anchoredPosition = new Vector2(0f, 0f);
        avatarRT.sizeDelta        = new Vector2(98f, 98f);

        // Foto / Potret Karakter Ksatria (Jelas, besar, tajam)
        GameObject portraitGO = new GameObject("Player_Portrait");
        portraitGO.transform.SetParent(avatarGO.transform, false);

        RectTransform portraitRT = portraitGO.AddComponent<RectTransform>();
        portraitRT.anchorMin = new Vector2(0.5f, 0.5f);
        portraitRT.anchorMax = new Vector2(0.5f, 0.5f);
        portraitRT.pivot     = new Vector2(0.5f, 0.5f);
        portraitRT.anchoredPosition = Vector2.zero;
        portraitRT.sizeDelta        = new Vector2(84f, 84f);

        Image portraitImg = portraitGO.AddComponent<Image>();
        if (playerPortraitSprite != null)
        {
            portraitImg.sprite = playerPortraitSprite;
        }
        else
        {
            portraitImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        }
        portraitImg.raycastTarget = false;

        // Bingkai Ornamen Emas Kuno Avatar (di lapisan luar melingkari portrait)
        GameObject frameGO = new GameObject("Avatar_Gold_Frame");
        frameGO.transform.SetParent(avatarGO.transform, false);

        RectTransform frameRT = frameGO.AddComponent<RectTransform>();
        frameRT.anchorMin = Vector2.zero;
        frameRT.anchorMax = Vector2.one;
        frameRT.offsetMin = Vector2.zero;
        frameRT.offsetMax = Vector2.zero;

        Image frameImg = frameGO.AddComponent<Image>();
        if (avatarFrameSprite != null)
        {
            frameImg.sprite = avatarFrameSprite;
        }
        else
        {
            frameImg.color = new Color(1f, 0.85f, 0.35f, 0.85f);
        }
        frameImg.raycastTarget = false;

        // ── 3. Bars Container (Turun sedikit ke bawah agar pas dengan Avatar) ──
        GameObject barsGO = new GameObject("Bars_Container");
        barsGO.transform.SetParent(hudRoot.transform, false);

        RectTransform barsRT = barsGO.AddComponent<RectTransform>();
        barsRT.anchorMin = new Vector2(0f, 1f);
        barsRT.anchorMax = new Vector2(0f, 1f);
        barsRT.pivot     = new Vector2(0f, 1f);
        barsRT.anchoredPosition = new Vector2(108f, -14f);
        barsRT.sizeDelta        = new Vector2(280f, 80f);

        // ── 4. HP Bar (Sedikit ke bawah) ─────────────────────────────────────
        GameObject hpBarGO = CreateBar("HP_Bar", barsGO.transform, new Vector2(0f, -6f), new Vector2(250f, 24f),
                                       barBgSprite, hpFillSprite, out hpFillImage, out hpText,
                                       new Color(1f, 0.85f, 0.4f, 1f), "HP");

        // ── 5. Mana Bar (Lebih ke bawah berjarak rapi) ────────────────────────
        GameObject manaBarGO = CreateBar("Mana_Bar", barsGO.transform, new Vector2(0f, -38f), new Vector2(210f, 18f),
                                         barBgSprite, manaFillSprite, out manaFillImage, out manaText,
                                         new Color(0.45f, 0.88f, 1f, 1f), "MP");
    }

    private GameObject CreateBar(string name, Transform parent, Vector2 pos, Vector2 size,
                                 Sprite bgSprite, Sprite fillSprite,
                                 out Image fillImgOut, out TextMeshProUGUI textOut,
                                 Color labelColor, string prefix)
    {
        // Bar Background
        GameObject barGO = new GameObject(name);
        barGO.transform.SetParent(parent, false);

        RectTransform barRT = barGO.AddComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0f, 1f);
        barRT.anchorMax = new Vector2(0f, 1f);
        barRT.pivot     = new Vector2(0f, 1f);
        barRT.anchoredPosition = pos;
        barRT.sizeDelta        = size;

        Image bgImg = barGO.AddComponent<Image>();
        if (bgSprite != null)
        {
            bgImg.sprite = bgSprite;
            bgImg.type   = Image.Type.Sliced;
        }
        else
        {
            bgImg.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);
        }
        bgImg.raycastTarget = false;

        // Bar Fill
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(barGO.transform, false);

        RectTransform fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(1f, 1f);
        fillRT.offsetMin = new Vector2(4f, 4f);
        fillRT.offsetMax = new Vector2(-4f, -4f);

        Image fillImg = fillGO.AddComponent<Image>();
        if (fillSprite != null)
        {
            fillImg.sprite = fillSprite;
        }
        else
        {
            fillImg.color = (prefix == "HP") ? new Color(0.85f, 0.15f, 0.2f, 1f) : new Color(0.15f, 0.55f, 0.95f, 1f);
        }
        fillImg.type       = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 1f;
        fillImg.raycastTarget = false;

        fillImgOut = fillImg;

        // Bar Text (Nilai HP / MP)
        GameObject textGO = new GameObject("Text_Value");
        textGO.transform.SetParent(barGO.transform, false);

        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(8f, 0f);
        textRT.offsetMax = new Vector2(-8f, 0f);

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = $"{prefix} 100 / 100";
        tmp.fontSize = (size.y > 20f) ? 13f : 11f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.MidlineRight;
        tmp.color = labelColor;
        tmp.raycastTarget = false;
        tmp.margin = Vector4.zero;

        textOut = tmp;

        return barGO;
    }

    private void Update()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
            if (player == null) return;
        }

        // ── Update Nilai HP ──────────────────────────────────────────────────
        int curHp = player.GetCurrentHealth();
        int maxHp = player.GetMaxHealth();
        if (maxHp <= 0) maxHp = 100;

        targetHpPercent = Mathf.Clamp01((float)curHp / maxHp);
        currentHpPercent = Mathf.Lerp(currentHpPercent, targetHpPercent, Time.unscaledDeltaTime * 12f);

        if (hpFillImage != null)
        {
            hpFillImage.fillAmount = currentHpPercent;
        }

        if (hpText != null)
        {
            hpText.text = $"{curHp} / {maxHp}";
        }

        // ── Update Nilai Mana ────────────────────────────────────────────────
        int curMana = player.GetCurrentMana();
        int maxMana = player.GetMaxMana();
        if (maxMana <= 0) maxMana = 100;

        targetManaPercent = Mathf.Clamp01((float)curMana / maxMana);
        currentManaPercent = Mathf.Lerp(currentManaPercent, targetManaPercent, Time.unscaledDeltaTime * 12f);

        if (manaFillImage != null)
        {
            manaFillImage.fillAmount = currentManaPercent;
        }

        if (manaText != null)
        {
            manaText.text = $"{curMana} / {maxMana}";
        }
    }
}
