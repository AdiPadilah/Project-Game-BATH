using UnityEngine;

public class Enemy_Respawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int totalEnemiesToSpawn = 10;
    [SerializeField] private int spawnPerWave = 2;

    [Header("Arena Walls")]
    [SerializeField] private GameObject leftWall;
    [SerializeField] private GameObject rightWall;

    [Header("Boss Access")]
    [SerializeField] private GameObject bossGate;      // barrier ke boss
    [SerializeField] private GameObject bossObject;    // boss (optional)

    private int spawnedCount = 0;
    private int aliveCount = 0;
    private bool fightStarted = false;

    void Update()
    {
        if (!fightStarted) return;

        // ✅ Arena Clear Condition
        if (spawnedCount >= totalEnemiesToSpawn && aliveCount <= 0)
        {
            fightStarted = false;

            if (leftWall) leftWall.SetActive(false);
            if (rightWall) rightWall.SetActive(false);

            if (bossGate) bossGate.SetActive(false); // buka jalan boss
            if (bossObject) bossObject.SetActive(true); // aktifkan boss

            Debug.Log("ARENA CLEAR - BOSS UNLOCKED");
            return;
        }

        // ✅ Spawn wave berikutnya
        if (aliveCount <= 0 && spawnedCount < totalEnemiesToSpawn)
        {
            SpawnWave();
        }
    }

    void SpawnWave()
    {
        if (enemyPrefab == null || spawnPoints == null || spawnPoints.Length == 0) return;

        int spawnAmount = Mathf.Min(spawnPerWave, totalEnemiesToSpawn - spawnedCount);

        for (int i = 0; i < spawnAmount; i++)
        {
            Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (randomPoint == null) continue;
            GameObject enemy = Instantiate(enemyPrefab, randomPoint.position, Quaternion.identity);

            spawnedCount++;
            aliveCount++;

            enemy.GetComponent<Enemy>()?.SetRespawner(this);
        }
    }

    public void EnemyDied()
    {
        aliveCount--;
        if (aliveCount < 0) aliveCount = 0;
    }

    public void StartFight()
    {
        Debug.Log(gameObject.name + " STARTED");
        fightStarted = true;

        if (leftWall) leftWall.SetActive(true);
        if (rightWall) rightWall.SetActive(true);

        spawnedCount = 0;
        aliveCount = 0;

        SpawnWave();
    }
}