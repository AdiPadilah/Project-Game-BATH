using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Player : Entity
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f; // Kecepatan gerak
    [SerializeField] private float jumpForce = 12f; // Kekuatan lompat

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 16f; // Kecepatan dash
    [SerializeField] private float dashDuration = 0.25f; // Durasi dash (detik)
    [SerializeField] private float dashCooldown = 0.5f; // Cooldown antar dash
    [SerializeField] private AudioClip dashSound; // Efek suara dash
    private bool isDashing = false;
    private float lastDashTime = -999f;

    [Header("Attack & Combo")]
    [SerializeField] private float attackCooldown = 0.35f;
    [SerializeField] private float comboWindowTime = 0.6f; // Jendela waktu untuk tekan tombol combo berikutnya
    [SerializeField] private GameObject swordGlowEffect; // Slot jika ada object glow manual
    [SerializeField] private Color swordGlowColor = new Color(1f, 0.85f, 0.2f, 1f); // Warna emas bersinar

    [Header("Dedicated Combat Audio (PlayerCombat/)")]
    [SerializeField] private AudioClip slash1Sound;          // Basic attack step 1
    [SerializeField] private AudioClip slash2Sound;          // Basic attack step 2
    [SerializeField] private AudioClip slash3Sound;          // Basic attack step 3
    [SerializeField] private AudioClip comboFlurrySound;     // Combo finisher swing sound
    [SerializeField] private AudioClip comboFinisherImpactSound; // Combo finisher heavy impact on hit

    private float lastAttackTime = -999f;
    private float lastAttackEndTime = -999f;
    private int currentComboStep = 0; // 0=none, 1=atk1, 2=atk2, 3=atk3, 4=combo finisher
    private bool isAttacking = false;
    private bool comboQueued = false;
    private bool hasHitEnemyInCombo = false; // HARUS kena musuh agar bisa masuk combo finisher!
    private Light dynamicSwordLight;

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
    private bool justJumped; // Flag untuk mencegah suara lompat bunyi 2 kali

    protected override void Awake()
    {
        base.Awake();

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        // Inisialisasi cahaya pedang emas dinamis untuk efek kombo
        if (entityAttackPoint != null)
        {
            GameObject lightObj = new GameObject("SwordGlowLight");
            lightObj.transform.SetParent(entityAttackPoint, false);
            dynamicSwordLight = lightObj.AddComponent<Light>();
            dynamicSwordLight.type = LightType.Point;
            dynamicSwordLight.color = swordGlowColor;
            dynamicSwordLight.range = 4f;
            dynamicSwordLight.intensity = 2.5f;
            dynamicSwordLight.enabled = false;
        }

        // Auto-load dedicated combat audio dari Resources/PlayerCombat/ saat runtime
        // Ini TIDAK memakai asset suara lama agar tidak tabrakan dengan suara monster
        if (slash1Sound == null)
            slash1Sound = Resources.Load<AudioClip>("PlayerCombat/player_slash_1");
        if (slash2Sound == null)
            slash2Sound = Resources.Load<AudioClip>("PlayerCombat/player_slash_2");
        if (slash3Sound == null)
            slash3Sound = Resources.Load<AudioClip>("PlayerCombat/player_slash_3");
        if (comboFlurrySound == null)
            comboFlurrySound = Resources.Load<AudioClip>("PlayerCombat/player_combo_flurry");
        if (comboFinisherImpactSound == null)
            comboFinisherImpactSound = Resources.Load<AudioClip>("PlayerCombat/player_combo_finisher_impact");
    }

    protected override void Update()
    {
        // Berhenti jika sudah mati atau game over
        if (isDead || (UI.instance != null && UI.instance.isGameOver))
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

        // Deteksi Dash: Panah Kanan + D atau Panah Kiri + D
        bool dDown = Input.GetKeyDown(KeyCode.D);
        bool dHeld = Input.GetKey(KeyCode.D);
        bool rightDown = Input.GetKeyDown(KeyCode.RightArrow);
        bool leftDown = Input.GetKeyDown(KeyCode.LeftArrow);
        bool rightHeld = Input.GetKey(KeyCode.RightArrow);
        bool leftHeld = Input.GetKey(KeyCode.LeftArrow);

        bool triggerDash = false;
        float dashDir = 0f;

        if (dDown)
        {
            triggerDash = true;
            if (rightHeld || xInput > 0.1f)
                dashDir = 1f;
            else if (leftHeld || xInput < -0.1f)
                dashDir = -1f;
            else
                dashDir = facingRight ? 1f : -1f;
        }
        else if (dHeld)
        {
            if (rightDown)
            {
                triggerDash = true;
                dashDir = 1f;
            }
            else if (leftDown)
            {
                triggerDash = true;
                dashDir = -1f;
            }
        }

        if (triggerDash && !isDashing && Time.time >= lastDashTime + dashCooldown)
        {
            StartCoroutine(DashRoutine(dashDir));
            return;
        }

        if (isDashing) return;

        // Lompat menggunakan Space
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            PlayJumpSound();
            justJumped = true; // Tandai agar HandleJumpFallSound tidak bunyi lagi
        }

        // Serang menggunakan Panah Atas
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            TryAttack();
        }
    }

    private void HandleJumpFallSound()
    {
        if (isDashing)
        {
            wasGrounded = isGrounded;
            return;
        }

        // Hanya bunyi jika player jatuh dari tepi (bukan dari lompat manual)
        if (wasGrounded && !isGrounded && !justJumped)
        {
            PlayJumpSound();
        }
        justJumped = false; // Reset flag setiap frame
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
        if (isDashing || isDead) return;

        // Jika sedang dalam animasi serangan, antrikan input serangan berikutnya
        if (isAttacking)
        {
            comboQueued = true;
            return;
        }

        // Cek apakah combo chain masih aktif (dalam window waktu setelah serangan sebelumnya)
        if (Time.time <= lastAttackEndTime + comboWindowTime && currentComboStep > 0)
        {
            AdvanceCombo();
        }
        else
        {
            // Reset ke Serangan 1 (Vertikal)
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                currentComboStep = 0;
                hasHitEnemyInCombo = false;
                AdvanceCombo();
            }
        }
    }

    private void AdvanceCombo()
    {
        if (currentComboStep == 0)
        {
            // Step 1: Tebas Vertikal
            currentComboStep = 1;
            hasHitEnemyInCombo = false; // Reset deteksi hit saat mulai rantai baru
            StartCoroutine(ExecuteAttackRoutine(1, "attack1", 0.40f));
        }
        else if (currentComboStep == 1)
        {
            // Step 2: Tebas Kiri
            currentComboStep = 2;
            StartCoroutine(ExecuteAttackRoutine(2, "attack2", 0.40f));
        }
        else if (currentComboStep == 2)
        {
            // Step 3: Tebas Kanan
            currentComboStep = 3;
            StartCoroutine(ExecuteAttackRoutine(3, "attack3", 0.40f));
        }
        else if (currentComboStep == 3)
        {
            // Step 4: Combo Finisher?
            // HANYA BISA KELUAR JIKA SERANGAN SEBELUMNYA KENA MUSUH!
            if (hasHitEnemyInCombo)
            {
                currentComboStep = 4;
                StartCoroutine(ExecuteAttackRoutine(4, "attackCombo", 0.85f));
            }
            else
            {
                // Tidak kena musuh: tidak bisa masuk combo finisher, kembali ke Step 1
                currentComboStep = 1;
                hasHitEnemyInCombo = false;
                StartCoroutine(ExecuteAttackRoutine(1, "attack1", 0.40f));
            }
        }
        else
        {
            // Selesai step 4, kembali ke step 1
            currentComboStep = 1;
            hasHitEnemyInCombo = false;
            StartCoroutine(ExecuteAttackRoutine(1, "attack1", 0.40f));
        }
    }

    private IEnumerator ExecuteAttackRoutine(int step, string triggerName, float duration)
    {
        isAttacking = true;
        comboQueued = false;
        lastAttackTime = Time.time;

        // Berhenti bergerak saat mulai menebas (tidak bisa menyerang sambil lari)
        if (rb != null)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        if (anim != null)
        {
            anim.ResetTrigger("attack1");
            anim.ResetTrigger("attack2");
            anim.ResetTrigger("attack3");
            anim.ResetTrigger("attackCombo");
            anim.SetTrigger(triggerName);
        }

        PlayAttackSoundForStep(step);

        // Jika Step 4 (Combo Finisher): AKTIFKAN EFEK PEDANG MENYALA EMAS!
        if (step == 4)
        {
            SetSwordGlow(true);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Matikan efek pedang menyala setelah combo finisher selesai
        if (step == 4)
        {
            SetSwordGlow(false);
            currentComboStep = 0;
            hasHitEnemyInCombo = false;
        }

        lastAttackEndTime = Time.time;
        isAttacking = false;

        // Jika pemain menekan tombol saat animasi masih berlangsung, langsung jalankan combo berikutnya!
        if (comboQueued && !isDead && !isDashing && currentComboStep > 0)
        {
            comboQueued = false;
            AdvanceCombo();
        }
    }

    private void SetSwordGlow(bool active)
    {
        if (dynamicSwordLight != null)
        {
            dynamicSwordLight.enabled = active;
        }
        if (swordGlowEffect != null)
        {
            swordGlowEffect.SetActive(active);
        }
        if (sr != null)
        {
            sr.color = active ? new Color(1.2f, 1.1f, 0.6f, 1f) : Color.white;
        }
    }

    private void PlayAttackSoundForStep(int step)
    {
        if (audioSource == null) return;

        // ===== DEDICATED COMBAT SOUNDS (tidak pakai asset lama) =====
        if (step == 4)
        {
            // Kombo Finisher: suara ayunan flurry khusus
            if (comboFlurrySound != null)
            {
                float origPitch = audioSource.pitch;
                audioSource.pitch = Random.Range(1.1f, 1.3f);
                audioSource.PlayOneShot(comboFlurrySound, 1f);
                audioSource.pitch = origPitch;
            }
        }
        else
        {
            // Basic Attack per step — tiap step punya warna suara tersendiri
            AudioClip clip = null;
            float pitch = 1f;

            if (step == 1)      { clip = slash1Sound; pitch = Random.Range(0.95f, 1.05f); }
            else if (step == 2) { clip = slash2Sound; pitch = Random.Range(1.05f, 1.15f); }
            else if (step == 3) { clip = slash3Sound; pitch = Random.Range(0.88f, 0.98f); }

            if (clip != null)
            {
                float origPitch = audioSource.pitch;
                audioSource.pitch = pitch;
                audioSource.PlayOneShot(clip, Random.Range(0.85f, 1f));
                audioSource.pitch = origPitch;
            }
            else if (hitSounds != null && hitSounds.Length > 0)
            {
                // Fallback ke hitSounds jika file belum ter-load
                AudioClip fallback = hitSounds[Random.Range(0, hitSounds.Length)];
                float origPitch = audioSource.pitch;
                audioSource.pitch = step == 1 ? 1.0f : (step == 2 ? 1.15f : 0.90f);
                audioSource.PlayOneShot(fallback, Random.Range(0.85f, 0.95f));
                audioSource.pitch = origPitch;
            }
        }
    }

    public override void DamageTargets()
    {
        if (UI.instance != null && UI.instance.isGameOver) return;
        if (entityAttackPoint == null) return;

        Collider2D[] targets = Physics2D.OverlapCircleAll(entityAttackPoint.position, attackRange, enemyLayer);
        foreach (Collider2D target in targets)
        {
            Entity enemy = target.GetComponent<Entity>();
            if (enemy != null && enemy != this)
            {
                // Musuh kena! Kunci terbuka untuk combo finisher!
                hasHitEnemyInCombo = true;

                // Damage seimbang: tidak langsung membuat musuh mati seketika
                int damage = 3;
                if (currentComboStep == 1) damage = 3;
                else if (currentComboStep == 2) damage = 4;
                else if (currentComboStep == 3) damage = 5;
                else if (currentComboStep == 4)
                {
                    damage = 4; // Multi-hit tebasan mutilasi (total ~16 damage)

                    // Efek suara dentuman berat saat eksekusi kombo mengenai musuh!
                    if (audioSource != null && comboFinisherImpactSound != null)
                    {
                        float orig = audioSource.pitch;
                        audioSource.pitch = Random.Range(0.9f, 1.1f);
                        audioSource.PlayOneShot(comboFinisherImpactSound, 1.2f);
                        audioSource.pitch = orig;
                    }
                }

                enemy.TakeDamage(damage);
            }
        }
    }

    protected override void HandleMovement()
    {
        if (!canMove || isDead || isDashing) return;

        // Player tidak boleh bergerak saat sedang menyerang (tidak boleh nyerang sambil lari)
        if (isAttacking)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        rb.velocity = new Vector2(xInput * moveSpeed, rb.velocity.y);
    }

    private IEnumerator DashRoutine(float direction)
    {
        isDashing = true;
        canMove = false;
        isInvincible = true;
        lastDashTime = Time.time;

        // Hadapkan player ke arah dash jika belum menghadap ke sana
        if (direction > 0 && !facingRight)
        {
            Flip();
        }
        else if (direction < 0 && facingRight)
        {
            Flip();
        }

        // Trigger animasi Dash di Animator
        if (anim != null)
        {
            anim.SetTrigger("dash");
        }

        // Putar suara Dash (whoosh)
        PlayDashSound();

        // Nonaktifkan gravitasi sementara agar gerak dash lurus & konsisten
        float originalGravity = rb != null ? rb.gravityScale : 1f;
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.velocity = new Vector2(direction * dashSpeed, 0f);
        }

        yield return new WaitForSeconds(dashDuration);

        // Kembalikan gravitasi dan kurangi momentum secara mulus
        if (rb != null)
        {
            rb.gravityScale = originalGravity;
            rb.velocity = new Vector2(rb.velocity.x * 0.3f, rb.velocity.y);
        }

        isDashing = false;
        canMove = true;
        isInvincible = false;
    }

    private void PlayDashSound()
    {
        AudioClip clip = dashSound != null ? dashSound : jumpSound;
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, 0.9f);
        }
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
        isDashing = false;
        isAttacking = false;
        currentComboStep = 0;
        hasHitEnemyInCombo = false;
        SetSwordGlow(false);
        canMove = false;
        StopAllCoroutines();
        rb.velocity = Vector2.zero;
        rb.simulated = false; // Mencegah player jatuh ke bawah terus

        // Matikan collider agar tidak terkena damage lagi
        if (col != null)
            col.enabled = false;

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

        // CATATAN: Tidak memanggil base.Die() karena:
        // - base.Die() akan SetTrigger("Die") lagi (double trigger)
        // - base.Die() akan lempar player ke atas (rb.velocity = (0,8))
        // - base.Die() akan Destroy(gameObject, 2f) → player hilang sebelum Game Over tampil

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