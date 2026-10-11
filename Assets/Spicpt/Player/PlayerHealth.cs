using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private int startingLives = 3;

    [Header("Invulnerable (ช่วงอมตะหลังเกิดใหม่)")]
    [SerializeField] private float invulnerableDuration = 2f;
    [SerializeField] private float blinkInterval = 0.1f;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 1f;
    [SerializeField] private float respawnHeightOffset = 5f;
    [SerializeField] private float respawnEdgeMargin = 1.5f;

    /// <summary>
    /// ความเร็วตอนโผล่มาเกิดใหม่ ให้ลอยเฉียงเข้าจอทางขวานิดหน่อย
    /// แบบต้นฉบับที่ตัวละครร่วงลงมาพร้อมเคลื่อนเข้าสนาม ไม่ใช่ตกดิ่งนิ่งๆ
    /// </summary>
    [SerializeField] private Vector2 respawnDropVelocity =
        new Vector2(2.5f, 1.5f);

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private CameraFollow cameraFollow;

    [Header("Fall Death (ตกเหวแล้วตาย)")]
    [SerializeField] private bool dieWhenFalling = true;

    /// <summary>ก้นเหว ต่ำกว่าเส้นนี้เมื่อไหร่ = ตาย เป็นเส้นตายตัวในโลก</summary>
    [SerializeField] private float fallDeathY = -8f;

    /// <summary>
    /// วัดจากขอบล่างจอแทนเส้นตายตัว
    /// เหมาะกับเกมที่กล้องเลื่อนขึ้นลงตามผู้เล่นเท่านั้น
    /// กล้องของเกมนี้ล็อกแนวตั้งอยู่ ใช้เส้นตายตัวจะคาดเดาง่ายกว่า
    /// </summary>
    [SerializeField] private bool useCameraBottom;

    [SerializeField] private float fallDeathMargin = 2f;

    [Header("Weapon")]
    [SerializeField] private bool resetWeaponOnDeath = true;

    private Rigidbody2D rb;
    private PlayerController playerController;
    private PlayerCombat playerCombat;
    private WeaponSystem weaponSystem;

    private int currentHealth;
    private int currentLives;
    private bool isDead;
    private bool isInvulnerable;
    private Vector2 spawnPosition;
    private Camera mainCamera;

    public bool IsDead => isDead;
    public int CurrentLives => currentLives;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        weaponSystem = GetComponent<WeaponSystem>();
        playerCombat = GetComponent<PlayerCombat>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        currentHealth = maxHealth;
        currentLives = startingLives;
        spawnPosition = transform.position;

        mainCamera = Camera.main;
    }

    private void Update()
    {
        CheckFallDeath();
    }

    /// <summary>ตกต่ำกว่าขอบล่างจอ = ตายทันที แบบ Contra ต้นฉบับ</summary>
    private void CheckFallDeath()
    {
        if (!dieWhenFalling || isDead)
            return;

        if (transform.position.y > GetFallDeathLine())
            return;

        // ตกจอแล้วตายเลย ไม่สนช่วงอมตะหลังเกิดใหม่
        currentHealth = 0;

        Die();
    }

    private float GetFallDeathLine()
    {
        if (!useCameraBottom)
            return fallDeathY;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null || !mainCamera.orthographic)
            return fallDeathY;

        return mainCamera.transform.position.y -
            mainCamera.orthographicSize -
            fallDeathMargin;
    }

    private void OnDrawGizmosSelected()
    {
        // ขีดเส้นก้นเหวให้เห็นว่าตกถึงไหนถึงตาย
        float y = Application.isPlaying
            ? GetFallDeathLine()
            : fallDeathY;

        Gizmos.DrawLine(
            new Vector3(transform.position.x - 20f, y, 0f),
            new Vector3(transform.position.x + 20f, y, 0f)
        );
    }

    public void TakeDamage(int damage)
    {
        // อมตะอยู่ (เพิ่งเกิดใหม่) หรือตายไปแล้ว ไม่ต้องรับดาเมจซ้ำ
        if (isDead || isInvulnerable)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        currentLives--;

        // ตายแล้วเสียอาวุธที่เก็บมา กลับไปใช้ปืนพื้นฐาน ตามต้นฉบับ Contra
        if (resetWeaponOnDeath && weaponSystem != null)
        {
            weaponSystem.SetWeapon(WeaponType.Normal);
            weaponSystem.DisableRapidFire();
        }

        if (currentLives <= 0)
        {
            Debug.Log("Game Over");
            SetPlayerActive(false);
            return;
        }

        Debug.Log("Player ตาย เหลืออีก " + currentLives + " ชีวิต");

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        SetPlayerActive(false);

        yield return new WaitForSeconds(respawnDelay);

        transform.position = GetRespawnPosition();

        currentHealth = maxHealth;
        isDead = false;

        SetPlayerActive(true);

        // ต้องสั่งหลังเปิดสคริปต์กลับมา ไม่งั้น HandleMovement เฟรมถัดไปจะล้างทิ้ง
        // ใส่ความเร็วแกน X ไว้ด้วย ตัวจะได้ลอยเฉียงเข้าจอ ดูมีน้ำหนัก
        if (rb != null)
        {
            rb.velocity = respawnDropVelocity;
        }

        yield return StartCoroutine(InvulnerableRoutine());
    }

    private Vector2 GetRespawnPosition()
    {
        // เกิดใหม่ที่ขอบซ้ายของจอแล้วตกลงมาจากด้านบน (แบบ Contra ต้นฉบับ)
        if (cameraFollow != null)
        {
            return new Vector2(
                cameraFollow.LeftBoundaryX + respawnEdgeMargin,
                spawnPosition.y + respawnHeightOffset
            );
        }

        return new Vector2(
            spawnPosition.x,
            spawnPosition.y + respawnHeightOffset
        );
    }

    private IEnumerator InvulnerableRoutine()
    {
        isInvulnerable = true;

        float elapsed = 0f;

        while (elapsed < invulnerableDuration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
            }

            yield return new WaitForSeconds(blinkInterval);

            elapsed += blinkInterval;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        isInvulnerable = false;
    }

    private void SetPlayerActive(bool active)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = active;
        }

        if (playerController != null)
        {
            playerController.enabled = active;
        }

        if (playerCombat != null)
        {
            playerCombat.enabled = active;
        }
    }
}
