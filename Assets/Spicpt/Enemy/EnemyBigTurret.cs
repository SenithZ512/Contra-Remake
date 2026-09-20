using System.Collections;
using UnityEngine;

/// <summary>
/// ป้อมปืนใหญ่แบบ Contra — ปิดฝาอยู่ยิงไม่เข้า พอเปิดฝาถึงจะโดนยิงได้
/// แต่ตอนเปิดมันก็จะยิงกระสุนเป็นพัดสวนออกมาด้วย ต้องเลือกจังหวะเข้าทำ
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class EnemyBigTurret : MonoBehaviour
{
    [Header("Shooting")]
    [SerializeField] private GameObject enemyBulletPrefab;
    [SerializeField] private int bulletsPerShot = 5;
    [SerializeField] private float spreadAngle = 40f;
    [SerializeField] private int shotsWhileOpen = 2;
    [SerializeField] private float shotInterval = 0.7f;

    [Header("Open / Close")]
    [SerializeField] private float closedDuration = 2f;
    [SerializeField] private float openWarmUp = 0.5f;
    [SerializeField] private float openHold = 1.5f;
    [SerializeField] private Color closedColor = new Color(0.35f, 0.35f, 0.4f);
    [SerializeField] private Color openColor = new Color(1f, 0.45f, 0.2f);

    [Header("Aiming")]
    [SerializeField] private float detectionRange = 16f;
    [SerializeField] private float aimHeightOffset = 0.9f;
    [SerializeField] private Vector2 facingDirection = Vector2.left;
    [SerializeField] private float maxAimAngle = 80f;
    [SerializeField] private float barrelLength = 1.2f;

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    [Header("Look")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private EnemyHealth health;
    private Transform player;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (enemyBulletPrefab == null)
        {
            Debug.LogError(
                "EnemyBigTurret: ยังไม่ได้กำหนด Enemy Bullet Prefab"
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

        StartCoroutine(CycleRoutine());
    }

    private IEnumerator CycleRoutine()
    {
        while (true)
        {
            SetOpen(false);

            yield return new WaitForSeconds(closedDuration);

            // เปิดฝาแล้วหน่วงนิดหน่อยก่อนยิง เปิดช่องให้ผู้เล่นสวนได้ทัน
            SetOpen(true);

            yield return new WaitForSeconds(openWarmUp);

            for (int i = 0; i < Mathf.Max(1, shotsWhileOpen); i++)
            {
                if (CanSeePlayer())
                {
                    FireSpread();
                }

                yield return new WaitForSeconds(shotInterval);
            }

            // ยิงเสร็จแล้วยังเปิดค้างอีกพัก ให้ผู้เล่นมีเวลาเข้าไปสอย
            yield return new WaitForSeconds(openHold);
        }
    }

    private void SetOpen(bool open)
    {
        if (health != null)
        {
            health.SetInvulnerable(!open);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = open ? openColor : closedColor;
        }
    }

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        if (playerHealth != null && playerHealth.IsDead)
            return false;

        Vector2 toPlayer = GetAimPoint() - GetFirePosition();

        if (toPlayer.magnitude > detectionRange)
            return false;

        return Vector2.Angle(facingDirection, toPlayer) <= maxAimAngle;
    }

    private void FireSpread()
    {
        if (enemyBulletPrefab == null)
            return;

        Vector2 firePosition = GetFirePosition();
        Vector2 aimDirection =
            (GetAimPoint() - firePosition).normalized;

        int count = Mathf.Max(1, bulletsPerShot);

        if (count == 1)
        {
            SpawnBullet(firePosition, aimDirection);
            return;
        }

        float step = (spreadAngle * 2f) / (count - 1);

        for (int i = 0; i < count; i++)
        {
            float angle = -spreadAngle + step * i;

            SpawnBullet(
                firePosition,
                RotateDirection(aimDirection, angle)
            );
        }
    }

    private void SpawnBullet(Vector2 position, Vector2 direction)
    {
        GameObject bulletObject = Instantiate(
            enemyBulletPrefab,
            position,
            Quaternion.identity
        );

        EnemyBullet bullet =
            bulletObject.GetComponent<EnemyBullet>();

        if (bullet == null)
        {
            Debug.LogError(
                "EnemyBigTurret: Enemy Bullet Prefab ไม่มี EnemyBullet.cs"
            );

            Destroy(bulletObject);
            return;
        }

        bullet.Initialize(direction);
    }

    private static Vector2 RotateDirection(Vector2 direction, float angle)
    {
        float radians = angle * Mathf.Deg2Rad;

        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos
        ).normalized;
    }

    private Vector2 GetFirePosition()
    {
        return (Vector2)transform.position +
            facingDirection.normalized * barrelLength;
    }

    private Vector2 GetAimPoint()
    {
        if (player == null)
            return transform.position;

        return (Vector2)player.position +
            Vector2.up * aimHeightOffset;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Vector3 facing = facingDirection.normalized;

        Gizmos.DrawLine(
            transform.position,
            transform.position + facing * detectionRange
        );
    }
}
