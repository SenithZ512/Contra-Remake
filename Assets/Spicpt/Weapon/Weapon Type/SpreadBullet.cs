using UnityEngine;

public class SpreadBullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float speed = 7.9f;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private int damage = 1;

    [Header("Ground")]
    /// <summary>ยิงทะลุพื้นได้ทุกแบบ กระสุนหายเองตามอายุอย่างเดียว</summary>
    [SerializeField] private bool passThroughGround = true;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError(
                "SpreadBullet: ต้องมี Rigidbody2D"
            );
        }
    }

    public void Initialize(Vector2 direction)
    {
        if (rb == null)
            return;

        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = Vector2.right;
        }

        direction.Normalize();

        rb.velocity = Vector2.zero;
        rb.velocity = direction * speed;

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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            return;

        EnemyHealth enemy =
            other.GetComponentInParent<EnemyHealth>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage, rb != null ? rb.velocity : Vector2.zero);
            DestroyBullet();
            return;
        }

        // พื้นต่างระดับ (One-Way Platform) ยิงทะลุได้ตามต้นฉบับ Contra
        // ทะลุพื้นทุกชนิด ปล่อยให้หายเองตามอายุกระสุน
        if (passThroughGround)
            return;

        // พื้นต่างระดับยิงทะลุได้ แต่พื้นตัน (Tag SolidGround) ต้องหยุด
        if (other.GetComponent<PlatformEffector2D>() != null &&
            !other.CompareTag("SolidGround"))
            return;

        if (other.gameObject.layer ==
            LayerMask.NameToLayer("groundLayer"))
        {
            DestroyBullet();
        }
    }

    private void DestroyBullet()
    {
        Destroy(gameObject);
    }
}