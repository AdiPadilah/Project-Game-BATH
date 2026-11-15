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
    private Player player;

    void Awake()
    {
        player = GetComponent<Player>();
        currentHealth = maxHealth;
    }

    void Start()
    {
        // Sembunyikan slider lama karena digantikan oleh PlayerHUD bergaya Dark Fantasy
        if (slider != null)
        {
            slider.gameObject.SetActive(false);
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
            
        UpdateColor();
    }

    void Update()
    {
        if (player != null)
        {
            currentHealth = player.GetCurrentHealth();
            maxHealth = player.GetMaxHealth();
            if (slider != null)
            {
                slider.maxValue = maxHealth;
                slider.value = currentHealth;
            }
        }

        UpdateColor();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        
        // Cek jika ada script UI global (Singleton) yang sedang game over
        if (UI.instance != null && UI.instance.isGameOver) return;

        if (player != null)
        {
            player.TakeDamage(Mathf.Abs(damage));
            currentHealth = player.GetCurrentHealth();
            if (slider != null)
                slider.value = currentHealth;
            UpdateColor();
            return;
        }

        // Menggunakan Mathf.Abs agar damage minus tetap dihitung benar
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
            Debug.LogError("UI.instance dan Game Over Panel tidak ditemukan!");
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}