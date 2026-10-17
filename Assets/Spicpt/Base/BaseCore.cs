using UnityEngine;

/// <summary>
/// แกนพลังงานบนกำแพง - Nuclear Ball (レーザー制御核ボル)
///
/// ตัวเดียวกันนี้ใช้ได้ทั้งสามแบบ ต่างกันแค่ค่าใน Inspector
///   - แกนธรรมดาในห้องทางเดิน เลือด 8
///   - Base Core แกนใหญ่ห้องสุดท้ายก่อนบอส เลือด 16
///   - Shoud ในห้องบอส เลือด 10 เปิดแล้วเปิดเลย ไม่ปิดอีก
///
/// จังหวะตามต้นฉบับ
///   เกิดมาปิดฝาอยู่ -> รอ 240 เฟรม (4 วิ) -> เปิดฝา ยิงเข้าได้
///   ทหารบุกครบ 7 รอบแล้วแกนถึงจะเริ่มยิงสวน ทุก 40 เฟรม
///   แต่แกนที่ติดอยู่ต่ำเกินไปจะไม่ยิงเลย เพราะเกมถือว่าเราต้องหมอบยิงอยู่แล้ว
/// </summary>
public class BaseCore : MonoBehaviour, IBaseDamageable
{
    [Header("Health")]
    /// <summary>8 = แกนธรรมดา, 16 = Base Core แกนใหญ่, 10 = Shoud ห้องบอส</summary>
    [SerializeField] private int maxHealth = 8;

    [Header("Opening")]
    /// <summary>หน่วงก่อนเปิดฝา ต้นฉบับใช้ 240 เฟรม = 4 วินาที</summary>
    [SerializeField] private float openDelay = 4f;

    /// <summary>มีแผงเกราะคลุมอีกชั้น ต้องยิงเกราะพังก่อนถึงจะโดนแกน (ด่าน 4)</summary>
    [SerializeField] private bool plated;

    [SerializeField] private int plateHealth = 4;

    /// <summary>Shoud ในห้องบอส เปิดแล้วไม่ปิดอีกตลอดการต่อสู้</summary>
    [SerializeField] private bool stayOpen = true;

    [Header("Return Fire (ยิงสวน)")]
    /// <summary>
    /// ปิดไว้เป็นค่าเริ่มต้น เพราะในเกมจริงแทบไม่มีใครเคยเห็นแกนยิงสวน
    ///
    /// ต้นฉบับมีโค้ดส่วนนี้อยู่จริงที่ wall_core_routine_03 แต่ประตูปิดแน่นมาก
    /// ต้องรอทหารวนครบ 7 รอบก่อน ซึ่งกินเวลา 13 ถึง 47 วินาทีแล้วแต่ห้อง
    /// ขณะที่แกนเลือดแค่ 8 หน่วย ยิงไม่กี่นัดก็แตก
    /// มันเลยเป็นกลไกกันคนยืนฟาร์มแต้ม ไม่ใช่การโจมตีปกติของด่าน
    ///
    /// เปิดได้ถ้าอยากได้พฤติกรรมครบตามโค้ดต้นฉบับ เงื่อนไข 7 รอบยังอยู่เหมือนเดิม
    /// </summary>
    [SerializeField] private bool canReturnFire;

    [SerializeField] private GameObject projectilePrefab;

    /// <summary>ต้นฉบับยิงทุก 40 เฟรม = 0.67 วินาที</summary>
    [SerializeField] private float fireInterval = 0.67f;

    /// <summary>
    /// แกนที่อยู่ต่ำกว่าเส้นนี้จะไม่ยิงสวนเลย
    /// ต้นฉบับเช็ค ENEMY_Y_POS >= $70 เพราะถือว่าผู้เล่นหมอบยิงอยู่แล้ว
    /// </summary>
    [SerializeField] private float lowCoreCutoffY = 0.34f;

    [Header("Sprites")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite plateSprite;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;
    [SerializeField] private Sprite brokenSprite;

    /// <summary>กะพริบตอนเปิดอยู่ ให้รู้ว่ายิงได้แล้ว</summary>
    [SerializeField] private Color blinkColor = new Color(1f, 0.5f, 0.4f);

    [SerializeField] private float blinkSpeed = 6f;

    [Header("Death")]
    [SerializeField] private GameObject explosionPrefab;

    private BaseRoom room;
    private Collider2D hitBox;

    private int health;
    private int plate;
    private float openTimer;
    private float fireTimer;
    private bool open;
    private bool destroyed;

    public bool IsDestroyed
    {
        get { return destroyed; }
    }

    /// <summary>ปิดฝาอยู่ยิงไม่เข้า เหมือนต้นฉบับ</summary>
    public bool CanBeHit
    {
        get { return !destroyed && (open || plate > 0); }
    }

    private void Awake()
    {
        room = GetComponentInParent<BaseRoom>();
        hitBox = GetComponent<Collider2D>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        ResetCore();
    }

    public void ResetCore()
    {
        health = maxHealth;
        plate = plated ? plateHealth : 0;
        openTimer = 0f;
        fireTimer = 0f;
        open = false;
        destroyed = false;

        if (hitBox != null)
        {
            hitBox.enabled = true;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            spriteRenderer.sprite = plated ? plateSprite : closedSprite;
        }
    }

    private void Update()
    {
        if (destroyed)
            return;

        if (!open)
        {
            UpdateOpening();
            return;
        }

        Blink();
        UpdateReturnFire();
    }

    private void UpdateOpening()
    {
        // เกราะยังอยู่ก็ยังเปิดไม่ได้ ต้องยิงเกราะให้พังก่อน
        if (plate > 0)
            return;

        openTimer += Time.deltaTime;

        if (openTimer < openDelay)
            return;

        open = true;

        if (spriteRenderer != null && openSprite != null)
        {
            spriteRenderer.sprite = openSprite;
        }
    }

    private void Blink()
    {
        if (spriteRenderer == null)
            return;

        float wave = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f;

        spriteRenderer.color = Color.Lerp(Color.white, blinkColor, wave);
    }

    private void UpdateReturnFire()
    {
        if (!canReturnFire || projectilePrefab == null)
            return;

        // ติดต่ำเกินไป ไม่ยิง
        if (transform.position.y <= lowCoreCutoffY)
            return;

        // ทหารยังบุกไม่ครบรอบ แกนยังไม่ยิงสวน
        if (room != null && !room.AttackRoundsFinished)
            return;

        fireTimer -= Time.deltaTime;

        if (fireTimer > 0f)
            return;

        fireTimer = fireInterval;

        Instantiate(projectilePrefab, transform.position, Quaternion.identity);
    }

    public void TakeHit(int damage, Vector2 from)
    {
        if (destroyed)
            return;

        if (plate > 0)
        {
            plate -= damage;

            if (plate <= 0 && spriteRenderer != null)
            {
                spriteRenderer.sprite = closedSprite;
            }

            return;
        }

        if (!open)
            return;

        health -= damage;

        if (health > 0)
            return;

        Destroyed();
    }

    private void Destroyed()
    {
        destroyed = true;

        if (!stayOpen)
        {
            open = false;
        }

        if (hitBox != null)
        {
            hitBox.enabled = false;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;

            if (brokenSprite != null)
            {
                spriteRenderer.sprite = brokenSprite;
            }
        }

        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        if (room != null)
        {
            room.NotifyCoreDestroyed();
        }
    }
}
