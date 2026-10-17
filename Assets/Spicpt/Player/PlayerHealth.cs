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

    [Header("Barrier (ไอเทม B)")]
    /// <summary>
    /// ต้นฉบับตั้งตัวนับไว้ที่ $80 แล้วลดทีละหนึ่งทุก 8 เฟรม
    /// 128 x 8 = 1024 เฟรม หารด้วย 60 ได้ 17.07 วินาที
    /// </summary>
    [SerializeField] private float barrierDuration = 17.07f;

    /// <summary>
    /// ด่าน Hangar ด่านเดียวที่ได้นานกว่า ต้นฉบับตั้ง $90 = 19.2 วินาที
    /// ใส่ชื่อฉากที่อยากให้ใช้ค่านี้ เว้นว่างไว้ก็ใช้ค่าปกติทุกด่าน
    /// </summary>
    [SerializeField] private string longBarrierScene = "Hangar";

    [SerializeField] private float longBarrierDuration = 19.2f;

    /// <summary>สีที่วนตอนมีเกราะ ต้นฉบับสลับพาเลตต์ให้ตัวละครเปลี่ยนสีรัว</summary>
    [SerializeField] private Color[] barrierColors =
    {
        new Color(1f, 1f, 1f),
        new Color(0.6f, 0.9f, 1f),
        new Color(1f, 0.8f, 0.4f),
        new Color(0.8f, 0.6f, 1f)
    };

    [SerializeField] private float barrierColorRate = 12f;

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

    private float barrierUntil;

    /// <summary>
    /// กำลังมีเกราะอยู่ไหม
    /// ต้นฉบับกันได้ทุกอย่างยกเว้นตกเหว และศัตรูที่มาโดนตัวจะตายทันที
    /// </summary>
    public bool HasBarrier => Time.time < barrierUntil;

    /// <summary>ไอเทม B เรียกตอนถูกเก็บ ใส่ 0 เพื่อใช้ระยะเวลามาตรฐานของด่าน</summary>
    public void ActivateBarrier(float duration)
    {
        if (duration <= 0f)
        {
            duration = GetBarrierDuration();
        }

        // เก็บซ้ำไม่ต่อเวลา แต่เริ่มนับใหม่ เหมือนต้นฉบับที่เขียนทับตัวนับ
        barrierUntil = Time.time + duration;
    }

    private float GetBarrierDuration()
    {
        if (string.IsNullOrEmpty(longBarrierScene))
            return barrierDuration;

        string scene = UnityEngine.SceneManagement.SceneManager
            .GetActiveScene().name;

        return scene == longBarrierScene
            ? longBarrierDuration
            : barrierDuration;
    }

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
        UpdateBarrierLook();
    }

    /// <summary>วนสีตัวละครตอนมีเกราะ ให้รู้ว่ายังอมตะอยู่</summary>
    private void UpdateBarrierLook()
    {
        if (spriteRenderer == null)
            return;

        if (!HasBarrier)
        {
            // ช่วงกะพริบหลังเกิดใหม่จัดการสีเองอยู่แล้ว อย่าไปทับ
            if (!isInvulnerable)
            {
                spriteRenderer.color = Color.white;
            }

            return;
        }

        if (barrierColors == null || barrierColors.Length == 0)
            return;

        int index = Mathf.FloorToInt(Time.time * barrierColorRate) %
            barrierColors.Length;

        spriteRenderer.color = barrierColors[index];
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
        // อมตะอยู่ (เพิ่งเกิดใหม่ หรือมีเกราะ) หรือตายไปแล้ว ไม่ต้องรับดาเมจซ้ำ
        if (isDead || isInvulnerable || HasBarrier)
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
