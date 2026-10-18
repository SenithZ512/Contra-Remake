using UnityEngine;

/// <summary>
/// ทหารในด่านฐาน สี่แบบตามต้นฉบับ
///
/// พฤติกรรมทุกอย่างในไฟล์นี้ถอดมาจาก ROM ไม่ได้กะเอา
///   Running   ($15) วิ่งผ่านไม่หยุดเลย ยิงเฉพาะตอนอยู่กลางจอ
///   Jumping   ($16) กระโดดเป็นจังหวะ ยิงตอนลงพื้นระหว่างกระโดด
///   Grenadier ($17) ไล่ตามเราในแนวนอน พอตรงกันแล้วหยุดโยนระเบิด
///   Group     ($18) ออกมาสี่คนพร้อมกัน วิ่ง-หยุดยิง-แยกทาง
///
/// ทหารแดงไม่ใช่ชนิดแยก แต่เป็นธง isRed บนทหารกระโดด
/// ต้นฉบับแค่สลับพาเลตต์ให้เป็นแดงและข้ามท่อนยิงทิ้ง
/// </summary>
public class BaseSoldier : MonoBehaviour, IBaseDamageable
{
    public enum Kind
    {
        Running,
        Jumping,
        Grenadier,
        Group
    }

    /// <summary>อาวุธของทหารวิ่ง มาจากบิต 1-2 ของ ENEMY_ATTRIBUTES</summary>
    public enum Weapon
    {
        Bullet,
        Grenade,
        Roller
    }

    [Header("Speed (พิกเซลต่อเฟรมของต้นฉบับ หารด้วย 24 แล้ว)")]
    /// <summary>ทหารวิ่ง 0.875 px/เฟรม = 52.5 px/วินาที</summary>
    [SerializeField] private float runnerSpeed = 2.19f;

    /// <summary>ชนิดอื่นทั้งหมด 0.75 px/เฟรม = 45 px/วินาที</summary>
    [SerializeField] private float otherSpeed = 1.875f;

    [Header("Running Soldier")]
    /// <summary>
    /// ยิงได้เฉพาะตอนอยู่ในแถบกลางจอ
    /// ต้นฉบับเช็ค X ระหว่าง $68 ถึง $98 (104-152 พิกเซล) = บวกลบ 1 หน่วย
    /// นอกแถบนี้มันแค่วิ่งผ่านเฉยๆ ไม่ยิงเลย
    /// </summary>
    [SerializeField] private float fireWindowHalfWidth = 1f;

    /// <summary>นัดแรกหลังเกิด 8 เฟรม</summary>
    [SerializeField] private float firstShotDelay = 0.133f;

    /// <summary>นัดต่อไปทุก 16 เฟรม</summary>
    [SerializeField] private float shotInterval = 0.267f;

    [Header("Jumping Soldier")]
    /// <summary>กระโดดสูง 15 พิกเซล ตามผลรวมของ jumping_soldier_y_vel_tbl</summary>
    [SerializeField] private float hopHeight = 0.625f;

    /// <summary>ตารางความเร็วมี 20 ช่อง = 20 เฟรม</summary>
    [SerializeField] private float hopDuration = 0.333f;

    /// <summary>ลงพื้นแล้วหยุด 16 เฟรมก่อนกระโดดใหม่ ยิงตอนกลางช่วงนี้</summary>
    [SerializeField] private float hopPause = 0.267f;

    [Header("Grenadier")]
    /// <summary>
    /// ต่างจากทุกตัวตรงที่มันไล่ตามเรา ตัวอื่นวิ่งทางเดียวจบ
    /// ต้นฉบับแบ่งจอเป็น 7 ช่อง แล้วเทียบว่าอยู่ช่องเดียวกับผู้เล่นหรือยัง
    /// </summary>
    [SerializeField] private float lineUpTolerance = 0.75f;

    /// <summary>ระเบิดห่างกัน 20 เฟรม</summary>
    [SerializeField] private float grenadeInterval = 0.333f;

    /// <summary>โยนกี่ลูกต่อหนึ่งครั้งที่เล็งตรง</summary>
    [SerializeField] private int grenadeBurst = 3;

    /// <summary>พักหลังโยนครบชุด ก่อนไล่ตามใหม่</summary>
    [SerializeField] private float grenadeCooldown = 1.2f;

