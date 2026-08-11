using UnityEngine;

public class CloseArenaTrigger : MonoBehaviour
{
    [SerializeField] private Enemy_Respawner nextRespawner;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (nextRespawner != null)
                nextRespawner.StartFight();

            gameObject.SetActive(false);
        }
    }
}