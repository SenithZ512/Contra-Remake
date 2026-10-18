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
    /// <summary>ต้นฉบับยิงครั้งละ 5 นัดเสมอ</summary>
    [SerializeField] private int spreadBulletCount = 5;

    /// <summary>
    /// องศาระหว่างนัดที่ติดกัน
    ///
    /// ต้นฉบับเก็บทิศกระสุนเป็นตาราง 32 ช่องรอบวงกลม = ช่องละ 11.25 องศา
    /// แล้ว S weapon หยิบช่อง -2, -1, 0, +1, +2 จากทิศที่เล็งอยู่
    /// (ตาราง s_bullet_num_index_modifier_tbl = $00,$01,$ff,$02,$fe)
    ///
    /// 5 นัด x 11.25 = กางรวม 45 องศา
    /// ของเดิมผมตั้ง 45 เป็น "ครึ่งหนึ่ง" ของพัด เลยกางเป็น 90 องศา กว้างเกินเท่าตัว
    /// </summary>
    [SerializeField] private float spreadAngleStep = 11.25f;

    [Header("On-Screen Bullet Limit (หัวใจของความรู้สึกแบบ Contra)")]
    /// <summary>
    /// ต้นฉบับไม่ได้คุมจังหวะยิงด้วยเวลาอย่างเดียว
    /// แต่คุมด้วย "จำนวนกระสุนที่ค้างอยู่บนจอ" เป็นหลัก
    ///
    /// ยิงใส่ศัตรูใกล้ๆ กระสุนหายเร็ว ก็ยิงรัวต่อได้
    /// ยิงลอยๆ ไปในที่ว่าง กระสุนค้างเต็มโควตา ต้องรอจนมันหมดอายุ
    /// นี่คือจังหวะที่ทำให้ Contra รู้สึกเป็น Contra
    /// </summary>
    [SerializeField] private bool limitBulletsOnScreen = true;

    [SerializeField] private int normalMaxBullets = 4;
    [SerializeField] private int machineGunMaxBullets = 6;
    [SerializeField] private int spreadMaxBullets = 10;
    [SerializeField] private int fireBallMaxBullets = 4;

    /// <summary>
    /// กระสุนที่ออกนอกกล้องถือว่าจบ คืนช่องว่างให้ทันที
    ///
    /// ต้นฉบับลบกระสุนทิ้งทันทีที่พ้นขอบจอ ช่องในตารางจึงว่างทันที
    /// ถ้าไม่ทำแบบนี้ กระสุนจะค้างกินโควตาจนหมดอายุ ทั้งที่มองไม่เห็นแล้ว
    /// ผลคือรู้สึกว่ายิงไม่ออกทั้งที่จอโล่ง
    /// </summary>
    [SerializeField] private bool despawnBulletsOffCamera = true;

    /// <summary>เผื่อพ้นขอบจอไปอีกเท่าไหร่ก่อนลบ กันลบตอนยังเห็นขอบกระสุนอยู่</summary>
    [SerializeField] private float offCameraMargin = 0.4f;

    [Header("Machine Gun (จังหวะกดค้างแบบต้นฉบับ)")]
    /// <summary>
    /// กดค้างแล้วยิงรัว 6 นัด จากนั้นจะมีช่องว่างยาวหนึ่งจังหวะ แล้ววนใหม่
    /// ต้นฉบับนับใน PLAYER_M_WEAPON_FIRE_TIME: ยิงทุก 8 เฟรม
    /// พอครบ 6 นัด ($60) เกณฑ์เปลี่ยนเป็น 15 เฟรม แล้วรีเซ็ตที่ $70
    ///
    /// ผลคือ "แตะรัวๆ เร็วกว่ากดค้าง" เพราะปล่อยปุ่มแล้วตัวนับรีเซ็ต
    /// </summary>
    [SerializeField] private bool machineGunBurstRhythm = true;

    [SerializeField] private int machineGunBurstLength = 6;

    /// <summary>ช่องว่างหลังครบชุด 16 เฟรม</summary>
    [SerializeField] private float machineGunBurstPause = 0.27f;

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
        PruneBullets();

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

        // ปล่อยปุ่มแล้วตัวนับชุดของปืนกลรีเซ็ต
        // นี่คือเหตุผลที่แตะรัวๆ ยิงได้เร็วกว่ากดค้างในต้นฉบับ
        if (!fireHeld)
        {
            machineGunShotsInBurst = 0;
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

    /// <summary>กระสุนที่ยิงออกไปแล้วและยังไม่หายไปจากจอ</summary>
    private readonly System.Collections.Generic.List<GameObject> liveBullets =
        new System.Collections.Generic.List<GameObject>();

    private int machineGunShotsInBurst;

    /// <summary>ยังมีที่ว่างให้กระสุนนัดใหม่ไหม</summary>
    private bool HasBulletRoom()
    {
        if (!limitBulletsOnScreen)
            return true;

        return liveBullets.Count < GetBulletLimit();
    }

    /// <summary>
    /// เก็บกวาดรายการกระสุนทุกเฟรม
    /// ตัวที่โดนทำลายไปแล้วเอาออก ตัวที่พ้นขอบจอไปก็ลบทิ้งแล้วคืนช่อง
    /// </summary>
    private void PruneBullets()
    {
        // Unity คืนค่า null ให้ออบเจกต์ที่ถูกทำลายแล้ว ใช้กวาดทิ้งได้เลย
        liveBullets.RemoveAll(bullet => bullet == null);

        if (!despawnBulletsOffCamera)
            return;

        Camera view = Camera.main;

        if (view == null || !view.orthographic)
            return;

        // ใช้ขอบที่มองเห็นจริง ถ้าล็อกจอเป็น 4:3 ไว้ aspect ก็จะเป็น 4:3 ตามนั้น
        float halfHeight = view.orthographicSize + offCameraMargin;
        float halfWidth = view.orthographicSize * view.aspect + offCameraMargin;

        Vector2 center = view.transform.position;

        for (int i = liveBullets.Count - 1; i >= 0; i--)
        {
            Vector3 spot = liveBullets[i].transform.position;

            if (Mathf.Abs(spot.x - center.x) <= halfWidth &&
                Mathf.Abs(spot.y - center.y) <= halfHeight)
            {
                continue;
            }

            Destroy(liveBullets[i]);

            liveBullets.RemoveAt(i);
        }
    }

    private int GetBulletLimit()
    {
        switch (weaponSystem.CurrentWeapon)
        {
            case WeaponType.MachineGun: return machineGunMaxBullets;
            case WeaponType.SpreadGun: return spreadMaxBullets;
            case WeaponType.FireBall: return fireBallMaxBullets;

            // เลเซอร์ไม่คิดโควตา เพราะยิงนัดใหม่แล้วนัดเก่าหายไปเองอยู่แล้ว
            case WeaponType.Laser: return int.MaxValue;

            default: return normalMaxBullets;
        }
    }

    private void RegisterBullet(GameObject bullet)
    {
        if (limitBulletsOnScreen && bullet != null)
        {
            liveBullets.Add(bullet);
        }
    }

    /// <summary>
    /// จังหวะกดค้างของปืนกล
    /// ครบชุดแล้วเว้นช่องว่างยาวหนึ่งที ปล่อยปุ่มเมื่อไหร่ตัวนับรีเซ็ต
    /// </summary>
    private float GetFireDelay()
    {
        float rate = weaponSystem.CurrentFireRate;

        if (!machineGunBurstRhythm ||
            weaponSystem.CurrentWeapon != WeaponType.MachineGun)
        {
            return rate;
        }

        machineGunShotsInBurst++;

        if (machineGunShotsInBurst < machineGunBurstLength)
            return rate;

        machineGunShotsInBurst = 0;

        return machineGunBurstPause;
    }

    private void TryShoot()
    {
        if (weaponSystem == null)
            return;

        if (Time.time < nextFireTime)
            return;

        // เต็มโควตาแล้วก็ยังกดไม่ออก ต้องรอให้กระสุนเก่าหายไปก่อน
        if (!HasBulletRoom())
            return;

        nextFireTime = Time.time + GetFireDelay();

        // เล่นเสียงครั้งเดียวต่อการกด ไม่ใช่ต่อกระสุน
        // ปืนกระจายยิงทีละ 5 นัด ถ้าเล่นทุกนัดจะดังซ้อน 5 เท่า
        GameAudio.PlayerShot(weaponSystem.CurrentWeapon);

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

        RegisterBullet(bulletObject);
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

        // กางออกจากนัดกลางข้างละเท่าๆ กัน ช่องละ spreadAngleStep องศา
        // 5 นัดที่ 11.25 องศา = -22.5, -11.25, 0, +11.25, +22.5
        float startAngle =
            -spreadAngleStep * (spreadBulletCount - 1) * 0.5f;

        for (int i = 0;
             i < spreadBulletCount;
             i++)
        {
            // เต็มโควตากลางพัดก็หยุด ต้นฉบับก็ออกไม่ครบชุดเหมือนกัน
            if (!HasBulletRoom())
                return;

            float angle =
                startAngle +
                spreadAngleStep * i;

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

        RegisterBullet(bulletObject);
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

        RegisterBullet(bulletObject);
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