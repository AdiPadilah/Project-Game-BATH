using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BossTrigger : MonoBehaviour
{
    [SerializeField] private BossAI boss;
    [SerializeField] private float roarDuration = 2.0f;
    [SerializeField] private float focusDelay = 1.8f; // Delay menegangkan membingkai bos sebelum auman

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            Player player = col.GetComponent<Player>();
            StartCoroutine(BossIntroCutscene(player));
        }
    }

    private IEnumerator BossIntroCutscene(Player player)
    {
        // 1. Kunci kontrol Player dan posisikan menghadap Boss
        if (player != null)
        {
            player.canMove = false;
            Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
            if (prb != null) prb.velocity = new Vector2(0f, prb.velocity.y);

            Animator panim = player.GetComponentInChildren<Animator>();
            if (panim != null) panim.SetFloat("xVelocity", 0f);

            // Hadapkan player ke arah boss
            if (boss != null)
            {
                float d = boss.transform.position.x - player.transform.position.x;
                if ((d > 0 && !player.facingRight) || (d < 0 && player.facingRight))
                    player.Flip();
            }
        }

        // 2. Munculkan letterbox hitam sinematik halus
        Image topBar = null, botBar = null;
        Canvas overlayCanvas = null;
        GameObject overlay = CreateLetterbox(out topBar, out botBar, out overlayCanvas);
        yield return StartCoroutine(AnimateLetterbox(topBar, botBar, 0f, 1f, 0.4f));

        // 3. Kamera menyorot ke Boss yang sedang DIAM / IDLE di tanah
        if (CameraFollow.instance != null && boss != null)
        {
            CameraFollow.instance.smoothSpeed = 4.5f;
            CameraFollow.instance.overrideTarget = boss.transform;
        }

        // 4. Munculkan Title Card nama bos "King Demon Slime"
        Canvas targetCanvas = overlayCanvas != null ? overlayCanvas : (topBar != null ? topBar.canvas : null);
        GameObject titleCard = CreateBossTitleCard(targetCanvas);

        // DELAY MENEGANGKAN: Kamera membingkai Boss yang berdiri tenang (Idle)
        // Kartu nama muncul perlahan (fade-in) membangun antisipasi
        yield return StartCoroutine(FadeTitleCard(titleCard, 0f, 1f, 0.9f));
        yield return new WaitForSeconds(focusDelay);

        // 5. Boss MENGAMUK / BERTERIAK DUA LENGAN seperti Monster Marah!
        if (boss != null)
        {
            boss.PlayRoar();
        }

        // Efek guncangan layar intimidasi
        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.TriggerScreenShake(2.0f, 0.45f);
        }

        // Efek flash aura intimidasi di letterbox & kartu nama berdenyut merah
        if (topBar != null && botBar != null)
        {
            StartCoroutine(IntimidationFlash(topBar, botBar));
        }
        if (titleCard != null)
        {
            StartCoroutine(PulseTitleCard(titleCard));
        }

        // Tunggu auman dua lengan selesai
        yield return new WaitForSeconds(roarDuration);

        // 6. Kartu nama memudar dan kamera kembali menyorot ke arah Player
        StartCoroutine(FadeTitleCard(titleCard, 1f, 0f, 0.5f));

        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.overrideTarget = null;
            CameraFollow.instance.smoothSpeed = 5.0f;
        }

        yield return new WaitForSeconds(0.6f);

        // 7. Letterbox keluar dan kontrol pemain dikembalikan
        yield return StartCoroutine(AnimateLetterbox(topBar, botBar, 1f, 0f, 0.35f));
        if (overlay != null) Destroy(overlay);
        if (titleCard != null) Destroy(titleCard);

        if (player != null)
        {
            player.canMove = true;
        }

        // 8. Boss baru mulai berjalan dan bertarung!
        if (boss != null)
        {
            boss.ActivateBoss();
        }
    }

    private GameObject CreateBossTitleCard(Canvas canvas)
    {
        if (canvas == null) return null;

        GameObject cardRoot = new GameObject("BossTitleCard");
        cardRoot.transform.SetParent(canvas.transform, false);

        RectTransform rt = cardRoot.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.22f);
        rt.anchorMax = new Vector2(0.5f, 0.22f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(600f, 100f);

        // Judul Utama: KING DEMON SLIME
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(cardRoot.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.text = "KING DEMON SLIME";
        titleText.fontSize = 36;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.85f, 0.35f, 0f); // Emas bernyala (mulai transparan)

        RectTransform titleRT = titleObj.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 0.35f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.anchoredPosition = Vector2.zero;
        titleRT.sizeDelta = Vector2.zero;

        Outline outline = titleObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.25f, 0.02f, 0.02f, 0f);
        outline.effectDistance = new Vector2(2.5f, -2.5f);

        Shadow shadow = titleObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0f);
        shadow.effectDistance = new Vector2(3.5f, -3.5f);

        // Subtitle: - Ancient Demon of the Ruined Forest -
        GameObject subObj = new GameObject("SubtitleText");
        subObj.transform.SetParent(cardRoot.transform, false);
        Text subText = subObj.AddComponent<Text>();
        subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subText.text = "- Ancient Demon of the Ruined Forest -";
        subText.fontSize = 17;
        subText.fontStyle = FontStyle.Italic;
        subText.alignment = TextAnchor.MiddleCenter;
        subText.color = new Color(0.9f, 0.75f, 0.75f, 0f);

        RectTransform subRT = subObj.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0f, 0f);
        subRT.anchorMax = new Vector2(1f, 0.38f);
        subRT.anchoredPosition = Vector2.zero;
        subRT.sizeDelta = Vector2.zero;

        Outline subOutline = subObj.AddComponent<Outline>();
        subOutline.effectColor = new Color(0f, 0f, 0f, 0f);
        subOutline.effectDistance = new Vector2(1.5f, -1.5f);

        return cardRoot;
    }

    private IEnumerator FadeTitleCard(GameObject card, float from, float to, float duration)
    {
        if (card == null) yield break;
        Text[] texts = card.GetComponentsInChildren<Text>();
        Outline[] outlines = card.GetComponentsInChildren<Outline>();
        Shadow[] shadows = card.GetComponentsInChildren<Shadow>();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float a = Mathf.Lerp(from, to, t);

            foreach (var txt in texts)
            {
                if (txt != null)
                {
                    Color c = txt.color;
                    c.a = a;
                    txt.color = c;
                }
            }
            foreach (var o in outlines)
            {
                if (o != null)
                {
                    Color c = o.effectColor;
                    c.a = a;
                    o.effectColor = c;
                }
            }
            foreach (var s in shadows)
            {
                if (s != null)
                {
                    Color c = s.effectColor;
                    c.a = a * 0.9f;
                    s.effectColor = c;
                }
            }
            yield return null;
        }

        foreach (var txt in texts)
        {
            if (txt != null)
            {
                Color c = txt.color;
                c.a = to;
                txt.color = c;
            }
        }
    }

    private IEnumerator PulseTitleCard(GameObject card)
    {
        if (card == null) yield break;
        Text[] texts = card.GetComponentsInChildren<Text>();
        Color crimson = new Color(1f, 0.25f, 0.15f, 1f);
        Color gold = new Color(1f, 0.85f, 0.35f, 1f);

        float elapsed = 0f;
        while (elapsed < 1.6f)
        {
            elapsed += Time.deltaTime;
            float pulse = (Mathf.Sin(elapsed * 12f) + 1f) * 0.5f;

            if (card != null)
            {
                float scale = 1f + pulse * 0.08f;
                card.transform.localScale = new Vector3(scale, scale, 1f);
            }

            if (texts.Length > 0 && texts[0] != null)
            {
                texts[0].color = Color.Lerp(gold, crimson, pulse);
            }
            yield return null;
        }

        if (card != null) card.transform.localScale = Vector3.one;
        if (texts.Length > 0 && texts[0] != null) texts[0].color = gold;
    }

    private IEnumerator IntimidationFlash(Image top, Image bot)
    {
        Color darkRed = new Color(0.55f, 0.05f, 0.05f, 0.95f);
        Color black = new Color(0f, 0f, 0f, 0.95f);
        float elapsed = 0f;
        while (elapsed < 1.6f)
        {
            elapsed += Time.deltaTime;
            float pulse = (Mathf.Sin(elapsed * 14f) + 1f) * 0.5f;
            Color c = Color.Lerp(black, darkRed, pulse * 0.85f);
            if (top != null) top.color = c;
            if (bot != null) bot.color = c;
            yield return null;
        }
        if (top != null) top.color = black;
        if (bot != null) bot.color = black;
    }

    private GameObject CreateLetterbox(out Image top, out Image bot, out Canvas overlayCanvas)
    {
        top = null; bot = null;
        Canvas canvas = null;
        foreach (var c in FindObjectsOfType<Canvas>())
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) { canvas = c; break; }

        GameObject root = new GameObject("BossLetterbox");
        if (canvas == null)
        {
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();
        }

        overlayCanvas = canvas;
        top = MakeBar(canvas, true);
        bot = MakeBar(canvas, false);
        return root;
    }

    private Image MakeBar(Canvas canvas, bool isTop)
    {
        GameObject go = new GameObject(isTop ? "LBTop" : "LBBot");
        go.transform.SetParent(canvas.transform, false);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = isTop ? new Vector2(0, 1) : new Vector2(0, 0);
        rt.anchorMax = isTop ? new Vector2(1, 1) : new Vector2(1, 0);
        rt.pivot     = isTop ? new Vector2(0.5f, 1) : new Vector2(0.5f, 0);
        rt.sizeDelta = new Vector2(0, Screen.height * 0.12f);
        return img;
    }

    private IEnumerator AnimateLetterbox(Image top, Image bot, float from, float to, float dur)
    {
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(from, to, elapsed / dur) * 0.95f;
            Color c = new Color(0f, 0f, 0f, a);
            if (top != null) top.color = c;
            if (bot != null) bot.color = c;
            yield return null;
        }
        Color fc = new Color(0f, 0f, 0f, to * 0.95f);
        if (top != null) top.color = fc;
        if (bot != null) bot.color = fc;
    }
}
