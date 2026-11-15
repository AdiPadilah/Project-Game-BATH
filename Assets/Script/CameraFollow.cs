using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow instance;

    public Transform target;
    public Transform overrideTarget;      // Target override saat cutscene
    public float smoothSpeed = 5f;
    public float cutsceneZoomSize = 4.0f; // Zoom saat menyorot boss (lebih dekat)

    private float fixedY;
    private float defaultOrthographicSize = 8f;
    private Camera cam;
    private float shakeTimer = 0f;
    private float shakeMagnitude = 0.2f;

    void Awake()
    {
        instance = this;
        cam = GetComponent<Camera>();
        if (cam != null) defaultOrthographicSize = cam.orthographicSize;
    }

    void Start()
    {
        fixedY = transform.position.y;
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }
    }

    void LateUpdate()
    {
        Transform currentFollow = overrideTarget != null ? overrideTarget : target;
        if (currentFollow == null) return;

        // Saat cutscene boss: ikuti posisi Y boss juga biar lebih dramatis
        float targetY = overrideTarget != null
            ? Mathf.Lerp(fixedY, overrideTarget.position.y + 1.0f, 0.65f)
            : fixedY;

        Vector3 newPos = new Vector3(currentFollow.position.x, targetY, transform.position.z);

        Vector3 smoothedPos = Vector3.Lerp(transform.position, newPos,
                                           smoothSpeed * Time.unscaledDeltaTime);

        // Zoom dramatis saat cutscene boss, zoom out ke default setelah selesai
        if (cam != null)
        {
            float targetSize = overrideTarget != null ? cutsceneZoomSize : defaultOrthographicSize;
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize,
                                              smoothSpeed * Time.unscaledDeltaTime);
        }

        // Screen shake
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.unscaledDeltaTime;
            Vector2 randomShake = Random.insideUnitCircle * shakeMagnitude;
            smoothedPos += new Vector3(randomShake.x, randomShake.y, 0f);
        }

        transform.position = smoothedPos;
    }

    public void TriggerScreenShake(float duration = 0.5f, float magnitude = 0.25f)
    {
        shakeTimer = duration;
        shakeMagnitude = magnitude;
    }
}
