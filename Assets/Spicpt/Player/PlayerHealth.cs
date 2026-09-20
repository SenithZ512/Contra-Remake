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

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private CameraFollow cameraFollow;

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
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        currentHealth = maxHealth;
        currentLives = startingLives;
        spawnPosition = transform.position;
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

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        currentHealth = maxHealth;
        isDead = false;

        SetPlayerActive(true);

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
