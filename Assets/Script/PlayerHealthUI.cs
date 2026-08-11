using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealthUI : MonoBehaviour
{
    public Slider slider;
    public Image fillImage;

    [Header("Game Over Settings")]
    public GameObject gameOverPanel;

    public int maxHealth = 100;
    public int currentHealth;

    private bool isDead = false;

    void Awake()
    {
        // Inisialisasi darah di awal
        currentHealth = maxHealth;
    }

    void Start()
    {
        if (slider != null)
        {
            slider.maxValue = maxHealth;
            slider.value = currentHealth;
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
            
        UpdateColor();
    }

    void Update()
    {
        // Tetap memanggil UpdateColor setiap frame sesuai struktur aslimu
        UpdateColor();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        
        // Cek jika ada script UI global (Singleton) yang sedang game over
        if (UI.instance != null && UI.instance.isGameOver) return;

        // PERBAIKAN LOGIKA: Menggunakan Mathf.Abs agar jika damage yang masuk minus, 
        // tetap dihitung sebagai pengurangan (positif).
        currentHealth -= Mathf.Abs(damage);

        // Memastikan darah tidak kurang dari 0
        if (currentHealth < 0)
            currentHealth = 0;

        if (slider != null)
            slider.value = currentHealth;

        UpdateColor();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateColor()
    {
        if (slider == null || fillImage == null) return;
        if (maxHealth <= 0) return;

        float percent = (float)currentHealth / maxHealth;

        // Logika warna sesuai aslinya
        if (percent >= 0.5f)
            fillImage.color = Color.green;
        else if (percent <= 0.2f)
            fillImage.color = Color.red;
        else
            fillImage.color = Color.yellow;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("💀 PLAYER TEWAS!");
        currentHealth = 0;

        if (slider != null)
            slider.value = currentHealth;

        UpdateColor();

        // Cek sistem Game Over
        if (UI.instance != null)
        {
            UI.instance.ShowGameOver();
        }
        else if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Time.timeScale = 0f;
        }
        else
        {
            Debug.LogError("❌ UI.instance dan Game Over Panel tidak ditemukan!");
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}