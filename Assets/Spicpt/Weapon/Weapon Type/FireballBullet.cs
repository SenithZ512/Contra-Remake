using UnityEngine;

public class FireballBullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float speed = 6f;
    [SerializeField] private float lifetime = 1f;

    [Header("Spin Orbit")]
    [SerializeField] private float orbitRadius = 0.3f;
    [SerializeField] private float orbitSpeed = 720f;

    private Rigidbody2D rb;
    private Vector2 origin;
    private Vector2 direction;
    private float elapsed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError(
                "FireballBullet: ต้องมี Rigidbody2D"
            );
        }
    }

    public void Initialize(Vector2 shootDirection)
    {
        if (rb == null)
            return;

        if (shootDirection.sqrMagnitude <= 0.001f)
        {
            shootDirection = Vector2.right;
        }

        direction = shootDirection.normalized;
        origin = rb.position;
        elapsed = 0f;

        rb.velocity = Vector2.zero;

        CancelInvoke(nameof(DestroyBullet));

        Invoke(
            nameof(DestroyBullet),
            lifetime
        );
    }

    private void FixedUpdate()
    {
        elapsed += Time.fixedDeltaTime;

        // ตำแหน่งวิ่งเป็นวงกลม (orbit) รอบเส้นทางที่พุ่งไปข้างหน้า ทำให้เห็น "หมุน" ชัดเจน
        // ต่างจากการหมุนสไปรท์ตรงๆ ซึ่งดูไม่ออกเพราะสไปรท์เป็นวงกลมสมมาตร
        Vector2 forwardOffset =
            direction * (speed * elapsed);

        float orbitAngle =
            elapsed * orbitSpeed * Mathf.Deg2Rad;

        Vector2 orbitOffset = new Vector2(
            Mathf.Cos(orbitAngle),
            Mathf.Sin(orbitAngle)
        ) * orbitRadius;

        rb.MovePosition(
            origin + forwardOffset + orbitOffset
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            return;

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
