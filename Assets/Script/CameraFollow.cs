using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow instance;

    public Transform target;
    public Transform overrideTarget;      // Target override saat cutscene (Boss)
    public float smoothSpeed = 5f;
    public float cutsceneZoomSize = 8.0f; // Framing sinematik seimbang (tidak terlalu zoom, default camera 8.0)

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

        // Pertahankan kamera tetap rata dengan lantai agar pemandangan tidak melonjak ke atas
        float targetY = fixedY;

        Vector3 newPos = new Vector3(currentFollow.position.x, targetY, transform.position.z);

        Vector3 smoothedPos = Vector3.Lerp(transform.position, newPos,
                                           smoothSpeed * Time.deltaTime);

        // Zoom lembut saat cutscene boss (7.0f), dan kembali halus ke default (8.0f)
        if (cam != null)
        {
            float targetSize = overrideTarget != null ? cutsceneZoomSize : defaultOrthographicSize;
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize,
                                              smoothSpeed * Time.deltaTime);
        }

        // Screen shake
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
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
