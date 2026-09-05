using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject spreadBulletPrefab;
    [SerializeField] private GameObject fireballBulletPrefab;
    [SerializeField] private GameObject laserBulletPrefab;
    [SerializeField] private WeaponSystem weaponSystem;
    [SerializeField] private PlayerController playerController;

    [Header("Spread Gun")]
    [SerializeField] private int spreadBulletCount = 5;
    [SerializeField] private float spreadAngle = 45f;

    [Header("Laser Beam")]
    [SerializeField] private int laserBulletCount = 2;
    [SerializeField] private float laserOffsetSpacing = 1.2f;

    [Header("Crouch Fire")]
    [SerializeField] private float crouchFirePointOffsetY = -0.4f;

    private float nextFireTime;

    private void Awake()
    {
        if (firePoint == null)
        {
            Debug.LogError(
                "PlayerCombat: ยังไม่ได้กำหนด Fire Point"
            );
        }

        if (bulletPrefab == null)
        {
            Debug.LogError(
                "PlayerCombat: ยังไม่ได้กำหนด Bullet Prefab"
            );
        }

        if (spreadBulletPrefab == null)
        {
            Debug.LogError(
                "PlayerCombat: ยังไม่ได้กำหนด Spread Bullet Prefab"
            );
        }

        if (fireballBulletPrefab == null)
        {
            Debug.LogError(
                "PlayerCombat: ยังไม่ได้กำหนด Fireball Bullet Prefab"
            );
        }

        if (laserBulletPrefab == null)
        {
            Debug.LogError(
                "PlayerCombat: ยังไม่ได้กำหนด Laser Bullet Prefab"
            );
        }

        if (weaponSystem == null)
        {
            weaponSystem = GetComponent<WeaponSystem>();
        }

        if (weaponSystem == null)
        {
            Debug.LogError(
                "PlayerCombat: ไม่พบ WeaponSystem"
            );
        }

        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (playerController == null)
        {
            Debug.LogError(
                "PlayerCombat: ไม่พบ PlayerController"
            );
        }
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Z))
        {
            TryShoot();
        }
    }

    private void TryShoot()
    {
        if (weaponSystem == null)
            return;

        if (Time.time < nextFireTime)
            return;

        if (firePoint == null)
            return;

        nextFireTime =
            Time.time + weaponSystem.CurrentFireRate;

        Vector2 direction =
            GetShootDirection();

        switch (weaponSystem.CurrentWeapon)
        {
            case WeaponType.SpreadGun:
                ShootSpread(direction);
                break;

            case WeaponType.FireBall:
                ShootFireball(direction);
                break;

            case WeaponType.Laser:
                ShootLaser(direction);
                break;

            default:
                ShootNormal(direction);
                break;
        }
    }

    private void ShootNormal(Vector2 direction)
    {
        if (bulletPrefab == null)
            return;

        GameObject bulletObject =
            Instantiate(
                bulletPrefab,
                GetFirePointPosition(),
                Quaternion.identity
            );

        Bullet bullet =
            bulletObject.GetComponent<Bullet>();

        if (bullet == null)
        {
            Debug.LogError(
                "PlayerCombat: Bullet Prefab ไม่มี Bullet.cs"
            );

            Destroy(bulletObject);
            return;
        }

        bullet.Initialize(direction);
    }

    private void ShootSpread(Vector2 direction)
    {
        if (spreadBulletPrefab == null)
            return;

        if (spreadBulletCount < 1)
            return;

        if (spreadBulletCount == 1)
        {
            SpawnSpreadBullet(direction);
            return;
        }

        float startAngle =
            -spreadAngle;

        float angleStep =
            (spreadAngle * 2f) /
            (spreadBulletCount - 1);

        for (int i = 0;
             i < spreadBulletCount;
             i++)
        {
            float angle =
                startAngle +
                angleStep * i;

            Vector2 spreadDirection =
                RotateDirection(
                    direction,
                    angle
                );

            SpawnSpreadBullet(
                spreadDirection
            );
        }
    }

    private void SpawnSpreadBullet(
        Vector2 direction)
    {
        GameObject bulletObject =
            Instantiate(
                spreadBulletPrefab,
                GetFirePointPosition(),
                Quaternion.identity
            );

        SpreadBullet bullet =
            bulletObject.GetComponent<SpreadBullet>();

        if (bullet == null)
        {
            Debug.LogError(
                "PlayerCombat: SpreadBullet Prefab ไม่มี SpreadBullet.cs"
            );

            Destroy(bulletObject);
            return;
        }

        bullet.Initialize(direction);
    }

    private void ShootFireball(Vector2 direction)
    {
        if (fireballBulletPrefab == null)
            return;

        GameObject bulletObject =
            Instantiate(
                fireballBulletPrefab,
                GetFirePointPosition(),
                Quaternion.identity
            );

        FireballBullet bullet =
            bulletObject.GetComponent<FireballBullet>();

        if (bullet == null)
        {
            Debug.LogError(
                "PlayerCombat: Fireball Bullet Prefab ไม่มี FireballBullet.cs"
            );

            Destroy(bulletObject);
            return;
        }

        bullet.Initialize(direction);
    }

    private void ShootLaser(Vector2 direction)
    {
        if (laserBulletPrefab == null)
            return;

        if (laserBulletCount < 1)
            return;

        if (laserBulletCount == 1)
        {
            SpawnLaserBullet(direction, GetFirePointPosition());
            return;
        }

        // ยิงหลายนัดต่อท้ายกันเป็นแถวยาว เรียงตามแนวทิศยิง (นัดหลังอยู่ด้านหลังนัดหน้า)
        for (int i = 0;
             i < laserBulletCount;
             i++)
        {
            float offset =
                -laserOffsetSpacing * i;

            Vector2 spawnPosition =
                GetFirePointPosition() +
                direction * offset;

            SpawnLaserBullet(direction, spawnPosition);
        }
    }

    private void SpawnLaserBullet(
        Vector2 direction,
        Vector2 position)
    {
        GameObject bulletObject =
            Instantiate(
                laserBulletPrefab,
                position,
                Quaternion.identity
            );

        LaserBullet bullet =
            bulletObject.GetComponent<LaserBullet>();

        if (bullet == null)
        {
            Debug.LogError(
                "PlayerCombat: Laser Bullet Prefab ไม่มี LaserBullet.cs"
            );

            Destroy(bulletObject);
            return;
        }

        bullet.Initialize(direction);
    }

    private Vector2 RotateDirection(
        Vector2 direction,
        float angle)
    {
        float radians =
            angle * Mathf.Deg2Rad;

        float cos =
            Mathf.Cos(radians);

        float sin =
            Mathf.Sin(radians);

        return new Vector2(
            direction.x * cos -
            direction.y * sin,

            direction.x * sin +
            direction.y * cos
        ).normalized;
    }

    private Vector2 GetShootDirection()
    {
        float horizontalInput =
            Input.GetAxisRaw("Horizontal");

        float verticalInput =
            Input.GetAxisRaw("Vertical");

        bool isGrounded =
            playerController != null &&
            playerController.IsGrounded;

        bool isCrouching =
            playerController != null &&
            playerController.IsCrouching;

        Vector2 direction = Vector2.zero;

        if (horizontalInput > 0.5f)
        {
            direction.x = 1f;
        }
        else if (horizontalInput < -0.5f)
        {
            direction.x = -1f;
        }

        // คลาน (Crouch) ยิงได้แค่แนวนอนเท่านั้น ยิงขึ้น/ลงไม่ได้
        if (isCrouching)
        {
            if (direction == Vector2.zero)
            {
                direction = FacingDirection();
            }

            return direction.normalized;
        }

        if (verticalInput > 0.5f)
        {
            direction.y = 1f;
        }
        else if (verticalInput < -0.5f)
        {
            // ยิงทะแยงลง (มีทิศซ้าย/ขวาร่วมด้วย) ยิงได้ตลอด แม้จะเดินอยู่บนพื้น
            // ยิงลงตรงๆ (ไม่มีทิศซ้าย/ขวา) ได้เฉพาะตอนกระโดด (ลอยตัวอยู่กลางอากาศ) เท่านั้น
            bool isDiagonal = direction.x != 0f;

            if (!isGrounded || isDiagonal)
            {
                direction.y = -1f;
            }
        }

        if (direction == Vector2.zero)
        {
            direction = FacingDirection();
        }

        return direction.normalized;
    }

    private Vector2 FacingDirection()
    {
        return transform.localScale.x >= 0f
            ? Vector2.right
            : Vector2.left;
    }

    private Vector2 GetFirePointPosition()
    {
        Vector2 position = firePoint.position;

        bool isCrouching =
            playerController != null &&
            playerController.IsCrouching;

        if (isCrouching)
        {
            position.y += crouchFirePointOffsetY;
        }

        return position;
    }
}