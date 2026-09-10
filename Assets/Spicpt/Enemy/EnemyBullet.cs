using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private int damage = 1;

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
        // กระสุนศัตรูไม่โดนศัตรูด้วยกันเอง
        if (other.GetComponentInParent<EnemyHealth>() != null)
            return;

        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            DestroyBullet();
            return;
        }

        // พื้นต่างระดับ (One-Way Platform) ยิงทะลุได้ตามต้นฉบับ Contra
        if (other.GetComponent<PlatformEffector2D>() != null)
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
