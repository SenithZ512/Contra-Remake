using UnityEngine;

public class EnemyRunner : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;

    [Header("Behaviour")]
    [SerializeField] private bool chasePlayer = true;
    [SerializeField] private float fixedDirection = -1f;

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    private Rigidbody2D rb;
    private Transform player;
    private bool facingRight = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError(
                "EnemyRunner: ต้องมี Rigidbody2D"
            );
        }
    }

    private void Start()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        float direction = GetMoveDirection();

        rb.velocity = new Vector2(
            direction * moveSpeed,
            rb.velocity.y
        );

        UpdateFacing(direction);
    }

    private float GetMoveDirection()
    {
        // ไม่ไล่ผู้เล่น = วิ่งไปทางเดียวตลอด (แบบทหารที่วิ่งเข้ามาจากขอบจอ)
        if (!chasePlayer || player == null)
        {
            return fixedDirection >= 0f ? 1f : -1f;
        }

        float difference =
            player.position.x - transform.position.x;

        // ยืนซ้อนกันพอดี ให้วิ่งทางเดิมต่อ กันอาการสั่นกลับไปกลับมา
        if (Mathf.Abs(difference) < 0.05f)
        {
            return facingRight ? 1f : -1f;
        }

        return Mathf.Sign(difference);
    }

    private void UpdateFacing(float direction)
    {
        if (direction > 0f && !facingRight)
        {
            Flip();
        }
        else if (direction < 0f && facingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }
}
