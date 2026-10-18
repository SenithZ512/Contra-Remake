using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private int damage = 1;

    [Header("Explosive (ลูกระเบิดของบอส)")]
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private float blastRadius = 0f;

    [Header("Ground")]
    /// <summary>
    /// หยุดที่พื้นทุกชนิด ไม่สนว่าเป็นพื้นลอดได้หรือไม่
    /// เปิดไว้สำหรับลูกระเบิดบอส ที่ต้องตกกระแทกพื้นแล้วระเบิด
    /// </summary>
    [SerializeField] private bool stopOnAnyGround;

    /// <summary>Tag ของแท่นลอยที่กระสุนทะลุผ่านได้เสมอ</summary>
    [SerializeField] private string oneWayPlatformTag = "OneWayPlatform";

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    [Header("Look")]
    [SerializeField] private bool rotateAlongVelocity;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError(
                "EnemyBullet: ต้องมี Rigidbody2D"
            );
        }
    }

    private void Start()
    {
        // ดักที่ตัวกระสุน ครอบคลุมศัตรูทุกตัวที่ยิงกระสุนชนิดนี้ในทีเดียว
        // ไม่ต้องไปไล่ใส่ทีละสคริปต์
        GameAudio.EnemyShot();
    }

    public void Initialize(Vector2 direction)
    {
        Initialize(direction, 1f);
    }

    /// <summary>speedMultiplier ใช้สุ่มระยะยิง ลูกเร็วกว่า = ตกไกลกว่า</summary>
    public void Initialize(Vector2 direction, float speedMultiplier)
    {
        if (rb == null)
            return;

        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = Vector2.right;
        }

        direction.Normalize();

        rb.velocity = Vector2.zero;
        rb.velocity = direction * speed * speedMultiplier;

        float angle = Mathf.Atan2(
            direction.y,
            direction.x
        ) * Mathf.Rad2Deg;

        transform.rotation =
            Quaternion.Euler(0f, 0f, angle);

        CancelInvoke(nameof(DestroyBullet));

        Invoke(
            nameof(DestroyBullet),
            lifetime
        );
    }

    private void Update()
    {
        // ลูกที่มีน้ำหนักจะโค้งลง หัวกระสุนต้องหันตามทางที่มันวิ่งจริง
        if (!rotateAlongVelocity || rb == null)
            return;

        if (rb.velocity.sqrMagnitude < 0.001f)
            return;

        float angle = Mathf.Atan2(
            rb.velocity.y,
            rb.velocity.x
        ) * Mathf.Rad2Deg;

        transform.rotation =
            Quaternion.Euler(0f, 0f, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // กระสุนศัตรูไม่โดนศัตรูด้วยกันเอง
        if (other.GetComponentInParent<EnemyHealth>() != null)
            return;

        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            Explode();
            return;
        }

        // แท่นลอยติด Tag ไว้ ทะลุเสมอ แม้ลูกนั้นจะตั้ง stopOnAnyGround
        // จะได้ระเบิดเฉพาะพื้นหลัก ไม่ไปแตกใส่แท่นบางๆ กลางอากาศ
        if (other.CompareTag(oneWayPlatformTag))
            return;

        // พื้นต่างระดับอื่น ยิงทะลุได้ตามต้นฉบับ Contra
        // ยกเว้นลูกที่ตั้ง stopOnAnyGround ไว้
        if (!stopOnAnyGround &&
            other.GetComponent<PlatformEffector2D>() != null &&
            !other.CompareTag("SolidGround"))
            return;

        if (other.gameObject.layer ==
            LayerMask.NameToLayer("groundLayer"))
        {
            Explode();
        }
    }

    /// <summary>ระเบิดตอนกระทบ ลูกที่มีรัศมีจะกินพื้นที่รอบจุดตกด้วย</summary>
    private void Explode()
    {
        if (impactEffectPrefab != null)
        {
            Instantiate(
                impactEffectPrefab,
                transform.position,
                Quaternion.identity
            );
        }

        if (blastRadius > 0f)
        {
            DamagePlayerInBlast();
        }

        DestroyBullet();
    }

    private void DamagePlayerInBlast()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            blastRadius
        );

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag(playerTag))
                continue;

            PlayerHealth playerHealth =
                hit.GetComponentInParent<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }

            return;
        }
    }

    private void DestroyBullet()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (blastRadius <= 0f)
            return;

        Gizmos.DrawWireSphere(transform.position, blastRadius);
    }
}
