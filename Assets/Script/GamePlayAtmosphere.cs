using UnityEngine;

/// <summary>
/// GamePlayAtmosphere - Menambahkan efek kunang-kunang (firefly) yang tersebar
/// di sepanjang level GamePlay. Attach script ini ke GameObject kosong di scene GamePlay.
/// </summary>
public class GamePlayAtmosphere : MonoBehaviour
{
    // ─── Konfigurasi Kunang-Kunang ───────────────────────────────────────────
    [Header("Firefly Settings")]
    [SerializeField] private int   emitterCount    = 6;        // jumlah emitter tersebar di level
    [SerializeField] private float levelStartX     = -20f;     // batas kiri level (world units)
    [SerializeField] private float levelEndX       =  80f;     // batas kanan level (world units)
    [SerializeField] private float emitterY        =   2f;     // posisi Y emitter (tengah level)
    [SerializeField] private float emitterWidth    =  18f;     // lebar area tiap emitter
    [SerializeField] private float emitterHeight   =   7f;     // tinggi area tiap emitter
    [SerializeField] private int   particlesPerEmitter = 25;   // max partikel per emitter
    [SerializeField] private float emissionRate    =   5f;     // partikel per detik per emitter

    // ─── Warna & Visual ─────────────────────────────────────────────────────
    [Header("Color")]
    [SerializeField] private Color fireflyColorA = new Color(0.50f, 1.00f, 0.20f, 0.90f); // hijau-kuning
    [SerializeField] private Color fireflyColorB = new Color(0.80f, 1.00f, 0.10f, 0.70f); // kuning-neon

    // ─── Gerak & Lifetime ───────────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float lifetimeMin  = 4f;
    [SerializeField] private float lifetimeMax  = 9f;
    [SerializeField] private float speedMin     = 0.05f;
    [SerializeField] private float speedMax     = 0.30f;
    [SerializeField] private float sizeMin      = 0.05f;
    [SerializeField] private float sizeMax      = 0.14f;

    // ─── Noise / Osilasi (agar kunang bergerak ke kiri-kanan acak) ──────────
    [Header("Turbulence")]
    [SerializeField] private float noiseStrength   = 0.6f;
    [SerializeField] private float noiseFrequency  = 0.4f;
    [SerializeField] private float noiseScrollSpeed= 0.2f;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        // Hindari duplikat saat scene di-reload
        if (GameObject.Find("GP_Fireflies_Root") != null)
            return;

        GameObject root = new GameObject("GP_Fireflies_Root");
        root.transform.SetParent(null); // letakkan di root scene

        float step = (levelEndX - levelStartX) / Mathf.Max(1, emitterCount - 1);

        for (int i = 0; i < emitterCount; i++)
        {
            float xPos = levelStartX + step * i;
            SpawnFireflyEmitter(root.transform, xPos, emitterY, i);
        }

        Debug.Log($"[GamePlayAtmosphere] {emitterCount} emitter kunang-kunang aktif dari X={levelStartX} s/d X={levelEndX}");
    }

    private void SpawnFireflyEmitter(Transform parent, float x, float y, int index)
    {
        GameObject go = new GameObject($"Firefly_Emitter_{index}");
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, y, 0f);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        // ── Main module ───────────────────────────────────────────────────────
        var main = ps.main;
        main.loop              = true;
        main.playOnAwake       = true;
        main.startLifetime     = new ParticleSystem.MinMaxCurve(lifetimeMin, lifetimeMax);
        main.startSpeed        = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize         = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startColor        = new ParticleSystem.MinMaxGradient(fireflyColorA, fireflyColorB);
        main.maxParticles      = particlesPerEmitter;
        main.simulationSpace   = ParticleSystemSimulationSpace.World;
        main.gravityModifier   = -0.04f;   // melayang sedikit ke atas

        // ── Emission ──────────────────────────────────────────────────────────
        var emission = ps.emission;
        emission.rateOverTime  = emissionRate;

        // ── Shape (kotak persegi lebar) ───────────────────────────────────────
        var shape = ps.shape;
        shape.enabled          = true;
        shape.shapeType        = ParticleSystemShapeType.Box;
        shape.scale            = new Vector3(emitterWidth, emitterHeight, 0.5f);

        // ── Color over lifetime (kedip-kedip) ─────────────────────────────────
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(fireflyColorA, 0.00f),
                new GradientColorKey(fireflyColorB, 0.50f),
                new GradientColorKey(fireflyColorA, 1.00f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.00f, 0.00f),   // fade in
                new GradientAlphaKey(1.00f, 0.15f),
                new GradientAlphaKey(0.80f, 0.50f),
                new GradientAlphaKey(1.00f, 0.80f),
                new GradientAlphaKey(0.00f, 1.00f)    // fade out
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // ── Size over lifetime (sedikit membesar-mengecil / berkedip) ─────────
        var sizeOL = ps.sizeOverLifetime;
        sizeOL.enabled = true;
        AnimationCurve sizeC = new AnimationCurve(
            new Keyframe(0.00f, 0.3f),
            new Keyframe(0.20f, 1.0f),
            new Keyframe(0.50f, 0.6f),
            new Keyframe(0.80f, 1.0f),
            new Keyframe(1.00f, 0.0f)
        );
        sizeOL.size = new ParticleSystem.MinMaxCurve(1f, sizeC);

        // ── Noise (osilasi kiri-kanan organik ala kunang-kunang) ─────────────
        var noise = ps.noise;
        noise.enabled     = true;
        noise.strength    = noiseStrength;
        noise.frequency   = noiseFrequency;
        noise.scrollSpeed = noiseScrollSpeed;
        noise.quality     = ParticleSystemNoiseQuality.Medium;
        noise.separateAxes = false;

        // ── Renderer (additive blending biar bercahaya) ───────────────────────
        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode        = ParticleSystemRenderMode.Billboard;
        rend.sortingLayerName  = "Default";
        rend.sortingOrder      = 50;

        // Material additive — coba pakai shader bawaan Unity yg additive
        Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
        if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
        {
            // Fallback ke Sprites/Default agar tetap tampil
            mat = new Material(Shader.Find("Sprites/Default"));
        }
        // Buat additive blending manual
        mat.SetFloat("_Mode", 1f);           // Additive
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        rend.material = mat;

        ps.Play();
    }
}
