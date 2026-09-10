using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    [Header("Contact Damage")]
    [SerializeField] private int damage = 1;

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    private Collider2D bodyCollider;
    private PlayerHealth playerHealth;
    private Collider2D playerCollider;

    private void Awake()
    {
        bodyCollider = GetComponent<Collider2D>();

        if (bodyCollider == null)
        {
            Debug.LogError(
                "EnemyContactDamage: ต้องมี Collider2D"
            );
        }
    }

    private void Start()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject == null)
            return;

        playerHealth = playerObject.GetComponent<PlayerHealth>();
        playerCollider = playerObject.GetComponent<Collider2D>();

        // ศัตรูเดินทะลุตัวผู้เล่นได้ ไม่ผลักกันไปมา แต่ยังชนพื้นตามปกติ
        if (bodyCollider != null && playerCollider != null)
        {
            Physics2D.IgnoreCollision(
                bodyCollider,
                playerCollider,
                true
            );
        }
    }

    private void Update()
    {
        if (playerHealth == null || playerCollider == null)
            return;

        if (bodyCollider == null || playerHealth.IsDead)
            return;

        // เช็คการซ้อนทับด้วยกรอบ bounds เพราะปิดการชนทางฟิสิกส์กับผู้เล่นไปแล้ว
        if (bodyCollider.bounds.Intersects(playerCollider.bounds))
        {
            playerHealth.TakeDamage(damage);
        }
    }
}
