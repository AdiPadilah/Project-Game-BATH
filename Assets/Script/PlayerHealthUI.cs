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
        // Pastikan Canvas Scaler ScaleWithScreenSize agar HP bar tidak terlempar keluar layar pada resolusi apapun
        if (slider != null)
        {
            CanvasScaler cs = slider.GetComponentInParent<CanvasScaler>();
            if (cs != null && cs.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cs.referenceResolution = new Vector2(1920, 1080);
                cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                cs.matchWidthOrHeight = 0.5f;
            }

            // Posisikan Bar HP tepat di bawah box Time dengan ukuran proporsional
            RectTransform rt = slider.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.localScale = Vector3.one;
                rt.sizeDelta = new Vector2(280f, 18f);
                rt.anchoredPosition = new Vector2(40f, -90f);
            }

            // Atur Background dan Fill Area agar mengisi penuh tinggi slider sehingga bar tebal dan jelas
            Transform bg = slider.transform.Find("Background");
            if (bg != null)
            {
                RectTransform bgRT = bg.GetComponent<RectTransform>();
                if (bgRT != null)
                {
                    bgRT.anchorMin = Vector2.zero;
                    bgRT.anchorMax = Vector2.one;
                    bgRT.sizeDelta = Vector2.zero;
                }
            }

            Transform fillArea = slider.transform.Find("Fill Area");
            if (fillArea != null)
            {
                RectTransform faRT = fillArea.GetComponent<RectTransform>();
                if (faRT != null)
                {
                    faRT.anchorMin = Vector2.zero;
                    faRT.anchorMax = Vector2.one;
                    faRT.sizeDelta = Vector2.zero;
                }
            }

            slider.maxValue = maxHealth;
            slider.value = currentHealth;
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