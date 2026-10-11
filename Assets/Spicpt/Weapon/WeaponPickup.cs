using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public enum PickupKind
    {
        /// <summary>เปลี่ยนอาวุธ</summary>
        Weapon,

        /// <summary>ไอเทม R เพิ่มความเร็วยิงของอาวุธที่ถืออยู่</summary>
        RapidFire
    }

    [Header("Weapon")]
    [SerializeField] private PickupKind kind = PickupKind.Weapon;
    [SerializeField] private WeaponType weaponType = WeaponType.SpreadGun;

    [Header("Falling")]
    [SerializeField] private bool fallWithGravity = true;
    [SerializeField] private float gravityScale = 3f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.25f;

    /// <summary>
    /// ห้ามลงจอดในช่วงนี้หลังถูกดีดออกมา
    /// ไม่งั้นถ้าป้อมตั้งอยู่บนพื้น ไอเทมจะนับว่าถึงพื้นตั้งแต่เฟรมแรก
    /// แล้วหยุดนิ่งทันทีโดยไม่ทันได้ลอย
    /// </summary>
    [SerializeField] private float launchLockDuration = 0.25f;

    [Header("Motion")]
    [SerializeField] private float bobAmplitude = 0.2f;
    [SerializeField] private float bobSpeed = 3f;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 12f;

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    [Header("Look")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;
    private Vector2 origin;
    private float elapsed;
    private bool hasLanded;
    private float landLockUntil;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        rb = GetComponent<Rigidbody2D>();

        origin = transform.position;

        if (fallWithGravity && rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = gravityScale;
        }
        else
        {
            hasLanded = true;
        }

        ApplyWeaponColor();
    }

    private void Start()
    {
        if (lifetime > 0f)
        {
            Destroy(gameObject, lifetime);
        }
    }

    private void FixedUpdate()
    {
        if (hasLanded || rb == null)
            return;

        if (Time.time < landLockUntil)
            return;

        // collider เป็น trigger เลยชนพื้นเองไม่ได้ ต้องเช็คด้วย overlap แล้วสั่งหยุดเอง
        Collider2D ground = Physics2D.OverlapCircle(
            transform.position,
            groundCheckRadius,
            groundLayer
        );

        if (ground == null)
            return;

        Land();
    }

    private void Land()
    {
        hasLanded = true;

        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        origin = transform.position;
        elapsed = 0f;
    }

    private void Update()
    {
        // ระหว่างร่วง ปล่อยให้ฟิสิกส์คุมตำแหน่ง ห้ามเขียนทับ ไม่งั้นจะลอยค้างกลางอากาศ
        if (!hasLanded)
            return;

        elapsed += Time.deltaTime;

        // ลอยขึ้นลงเบาๆ ให้สังเกตเห็นง่าย ขยับขึ้นอย่างเดียวจะได้ไม่จมพื้น
        float bob =
            (Mathf.Sin(elapsed * bobSpeed) + 1f) * 0.5f * bobAmplitude;

        transform.position = origin + new Vector2(0f, bob);
    }

    /// <summary>
    /// ดีดไอเทมให้กระเด็นลอยเป็นเส้นโค้งตอนเกิด
    /// เรียกหลัง Instantiate ได้เลย
    /// </summary>
    public void Launch(Vector2 velocity)
    {
        if (rb == null)
            return;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = gravityScale;
        rb.velocity = velocity;

        hasLanded = false;

        landLockUntil = Time.time + launchLockDuration;
    }

    /// <summary>
    /// สั่งให้ชิ้นนี้กลายเป็นไอเทมยิงรัวหลัง Instantiate
    /// ใช้ prefab เดิมได้เลย ไม่ต้องมี prefab แยก
    /// </summary>
    public void SetRapidFire()
    {
        kind = PickupKind.RapidFire;

        ApplyWeaponColor();
    }

    /// <summary>ให้โดรนกำหนดว่าไอเทมชิ้นนี้เป็นอาวุธอะไร ตอนดรอปออกมา</summary>
    public void SetWeaponType(WeaponType type)
    {
        weaponType = type;

        ApplyWeaponColor();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        WeaponSystem weaponSystem =
            other.GetComponentInParent<WeaponSystem>();

        if (weaponSystem == null)
            return;

        if (kind == PickupKind.RapidFire)
        {
            weaponSystem.EnableRapidFire();
        }
        else
        {
            weaponSystem.SetWeapon(weaponType);
        }

        Destroy(gameObject);
    }

    private void ApplyWeaponColor()
    {
        if (spriteRenderer == null)
            return;

        // ไอเทมยิงรัวใช้สีเหลืองสดแยกจากอาวุธ จะได้ไม่สับสนกับ Spread/FireBall
        spriteRenderer.color = kind == PickupKind.RapidFire
            ? new Color(1f, 0.95f, 0.2f)
            : GetWeaponColor(weaponType);
    }

    private static Color GetWeaponColor(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.MachineGun:
                return new Color(0.7f, 0.75f, 0.85f);

            case WeaponType.SpreadGun:
                return new Color(1f, 0.35f, 0.35f);

            case WeaponType.Laser:
                return new Color(0f, 1f, 1f);

            case WeaponType.FireBall:
                return new Color(1f, 0.55f, 0.1f);

            default:
                return Color.white;
        }
    }
}
