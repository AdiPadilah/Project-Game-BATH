using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    public static UI instance;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverUI;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI killCountText;

    [Header("Health UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image healthFill;
    [SerializeField] private Player player;

    public bool isGameOver = false;

    private int killCount;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        Time.timeScale = 1f;

        if (gameOverUI != null)
            gameOverUI.SetActive(false);
    }

    private void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<Player>();
    }

    private void Update()
    {
        if (isGameOver) return;

        UpdateTimer();
        UpdateHealthUI();
    }

    void UpdateTimer()
    {
        if (timerText == null || player == null) return;
        timerText.text = player.waktuMain.ToString("F2") + "s";
    }

    void UpdateHealthUI()
    {
        if (player == null || healthSlider == null) return;

        int current = player.GetCurrentHealth();
        int max = player.GetMaxHealth();

        healthSlider.maxValue = max;
        healthSlider.value = current;

        float percent = (float)current / max;

        if (healthFill != null)
        {
            if (percent <= 0.25f)
                healthFill.color = Color.red;
            else if (percent <= 0.5f)
                healthFill.color = Color.yellow;
            else
                healthFill.color = Color.green;
        }
    }

    public void ShowGameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        Time.timeScale = 0f;

        if (gameOverUI != null)
            gameOverUI.SetActive(true);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void AddkillCount()
    {
        killCount++;

        if (killCountText != null)
            killCountText.text = killCount.ToString();
    }
}