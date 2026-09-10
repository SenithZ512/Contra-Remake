using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("Shooting")]
    [SerializeField] private GameObject enemyBulletPrefab;
    [SerializeField] private float fireRate = 1.5f;
    [SerializeField] private float firstShotDelay = 0.5f;

    [Header("Fire Point")]
    [SerializeField] private Vector2 firePointOffset = new Vector2(0.55f, 1f);

    [Header("Aiming")]
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private bool aimAtPlayer = true;
    [SerializeField] private float aimHeightOffset = 0.9f;

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

        Vector2 firePosition = GetFirePosition();

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

        bullet.Initialize(GetShootDirection(firePosition));
    }

    private Vector2 GetFirePosition()
    {
        float sign = facingRight ? 1f : -1f;

        return (Vector2)transform.position +
            new Vector2(
                firePointOffset.x * sign,
                firePointOffset.y
            );
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
                return toPlayer.normalized;
            }
        }

        return facingRight ? Vector2.right : Vector2.left;
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
