using UnityEngine;

/// <summary>
/// บอสท้ายด่าน Waterfall - Mechanical Alien (Dragon / Gromaides)
///
/// ตัวนี้คือ "ปาก" ที่อยู่ตรงกลาง ส่วนแขนสองข้างอยู่ใน DragonArm
///
/// วงจรตาม boss_mouth_routine ของ ROM
///   ปิดปากอยู่ (ยิงไม่เข้า) -> อ้าปาก -> ยิงไฟ 3 ทาง -> อ้าค้างไว้ -> หุบ -> วนใหม่
///
/// หัวใจของบอสตัวนี้คือ **ยิ่งเก็บแขนได้ ปากยิ่งดุ**
/// ต้นฉบับเก็บไว้ในตาราง boss_mouth_anim_delay_tbl
///   เหลือ 2 แขน ปากหุบนาน 192 เฟรม
///   เหลือ 1 แขน หุบ 112 เฟรม
///   ไม่เหลือแขน หุบแค่ 32 เฟรม  (ถี่ขึ้น 6 เท่า)
/// และกระสุนก็เร็วขึ้นด้วยเมื่อแขนหมด
///
/// แปลว่าการเก็บแขนไม่ได้ทำให้ง่ายขึ้นเฉยๆ แต่แลกมาด้วยปากที่โผล่ถี่กว่าเดิมมาก
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class DragonBoss : MonoBehaviour
{
    private enum Phase
    {
        Sleeping,
        Opening,
        Open,
        Closing,
        Closed
    }

    // เลือดอยู่ที่ EnemyHealth บนออบเจกต์เดียวกัน ต้นฉบับตั้งไว้ 32 ($20)

    [Header("Intro")]
    /// <summary>หน่วงก่อนอ้าปากครั้งแรก ต้นฉบับใช้ $ff = 255 เฟรม</summary>
    [SerializeField] private float wakeDelay = 4.25f;

    [Header("Mouth Cycle")]
    /// <summary>
    /// อ้าปากเฉพาะตอนแขนพังครบสองข้าง
    ///
    /// **อันนี้ต่างจาก ROM** ต้นฉบับอ้าๆ หุบๆ ตลอดเวลา
    /// แค่หุบนานขึ้นถ้าแขนยังอยู่ (3.2 วิ เหลือ 0.53 วิ เมื่อแขนหมด)
    /// ปิดสวิตช์นี้ถ้าอยากได้พฤติกรรมตามต้นฉบับเป๊ะ
    ///
    /// เปิดไว้จะอ่านง่ายกว่าสำหรับคนเล่น เพราะบังคับลำดับชัดเจน
    /// เก็บแขนสองข้างก่อน แล้วปากถึงจะเปิดให้ยิง
    /// </summary>
    [SerializeField] private bool openOnlyWhenArmless = true;

    /// <summary>เวลาอ้าปากค้าง ต้นฉบับ $70 = 112 เฟรม ยิงเข้าได้เฉพาะช่วงนี้</summary>
    [SerializeField] private float openDuration = 1.87f;

    /// <summary>อนิเมชั่นอ้ากับหุบ ต้นฉบับใช้ $06 เฟรมต่อขั้น</summary>
    [SerializeField] private float shutterDuration = 0.2f;

    /// <summary>
    /// เวลาหุบปาก เรียงตามจำนวนแขนที่เหลือ [2 แขน, 1 แขน, ไม่เหลือ]
    /// ตรงกับ boss_mouth_anim_delay_tbl = $c0, $70, $20
    /// </summary>
    [SerializeField] private float[] closedDurationByArms =
    {
        3.2f,
        1.87f,
        0.53f
    };

    [Header("Fire Breath")]
    [SerializeField] private GameObject fireballPrefab;

    [SerializeField] private Transform muzzle;

    /// <summary>หน่วงหลังอ้าปากก่อนพ่นไฟ ต้นฉบับ $06 เฟรม</summary>
    [SerializeField] private float breathDelay = 0.1f;

    /// <summary>
    /// มุมของไฟทั้งสามลูก วัดจากทิศลงตรง
    /// ตรงกับ mouth_projectile_type_angle = $88, $86, $84 คือช่องมุม 8, 6, 4
    /// ช่องห่างกัน 15 องศา จึงได้ -30, 0, +30
    /// </summary>
    [SerializeField] private float[] breathAngles = { -30f, 0f, 30f };

    /// <summary>ความเร็วไฟตอนแขนยังอยู่ (bullet speed code 6)</summary>
    [SerializeField] private float fireballSpeed = 6f;

    /// <summary>เร็วขึ้นเมื่อแขนหมดทั้งสองข้าง (speed code 7)</summary>
    [SerializeField] private float fireballSpeedNoArms = 7f;

    [Header("Camera (โจมตีเฉพาะตอนอยู่ในจอ)")]
    /// <summary>
    /// หยุดทุกอย่างตอนบอสยังไม่เข้าจอ ทั้งปากและแขนทั้งสองข้าง
    ///
    /// **ต้นฉบับไม่ต้องเช็ค** เพราะ NES ถอดศัตรูที่อยู่นอกจอทิ้งอัตโนมัติ
    /// ของเราวางบอสไว้ในฉากตั้งแต่แรก ถ้าไม่เช็คมันจะเริ่มนับเวลา
    /// แล้วพ่นไฟใส่ตั้งแต่ยังไม่เห็นตัวบอสด้วยซ้ำ ซึ่งหลบไม่ได้เลย
    ///
    /// หน่วง 4.25 วิก่อนอ้าปากครั้งแรกก็จะเริ่มนับตอนเข้าจอ
    /// ไม่ใช่ตอนโหลดฉาก เลยได้เวลาตั้งตัวเต็มๆ ตามที่ควรจะเป็น
    /// </summary>
    [SerializeField] private bool onlyAttackOnScreen = true;

    /// <summary>
    /// เผื่อขอบจอเท่าไหร่ถึงจะเริ่มโจมตี
    ///
    /// 0 = ต้องเห็นสไปรต์สักส่วนในจอจริงๆ ก่อน (วัดจากกรอบสไปรต์ ไม่ใช่จุดศูนย์กลาง)
    /// ติดลบ = ต้องโผล่เข้ามาลึกกว่านั้นอีกถึงจะเริ่ม ใจดีกับผู้เล่นขึ้น
    /// </summary>
    [SerializeField] private float screenMargin = 0f;

    [Header("Arms")]
    [SerializeField] private DragonArm[] arms;

    [Header("Sprites")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openingSprite;
    [SerializeField] private Sprite openSprite;

    [Header("Death")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private int deathExplosionCount = 14;
    [SerializeField] private float deathExplosionSpread = 2.2f;
    [SerializeField] private float deathExplosionInterval = 0.1f;

    private EnemyHealth health;
    private Phase phase = Phase.Sleeping;
    private float timer;
    private bool breathed;

    /// <summary>แขนที่ถูกทำลายไปแล้วกี่ข้าง ใช้ตัดสินจังหวะของปาก</summary>
    public int ArmsDestroyed { get; private set; }

    public bool IsMouthOpen
    {
        get { return phase == Phase.Open; }
    }

    /// <summary>ยังเปิดปากไม่ได้ถ้าตั้งให้รอแขนพังก่อน และแขนยังไม่พังครบ</summary>
    private bool CanOpenMouth
    {
        get { return !openOnlyWhenArmless || ArmsDestroyed >= 2; }
    }

    /// <summary>แขนอ่านค่าจากตรงนี้ จะได้มีสวิตช์เดียวคุมทั้งตัวบอส</summary>
    public bool OnlyAttackOnScreen
    {
        get { return onlyAttackOnScreen; }
    }

    public float ScreenMargin
    {
        get { return screenMargin; }
    }

    /// <summary>
    /// กรอบนี้โผล่ในจอแล้วหรือยัง ใช้ร่วมกันระหว่างปากกับแขน
    ///
    /// วัดจาก "กรอบสไปรต์" ไม่ใช่จุดศูนย์กลาง เพราะบอสตัวใหญ่มาก
    /// ถ้าวัดจุดศูนย์กลาง มันจะยังไม่โจมตีทั้งที่เห็นเต็มครึ่งจอไปแล้ว
    /// และถ้าจุดหมุนบังเอิญอยู่นอกกรอบที่กล้องกวาดถึง บอสจะไม่ขยับเลยทั้งเกม
    /// </summary>
    public static bool IsInsideView(Bounds box, float margin)
    {
        Camera view = Camera.main;

        if (view == null || !view.orthographic)
            return true;

        Vector2 center = view.transform.position;

        float halfWidth =
            view.orthographicSize * view.aspect + margin + box.extents.x;

        float halfHeight =
            view.orthographicSize + margin + box.extents.y;

        return Mathf.Abs(box.center.x - center.x) <= halfWidth &&
            Mathf.Abs(box.center.y - center.y) <= halfHeight;
    }

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (arms == null || arms.Length == 0)
        {
            arms = GetComponentsInChildren<DragonArm>(true);
        }

        foreach (DragonArm arm in arms)
        {
            if (arm != null)
            {
                arm.Bind(this);
            }
        }
    }

    private void Start()
    {
        health.OnDied += OnMouthDestroyed;

        EnterPhase(Phase.Sleeping, wakeDelay);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDied -= OnMouthDestroyed;
        }
    }

    /// <summary>แขนเรียกตอนตัวเองพัง</summary>
    public void NotifyArmDestroyed()
    {
        ArmsDestroyed++;
    }

    private void Update()
    {
        // ยังไม่เข้าจอก็ค้างอยู่เฟสเดิม ไม่นับเวลา ไม่พ่นไฟ
        // นาฬิกาทุกตัวของบอสเริ่มเดินตอนผู้เล่นเห็นตัวมันแล้วเท่านั้น
        if (onlyAttackOnScreen && !IsOnScreen())
            return;

        timer -= Time.deltaTime;

        switch (phase)
        {
            case Phase.Sleeping:
                if (timer <= 0f && CanOpenMouth)
                {
                    EnterPhase(Phase.Opening, shutterDuration);
                }
                break;

            case Phase.Opening:
                if (timer <= 0f) EnterPhase(Phase.Open, openDuration);
                break;

            case Phase.Open:
                UpdateOpen();
                break;

            case Phase.Closing:
                if (timer <= 0f) EnterPhase(Phase.Closed, GetClosedDuration());
                break;

            case Phase.Closed:
                if (timer <= 0f && CanOpenMouth)
                {
                    EnterPhase(Phase.Opening, shutterDuration);
                }
                break;
        }
    }

    /// <summary>ไม่มีสไปรต์ก็ใช้จุดหมุนแทน ยังดีกว่าไม่เช็คอะไรเลย</summary>
    private bool IsOnScreen()
    {
        Bounds box = spriteRenderer != null
            ? spriteRenderer.bounds
            : new Bounds(transform.position, Vector3.zero);

        return IsInsideView(box, screenMargin);
    }

    private void UpdateOpen()
    {
        // พ่นไฟครั้งเดียวต่อการอ้าหนึ่งรอบ ไม่ได้พ่นรัว
        if (!breathed && timer <= openDuration - breathDelay)
        {
            breathed = true;

            Breathe();
        }

        if (timer <= 0f)
        {
            EnterPhase(Phase.Closing, shutterDuration);
        }
    }

    private void EnterPhase(Phase next, float duration)
    {
        phase = next;
        timer = duration;

        if (next == Phase.Open)
        {
            breathed = false;
        }

        // ยิงเข้าได้เฉพาะตอนอ้าปากเท่านั้น ตรงกับ enable_bullet_enemy_collision
        if (health != null)
        {
            health.SetInvulnerable(next != Phase.Open);
        }

        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (spriteRenderer == null)
            return;

        Sprite next = closedSprite;

        if (phase == Phase.Open)
        {
            next = openSprite;
        }
        else if (phase == Phase.Opening || phase == Phase.Closing)
        {
            next = openingSprite;
        }

        if (next != null)
        {
            spriteRenderer.sprite = next;
        }
    }

    private float GetClosedDuration()
    {
        if (closedDurationByArms == null || closedDurationByArms.Length == 0)
            return 3.2f;

        int index = Mathf.Clamp(
            ArmsDestroyed, 0, closedDurationByArms.Length - 1
        );

        return closedDurationByArms[index];
    }

    /// <summary>พ่นไฟสามลูก ซ้าย กลาง ขวา</summary>
    private void Breathe()
    {
        if (fireballPrefab == null || breathAngles == null)
            return;

        Vector3 spot = muzzle != null ? muzzle.position : transform.position;

        bool armless = ArmsDestroyed >= 2;

        float speed = armless ? fireballSpeedNoArms : fireballSpeed;

        foreach (float angle in breathAngles)
        {
            GameObject shot = Instantiate(
                fireballPrefab, spot, Quaternion.identity
            );

            EnemyBullet bullet = shot.GetComponent<EnemyBullet>();

            if (bullet == null)
                continue;

            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.down;

            bullet.Initialize(direction, speed / fireballSpeed);
        }
    }

    private void OnMouthDestroyed()
    {
        GameAudio.BossCleared();

        // ต้องฝากชุดระเบิดไว้กับออบเจกต์อื่น
        // เพราะ EnemyHealth จะทำลายบอสทิ้งทันทีหลังเรียก OnDied
        ExplosionBurst.Spawn(
            transform.position,
            explosionPrefab,
            deathExplosionCount,
            deathExplosionSpread,
            deathExplosionInterval
        );
    }
}