    [Header("Group of Four")]
    /// <summary>
    /// ตารางเวลาของกลุ่มสี่คน หน่วยเป็นเฟรม แต่ละแถวคือหนึ่งรอบ
    /// แต่ละคอลัมน์คือคนที่เท่าไหร่ในกลุ่ม
    ///
    /// รอบ 0 ยืนสั้นมากแบบเหลื่อมกัน แล้ววิ่งยาว
    /// รอบ 1 ยืนยิงพร้อมกัน แล้วคนที่ 3 กับ 4 กลับหลังหัน
    /// รอบ 2 ยืนยิงอีกที แล้ววิ่งออกจอไปเลย
    /// </summary>
    [SerializeField] private int[] groupStandFrames =
    {
        1, 7, 13, 19,
        24, 24, 24, 24,
        16, 24, 24, 16
    };

    [SerializeField] private int[] groupRunFrames =
    {
        63, 57, 51, 45,
        24, 16, 16, 24,
        255, 255, 255, 255
    };

    /// <summary>รอบที่คนครึ่งหลังของกลุ่มจะกลับหลังหัน</summary>
    [SerializeField] private int groupSplitRound = 1;

    /// <summary>ยิงตอนเหลือเวลายืนอีก 4 เฟรม</summary>
    [SerializeField] private int groupFireAtFramesLeft = 4;

    [Header("Prefabs")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject grenadePrefab;
    [SerializeField] private GameObject rollerPrefab;
    [SerializeField] private GameObject weaponItemPrefab;
    [SerializeField] private GameObject explosionPrefab;

    [Header("Despawn")]
    /// <summary>ต้นฉบับลบทิ้งที่ X = $b0 (176 พิกเซล) = 2 หน่วย</summary>
    [SerializeField] private float despawnX = 2f;

    [Header("Sprites")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] frames;

    /// <summary>ต้นฉบับเปลี่ยนเฟรมทุก 4 เฟรม วน 3 ภาพ = 15 ภาพต่อวินาที</summary>
    [SerializeField] private float frameRate = 15f;

    /// <summary>สีของทหารแดง ต้นฉบับแค่สลับพาเลตต์ ไม่ได้วาดสไปรท์ชุดใหม่</summary>
    [SerializeField] private Color redTint = new Color(1f, 0.35f, 0.3f);

    /// <summary>มีทหารโยนระเบิดอยู่ในจอกี่ตัว เครื่องปล่อยใช้ค่านี้หยุดส่งทหาร</summary>
    public static int GrenadierCount { get; private set; }

    private Kind kind = Kind.Running;
    private Weapon weapon = Weapon.Bullet;
    private WeaponType droppedWeapon = WeaponType.MachineGun;
    private bool dropsRapidFire;
    private bool isRed;
    private int groupIndex;

    private float direction = 1f;
    private float speed;
    private float baseY;
    private float shotTimer;
    private float animTime;
    private bool dead;

    // ทหารกระโดด
    private float hopTimer;
    private bool inAir = true;
    private bool firedThisPause;

    // คนโยนระเบิด
    private float grenadeTimer;
    private int grenadesLeft;
    private float cooldownTimer;

    // กลุ่มสี่คน
    private int groupRound;
    private bool groupStanding = true;
    private float groupTimer;
    private bool groupFired;

    public bool CanBeHit
    {
        get { return !dead; }
    }

    public static void ResetGlobalState()
    {
        GrenadierCount = 0;
    }

    /// <summary>เครื่องปล่อยเรียกทันทีหลังสร้าง ก่อน Start จะทำงาน</summary>
    public void Setup(Kind soldierKind, bool fromLeft, bool red, Weapon gun,
        int indexInGroup, WeaponType drop, bool rapidFire)
    {
        kind = soldierKind;
        isRed = red;
        weapon = gun;
        groupIndex = indexInGroup;
        droppedWeapon = drop;
        dropsRapidFire = rapidFire;

        // ออกจากฝั่งซ้ายก็วิ่งไปขวา ออกจากฝั่งขวาก็วิ่งไปซ้าย
        direction = fromLeft ? 1f : -1f;
    }

    private void Start()
    {
        baseY = transform.position.y;

        speed = kind == Kind.Running ? runnerSpeed : otherSpeed;

        shotTimer = firstShotDelay;
        grenadeTimer = grenadeInterval;
        grenadesLeft = grenadeBurst;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction < 0f;
            spriteRenderer.color = isRed ? redTint : Color.white;
        }

        if (kind == Kind.Group)
        {
            groupTimer = GroupFrames(groupStandFrames, 0, groupIndex);
        }

        if (kind == Kind.Grenadier)
        {
            GrenadierCount++;
        }
    }

