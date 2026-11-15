using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EnergiBarierEffect : MonoBehaviour
{
    private SpriteRenderer sr;

    [Header("Line Barrier Scale")]
    [SerializeField] private float baseScaleX = 0.25f;       // Garis tipis segaris
    [SerializeField] private float shieldProtectScaleX = 0.65f; // Melebar saat shield protect aktif
    [SerializeField] private float pushbackSpeed = 5.0f;     // Gaya tolak halus saat maksa masuk

    [Header("Transparency Settings")]
    [SerializeField] [Range(0.05f, 0.4f)] private float minAlpha = 0.20f;
    [SerializeField] [Range(0.2f, 0.9f)] private float maxAlpha = 0.50f;
    [SerializeField] private float pulseSpeed = 2.5f;

    [Header("Energy Colors")]
    [SerializeField] private Color primaryColor = new Color(0f, 0.95f, 1f);     // Neon Cyan
    [SerializeField] private Color secondaryColor = new Color(0.1f, 0.65f, 1f); // Electric Blue
    [SerializeField] private Color flareColor = new Color(0.9f, 1f, 1f);        // Shield Protect White-Cyan

    private float touchFlare = 0f;
    private float initialScaleY = 17f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        initialScaleY = transform.localScale.y;

        // Atur skala awal menjadi segaris tipis
        transform.localScale = new Vector3(baseScaleX, initialScaleY, 1f);

        // Pastikan memakai sprite EnergyBarrier laser line
        if (sr != null && (sr.sprite == null || sr.sprite.name == "ArenaSprite" || sr.sprite.name == "Square"))
        {
            Sprite barrier = Resources.Load<Sprite>("EnergyBarrier");
#if UNITY_EDITOR
            if (barrier == null)
            {
                barrier = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Grafik/EnergyBarrier.png");
            }
#endif
            if (barrier != null)
            {
                sr.sprite = barrier;
            }
        }
    }

    void Update()
    {
        if (sr == null) return;

        // Gelombang energi halus di sepanjang garis
        float wave1 = Mathf.Sin(Time.time * pulseSpeed);
        float wave2 = Mathf.Sin(Time.time * (pulseSpeed * 2.8f)) * 0.25f;
        float normWave = (wave1 + wave2 + 1.25f) / 2.5f;

        float targetAlpha = Mathf.Lerp(minAlpha, maxAlpha, normWave);

        // Transisi warna berkilau
        float colorShift = (Mathf.Sin(Time.time * (pulseSpeed * 1.3f)) + 1f) * 0.5f;
        Color currentColor = Color.Lerp(primaryColor, secondaryColor, colorShift);

        // Efek Shield Protect saat ditabrak/dipaksa masuk
        float targetScaleX = baseScaleX;
        if (touchFlare > 0.01f)
        {
            currentColor = Color.Lerp(currentColor, flareColor, touchFlare);
            targetAlpha = Mathf.Min(0.85f, targetAlpha + (touchFlare * 0.45f));
            targetScaleX = Mathf.Lerp(baseScaleX, shieldProtectScaleX, touchFlare);
            touchFlare = Mathf.MoveTowards(touchFlare, 0f, Time.deltaTime * 2.5f);
        }

        // Transisi lebar garis halus (melebar saat shield aktif, lalu kembali segaris tipis)
        transform.localScale = new Vector3(
            Mathf.Lerp(transform.localScale.x, targetScaleX, Time.deltaTime * 10f),
            initialScaleY,
            1f
        );

        sr.color = new Color(currentColor.r, currentColor.g, currentColor.b, targetAlpha);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TriggerShieldProtect(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TriggerShieldProtect(collision);
    }

    private void TriggerShieldProtect(Collision2D collision)
    {
        touchFlare = 1.0f;

        // Efek gaya tolak perisai (pushback) agar tidak bisa ditembus saat dipaksa dorong
        Rigidbody2D otherRb = collision.rigidbody;
        if (otherRb != null)
        {
            float pushDir = Mathf.Sign(collision.transform.position.x - transform.position.x);
            if (pushDir == 0f) pushDir = 1f;

            // Berikan tolakan halus yang kokoh menjauh dari dinding
            otherRb.velocity = new Vector2(pushDir * pushbackSpeed, otherRb.velocity.y);
        }
    }
}

