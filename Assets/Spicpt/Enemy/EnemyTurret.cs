using System.Collections;
using UnityEngine;

public class EnemyTurret : MonoBehaviour
{
    [Header("Shooting")]
    [SerializeField] private GameObject enemyBulletPrefab;
    [SerializeField] private float fireRate = 2f;
    [SerializeField] private float firstShotDelay = 0.5f;
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float burstInterval = 0.15f;

    [Header("Barrel")]
    [SerializeField] private Transform barrel;
    [SerializeField] private float barrelLength = 0.6f;

    /// <summary>
    /// ขยับปลายกระบอกแบบอิสระ ใช้ตอนไม่ได้ลาก Barrel
    ///
    /// **จำเป็นเพราะ Barrel Length ขยับได้แค่แกนเดียว**
    /// มันเลื่อนไปตามทิศหันเท่านั้น ถ้ากระบอกบนสไปรต์ไม่ได้อยู่ระดับเดียว
    /// กับจุดหมุนของออบเจกต์ จะเลื่อนยังไงก็ไม่มีวันตรง
    ///
    /// นับเป็นพิกัดในตัวออบเจกต์ ย่อขยายหรือพลิกสไปรต์แล้วมันตามให้เอง
    /// </summary>
    [SerializeField] private Vector2 muzzleOffset = Vector2.zero;

    /// <summary>วาดจุดปลายกระบอกใน Scene view ตอนเลือกป้อม</summary>
    [SerializeField] private bool showMuzzleGizmo = true;

    [Header("Aiming")]
    [SerializeField] private float detectionRange = 14f;
    [SerializeField] private float aimHeightOffset = 0.9f;
    [SerializeField] private Vector2 facingDirection = Vector2.left;
    [SerializeField] private float maxAimAngle = 80f;

    /// <summary>ยิงได้แค่ 8 ทิศ ไม่เล็งองศาอิสระ</summary>
    [SerializeField] private bool useEightDirections = true;

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    private Transform player;
    private PlayerHealth playerHealth;
    private float nextFireTime;
    private bool isFiring;

    private void Awake()
    {
        if (enemyBulletPrefab == null)
        {
            Debug.LogError(
                "EnemyTurret: ยังไม่ได้กำหนด Enemy Bullet Prefab"
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
        if (!CanSeePlayer())
            return;

        AimBarrel();

        if (isFiring || Time.time < nextFireTime)
            return;

        nextFireTime = Time.time + fireRate;

        StartCoroutine(FireBurst());
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

        // ป้อมติดกำแพง/พื้น ยิงได้แค่ในกรวยที่หันออกเท่านั้น ไม่ยิงทะลุของที่มันเกาะอยู่
        return Vector2.Angle(facingDirection, toPlayer) <= maxAimAngle;
    }

    private IEnumerator FireBurst()
    {
        isFiring = true;

        for (int i = 0; i < Mathf.Max(1, burstCount); i++)
        {
            // เล็งใหม่ทุกนัด ผู้เล่นที่วิ่งหนีจะได้ยังโดนไล่ยิง
            if (!CanSeePlayer())
                break;

            Shoot();

            yield return new WaitForSeconds(burstInterval);
        }

        isFiring = false;
    }

    private void Shoot()
    {
        if (enemyBulletPrefab == null)
            return;

        Vector2 firePosition = GetFirePosition();
        Vector2 direction =
            Snap((GetAimPoint() - firePosition).normalized);

        GameObject bulletObject = Instantiate(
            enemyBulletPrefab,
            firePosition,
            Quaternion.identity
        );

        EnemyBullet bullet =
            bulletObject.GetComponent<EnemyBullet>();

        if (bullet == null)
        {
            Debug.LogError(
                "EnemyTurret: Enemy Bullet Prefab ไม่มี EnemyBullet.cs"
            );

            Destroy(bulletObject);
            return;
        }

        bullet.Initialize(direction);
    }

    private void AimBarrel()
    {
        if (barrel == null)
            return;

        Vector2 direction = GetAimPoint() - (Vector2)barrel.position;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        direction = Snap(direction.normalized);

        float angle = Mathf.Atan2(
            direction.y,
            direction.x
        ) * Mathf.Rad2Deg;

        barrel.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>
    /// ปัดมุมลงช่องละ 45 องศา เหลือ 8 ทิศ (แนวนอน แนวตั้ง และเฉียง)
    /// กติกาเดียวกับผู้เล่นและทหารเดินดิน
    /// </summary>
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

    private Vector2 GetFirePosition()
    {
        if (barrel != null)
        {
            return (Vector2)barrel.position +
                (Vector2)(barrel.right * barrelLength);
        }

        return (Vector2)transform.position +
            facingDirection.normalized * barrelLength +
            (Vector2)transform.TransformVector(muzzleOffset);
    }

    private Vector2 GetAimPoint()
    {
        if (player == null)
            return transform.position;

        // จุดหมุนสไปรท์ผู้เล่นอยู่ที่เท้า ต้องเล็งสูงขึ้นมากลางลำตัว
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

        if (!showMuzzleGizmo)
            return;

        // จุดที่กระสุนเกิดจริง ไม่ใช่จุดหมุนของป้อม
        Vector3 spot = GetFirePosition();

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(spot, 0.12f);

        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawLine(transform.position, spot);

        Gizmos.color = Color.white;
    }
}