    private void OnDestroy()
    {
        if (kind == Kind.Grenadier && GrenadierCount > 0)
        {
            GrenadierCount--;
        }
    }

    private void Update()
    {
        if (dead)
            return;

        switch (kind)
        {
            case Kind.Jumping:
                UpdateJumping();
                break;

            case Kind.Grenadier:
                UpdateGrenadier();
                break;

            case Kind.Group:
                UpdateGroup();
                break;

            default:
                UpdateRunning();
                break;
        }

        Animate();
        DespawnIfOffScreen();
    }

    private void MoveForward(float delta)
    {
        transform.position += Vector3.right * (direction * speed * delta);
    }

    // ---------- ทหารวิ่ง ----------

    /// <summary>
    /// วิ่งผ่านอย่างเดียว ไม่เคยหยุด
    /// ที่ผมเคยทำให้หยุดกลางทางก่อนยิงนั้นผิด ต้นฉบับยิงไปวิ่งไป
    /// </summary>
    private void UpdateRunning()
    {
        MoveForward(Time.deltaTime);

        shotTimer -= Time.deltaTime;

        if (shotTimer > 0f)
            return;

        shotTimer = shotInterval;

        // อยู่นอกแถบกลางจอก็รีเซ็ตเวลาแล้วผ่านไป ไม่ยิง
        if (Mathf.Abs(transform.position.x) > fireWindowHalfWidth)
            return;

        FireRunnerWeapon();
    }

    private void FireRunnerWeapon()
    {
        switch (weapon)
        {
            case Weapon.Grenade:
                // ต้นฉบับโยนเว้นครั้ง ทำให้จังหวะห่างเป็นสองเท่า
                firedThisPause = !firedThisPause;

                if (firedThisPause)
                    return;

                Spawn(grenadePrefab, Vector2.zero);
                return;

            case Weapon.Roller:
                Spawn(rollerPrefab, new Vector2(0f, -8f / 24f));
                return;

            default:
                Spawn(bulletPrefab, Vector2.zero);
                return;
        }
    }

    // ---------- ทหารกระโดด ----------

    private void UpdateJumping()
    {
        MoveForward(Time.deltaTime);

        hopTimer += Time.deltaTime;

        if (inAir)
        {
            float t = hopTimer / hopDuration;

            if (t >= 1f)
            {
                inAir = false;
                hopTimer = 0f;
                firedThisPause = false;

                SetHeight(0f);

                return;
            }

            // เส้นโค้งพาราโบลา ขึ้นสุดกลางทางแล้วลงมาจบที่ระดับเดิม
            SetHeight(4f * hopHeight * t * (1f - t));

            return;
        }

        // ยิงกลางช่วงพัก และทหารแดงไม่ยิงเลย ตามเงื่อนไขบิต 1 ของต้นฉบับ
        if (!firedThisPause && !isRed && hopTimer >= hopPause * 0.5f)
        {
            firedThisPause = true;

            Spawn(bulletPrefab, Vector2.zero);
        }

        if (hopTimer >= hopPause)
        {
            inAir = true;
            hopTimer = 0f;
        }
    }

    private void SetHeight(float height)
    {
        transform.position = new Vector3(
            transform.position.x,
            baseY + height,
            transform.position.z
        );
    }

    // ---------- คนโยนระเบิด ----------

    /// <summary>
    /// ตัวเดียวในด่านที่ไล่ตามผู้เล่น ตัวอื่นวิ่งทางเดียวแล้วออกจอไป
    /// และตราบใดที่มันยังอยู่ เครื่องปล่อยจะไม่ส่งทหารตัวอื่นมาเลย
    /// เพราะงั้นยิ่งปล่อยไว้นาน ห้องยิ่งค้าง
    /// </summary>
    private void UpdateGrenadier()
    {
        BasePlayer player = BaseStage.Instance != null
            ? BaseStage.Instance.Player
            : null;

        if (player == null)
        {
            MoveForward(Time.deltaTime);
            return;
        }

        float gap = player.transform.position.x - transform.position.x;

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;

            Chase(gap);

            return;
        }

