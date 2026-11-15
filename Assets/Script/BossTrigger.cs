using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BossTrigger : MonoBehaviour
{
    [SerializeField] private BossAI boss;
    [SerializeField] private float roarDuration = 2.2f;

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
        // ===== FASE 0: KUNCI PLAYER =====
        Rigidbody2D prb = null;
        if (player != null)
        {
            player.canMove = false;
            prb = player.GetComponent<Rigidbody2D>();
            if (prb != null) prb.velocity = Vector2.zero;

            Animator panim = player.GetComponentInChildren<Animator>();
            if (panim != null) panim.SetFloat("xVelocity", 0f);

            // Hadapkan player ke boss
            if (boss != null)
            {
                float d = boss.transform.position.x - player.transform.position.x;
                if ((d > 0 && !player.facingRight) || (d < 0 && player.facingRight))
                    player.Flip();
            }
        }

        // ===== FASE 1: BUAT LETTERBOX HITAM =====
        Image topBar = null, botBar = null;
        GameObject overlay = CreateLetterbox(out topBar, out botBar);
        yield return StartCoroutine(AnimateLetterbox(topBar, botBar, 0f, 1f, 0.45f));

        // ===== FASE 2: KAMERA MELESAT KE BOSS =====
        if (CameraFollow.instance != null && boss != null)
        {
            CameraFollow.instance.smoothSpeed = 22f;
            CameraFollow.instance.overrideTarget = boss.transform;
        }

        // Boss mulai dari ukuran sangat kecil (muncul secara dramatis)
        Vector3 bossTargetScale = boss != null ? boss.transform.localScale : Vector3.one;
        float bossScaleAbs = Mathf.Abs(bossTargetScale.y);
        float xSign = Mathf.Sign(bossTargetScale.x);
        if (boss != null)
            boss.transform.localScale = new Vector3(xSign * bossScaleAbs * 0.05f,
                                                    bossScaleAbs * 0.05f, 1f);

        yield return new WaitForSeconds(0.25f);

        // ===== FASE 3: DRAMATIC PAUSE — SLOW MOTION =====
        Time.timeScale = 0.15f;
        yield return new WaitForSecondsRealtime(0.4f);
        Time.timeScale = 1f;

        // ===== FASE 4: BOSS TUMBUH ELASTIC (kecil → penuh dengan overshoot) =====
        yield return StartCoroutine(BossGrowIn(boss, bossScaleAbs, xSign, 0.6f));

        // Zoom kamera lebih dekat ke boss
        if (CameraFollow.instance != null)
            CameraFollow.instance.cutsceneZoomSize = 4.0f;

        yield return new WaitForSeconds(0.2f);

        // ===== FASE 5: BOSS MENGAUM + GUNCANGAN BERLAPIS =====
        if (boss != null) boss.PlayRoar();

        if (CameraFollow.instance != null)
            CameraFollow.instance.TriggerScreenShake(0.5f, 0.45f);

        yield return new WaitForSeconds(0.35f);

        if (CameraFollow.instance != null)
            CameraFollow.instance.TriggerScreenShake(1.5f, 0.55f);

        // Flash letterbox merah sebentar (boss mengancam)
        Color red = new Color(0.4f, 0f, 0f, 0.95f);
        Color blk = new Color(0f, 0f, 0f, 0.95f);
        if (topBar != null) topBar.color = red;
        if (botBar != null) botBar.color = red;
        yield return new WaitForSeconds(0.22f);
        if (topBar != null) topBar.color = blk;
        if (botBar != null) botBar.color = blk;

        yield return new WaitForSeconds(roarDuration - 0.6f);

        // ===== FASE 6: KAMERA BALIK KE PLAYER =====
        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.overrideTarget = null;
            CameraFollow.instance.smoothSpeed = 10f;
            CameraFollow.instance.cutsceneZoomSize = 5.5f;
        }

        yield return new WaitForSeconds(0.5f);

        // ===== FASE 7: LETTERBOX KELUAR =====
        yield return StartCoroutine(AnimateLetterbox(topBar, botBar, 1f, 0f, 0.4f));
        if (overlay != null) Destroy(overlay);

        if (CameraFollow.instance != null)
            CameraFollow.instance.smoothSpeed = 5f;

        // ===== FASE 8: MULAI FIGHT =====
        if (player != null) player.canMove = true;
        if (boss != null) boss.ActivateBoss();
    }

    // ─── Boss tumbuh dari kecil ke besar dengan Elastic Out ───
    private IEnumerator BossGrowIn(BossAI bossAI, float targetScale, float xSign, float duration)
    {
        if (bossAI == null) yield break;
        float elapsed = 0f;
        float startScale = targetScale * 0.05f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float s = Mathf.Lerp(startScale, targetScale, ElasticOut(t));
            bossAI.transform.localScale = new Vector3(xSign * s, s, 1f);
            yield return null;
        }
        bossAI.transform.localScale = new Vector3(xSign * targetScale, targetScale, 1f);
    }

    private float ElasticOut(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        const float c4 = (2f * Mathf.PI) / 3f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }

    // ─── Buat letterbox hitam atas & bawah ───
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
        rt.sizeDelta = new Vector2(0, Screen.height * 0.13f);
        return img;
    }

    private IEnumerator AnimateLetterbox(Image top, Image bot, float from, float to, float dur)
    {
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
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
