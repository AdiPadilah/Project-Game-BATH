using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DemonGuard — Iblis Penjaga di Stage 1 (Chapter 1):
/// - Bertugas menjaga jalur Lembah Reruntuhan Kuno dan mengawasi Sel Tawanan Tumbal.
/// - Memiliki patroli platform, deteksi pemain, tebasan kapak iblis berapi kegelapan.
/// - Varian Warden (Komandan Penjaga): Berukuran lebih besar, HP tebal, dan menjatuhkan Kunci Sel Penjara saat tewas!
/// </summary>
public class DemonGuard : Entity
{
    [Header("Demon Role")]
    public bool isWarden = false;
    public string demonName = "Iblis Penjaga";

    [Header("Patrol & Combat")]
    [SerializeField] private float moveSpeed = 3.2f;
    [SerializeField] private float aggroRange = 7.5f;
    [SerializeField] private float attackRange = 1.6f;
    [SerializeField] private float attackCooldown = 1.3f;
    [SerializeField] private int demonAttackDamage = 14;

    private float lastAttackTime = -999f;
    private Transform playerTransform;
    private bool playerDetected = false;
    private Vector2 patrolStartPos;
    private float patrolDistance = 5.0f;
    private bool movingRight = true;

    // Visual & Audio
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private AudioClip sfxAttack;
    private AudioClip sfxDeath;
    private AudioClip sfxKeyDrop;

    // HP Bar
    private GameObject hpBarRoot;
    private Image hpFill;

    protected override void Awake()
    {
        base.Awake();

        if (isWarden)
        {
            maxHealth = 180;
            attackDamage = 22;
            demonAttackDamage = 22;
            moveSpeed = 3.6f;
            demonName = "Komandan Iblis Warden";
        }
        else
        {
            maxHealth = 80;
            attackDamage = 14;
            demonAttackDamage = 14;
            demonName = "Iblis Penjaga";
        }

        currentHealth = maxHealth;
        patrolStartPos = transform.position;

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        LoadAssets();
        BuildWorldHPBar();
    }

    private void LoadAssets()
    {
        sfxAttack = Resources.Load<AudioClip>("Audio/ui_click");
        sfxDeath  = Resources.Load<AudioClip>("Audio/star_1");
        sfxKeyDrop= Resources.Load<AudioClip>("Audio/star_all");

        if (spriteRenderer.sprite == null)
        {
            string spriteName = isWarden ? "Stage1/demon_warden_pixel" : "Stage1/demon_guard_pixel";
            spriteRenderer.sprite = Resources.Load<Sprite>(spriteName);
        }
    }

