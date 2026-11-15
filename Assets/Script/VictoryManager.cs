using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

public class VictoryManager : MonoBehaviour
{
    [Header("UI Settings")]
    public GameObject victoryPanel;
    public Image[] stars;
    public Sprite yellowStar;
    public Sprite defaultStar;

    [Header("Animation Settings")]
    public float delayInSeconds = 0.5f;

    private bool sudahMenang = false;

    // Dark Fantasy UI Assets
    private Sprite modalPanelSprite;
    private Sprite btnNormal;
    private Sprite btnHover;
    private Sprite btnPressed;
    private AudioClip sfxHover;
    private AudioClip sfxClick;
    private AudioClip[] sfxStars;
    private AudioClip sfxStarAll;
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        LoadFantasyAssets();
    }

    void Start()
    {
        // Pastikan panel mati saat game dimulai
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
    }

    void LoadFantasyAssets()
    {
        modalPanelSprite = Resources.Load<Sprite>("UI/panel_fantasy_modal");
        btnNormal        = Resources.Load<Sprite>("UI/btn_fantasy_normal");
        btnHover         = Resources.Load<Sprite>("UI/btn_fantasy_hover");
        btnPressed       = Resources.Load<Sprite>("UI/btn_fantasy_pressed");
        sfxHover         = Resources.Load<AudioClip>("Audio/ui_hover");
        sfxClick         = Resources.Load<AudioClip>("Audio/ui_click");

        sfxStars = new AudioClip[3];
        sfxStars[0] = Resources.Load<AudioClip>("Audio/star_1");
        sfxStars[1] = Resources.Load<AudioClip>("Audio/star_2");
        sfxStars[2] = Resources.Load<AudioClip>("Audio/star_3");
        sfxStarAll  = Resources.Load<AudioClip>("Audio/star_all");
    }

    public void MunculkanMenang(float waktu)
    {
        if (sudahMenang) return;
        sudahMenang = true;

        // 1. Hentikan pergerakan/logika di script Player
        Player p = FindFirstObjectByType<Player>();
        if (p != null)
        {
            p.sudahMenang = true;
        }

        // 2. Sembunyikan Boss Health Bar & Label agar tidak bertumpuk di layar kemenangan
        BossAI boss = FindFirstObjectByType<BossAI>();
        if (boss != null && boss.bossHealthBar != null)
        {
            boss.bossHealthBar.gameObject.SetActive(false);
        }
        GameObject bhb = GameObject.Find("BossHealthBar");
        if (bhb != null) bhb.SetActive(false);

        // 3. Sembunyikan HUD Gameplay agar tampilan kemenangan bersih dan fokus
        GameObject inGameUI = GameObject.Find("InGame_UI");
        if (inGameUI != null) inGameUI.SetActive(false);

        GameObject inGameKill = GameObject.Find("InGame_KillCount");
        if (inGameKill != null) inGameKill.SetActive(false);

        PlayerHealthUI ph = FindFirstObjectByType<PlayerHealthUI>();
        if (ph != null && ph.slider != null)
        {
            ph.slider.gameObject.SetActive(false);
        }

        // 4. Validasi Panel
        if (victoryPanel == null)
        {
            Debug.LogError("❌ VictoryPanel belum diisi di Inspector!");
            return;
        }

        // 5. Pastikan Sorting Order Canvas tertinggi dan responsif
        Canvas canvas = victoryPanel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = 100;
            CanvasScaler cs = canvas.GetComponent<CanvasScaler>();
            if (cs != null)
            {
                cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cs.referenceResolution = new Vector2(1920, 1080);
                cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                cs.matchWidthOrHeight = 0.5f;
            }
        }

        // 6. Hitung Bintang berdasarkan durasi
        int jumlahBintang = HitungBintang(waktu);

        // 7. Percantik dan format layout Dark Fantasy untuk panel kemenangan
        SetupVictoryTheme(waktu, jumlahBintang);

        // 8. Munculkan Panel & Pause Game
        victoryPanel.SetActive(true);
        Time.timeScale = 0f;

        Debug.Log("🏆 Menang! Waktu Akhir: " + waktu + "s | Bintang didapat: " + jumlahBintang);

        // 9. Jalankan animasi bintang
        StartCoroutine(AnimasiBintang(jumlahBintang));
    }

    private void SetupVictoryTheme(float waktu, int jumlahBintang)
    {
        if (victoryPanel == null) return;

        // Backdrop gelap sinematik (menutupi seluruh layar)
        RectTransform vpRT = victoryPanel.GetComponent<RectTransform>();
        if (vpRT != null)
        {
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero;
            vpRT.offsetMax = Vector2.zero;
            vpRT.localScale = Vector3.one;
        }

        Image vpImg = victoryPanel.GetComponent<Image>();
        if (vpImg != null)
        {
            vpImg.color = new Color(0.02f, 0.02f, 0.04f, 0.88f);
        }

        // Frame Modal Batu Obsidian Ornamen Emas (Modal_Frame)
        Transform frameT = victoryPanel.transform.Find("Modal_Frame");
        if (frameT == null)
        {
            GameObject frameGO = new GameObject("Modal_Frame");
            frameGO.transform.SetParent(victoryPanel.transform, false);
            frameGO.transform.SetSiblingIndex(0);

            RectTransform frt = frameGO.AddComponent<RectTransform>();
            frt.anchorMin = new Vector2(0.5f, 0.5f);
            frt.anchorMax = new Vector2(0.5f, 0.5f);
            frt.pivot     = new Vector2(0.5f, 0.5f);
            frt.sizeDelta = new Vector2(760f, 520f);
            frt.anchoredPosition = Vector2.zero;
            frt.localScale = Vector3.one;

            Image fimg = frameGO.AddComponent<Image>();
            if (modalPanelSprite != null)
            {
                fimg.sprite = modalPanelSprite;
                fimg.type = Image.Type.Sliced;
                fimg.color = Color.white;
            }
        }
        else
        {
            frameT.SetSiblingIndex(0);
            RectTransform frt = frameT.GetComponent<RectTransform>();
            if (frt != null)
            {
                frt.sizeDelta = new Vector2(760f, 520f);
                frt.anchoredPosition = Vector2.zero;
                frt.localScale = Vector3.one;
            }
        }

        // Judul Utama: "MISSION COMPLETE"
        TextMeshProUGUI titleTMP = victoryPanel.GetComponentInChildren<TextMeshProUGUI>();
        if (titleTMP != null)
        {
            titleTMP.text = "MISSION COMPLETE";
            titleTMP.fontSize = 34f;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(1f, 0.86f, 0.38f, 1f);
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.margin = Vector4.zero; // Reset margin agar benar-benar simetris di tengah!

            RectTransform trt = titleTMP.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.5f, 0.5f);
            trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.pivot     = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = new Vector2(700f, 50f);
            trt.anchoredPosition = new Vector2(0f, 165f);
            trt.localScale = Vector3.one;
        }

        // Informasi Statistik: Waktu & Bintang
        Transform statsT = victoryPanel.transform.Find("Victory_Stats");
        TextMeshProUGUI statsTMP = null;
        if (statsT == null)
        {
            GameObject statsGO = new GameObject("Victory_Stats");
            statsGO.transform.SetParent(victoryPanel.transform, false);

            RectTransform srt = statsGO.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 0.5f);
            srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.pivot     = new Vector2(0.5f, 0.5f);
            srt.sizeDelta = new Vector2(600f, 35f);
            srt.anchoredPosition = new Vector2(0f, 118f);
            srt.localScale = Vector3.one;

            statsTMP = statsGO.AddComponent<TextMeshProUGUI>();
            if (titleTMP != null && titleTMP.font != null)
                statsTMP.font = titleTMP.font;
        }
        else
        {
            statsTMP = statsT.GetComponent<TextMeshProUGUI>();
        }

        if (statsTMP != null)
        {
            statsTMP.text = $"WAKTU SELESAI: {waktu:F1}s  |  BINTANG: {jumlahBintang}/3";
            statsTMP.fontSize = 19f;
            statsTMP.color = new Color(0.92f, 0.89f, 0.82f, 0.95f);
            statsTMP.alignment = TextAlignmentOptions.Center;
            statsTMP.margin = Vector4.zero;
        }

        // Posisi StarsContainer
        Transform scT = victoryPanel.transform.Find("StarsContainer");
        if (scT != null)
        {
            RectTransform scRT = scT.GetComponent<RectTransform>();
            scRT.anchorMin = new Vector2(0.5f, 0.5f);
            scRT.anchorMax = new Vector2(0.5f, 0.5f);
            scRT.pivot     = new Vector2(0.5f, 0.5f);
            scRT.anchoredPosition = new Vector2(0f, 40f);
            scRT.sizeDelta = new Vector2(300f, 90f);
            scRT.localScale = Vector3.one;

            // Rapikan posisi 3 bintang di dalamnya secara simetris
            if (stars != null && stars.Length >= 3)
            {
                if (stars[0] != null)
                {
                    RectTransform s0 = stars[0].rectTransform;
                    s0.anchorMin = new Vector2(0.5f, 0.5f);
                    s0.anchorMax = new Vector2(0.5f, 0.5f);
                    s0.pivot     = new Vector2(0.5f, 0.5f);
                    s0.anchoredPosition = new Vector2(-88f, -6f);
                    s0.sizeDelta = new Vector2(66f, 66f);
                    s0.localScale = Vector3.one;
                }
                if (stars[1] != null)
                {
                    RectTransform s1 = stars[1].rectTransform;
                    s1.anchorMin = new Vector2(0.5f, 0.5f);
                    s1.anchorMax = new Vector2(0.5f, 0.5f);
                    s1.pivot     = new Vector2(0.5f, 0.5f);
                    s1.anchoredPosition = new Vector2(0f, 12f);
                    s1.sizeDelta = new Vector2(80f, 80f);
                    s1.localScale = Vector3.one;
                }
                if (stars[2] != null)
                {
                    RectTransform s2 = stars[2].rectTransform;
                    s2.anchorMin = new Vector2(0.5f, 0.5f);
                    s2.anchorMax = new Vector2(0.5f, 0.5f);
                    s2.pivot     = new Vector2(0.5f, 0.5f);
                    s2.anchoredPosition = new Vector2(88f, -6f);
                    s2.sizeDelta = new Vector2(66f, 66f);
                    s2.localScale = Vector3.one;
                }
            }
        }

        // Tombol "Next" (Lanjutkan)
        Transform nextBtn = victoryPanel.transform.Find("Next");
        SetupButton(nextBtn, "LANJUTKAN", new Vector2(0f, -62f), new Color(1f, 0.88f, 0.50f, 1f), KeNext);

        // Tombol "Home" (Menu Utama)
        Transform homeBtn = victoryPanel.transform.Find("Home");
        SetupButton(homeBtn, "MENU UTAMA", new Vector2(0f, -128f), new Color(0.92f, 0.90f, 0.84f, 1f), KeHome);
    }

    private void SetupButton(Transform btnT, string label, Vector2 pos, Color textColor, UnityEngine.Events.UnityAction onClick)
    {
        if (btnT == null) return;

        RectTransform rt = btnT.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(260f, 50f);
            rt.localScale = Vector3.one;
        }

        Image img = btnT.GetComponent<Image>();
        if (img != null && btnNormal != null)
        {
            img.sprite = btnNormal;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }

        Button btn = btnT.GetComponent<Button>();
        if (btn != null)
        {
            btn.transition = Selectable.Transition.SpriteSwap;
            SpriteState ss = btn.spriteState;
            ss.highlightedSprite = btnHover;
            ss.pressedSprite     = btnPressed;
            ss.selectedSprite    = btnHover;
            btn.spriteState      = ss;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                if (audioSource != null && sfxClick != null)
                    audioSource.PlayOneShot(sfxClick, 0.55f);
                onClick?.Invoke();
            });
        }

        AddButtonHoverEffect(btnT.gameObject);

        // Tambah/Update Teks Tombol
        Transform txtT = btnT.Find("Btn_Label");
        TextMeshProUGUI tmp = null;
        if (txtT == null)
        {
            GameObject txtGO = new GameObject("Btn_Label");
            txtGO.transform.SetParent(btnT, false);

            RectTransform trt = txtGO.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            trt.localScale = Vector3.one;

            tmp = txtGO.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            tmp = txtT.GetComponent<TextMeshProUGUI>();
        }

        if (tmp != null)
        {
            tmp.text = label;
            tmp.fontSize = 21f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.margin = Vector4.zero;
        }
    }

    private void AddButtonHoverEffect(GameObject go)
    {
        if (go == null) return;
        EventTrigger trigger = go.GetComponent<EventTrigger>();
        if (trigger == null) trigger = go.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        EventTrigger.Entry enter = new EventTrigger.Entry();
        enter.eventID = EventTriggerType.PointerEnter;
        enter.callback.AddListener((data) => {
            if (!isActiveAndEnabled || !go.activeInHierarchy) return;
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
            if (!isActiveAndEnabled || !go.activeInHierarchy) return;
            StopAllCoroutines();
            StartCoroutine(AnimateScale(go.transform, 1.00f, 0.12f));
        });
        trigger.triggers.Add(exit);
    }

    private IEnumerator AnimateScale(Transform tr, float targetScale, float duration)
    {
        if (tr == null) yield break;
        Vector3 start = tr.localScale;
        Vector3 end   = new Vector3(targetScale, targetScale, 1f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (tr == null || !isActiveAndEnabled) yield break;
            elapsed += Time.unscaledDeltaTime;
            tr.localScale = Vector3.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        if (tr != null) tr.localScale = end;
    }

    int HitungBintang(float waktu)
    {
        if (waktu <= 60f)
        {
            return 3;
        }
        else if (waktu <= 100f)
        {
            return 2;
        }
        else if (waktu <= 150f)
        {
            return 1;
        }
        else
        {
            return 0; // Lebih dari 150 detik tidak dapat bintang
        }
    }

    IEnumerator AnimasiBintang(int jumlah)
    {
        if (stars == null || stars.Length == 0)
        {
            Debug.LogError("❌ Array Stars kosong! Masukkan Image bintang ke Inspector.");
            yield break;
        }

        // --- RESET TAMPILAN AWAL (Bintang Redup / Batu Kuno) ---
        Color lockedColor = new Color(0.28f, 0.26f, 0.24f, 0.75f);
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] != null)
            {
                if (defaultStar != null)
                    stars[i].sprite = defaultStar;

                stars[i].color = lockedColor;
                stars[i].transform.localScale = Vector3.one;
            }
        }

        // Tunggu sebentar sebelum animasi mulai (Gunakan Realtime karena timescale = 0)
        yield return new WaitForSecondsRealtime(0.4f);

        // --- PROSES MENGISI BINTANG (Emas Menyala + Animasi Pop) ---
        int jumlahFix = Mathf.Min(jumlah, stars.Length);
        Color goldColor = new Color(1f, 0.86f, 0.24f, 1f);

        for (int i = 0; i < jumlahFix; i++)
        {
            if (stars[i] == null) continue;

            // Efek suara denting bintang magis (nada bertingkat 1 -> 2 -> 3)
            AudioClip clipToPlay = null;
            if (sfxStars != null && i < sfxStars.Length && sfxStars[i] != null)
            {
                clipToPlay = sfxStars[i];
            }
            else
            {
                clipToPlay = sfxClick;
            }

            if (audioSource != null && clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay, 0.90f);
            }

            // Ganti sprite dan beri warna emas menyala
            if (yellowStar != null)
                stars[i].sprite = yellowStar;

            stars[i].color = goldColor;

            // Animasi Pop (Membesar lalu mengecil mulus)
            float t = 0f;
            float speed = 6f;
            Vector3 scaleBesar = Vector3.one * 1.45f;

            while (t < 1.0f)
            {
                t += Time.unscaledDeltaTime * speed;
                stars[i].transform.localScale = Vector3.Lerp(scaleBesar, Vector3.one, t);
                yield return null;
            }
            stars[i].transform.localScale = Vector3.one;

            // Jeda antar bintang
            yield return new WaitForSecondsRealtime(delayInSeconds);
        }

        // Jika berhasil mendapatkan bintang (terutama 3 bintang), putar perayaan shimmer fanfare singkat
        if (jumlahFix >= 3 && audioSource != null && sfxStarAll != null)
        {
            yield return new WaitForSecondsRealtime(0.2f);
            audioSource.PlayOneShot(sfxStarAll, 0.85f);
        }
    }

    // Fungsi untuk Button UI
    public void KeHome()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main Menu");
    }

    public void KeNext()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
