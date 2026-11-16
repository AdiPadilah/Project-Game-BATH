using System.Collections;
using System.Collections.Generic;
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

    [Header("Combo Audio")]
    // Tidak ada field tambahan — kombo pakai hitSounds yang sama dengan basic attack

    private float lastAttackTime = -999f;
    private float lastAttackEndTime = -999f;
    private int currentComboStep = 0; // 0=none, 1=atk1, 2=atk2, 3=atk3, 4=combo finisher
    private bool isAttacking = false;
    private bool comboQueued = false;
    private bool hasHitEnemyInCombo = false; // HARUS kena musuh agar bisa masuk combo finisher!
    private HashSet<Entity> hitEntitiesInCurrentAttack = new HashSet<Entity>();
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

    [Header("Mana / Skill Energy")]
    [SerializeField] private int maxMana = 100;
    [SerializeField] private int currentMana = 100;

    public int GetCurrentMana() => currentMana;
    public int GetMaxMana() => maxMana;
    public void UseMana(int amount) { currentMana = Mathf.Clamp(currentMana - amount, 0, maxMana); }
    public void RestoreMana(int amount) { currentMana = Mathf.Clamp(currentMana + amount, 0, maxMana); }
    public LayerMask GetEnemyLayer() => enemyLayer;

    public float waktuMain = 0f;
    public bool sudahMenang = false;

    private float xInput;
    private bool isDead = false;
    private bool wasGrounded;
    private bool justJumped; // Flag untuk mencegah suara lompat bunyi 2 kali
    private float manaRegenTimer = 0f;

    protected override void Awake()
    {
        base.Awake();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (entityAttackPoint == null)
        {
            Transform ap = transform.Find("AttackPoint");
            if (ap == null)
            {
                GameObject apGO = new GameObject("AttackPoint");
                apGO.transform.SetParent(transform, false);
                apGO.transform.localPosition = new Vector3(1.4f, -0.3f, 0f);
                ap = apGO.transform;
            }
            entityAttackPoint = ap;
        }

        if (enemyLayer.value == 0) enemyLayer = LayerMask.GetMask("Enemy");
        if (whatIsGround.value == 0) whatIsGround = LayerMask.GetMask("Ground");
        if (whatIsTarget.value == 0) whatIsTarget = LayerMask.GetMask("Enemy");

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

        // Tidak ada auto-load tambahan — hitSounds diisi dari Inspector
    }

    protected virtual void Start()
    {
        if (GetComponent<PlayerSkills>() == null)
        {
            gameObject.AddComponent<PlayerSkills>();
        }
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

        // Regenerasi mana pasif (3 MP per detik)
        manaRegenTimer += Time.deltaTime;
        if (manaRegenTimer >= 1f)
        {
            manaRegenTimer = 0f;
            RestoreMana(3);
        }

        HandleInput();
        HandleJumpFallSound();
        base.Update();
    }

    private void HandleInput()
    {
        // Gerakan horizontal: Arrow Keys (Panah Kiri / Kanan) diprioritaskan
        // agar tombol A tidak menggerakkan player saat menekan Skill 1
        float rawH = 0f;
        if (Input.GetKey(KeyCode.RightArrow)) rawH += 1f;
        if (Input.GetKey(KeyCode.LeftArrow)) rawH -= 1f;
        xInput = rawH;

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

    public void TryAttack()
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
        hitEntitiesInCurrentAttack.Clear();
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
        if (audioSource == null || hitSounds == null || hitSounds.Length == 0) return;

        AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];
        float origPitch = audioSource.pitch;

        if (step == 4)
        {
            // Kombo Finisher: pakai hitSounds yang sama tapi pitch lebih tinggi & cepat
            // terdengar lebih intens/urgent dibanding basic attack
            audioSource.pitch = Random.Range(1.3f, 1.5f);
            audioSource.PlayOneShot(clip, 1f);

            // Langsung bunyikan lagi dengan pitch beda — efek flurry dobel
            AudioClip clip2 = hitSounds[Random.Range(0, hitSounds.Length)];
            audioSource.pitch = Random.Range(1.4f, 1.6f);
            audioSource.PlayOneShot(clip2, 0.75f);
        }
        else
        {
            // Basic attack: pitch naik tiap step biar terasa progresif
            // Step 1 = pitch normal, Step 2 = sedikit naik, Step 3 = sedikit turun (berat)
            float pitch = step == 1 ? 1.0f : (step == 2 ? 1.15f : 0.90f);
            audioSource.pitch = pitch * Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(clip, Random.Range(0.85f, 0.95f));
        }

        audioSource.pitch = origPitch;
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
                // Cegah multi-hit ganda pada musuh yang sama dalam satu ayunan serangan
                if (hitEntitiesInCurrentAttack.Contains(enemy))
                    continue;
                hitEntitiesInCurrentAttack.Add(enemy);

                // Musuh kena! Kunci terbuka untuk combo finisher!
                hasHitEnemyInCombo = true;

                // Tambah Mana saat tebasan berhasil mengenai musuh (+8 MP)
                RestoreMana(8);

                // Player: 20 damage per hit (step 1-3), combo finisher 30 damage (step 4)
                int damage = 20;
                if (currentComboStep == 1) damage = 20;
                else if (currentComboStep == 2) damage = 20;
                else if (currentComboStep == 3) damage = 20;
                else if (currentComboStep == 4)
                {
                    damage = 30;
                    // Suara combo finisher saat kena — pakai hitSounds dengan pitch rendah (berat)
                    if (audioSource != null && hitSounds != null && hitSounds.Length > 0)
                    {
                        float orig = audioSource.pitch;
                        audioSource.pitch = Random.Range(0.75f, 0.88f); // rendah = berat/impact
                        audioSource.PlayOneShot(hitSounds[Random.Range(0, hitSounds.Length)], 1.1f);
                        audioSource.pitch = orig;
                    }
                }

                // Efek stun/knockback HANYA diberikan jika serangan berupa combo finisher (step 4)
                bool applyStun = (currentComboStep == 4);
                enemy.TakeDamage(damage, applyStun);
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

    public void PerformJump()
    {
        if (rb != null)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            PlayJumpSound();
            justJumped = true;
        }
    }

    public void PerformDash(float dir = 0f)
    {
        if (dir == 0f) dir = facingRight ? 1f : -1f;
        if (!isDashing)
        {
            StartCoroutine(DashRoutine(dir));
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