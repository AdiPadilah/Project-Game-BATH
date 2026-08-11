using UnityEngine;

public class ArenaTrigger : MonoBehaviour
{
    [SerializeField] private Enemy_Respawner respawner;
    [SerializeField] private BossAI boss;

    [Header("Walls")]
    [SerializeField] private GameObject leftWall;
    [SerializeField] private GameObject rightWall;

    private int killCount = 0;
    private bool fightStarted = false;
    private bool arenaCleared = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !fightStarted)
        {
            fightStarted = true;
            arenaCleared = false;
            killCount = 0;

            CloseWalls();

            if (respawner != null)
                respawner.StartFight();

            // 🔥 Aktifkan boss
            if (boss != null)
                boss.ActivateBoss();
        }
    }

    public void AddKill()
    {
        killCount++;

        if (killCount >= 5 && !arenaCleared)
        {
            arenaCleared = true;
            OpenWalls();
            ResetArena();
        }
    }

    void OpenWalls()
    {
        if (leftWall != null)
            leftWall.SetActive(false);

        if (rightWall != null)
            rightWall.SetActive(false);
    }

    public void CloseWalls()
    {
        if (leftWall != null)
            leftWall.SetActive(true);

        if (rightWall != null)
            rightWall.SetActive(true);
    }

    public void ResetArena()
    {
        killCount = 0;
        fightStarted = false;
        arenaCleared = false;
    }
}