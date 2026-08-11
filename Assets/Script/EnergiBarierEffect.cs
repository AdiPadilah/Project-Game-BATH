using UnityEngine;

public class EnergiBarierEffect : MonoBehaviour
{
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float glow = 0.5f + Mathf.Sin(Time.time * 6f) * 0.5f;

        sr.color = new Color(0f, 1f, 1f, glow);
    }
}