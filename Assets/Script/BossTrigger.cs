using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BossTrigger : MonoBehaviour
{
    [SerializeField] private BossAI boss;
    [SerializeField] private float roarDuration = 1.8f;

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
        GameObject overlay = CreateLetterbox(out topBar, out botBar);
        yield return StartCoroutine(AnimateLetterbox(topBar, botBar, 0f, 1f, 0.4f));

        // 3. Kamera menyorot ke Boss yang sedang DIAM / IDLE di tanah
        if (CameraFollow.instance != null && boss != null)
        {
            CameraFollow.instance.smoothSpeed = 4.5f;
            CameraFollow.instance.overrideTarget = boss.transform;
        }

        // Jeda sejenak kamera membingkai Boss yang sedang berdiri tenang (membangun atmosfer tegang)
        yield return new WaitForSeconds(0.8f);

        // 4. Boss MENGAMUK / BERTERIAK dengan Efek Intimidasi!
        if (boss != null)
        {
            boss.PlayRoar();
        }

        // Efek guncangan layar intimidasi
        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.TriggerScreenShake(1.6f, 0.35f);
        }

        // Efek aura intimidasi merah gelap di border layar
        if (topBar != null && botBar != null)
        {
            StartCoroutine(IntimidationFlash(topBar, botBar));
        }

        // Tunggu auman selesai
        yield return new WaitForSeconds(roarDuration);

        // 5. Kamera kembali menyorot ke arah Player
        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.overrideTarget = null;
            CameraFollow.instance.smoothSpeed = 5.0f;
        }

        yield return new WaitForSeconds(0.6f);

        // 6. Letterbox keluar dan kontrol pemain dikembalikan
        yield return StartCoroutine(AnimateLetterbox(topBar, botBar, 1f, 0f, 0.35f));
        if (overlay != null) Destroy(overlay);

        if (player != null)
        {
            player.canMove = true;
        }

        // 7. Boss baru mulai berjalan dan bertarung!
        if (boss != null)
        {
            boss.ActivateBoss();
        }
    }

    private IEnumerator IntimidationFlash(Image top, Image bot)
    {
        Color darkRed = new Color(0.45f, 0.05f, 0.05f, 0.95f);
        Color black = new Color(0f, 0f, 0f, 0.95f);
        float elapsed = 0f;
        while (elapsed < 1.4f)
        {
            elapsed += Time.deltaTime;
            float pulse = (Mathf.Sin(elapsed * 14f) + 1f) * 0.5f;
            Color c = Color.Lerp(black, darkRed, pulse * 0.75f);
            if (top != null) top.color = c;
            if (bot != null) bot.color = c;
            yield return null;
        }
        if (top != null) top.color = black;
        if (bot != null) bot.color = black;
    }

    private GameObject CreateLetterbox(out Image top, out Image bot)
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