        if (Mathf.Abs(gap) > lineUpTolerance)
        {
            Chase(gap);

            return;
        }

        // เล็งตรงแล้ว หยุดโยน
        grenadeTimer -= Time.deltaTime;

        if (grenadeTimer > 0f)
            return;

        grenadeTimer = grenadeInterval;

        Spawn(grenadePrefab, Vector2.zero);

        grenadesLeft--;

        if (grenadesLeft > 0)
            return;

        grenadesLeft = grenadeBurst;
        cooldownTimer = grenadeCooldown;
    }

    private void Chase(float gap)
    {
        direction = Mathf.Sign(gap);

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction < 0f;
        }

        MoveForward(Time.deltaTime);
    }

    // ---------- กลุ่มสี่คน ----------

    private void UpdateGroup()
    {
        groupTimer -= Time.deltaTime * 60f;

        if (groupStanding)
        {
            // ยืนยิงหนึ่งนัดตอนเหลือเวลาอีก 4 เฟรม
            if (!groupFired && groupTimer <= groupFireAtFramesLeft)
            {
                groupFired = true;

                Spawn(bulletPrefab, Vector2.zero);
            }

            if (groupTimer > 0f)
                return;

            // จบช่วงยืนของรอบที่แยกทาง คนครึ่งหลังกลับหลังหัน
            if (groupRound == groupSplitRound && groupIndex >= 2)
            {
                direction = -direction;

                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = direction < 0f;
                }
            }

            groupStanding = false;
            groupTimer = GroupFrames(groupRunFrames, groupRound, groupIndex);

            return;
        }

        MoveForward(Time.deltaTime);

        if (groupTimer > 0f)
            return;

        groupRound++;
        groupStanding = true;
        groupFired = false;
        groupTimer = GroupFrames(groupStandFrames, groupRound, groupIndex);
    }

    private float GroupFrames(int[] table, int round, int index)
    {
        if (table == null || table.Length == 0)
            return 60f;

        int slot = round * 4 + Mathf.Clamp(index, 0, 3);

        // เลยตารางแล้วก็วิ่งยาวไปเลย ไม่หยุดอีก
        if (slot >= table.Length)
            return 255f;

        return table[slot];
    }

    // ---------- ร่วมกัน ----------

    private void Spawn(GameObject prefab, Vector2 offset)
    {
        if (prefab == null)
            return;

        Instantiate(
            prefab,
            (Vector2)transform.position + offset,
            Quaternion.identity
        );
    }

    private void Animate()
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0)
            return;

        animTime += Time.deltaTime;

        int index = Mathf.FloorToInt(animTime * frameRate) % frames.Length;

        spriteRenderer.sprite = frames[index];
    }

    private void DespawnIfOffScreen()
    {
        if (Mathf.Abs(transform.position.x) > despawnX)
        {
            Destroy(gameObject);
        }
    }

    public void TakeHit(int damage, Vector2 from)
    {
        if (dead)
            return;

        dead = true;

        GameAudio.EnemyHit();

        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        if (isRed)
        {
            DropWeapon();
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// ไอเท็มเกิดที่ผนังหลังแล้วลอยเข้าหาเรา
    /// ต้นฉบับก็ให้มันบินมาจากข้างหลังเหมือนกัน ไม่ได้โผล่ตรงหน้า
    /// </summary>
    private void DropWeapon()
    {
        if (weaponItemPrefab == null)
            return;

        GameObject item = Instantiate(
            weaponItemPrefab,
            transform.position,
            Quaternion.identity
        );

        BaseWeaponItem pickup = item.GetComponent<BaseWeaponItem>();

        if (pickup != null)
        {
            pickup.Setup(droppedWeapon, dropsRapidFire);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        BasePlayer player = other.GetComponentInParent<BasePlayer>();

        if (player != null)
        {
            player.Kill();
        }
    }
}
