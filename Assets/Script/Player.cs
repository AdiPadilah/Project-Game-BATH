using UnityEngine;
using UnityEngine.UI;

public class Player : Entity
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f; // Kecepatan gerak
    [SerializeField] private float jumpForce = 12f; // Kekuatan lompat

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 0.4f;
    private float lastAttackTime;

    [SerializeField] private float attackRange = 2f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip deathSound; // Slot untuk suara mati player

    [Header("UI")]
    [SerializeField] private Slider healthSlider;

    public float waktuMain = 0f;
    public bool sudahMenang = false;

    private float xInput;
    private bool isDead = false;
    private bool wasGrounded;

    protected override void Awake()
    {
        base.Awake();

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    protected override void Update()
    {
        // Berhenti jika sudah mati atau game over
        if (isDead || UI.instance == null || UI.instance.isGameOver)
            return;

        if (!sudahMenang)
        {
            waktuMain += Time.deltaTime;
        }

        HandleInput();
        HandleJumpFallSound();
        base.Update();
    }

    private void HandleInput()
    {
        xInput = Input.GetAxisRaw("Horizontal");

        // Lompat menggunakan Space
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            PlayJumpSound();
        }

        // Serang menggunakan Panah Atas
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            TryAttack();
        }
    }

    private void HandleJumpFallSound()
    {
        if (wasGrounded && !isGrounded)
        {
            PlayJumpSound();
        }
        wasGrounded = isGrounded;
    }

    private void PlayJumpSound()
    {
        if (audioSource != null && jumpSound != null)
        {
            float volume = Random.Range(0.8f, 1f);
            audioSource.PlayOneShot(jumpSound, volume);
        }
    }

    private void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;

        if (anim != null)
            anim.SetTrigger("attack");

        PlayAttackSound();
    }

    private void PlayAttackSound()
    {
        if (audioSource != null && hitSounds.Length > 0)
        {
            AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];
            float volume = Random.Range(0.8f, 1f);
            audioSource.PlayOneShot(clip, volume);
        }
    }

    public void DealDamage()
    {
        if (UI.instance != null && UI.instance.isGameOver)
            return;

        if (entityAttackPoint == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            entityAttackPoint.position,
            attackRange,
            enemyLayer
        );

        foreach (Collider2D hit in hits)
        {
            Entity enemy = hit.GetComponent<Entity>();
            if (enemy != null)
            {
                enemy.TakeDamage(10);
            }
        }
    }

    protected override void HandleMovement()
    {
        if (!canMove || isDead) return;
        rb.velocity = new Vector2(xInput * moveSpeed, rb.velocity.y);
    }

    protected override void OnHealthChanged()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }

    // --- LOGIKA MATI TERBARU ---
    protected override void Die()
    {
        if (isDead) return;

        isDead = true;
        rb.velocity = Vector2.zero;
        rb.simulated = false; // Mencegah player jatuh ke bawah terus

        // Putar suara mati dari slot yang kamu isi (Dark Souls Sound)
        if (deathSound != null)
        {
            // PlayClipAtPoint lebih aman karena tetap bunyi meskipun AudioSource mati
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }

        // Jalankan animasi mati di Animator
        if (anim != null)
        {
            anim.SetTrigger("Die");
        }

        base.Die();

        // Munculkan panel Game Over
        if (UI.instance != null)
        {
            UI.instance.ShowGameOver();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (entityAttackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(entityAttackPoint.position, attackRange);
        }
    }
}