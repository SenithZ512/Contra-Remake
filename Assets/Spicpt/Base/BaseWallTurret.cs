using UnityEngine;

/// <summary>
/// ป้อมปืนติดผนังในด่านฐาน
///
/// ใช้ได้ทั้งสองแบบ ต่างกันแค่ค่าใน Inspector
///   - Gelguge (烈撃砲) ป้อมเล็กตามทางเดิน เลือด 4 ยิงทีละนัด
///   - Zark (巨弾三連砲) ป้อมห้องบอส เลือด 8 ยิงทีเดียวสามนัดกระจาย
///
/// หัวใจคือมันสลับเปิดปิดฝา ตอนปิดยิงไม่เข้าเลย
/// ต้องรอจังหวะมันเปิดถึงจะทำอะไรได้ ซึ่งก็เป็นจังหวะที่มันยิงเราพอดี
/// </summary>
public class BaseWallTurret : MonoBehaviour, IBaseDamageable
{
    [Header("Health")]
    /// <summary>4 = Gelguge ป้อมเล็ก, 8 = Zark ป้อมห้องบอส</summary>
    [SerializeField] private int maxHealth = 4;

    [Header("Open / Close Cycle")]
    /// <summary>หน่วงก่อนเปิดฝาครั้งแรก</summary>
    [SerializeField] private float firstOpenDelay = 1.5f;

    [SerializeField] private float openDuration = 2.2f;
    [SerializeField] private float closedDuration = 1.6f;

    /// <summary>
    /// เวลาปิดฝาเมื่อผู้เล่นถือปืนแรง (ความแรงตั้งแต่ 2 ขึ้นไป) 0 = ไม่ใช้
    ///
    /// ต้นฉบับของ Zark ตั้ง #$c0 (192 เฟรม) กับ #$60 (96 เฟรม)
    /// ถือปืนแรงมาป้อมจะเปิดถี่ขึ้นเท่าตัว เป็นการปรับความยากที่ซ่อนไว้
    /// </summary>
    [SerializeField] private float closedDurationStrongWeapon;

    [Header("Fire")]
    [SerializeField] private GameObject projectilePrefab;

    [SerializeField] private float fireInterval = 0.9f;

    /// <summary>หน่วงหลังเปิดฝาก่อนยิงนัดแรก ให้ทันตั้งตัว</summary>
    [SerializeField] private float warmUp = 0.35f;

    /// <summary>1 = Gelguge, 3 = Zark ยิงกระจายสามนัด</summary>
    [SerializeField] private int shotsPerVolley = 1;

    /// <summary>
    /// เล็งผู้เล่นไหม
    ///
    /// Gelguge เล็ง (ผ่าน aim_and_create_enemy_bullet แต่ปัดเป็นขั้นละ 15 องศา)
    /// Zark ไม่เล็งเลย ยิงลงเป็นพัดตายตัวไม่ว่าเราจะอยู่ตรงไหน
    /// ตามตาราง wall_cannon_bullet_type_and_angle คือมุม 8, 6, 4
    /// </summary>
    [SerializeField] private bool aimsAtPlayer = true;

    /// <summary>
    /// องศาที่กางออกระหว่างนัด
    /// ของ Zark คือ 30 องศา เพราะช่องในตารางห่างกันทิศละ 15 องศา และข้ามทีละ 2
    /// </summary>
    [SerializeField] private float spreadAngle = 30f;

    /// <summary>
    /// ปากกระบอกแต่ละนัดห่างกันเท่าไหร่
    /// ต้นฉบับใช้ -8, 0, +8 พิกเซล ซึ่งที่ 24 px ต่อหน่วยคือ 0.333
    /// </summary>
    [SerializeField] private float muzzleSpacing = 0.333f;

    [Header("Sprites")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;
    [SerializeField] private Sprite brokenSprite;

    [Header("Death")]
    [SerializeField] private GameObject explosionPrefab;

    private Collider2D hitBox;

    private int health;
    private bool open;
    private bool destroyed;
    private float stateTimer;
    private float fireTimer;

    public bool CanBeHit
    {
        get { return !destroyed && open; }
    }

    public bool IsDestroyed
    {
        get { return destroyed; }
    }

    private void Awake()
    {
        hitBox = GetComponent<Collider2D>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void OnEnable()
    {
        health = maxHealth;
        destroyed = false;
        open = false;
        stateTimer = firstOpenDelay;
        fireTimer = 0f;

        if (hitBox != null)
        {
            hitBox.enabled = true;
        }

        if (spriteRenderer != null && closedSprite != null)
        {
            spriteRenderer.sprite = closedSprite;
        }
    }

    private void Update()
    {
        if (destroyed)
            return;

        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            ToggleShutter();
        }

        if (open)
        {
            UpdateFire();
        }
    }

    private void ToggleShutter()
    {
        open = !open;

        stateTimer = open ? openDuration : GetClosedDuration();

        // เปิดฝาแล้วยังไม่ยิงทันที ให้เวลาคนเล่นเห็นก่อน
        fireTimer = open ? warmUp : 0f;

        if (spriteRenderer == null)
            return;

        Sprite next = open ? openSprite : closedSprite;

        if (next != null)
        {
            spriteRenderer.sprite = next;
        }
    }

    private void UpdateFire()
    {
        if (projectilePrefab == null)
            return;

        fireTimer -= Time.deltaTime;

        if (fireTimer > 0f)
            return;

        fireTimer = fireInterval;

        Volley();
    }

    /// <summary>ปืนแรงอยู่ในมือผู้เล่น ป้อมจะรีบเปิดฝากลับมาเร็วขึ้น</summary>
    private float GetClosedDuration()
    {
        if (closedDurationStrongWeapon <= 0f)
            return closedDuration;

        return BaseStage.PlayerWeaponStrength >= 2
            ? closedDurationStrongWeapon
            : closedDuration;
    }

    private void Volley()
    {
        int shots = Mathf.Max(1, shotsPerVolley);

        for (int i = 0; i < shots; i++)
        {
            // นัดกลางอยู่ที่ 0 นัดข้างเยื้องออกคนละทาง
            float step = i - (shots - 1) * 0.5f;

            Vector3 spot = transform.position +
                Vector3.right * (step * muzzleSpacing);

            GameObject shot = Instantiate(
                projectilePrefab,
                spot,
                Quaternion.identity
            );

            BaseProjectile projectile = shot.GetComponent<BaseProjectile>();

            if (projectile != null)
            {
                // ป้อมที่ยิงเป็นแพตเทิร์นต้องสั่งปิดการเล็งก่อน Start จะทำงาน
                projectile.OverrideAim(aimsAtPlayer);
            }

            if (shots == 1)
                continue;

            shot.transform.Rotate(0f, 0f, step * spreadAngle);
        }
    }

    public void TakeHit(int damage, Vector2 from)
    {
        if (!CanBeHit)
            return;

        health -= damage;

        if (health > 0)
            return;

        destroyed = true;

        if (hitBox != null)
        {
            hitBox.enabled = false;
        }

        if (spriteRenderer != null && brokenSprite != null)
        {
            spriteRenderer.sprite = brokenSprite;
        }

        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
    }
}
