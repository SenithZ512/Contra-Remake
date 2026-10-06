using UnityEngine;

/// <summary>
/// โซนน้ำแบบ Contra NES
/// ผู้เล่นลุยน้ำได้แต่เดินช้าลงและกระโดดไม่สูงเท่าเดิม
/// ศัตรูที่เป็นคนตกลงมาโดนน้ำ = ตายทันที
///
/// วางเป็น Collider2D แบบ Is Trigger ทับผืนน้ำไว้
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WaterZone : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    /// <summary>คูณความเร็วเดิน 0.5 = ช้าลงครึ่งหนึ่ง</summary>
    [SerializeField] private float playerSpeedMultiplier = 0.5f;

    /// <summary>คูณแรงกระโดด น้ำหน่วงไว้เลยกระโดดได้ไม่สูงเท่าบนบก</summary>
    [SerializeField] private float playerJumpMultiplier = 0.65f;

    [Header("Enemy")]
    [SerializeField] private bool killEnemies = true;

    /// <summary>ใช้ระเบิดตอนตายของศัตรูเองด้วยไหม ปิดไว้ถ้าอยากให้แค่จมหายไปเงียบๆ</summary>
    [SerializeField] private bool useEnemyDeathEffect;

    [Header("Effect")]
    [SerializeField] private GameObject splashPrefab;

    private Collider2D zone;

    private void Awake()
    {
        zone = GetComponent<Collider2D>();

        if (zone != null && !zone.isTrigger)
        {
            Debug.LogWarning(
                "WaterZone: Collider ของ \"" + name +
                "\" ต้องติ๊ก Is Trigger ไม่งั้นตัวละครจะเดินชนผิวน้ำ"
            );
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            PlayerController player =
                other.GetComponentInParent<PlayerController>();

            if (player != null)
            {
                player.EnterWater(
                    playerSpeedMultiplier,
                    playerJumpMultiplier
                );

                Splash(other);
            }

            return;
        }

        if (!killEnemies)
            return;

        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();

        if (enemy == null || enemy.IsDead)
            return;

        Splash(other);

        enemy.KillInstantly(useEnemyDeathEffect);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player != null)
        {
            player.ExitWater();

            Splash(other);
        }
    }

    /// <summary>น้ำกระเซ็นตรงผิวน้ำเหนือจุดที่ตกลงมา</summary>
    private void Splash(Collider2D other)
    {
        if (splashPrefab == null || zone == null)
            return;

        Vector2 position = new Vector2(
            other.bounds.center.x,
            zone.bounds.max.y
        );

        Instantiate(splashPrefab, position, Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D preview = GetComponent<Collider2D>();

        if (preview == null)
            return;

        Bounds area = preview.bounds;

        // ขีดเส้นผิวน้ำให้เห็นว่าระดับน้ำอยู่ตรงไหน
        Gizmos.DrawLine(
            new Vector3(area.min.x, area.max.y, 0f),
            new Vector3(area.max.x, area.max.y, 0f)
        );

        Gizmos.DrawWireCube(area.center, area.size);
    }
}
