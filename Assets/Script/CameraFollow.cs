using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;

    private float fixedY;

    void Start()
    {
        fixedY = transform.position.y; // simpan posisi Y awal
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 newPosition = new Vector3(
            target.position.x,
            fixedY, // Y tidak ikut turun
            transform.position.z
        );

        transform.position = Vector3.Lerp(
            transform.position,
            newPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}