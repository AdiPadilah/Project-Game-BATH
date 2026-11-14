using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EnergiBarierEffect : MonoBehaviour
{
    private SpriteRenderer sr;

    [Header("Transparency Settings")]
    [SerializeField] [Range(0.05f, 0.4f)] private float minAlpha = 0.18f;
    [SerializeField] [Range(0.2f, 0.7f)] private float maxAlpha = 0.40f;
    [SerializeField] private float pulseSpeed = 2.5f;

    [Header("Energy Colors")]
    [SerializeField] private Color primaryColor = new Color(0f, 0.95f, 1f);     // Neon Cyan
    [SerializeField] private Color secondaryColor = new Color(0.1f, 0.65f, 1f); // Electric Blue
    [SerializeField] private Color flareColor = new Color(0.85f, 1f, 1f);       // Bright White-Cyan

    private float touchFlare = 0f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        // Otomatis pakai sprite EnergyBarrier jika masih memakai kotak putih lama (Square / ArenaSprite)
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

        // Gelombang energi halus (kombinasi harmonic wave + shimmer halus)
        float wave1 = Mathf.Sin(Time.time * pulseSpeed);
        float wave2 = Mathf.Sin(Time.time * (pulseSpeed * 2.8f)) * 0.25f;
        float normWave = (wave1 + wave2 + 1.25f) / 2.5f; // Normalisasi rentang 0..1

        float targetAlpha = Mathf.Lerp(minAlpha, maxAlpha, normWave);

        // Transisi warna berkilau antara neon cyan dan electric blue
        float colorShift = (Mathf.Sin(Time.time * (pulseSpeed * 1.3f)) + 1f) * 0.5f;
        Color currentColor = Color.Lerp(primaryColor, secondaryColor, colorShift);

        // Efek cahaya pengadang jika disentuh/ditabrak player atau musuh
        if (touchFlare > 0.01f)
        {
            currentColor = Color.Lerp(currentColor, flareColor, touchFlare);
            targetAlpha = Mathf.Min(0.70f, targetAlpha + (touchFlare * 0.30f));
            touchFlare = Mathf.MoveTowards(touchFlare, 0f, Time.deltaTime * 3.0f);
        }

        sr.color = new Color(currentColor.r, currentColor.g, currentColor.b, targetAlpha);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Kilatan energi saat ditabrak objek
        touchFlare = 1.0f;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Cahaya tetap berpendar lebih terang selama bersentuhan dengan dinding
        touchFlare = Mathf.Max(touchFlare, 0.45f);
    }
}

