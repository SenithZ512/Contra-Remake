using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    [Header("Contact Damage")]
    [SerializeField] private int damage = 1;

    [Header("Barrier (ไอเทม B ของผู้เล่น)")]
    /// <summary>
    /// ตัวนี้ตายไหมถ้ามาโดนผู้เล่นที่มีเกราะ
    ///
    /// ต้นฉบับ: ทหารเดินชนแล้วตายทันที แต่ยานพาหนะกับป้อมปืนไม่เป็นอะไร
    /// เปิดไว้สำหรับศัตรูที่เป็นคน ปิดสำหรับรถถังและป้อม
    /// </summary>
    [SerializeField] private bool diesToBarrier = true;

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    private Collider2D bodyCollider;
    private PlayerHealth playerHealth;
    private Collider2D playerCollider;
    private EnemyHealth health;

    private void Awake()
    {
        bodyCollider = GetComponent<Collider2D>();
        health = GetComponent<EnemyHealth>();

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
        if (!bodyCollider.bounds.Intersects(playerCollider.bounds))
            return;

        // ผู้เล่นมีเกราะอยู่ คนที่มาชนตายแทน ส่วนรถถังกับป้อมไม่เป็นอะไร
        if (playerHealth.HasBarrier)
        {
            if (diesToBarrier && health != null && !health.IsDead)
            {
                health.KillInstantly(true);
            }

            return;
        }

        playerHealth.TakeDamage(damage);
    }
}
