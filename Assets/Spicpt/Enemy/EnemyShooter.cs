using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("Shooting")]
    [SerializeField] private GameObject enemyBulletPrefab;
    [SerializeField] private float fireRate = 1.5f;
    [SerializeField] private float firstShotDelay = 0.5f;

    [Header("Fire Point")]
    /// <summary>จุดไหล่ที่ประทับปืน วัดจากเท้า</summary>
    [SerializeField] private Vector2 shoulderOffset = new Vector2(0f, 1f);

    /// <summary>ความยาวลำกล้อง ปากกระบอกจะยื่นไปตามทิศที่ยิง</summary>
    [SerializeField] private float barrelLength = 0.55f;

    [Header("Aiming")]
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private bool aimAtPlayer = true;
    [SerializeField] private float aimHeightOffset = 0.9f;

    /// <summary>ยิงได้แค่ 8 ทิศเหมือนผู้เล่น ไม่ใช่เล็งองศาอิสระ</summary>
    [SerializeField] private bool useEightDirections = true;

    /// <summary>
    /// ค่า y ต่ำสุดที่ยอมให้เล็งได้ กันยิงลงพื้นที่ตัวเองยืนอยู่
    ///  -0.8 = ห้ามยิงลงตรงๆ แต่เฉียงลงได้ (กติกาเดียวกับผู้เล่น)
    ///   0   = ห้ามยิงลงทุกแบบ ยิงได้แค่แนวนอนขึ้นไป
    /// </summary>
    [SerializeField] private float lowestAimY = -0.8f;

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    private Transform player;
    private PlayerHealth playerHealth;
    private bool facingRight = true;
    private float nextFireTime;

    private void Awake()
    {
        if (enemyBulletPrefab == null)
        {
            Debug.LogError(
                "EnemyShooter: ยังไม่ได้กำหนด Enemy Bullet Prefab"
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
            playerHealth = playerObject.GetComponent<PlayerHealth>();
        }

        nextFireTime = Time.time + firstShotDelay;
    }

    private void Update()
    {
        if (player == null)
            return;

        if (playerHealth != null && playerHealth.IsDead)
            return;

        FacePlayer();

        if (Time.time < nextFireTime)
            return;

        float distance = Vector2.Distance(
            transform.position,
            player.position
        );

        // ผู้เล่นอยู่ไกลเกินไป ยังไม่ต้องยิง
        if (distance > detectionRange)
            return;

        nextFireTime = Time.time + fireRate;

        Shoot();
    }

    private void Shoot()
    {
        if (enemyBulletPrefab == null)
            return;

        Vector2 shoulder = GetShoulderPosition();
        Vector2 direction = GetShootDirection(shoulder);

        // ปากกระบอกยื่นไปตามทิศที่เล็ง กระสุนเลยออกตรงปลายปืนทุกมุม
        Vector2 firePosition =
            shoulder + direction * (barrelLength * GetScale());

        GameObject bulletObject =
            Instantiate(
                enemyBulletPrefab,
                firePosition,
                Quaternion.identity
            );

        EnemyBullet bullet =
            bulletObject.GetComponent<EnemyBullet>();

        if (bullet == null)
        {
            Debug.LogError(
                "EnemyShooter: Enemy Bullet Prefab ไม่มี EnemyBullet.cs"
            );

            Destroy(bulletObject);
            return;
        }

        bullet.Initialize(direction);
    }

    /// <summary>ย่อขยายตัวศัตรูแล้วจุดยิงเลื่อนตามเอง ไม่ต้องแก้ตัวเลขใหม่</summary>
    private float GetScale()
    {
        return Mathf.Abs(transform.lossyScale.y);
    }

    private Vector2 GetShoulderPosition()
    {
        float sign = facingRight ? 1f : -1f;

        return (Vector2)transform.position +
            new Vector2(
                shoulderOffset.x * sign,
                shoulderOffset.y
            ) * GetScale();
    }

    private Vector2 GetShootDirection(Vector2 firePosition)
    {
        if (aimAtPlayer && player != null)
        {
            // เล็งกลางลำตัวผู้เล่น ไม่ใช่ที่เท้า (จุดหมุนสไปรท์อยู่ที่เท้า)
            Vector2 targetPosition =
                (Vector2)player.position +
                Vector2.up * aimHeightOffset;

            Vector2 toPlayer = targetPosition - firePosition;

            if (toPlayer.sqrMagnitude > 0.001f)
            {
                return BlockSteepDown(Snap(toPlayer.normalized));
            }
        }

        return facingRight ? Vector2.right : Vector2.left;
    }

    /// <summary>
    /// เล็งต่ำเกินกำหนดก็ดึงกลับขึ้นมาเป็นทิศที่ใกล้ที่สุดที่ยังยิงได้
    /// ไม่งั้นทหารที่ยืนบนพื้นจะยิงลงพื้นใต้เท้าตัวเอง
    /// </summary>
    private Vector2 BlockSteepDown(Vector2 direction)
    {
        if (direction.y >= lowestAimY)
            return direction;

        float sign = Mathf.Abs(direction.x) > 0.01f
            ? Mathf.Sign(direction.x)
            : (facingRight ? 1f : -1f);

        Vector2 diagonal = new Vector2(sign, -1f).normalized;

        return diagonal.y >= lowestAimY
            ? diagonal
            : new Vector2(sign, 0f);
    }

    /// <summary>ปัดมุมลงช่องละ 45 องศา ได้ 8 ทิศเท่าผู้เล่น</summary>
    private Vector2 Snap(Vector2 direction)
    {
        if (!useEightDirections)
            return direction;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        float snapped = Mathf.Round(angle / 45f) * 45f * Mathf.Deg2Rad;

        return new Vector2(
            Mathf.Cos(snapped),
            Mathf.Sin(snapped)
        ).normalized;
    }

    private void FacePlayer()
    {
        float difference =
            player.position.x - transform.position.x;

        if (difference > 0.05f && !facingRight)
        {
            Flip();
        }
        else if (difference < -0.05f && facingRight)
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );
    }
}
