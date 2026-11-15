using UnityEngine;
using UnityEngine.UI;

public class Enemy : Entity
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip deathSound; // SUDAH DIUBAH: Slot untuk suara mati
    [SerializeField] private AudioClip attackSound;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 1f;
    private float lastAttackTime;

    [Header("UI")]
    [SerializeField] private Slider healthSlider;

    private bool playerDetected;
    private Transform player;
    private Enemy_Respawner respawner;

    protected override void Awake()
    {
        base.Awake();

        // Singkirkan script Health sisa jika ada agar tidak bentrok dengan Enemy
        Health leftoverHealth = GetComponent<Health>();
        if (leftoverHealth != null)
            Destroy(leftoverHealth);

        invincibleTime = 0.1f;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;

            // Hilangkan offset bawaan Unity Slider agar fill bisa benar-benar mencapai 0 tanpa sisa setetes
            if (healthSlider.fillRect != null)
            {
                healthSlider.fillRect.sizeDelta = Vector2.zero;
                healthSlider.fillRect.anchoredPosition = Vector2.zero;
            }
            Transform fillArea = healthSlider.transform.Find("Fill Area");
            if (fillArea != null)
            {
                RectTransform faRT = fillArea.GetComponent<RectTransform>();
                if (faRT != null)
                {
                    faRT.sizeDelta = Vector2.zero;
                    faRT.anchoredPosition = Vector2.zero;
                }
            }
        }

        Player foundPlayer = FindFirstObjectByType<Player>();
        if (foundPlayer != null)
            player = foundPlayer.transform;
    }

    protected virtual void Start()
    {
        // Pastikan Slider selalu sinkron dengan maxHealth (50) dan darah penuh saat baru spawn
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    protected override void Update()
    {
        if (UI.instance != null && UI.instance.isGameOver)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            if (anim != null)
                anim.SetFloat("Velocity", 0);
            return;
        }

        if (player == null)
        {
            Player foundPlayer = FindFirstObjectByType<Player>();
            if (foundPlayer != null)
                player = foundPlayer.transform;
        }

        base.Update();
    }

    protected override void HandleAnimations()
    {
        if (anim == null) return;
        anim.SetFloat("xVelocity", Mathf.Abs(rb != null ? rb.velocity.x : 0));
    }

    protected override void OnHealthChanged()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = Mathf.Max(0, currentHealth);
            if (currentHealth <= 0)
            {
                healthSlider.value = 0;
                if (healthSlider.fillRect != null)
                    healthSlider.fillRect.gameObject.SetActive(false);
                healthSlider.gameObject.SetActive(false);
            }
            else
            {
                if (healthSlider.fillRect != null)
                    healthSlider.fillRect.gameObject.SetActive(true);
                healthSlider.gameObject.SetActive(true);
            }
        }
    }

    protected override void HandleMovement()
    {
        if (!canMove || player == null)
            return;

        float distance = player.position.x - transform.position.x;

        if (Mathf.Abs(distance) > 0.2f)
        {
            float direction = Mathf.Sign(distance);
            rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);

            if (direction > 0 && !facingRight)
                Flip();
            else if (direction < 0 && facingRight)
                Flip();
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }

    protected override void HandleAttack()
    {
        if (UI.instance != null && UI.instance.isGameOver)
            return;

        if (!playerDetected || anim == null)
            return;

        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;
        rb.velocity = Vector2.zero;

        anim.SetTrigger("attack");

        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
            Debug.Log("Skeleton Menyerang: Suara diputar!");
        }
    }

    protected override void HandleCollision()
    {
        base.HandleCollision();

        if (entityAttackPoint != null)
        {
            Collider2D hit = Physics2D.OverlapCircle(
                entityAttackPoint.position,
                attackRadius,
                whatIsTarget
            );
            playerDetected = hit != null;
        }
    }

    protected override void HandleFlip() { }

    public void SetRespawner(Enemy_Respawner r)
    {
        respawner = r;
    }

    protected override void Die()
    {
        canMove = false;

        // Mainkan suara mati
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        if (UI.instance != null)
            UI.instance.AddkillCount();

        if (respawner != null)
            respawner.EnemyDied();

        if (healthSlider != null)
        {
            healthSlider.value = 0;
            if (healthSlider.fillRect != null)
                healthSlider.fillRect.gameObject.SetActive(false);
            healthSlider.gameObject.SetActive(false);
        }

        if (anim != null)
            anim.SetTrigger("Die");

        if (col != null)
            col.enabled = false;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.gravityScale = 0f;
        }

        Destroy(gameObject, 1.2f);
    }
}