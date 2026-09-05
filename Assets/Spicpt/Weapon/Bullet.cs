using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifetime = 2f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError(
                "Bullet: ต้องมี Rigidbody2D บน Bullet"
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

        // ล้างความเร็วเดิมก่อน
        rb.velocity = Vector2.zero;

        // กำหนดความเร็วใหม่จากทิศยิงเท่านั้น
        rb.velocity = direction * speed;

        // หมุนกระสุนตามทิศทาง
        float angle = Mathf.Atan2(
            direction.y,
            direction.x
        ) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(
            0f,
            0f,
            angle
        );

        CancelInvoke(nameof(DestroyBullet));

        Invoke(
            nameof(DestroyBullet),
            lifetime
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // กระสุนของ Player ไม่ยิงตัวเอง
        if (other.CompareTag("Player"))
            return;

        // ชนพื้นแล้วหาย
        if (other.gameObject.layer ==
            LayerMask.NameToLayer("Ground"))
        {
            DestroyBullet();
        }
    }

    private void DestroyBullet()
    {
        Destroy(gameObject);
    }
}