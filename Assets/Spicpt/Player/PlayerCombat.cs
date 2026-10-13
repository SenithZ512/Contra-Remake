using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
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
    [SerializeField] private float laserOffsetSpacing = 1.6f;

    [Header("ปากกระบอกปืน (วัดจากเท้า เลข x เป็นตอนหันขวา)")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    /// <summary>ย่อ/ขยายตัวละครแล้วปากกระบอกเลื่อนตามเอง ไม่ต้องวัดใหม่</summary>
    [SerializeField] private bool autoScaleMuzzle = true;

    /// <summary>ความสูงของสไปรท์ตอนที่นั่งวัดค่าข้างล่างไว้ = 1080 px ÷ PPU 410</summary>
    [SerializeField] private float referenceSpriteHeight = 2.6341f;

    [SerializeField] private Vector2 muzzleHorizontal = new Vector2(1.03f, 1.42f);
    [SerializeField] private Vector2 muzzleDiagonalUp = new Vector2(0.84f, 2.25f);
    [SerializeField] private Vector2 muzzleUp = new Vector2(0.84f, 2.25f);
    [SerializeField] private Vector2 muzzleDiagonalDown = new Vector2(0.87f, 1.04f);
    [SerializeField] private Vector2 muzzleDown = new Vector2(0.25f, 0.7f);
    [SerializeField] private Vector2 muzzleCrouch = new Vector2(1f, 0.55f);

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private KeyCode fireKey = KeyCode.Z;

    /// <summary>
    /// อาวุธที่กดค้างแล้วยิงรัวได้ นอกรายการนี้ต้องกดทีละนัด
    /// ตรงกับต้นฉบับที่มีแค่ Machine Gun ยิงอัตโนมัติ
    /// </summary>
    [SerializeField] private WeaponType[] autoFireWeapons =
    {
        WeaponType.MachineGun
    };

    /// <summary>
    /// กดลูกศรขึ้น/เฉียงแล้วเข้าท่าเล็งเลย ไม่ต้องกดปุ่มยิง
    /// แบบต้นฉบับที่เงยปืนขึ้นได้โดยไม่ต้องยิงออกมา
    /// </summary>
    [SerializeField] private bool poseOnAimInput = true;

    /// <summary>ค้างท่ายิงไว้อีกนิดหลังปล่อยปุ่ม ไม่งั้นท่าจะกระพริบ</summary>
    [SerializeField] private float firePoseHold = 0.2f;

    private float nextFireTime;
    private Vector2 currentShootDirection = Vector2.right;
    private float firePoseUntil;
    private float aimY;

    private void Awake()
    {
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

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Update()
    {
        bool fireHeld = Input.GetKey(fireKey);

        // กดลูกศรแนวตั้งค้างไว้ก็เข้าท่าเล็งแล้ว ยังไม่ต้องยิง
        // กดลงเฉยๆ ไม่นับ เพราะนั่นคือคลาน ซึ่งมีท่าของตัวเองอยู่แล้ว
        bool aiming = poseOnAimInput &&
            Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.5f;

        // ท่าทางยึดตามการ "กดค้าง" เสมอ กดค้างเล็งอยู่ก็ยังเป็นท่ายิง
        // ถ้าผูกท่ากับจังหวะยิงจริง ปืนกดทีละนัดจะเห็นท่ากระพริบไปมา
        if (fireHeld || aiming)
        {
            aimY = GetShootDirection().y;
            firePoseUntil = Time.time + firePoseHold;
        }

        // ส่วนกระสุนออกจริงยังแยกตามชนิดปืน
        bool firePressed = IsAutoFireWeapon()
            ? fireHeld
            : Input.GetKeyDown(fireKey);

        if (firePressed)
        {
            TryShoot();
        }

        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        bool isFiring = Time.time < firePoseUntil;

        animator.SetBool("IsFiring", isFiring);
        animator.SetFloat("AimY", isFiring ? aimY : 0f);
    }

    private bool IsAutoFireWeapon()
    {
        if (weaponSystem == null || autoFireWeapons == null)
            return false;

        foreach (WeaponType type in autoFireWeapons)
        {
            if (type == weaponSystem.CurrentWeapon)
                return true;
        }

        return false;
    }

    private void TryShoot()
    {
        if (weaponSystem == null)
            return;

        if (Time.time < nextFireTime)
            return;

        nextFireTime =
            Time.time + weaponSystem.CurrentFireRate;

        Vector2 direction =
            GetShootDirection();

        // ต้องเก็บไว้ก่อน เพราะ GetFirePointPosition() ใช้ทิศนี้เลือกปากกระบอก
        currentShootDirection = direction;

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

        // ยิงหลายนัดต่อท้ายกันเป็นแถวยาว เรียงออกไปข้างหน้าตามแนวทิศยิง
        // ห้ามเรียงถอยหลัง ไม่งั้นตอนยิงขึ้น นัดท้ายจะไปโผล่ใต้เท้า
        // เพราะปากกระบอกตอนเล็งขึ้นสูงจากเท้าแค่ 1.1 แต่แถวกระสุนยาวกว่านั้น
        for (int i = 0;
             i < laserBulletCount;
             i++)
        {
            // คูณสเกลตัวละคร ย่อขยายตัวแล้วระยะเรียงตามให้เอง
            float offset =
                laserOffsetSpacing * i * GetMuzzleScale();

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

    /// <summary>
    /// ปากกระบอกปืนย้ายตามท่าที่กำลังเล่นอยู่ ค่าทุกตัววัดจากสไปรท์จริง
    /// จุดอ้างอิงคือ transform.position ซึ่งอยู่ที่เท้า
    /// </summary>
    private Vector2 GetFirePointPosition()
    {
        Vector2 offset = GetMuzzleOffset() * GetMuzzleScale();

        // หันซ้ายก็สะท้อนแกน x เอา ไม่ต้องตั้งค่าซ้ำ
        if (transform.localScale.x < 0f)
        {
            offset.x = -offset.x;
        }

        return (Vector2)transform.position + offset;
    }

    /// <summary>
    /// เทียบความสูงสไปรท์ตอนนี้กับตอนที่วัดค่าไว้ แล้วย่อระยะตามส่วน
    /// ใช้ sprite.bounds ไม่ใช่ renderer.bounds เพราะตอนกระโดดตัวหมุน
    /// กรอบของ renderer จะบานออก ค่าจะเพี้ยน
    /// </summary>
    private float GetMuzzleScale()
    {
        if (!autoScaleMuzzle)
            return 1f;

        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return 1f;

        if (referenceSpriteHeight <= 0.0001f)
            return 1f;

        float height =
            spriteRenderer.sprite.bounds.size.y *
            Mathf.Abs(spriteRenderer.transform.lossyScale.y);

        return height / referenceSpriteHeight;
    }

    private Vector2 GetMuzzleOffset()
    {
        bool isCrouching =
            playerController != null &&
            playerController.IsCrouching;

        if (isCrouching)
            return muzzleCrouch;

        Vector2 direction = currentShootDirection;

        bool hasHorizontal = Mathf.Abs(direction.x) > 0.01f;

        if (direction.y > 0.5f)
            return hasHorizontal ? muzzleDiagonalUp : muzzleUp;

        if (direction.y < -0.5f)
            return hasHorizontal ? muzzleDiagonalDown : muzzleDown;

        return muzzleHorizontal;
    }

    private void OnDrawGizmosSelected()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        float sign = transform.localScale.x < 0f ? -1f : 1f;
        float scale = GetMuzzleScale();

        Vector2[] points =
        {
            muzzleHorizontal,
            muzzleDiagonalUp,
            muzzleUp,
            muzzleDiagonalDown,
            muzzleDown,
            muzzleCrouch
        };

        foreach (Vector2 point in points)
        {
            Vector3 world = transform.position +
                new Vector3(point.x * sign, point.y, 0f) * scale;

            Gizmos.DrawWireSphere(world, 0.08f);
        }
    }
}