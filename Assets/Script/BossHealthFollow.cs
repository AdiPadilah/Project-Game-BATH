using UnityEngine;

public class BossHealthFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 3f, 0);

    void LateUpdate()
    {
        if (target == null) return;

        // Ambil posisi target (boss)
        Vector3 targetPos = target.position + offset;

        // HANYA pindahin UI, bukan boss
        transform.position = targetPos;
    }
}