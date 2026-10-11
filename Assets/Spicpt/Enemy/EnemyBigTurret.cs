using System.Collections;
using UnityEngine;

/// <summary>
/// ป้อมปืนใหญ่แบบ Contra — ปิดฝาอยู่ยิงไม่เข้า พอเปิดฝาถึงจะโดนยิงได้
/// แต่ตอนเปิดมันก็จะยิงกระสุนเป็นพัดสวนออกมาด้วย ต้องเลือกจังหวะเข้าทำ
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class EnemyBigTurret : MonoBehaviour
{
    public enum FireMode
    {
        /// <summary>ยิงเป็นชุดเล็งตรงไปที่ผู้เล่น เหมือน EnemyTurret ทุกอย่าง</summary>
        Burst,

        /// <summary>ยิงลูกระเบิดวิถีโค้งตามมุมที่ตั้ง ใช้กับป้อมบอส</summary>
        Lobbed
    }

    [Header("Fire Mode")]
    [SerializeField] private FireMode fireMode = FireMode.Burst;

    [Header("Burst (โหมด Burst เท่านั้น)")]
    /// <summary>เว้นระหว่างชุด เหมือน Fire Rate ของ EnemyTurret</summary>
    [SerializeField] private float fireRate = 2.8f;

    /// <summary>หน่วงก่อนเริ่มรอบแรก กันป้อมทุกตัวในจอยิงพร้อมกันเป๊ะ</summary>
    [SerializeField] private float firstShotDelay = 0.5f;

    /// <summary>จำนวนนัดในหนึ่งชุด</summary>
    [SerializeField] private int burstCount = 3;

    /// <summary>เว้นระหว่างนัดในชุดเดียวกัน</summary>
    [SerializeField] private float burstInterval = 0.15f;

    /// <summary>เปิดฝาแล้วยิงเป็นชุดต่อเนื่องนานเท่านี้ ก่อนเข้าช่วง Open Hold</summary>
    [SerializeField] private float openFireDuration = 6f;

    [Header("Shooting")]
    [SerializeField] private GameObject enemyBulletPrefab;
    [SerializeField] private int bulletsPerShot = 5;
    [SerializeField] private float spreadAngle = 40f;
    [SerializeField] private int shotsWhileOpen = 2;
    [SerializeField] private float shotInterval = 0.7f;

    [Header("Random (สุ่มให้จับทางยาก)")]
    /// <summary>สุ่มจำนวนนัดต่อรอบ แทนการใช้ Shots While Open ตายตัว</summary>
    [SerializeField] private bool randomizeShotCount = true;
    [SerializeField] private int minShots = 3;
    [SerializeField] private int maxShots = 7;

    /// <summary>สุ่มมุมยิงอิสระ แทนการวนตาม Launch Angles</summary>
    [SerializeField] private bool randomizeLaunchAngle = true;
    [SerializeField] private Vector2 launchAngleRange = new Vector2(10f, 45f);

    /// <summary>คูณความเร็วกระสุน ทำให้ระยะตกต่างกันไปในแต่ละนัด</summary>
    [SerializeField] private Vector2 speedMultiplierRange = new Vector2(0.85f, 1.2f);

    [Header("Open / Close")]
    [SerializeField] private float closedDuration = 2f;
    [SerializeField] private float openWarmUp = 0.5f;
    [SerializeField] private float openHold = 1.5f;
    [SerializeField] private Color closedColor = new Color(0.35f, 0.35f, 0.4f);
    [SerializeField] private Color openColor = new Color(1f, 0.45f, 0.2f);
    [SerializeField] private Color openPulseColor = new Color(1f, 0.95f, 0.5f);
    [SerializeField] private float pulseSpeed = 10f;

    [Header("Aiming")]
    [SerializeField] private bool aimAtPlayer = true;
    [SerializeField] private float[] launchAngles = { 20f, 35f, 12f, 45f };
    [SerializeField] private float detectionRange = 16f;
    [SerializeField] private float aimHeightOffset = 0.9f;
    [SerializeField] private Vector2 facingDirection = Vector2.left;
    [SerializeField] private float maxAimAngle = 80f;
    [SerializeField] private float barrelLength = 1.2f;

    /// <summary>ยิงได้แค่ 8 ทิศ ไม่เล็งองศาอิสระ</summary>
    [SerializeField] private bool useEightDirections = true;

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    [Header("Look")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private EnemyHealth health;
    private Transform player;
    private PlayerHealth playerHealth;
    private bool isOpen;
    private int shotIndex;

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
        if (firstShotDelay > 0f)
        {
            yield return new WaitForSeconds(firstShotDelay);
        }

        while (true)
        {
            SetOpen(false);

            yield return new WaitForSeconds(closedDuration);

            // เปิดฝาแล้วหน่วงนิดหน่อยก่อนยิง เปิดช่องให้ผู้เล่นสวนได้ทัน
            SetOpen(true);

            yield return new WaitForSeconds(openWarmUp);

            if (fireMode == FireMode.Burst)
            {
                // ยิงเป็นชุดซ้ำๆ ทุก fireRate ตลอดช่วงที่เปิดฝา
                // จังหวะเดียวกับ EnemyTurret แค่ทำได้เฉพาะตอนฝาเปิด
                float stopAt = Time.time + openFireDuration;

                while (Time.time < stopAt)
                {
                    yield return StartCoroutine(FireBurst());

                    yield return new WaitForSeconds(fireRate);
                }
            }
            else
            {
                int shots = randomizeShotCount
                    ? Random.Range(Mathf.Min(minShots, maxShots),
                                   Mathf.Max(minShots, maxShots) + 1)
                    : shotsWhileOpen;

                for (int i = 0; i < Mathf.Max(1, shots); i++)
                {
                    if (CanSeePlayer())
                    {
                        FireSpread();
                    }

                    yield return new WaitForSeconds(shotInterval);
                }
            }

            // ยิงเสร็จแล้วยังเปิดค้างอีกพัก ให้ผู้เล่นมีเวลาเข้าไปสอย
            yield return new WaitForSeconds(openHold);
        }
    }

    private void SetOpen(bool open)
    {
        isOpen = open;

        if (health != null)
        {
            health.SetInvulnerable(!open);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = open ? openColor : closedColor;
        }
    }

    private void Update()
    {
        if (!isOpen || spriteRenderer == null)
            return;

        // กะพริบตอนเปิดฝา ให้เห็นชัดว่าช่วงนี้แหละที่ยิงเข้า
        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;

        spriteRenderer.color = Color.Lerp(openColor, openPulseColor, pulse);
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

        // แบบไม่ล็อคเป้า ขอแค่ผู้เล่นเข้ามาในระยะก็สาดออกไปตามมุมที่ตั้งไว้
        if (!aimAtPlayer)
            return true;

        return Vector2.Angle(facingDirection, toPlayer) <= maxAimAngle;
    }

    /// <summary>มุมยิงคงที่ วนไปทีละมุมทุกนัด ค่าบวก = เชิดขึ้นเสมอ</summary>
    private Vector2 GetLaunchDirection()
    {
        Vector2 baseDirection = facingDirection.normalized;

        float elevation;

        if (randomizeLaunchAngle)
        {
            elevation = Random.Range(
                Mathf.Min(launchAngleRange.x, launchAngleRange.y),
                Mathf.Max(launchAngleRange.x, launchAngleRange.y)
            );
        }
        else
        {
            if (launchAngles == null || launchAngles.Length == 0)
                return baseDirection;

            elevation = launchAngles[shotIndex % launchAngles.Length];

            shotIndex++;
        }

        Vector2 up = RotateDirection(baseDirection, elevation);
        Vector2 down = RotateDirection(baseDirection, -elevation);

        return up.y >= down.y ? up : down;
    }

    /// <summary>
    /// ยกโครงมาจาก EnemyTurret ทั้งดุ้น เล็งใหม่ทุกนัด ยิงตรง ไม่กระจาย
    /// ต่างกันแค่ป้อมใหญ่ยิงได้เฉพาะตอนเปิดฝาเท่านั้น
    /// </summary>
    private IEnumerator FireBurst()
    {
        for (int i = 0; i < Mathf.Max(1, burstCount); i++)
        {
            // เล็งใหม่ทุกนัด ผู้เล่นที่วิ่งหนีจะได้ยังโดนไล่ยิง
            if (!CanSeePlayer())
                break;

            Vector2 firePosition = GetFirePosition();

            Vector2 direction =
                Snap((GetAimPoint() - firePosition).normalized);

            SpawnBullet(firePosition, direction);

            yield return new WaitForSeconds(burstInterval);
        }
    }

    private void FireSpread()
    {
        if (enemyBulletPrefab == null)
            return;

        Vector2 firePosition = GetFirePosition();

        Vector2 aimDirection = aimAtPlayer
            ? Snap((GetAimPoint() - firePosition).normalized)
            : GetLaunchDirection();

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

        float speedMultiplier = Random.Range(
            Mathf.Min(speedMultiplierRange.x, speedMultiplierRange.y),
            Mathf.Max(speedMultiplierRange.x, speedMultiplierRange.y)
        );

        bullet.Initialize(direction, speedMultiplier);
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
