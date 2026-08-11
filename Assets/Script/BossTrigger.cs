using UnityEngine;

public class BossTrigger : MonoBehaviour
{
    [SerializeField] private BossAI boss;

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            if (boss != null)
            {
                boss.ActivateBoss();
            }
            else
            {
                Debug.LogWarning("Boss belum di assign di Inspector!");
            }
        }
    }
}