    private void Start()
    {
        FindPlayer();
        RegisterToManager();
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            playerTransform = p.transform;
        }
    }

    private void RegisterToManager()
    {
        Chapter1Manager mgr = FindFirstObjectByType<Chapter1Manager>();
        if (mgr != null)
        {
            mgr.RegisterDemon(this);
        }
    }

    private void BuildWorldHPBar()
    {
        hpBarRoot = new GameObject("HPBar_World");
        hpBarRoot.transform.SetParent(transform, false);
        hpBarRoot.transform.localPosition = new Vector3(0f, isWarden ? 1.6f : 1.3f, 0f);

        Canvas c = hpBarRoot.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        c.sortingOrder = 50;

        RectTransform crt = hpBarRoot.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(1.2f, 0.18f);

        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(hpBarRoot.transform, false);
        RectTransform bgrt = bg.AddComponent<RectTransform>();
        bgrt.sizeDelta = new Vector2(1.2f, 0.18f);
        Image bgi = bg.AddComponent<Image>();
        bgi.color = new Color(0.1f, 0.05f, 0.15f, 0.85f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(hpBarRoot.transform, false);
        RectTransform fillrt = fill.AddComponent<RectTransform>();
        fillrt.sizeDelta = new Vector2(1.16f, 0.14f);
        hpFill = fill.AddComponent<Image>();
        hpFill.color = isWarden ? new Color(0.9f, 0.2f, 0.2f, 1f) : new Color(0.85f, 0.5f, 0.15f, 1f);
    }

    private void Update()
    {
        if (currentHealth <= 0) return;

        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distToPlayer <= aggroRange)
        {
            playerDetected = true;
        }

        if (playerDetected)
        {
            ChaseAndAttack(distToPlayer);
        }
        else
        {
            PatrolRoutine();
        }

        UpdateVisualFacing();
    }

    private void PatrolRoutine()
    {
        if (!canMove) return;

        float targetX = movingRight ? (patrolStartPos.x + patrolDistance) : (patrolStartPos.x - patrolDistance);
        float step = moveSpeed * 0.6f * Time.deltaTime;

        transform.position = Vector2.MoveTowards(transform.position, new Vector2(targetX, transform.position.y), step);

        if (Mathf.Abs(transform.position.x - targetX) < 0.2f)
        {
            movingRight = !movingRight;
        }
    }

    private void ChaseAndAttack(float dist)
    {
        if (!canMove) return;

        float dir = Mathf.Sign(playerTransform.position.x - transform.position.x);

        if (dist > attackRange)
        {
            // Bergerak mendekat
            transform.position += new Vector3(dir * moveSpeed * Time.deltaTime, 0f, 0f);
        }
        else
        {
            // Dalam jarak serang
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                PerformSlashAttack();
            }
        }
    }

    private void PerformSlashAttack()
    {
        lastAttackTime = Time.time;
        if (sfxAttack != null && audioSource != null)
        {
            audioSource.PlayOneShot(sfxAttack, 0.8f);
        }

        // Efek Flash Serangan Merah Iblis
        StartCoroutine(AttackFlashRoutine());

        // Serang Player jika dalam jangkauan
        if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= attackRange + 0.6f)
        {
            Entity pEntity = playerTransform.GetComponent<Entity>();
            if (pEntity != null)
            {
                pEntity.TakeDamage(demonAttackDamage);
            }
        }
    }

    private IEnumerator AttackFlashRoutine()
    {
        if (spriteRenderer != null)
        {
            Color orig = spriteRenderer.color;
            spriteRenderer.color = new Color(1f, 0.3f, 0.3f, 1f);
            yield return new WaitForSeconds(0.12f);
            spriteRenderer.color = orig;
        }
    }

    private void UpdateVisualFacing()
    {
        if (spriteRenderer == null) return;

        float vx = 0f;
        if (playerDetected && playerTransform != null)
        {
            vx = playerTransform.position.x - transform.position.x;
        }
        else
        {
            vx = movingRight ? 1f : -1f;
        }

        if (vx > 0.05f)
        {
            spriteRenderer.flipX = false;
        }
        else if (vx < -0.05f)
        {
            spriteRenderer.flipX = true;
        }
    }

    public override void TakeDamage(int damage = 1, bool applyStun = false)
    {
        base.TakeDamage(damage, applyStun);

        playerDetected = true; // Aggro otomatis saat dipukul

        if (hpFill != null)
        {
            hpFill.fillAmount = Mathf.Clamp01((float)currentHealth / maxHealth);
        }

        StartCoroutine(HitFlash());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator HitFlash()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            spriteRenderer.color = isWarden ? new Color(1f, 0.85f, 0.85f, 1f) : Color.white;
        }
    }

    private void Die()
    {
        canMove = false;
        if (col != null) col.enabled = false;
        if (hpBarRoot != null) hpBarRoot.SetActive(false);

        if (sfxDeath != null && audioSource != null)
        {
            audioSource.PlayOneShot(sfxDeath, 1.0f);
        }

        // Kabari Manager
        Chapter1Manager mgr = FindFirstObjectByType<Chapter1Manager>();
        if (mgr != null)
        {
            mgr.OnDemonKilled(this, isWarden);
        }

        StartCoroutine(DeathFadeRoutine());
    }

    private IEnumerator DeathFadeRoutine()
    {
        float elapsed = 0f;
        float dur = 0.5f;
        Vector3 startScale = transform.localScale;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            transform.localScale = Vector3.Lerp(startScale, new Vector3(startScale.x, 0.1f, startScale.z), t);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(0.8f, 0.2f, 0.2f, 1f - t);
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}
