using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;

    private float fixedY;

    void Start()
    {
        fixedY = transform.position.y; // simpan posisi Y awal
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