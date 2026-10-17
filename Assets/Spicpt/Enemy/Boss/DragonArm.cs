using UnityEngine;

/// <summary>
/// แขนของบอส Mechanical Alien - Dragon Tentacle Orb
///
/// ต้นฉบับเรียกตัวนี้ว่า "ศัตรูที่ซับซ้อนที่สุดในเกม"
/// แขนหนึ่งข้างประกอบด้วยลูกกลม 5 ลูก
///   หัวไหล่ - ลูกที่ติดลำตัว เป็นตัวสร้างลูกที่เหลืออีก 4
///   มือ     - ลูกแดงปลายสุด **ลูกเดียวที่ยิงโดน** ลูกกลางเป็นแค่ข้อต่อ
///
/// ที่นี่ทำแบบย่อ: วางลูกกลมเรียงตามส่วนโค้งแล้วแกว่งขึ้นลง
/// ได้ภาพและกติกาเหมือนกัน แต่ไม่ได้ลอกตาราง dragon_arm_orb_pos_tbl มาทั้งดุ้น
/// ต้นฉบับมีท่าโจมตี 5 แบบวนกัน ของเราทำแบบแกว่งขึ้นลงอย่างเดียว
///
/// เลือด 16 และยิงไฟใส่ผู้เล่นทุก 144 เฟรม (2.4 วินาที) จาก ENEMY_VAR_A
/// </summary>
public class DragonArm : MonoBehaviour
{
    // เลือดอยู่ที่ EnemyHealth บนลูกมือปลายสุด ต้นฉบับให้แขนละ 16

    [Header("Shape")]
    /// <summary>ลูกกลมทั้งแขน เรียงจากหัวไหล่ไปหามือ ต้นฉบับใช้ 5 ลูก</summary>
    [SerializeField] private Transform[] orbs;

    /// <summary>
    /// ข้อต่อสีเขียวที่คั่นระหว่างลูกกลม
    /// มีน้อยกว่าลูกกลมหนึ่งชิ้นเสมอ เพราะวางไว้ตรงกลางระหว่างคู่
    /// ว่างไว้ได้ แขนจะเหลือแต่ลูกกลมลอยเป็นแถว
    /// </summary>
    [SerializeField] private Transform[] segments;

    /// <summary>
    /// ระยะห่างระหว่างลูก
    /// ต้องกว้างพอให้แขนกางออกจากตัว ไม่ใช่กองรวมกันอยู่ข้างหัว
    /// ข้อต่อจะยืดตามค่านี้เอง ไม่ต้องไปแก้สไปรท์
    /// </summary>
    [SerializeField] private float segmentLength = 1.35f;

    [Header("Spin (ลูกกลมหมุน)")]
    /// <summary>ลูกมือปลายแขนหมุนกี่องศาต่อวินาที ติดลบคือหมุนกลับทาง</summary>
    [SerializeField] private float handSpinSpeed = 240f;

    /// <summary>ลูกอื่นหมุนช้ากว่า ตั้ง 0 ถ้าอยากให้นิ่ง</summary>
    [SerializeField] private float orbSpinSpeed = 110f;

    /// <summary>แขนซ้ายหมุนสวนทางแขนขวา ดูมีชีวิตกว่าหมุนทางเดียวกันทั้งคู่</summary>
    [SerializeField] private bool mirrorSpinPerSide = true;

    /// <summary>
    /// องศาที่แขนงอต่อหนึ่งข้อ
    /// ค่าติดลบ = ยกขึ้นจากหัวไหล่แล้วค่อยโค้งลงที่ปลาย ตามรูปทรงในภาพต้นฉบับ
    /// </summary>
    [SerializeField] private float curlPerSegment = -13f;

    /// <summary>แขนข้างขวาของจอใช้ 1 ข้างซ้ายใช้ -1</summary>
    [SerializeField] private float side = 1f;

    [Header("Shoulder (กวาดทั้งแขนรอบหัวไหล่)")]
    /// <summary>มุมตั้งต้นที่หัวไหล่ชี้ ค่าบวกคือเชิดขึ้น</summary>
    [SerializeField] private float baseAngle = 25f;

    /// <summary>
    /// ทั้งแขนกวาดรอบหัวไหล่กี่องศาต่อวินาที
    ///
    /// ต้นฉบับเก็บมุมหัวไหล่เป็นดัชนีเข้าตาราง dragon_arm_orb_pos_tbl
    /// แล้วไล่ค่าไปเรื่อยๆ ทั้งแขนจึงกวาดรอบตัวบอส ไม่ได้ปักอยู่ทางเดียว
    ///
    /// ตรงนี้คนละเรื่องกับการบิดทีละข้อข้างล่าง
    /// อันนี้คือหมุนทั้งแขน อันนั้นคือหนวดสะบัดอยู่กับที่
    /// </summary>
    [SerializeField] private float shoulderSpinSpeed = 26f;

    /// <summary>
    /// จำกัดการกวาดไว้กี่องศาจากมุมตั้งต้น ชนขอบแล้วย้อนทาง
    ///
    /// 30 = ปลายแขนกวาดขึ้นลงราว 6.4 หน่วย โดยยังยื่นออกข้างอยู่ตลอด
    /// ไม่ตกใต้พื้นและไม่ข้ามหัวบอส
    ///
    /// **ตั้ง 0 = หมุนครบรอบ 360 องศาไม่หยุด** (รอบละราว 14 วินาทีที่ความเร็วนี้)
    /// แต่ระหว่างทางแขนจะกวาดลอดใต้พื้น ข้ามหัวบอส และเลยไปอีกฝั่งด้วย
    /// ถ้าฉากเปิดโล่งพอก็ใช้ได้ ถ้ามีพื้นบังจะเห็นแขนทะลุ
    /// </summary>
    [SerializeField] private float shoulderSweepLimit = 30f;

    /// <summary>
    /// สุ่มสลับทิศกวาดทุกกี่วินาที
    ///
    /// ถ้าไม่มีตัวนี้ แขนจะกวาดไปกลับเป็นจังหวะเป๊ะเหมือนลูกตุ้ม เดาทางได้หมด
    /// ใส่แล้วมันจะหมุนขวาสักพัก แล้วอยู่ดีๆ ก็ย้อนกลับไปทางซ้าย
    /// ตั้งช่วงให้กว้างจะยิ่งเดาทางยาก
    /// </summary>
    [SerializeField] private Vector2 shoulderFlipInterval =
        new Vector2(1.5f, 4f);

    /// <summary>สุ่มทิศตอนเริ่ม สองแขนจะได้ไม่ออกตัวทางเดียวกันเสมอ</summary>
    [SerializeField] private bool randomizeShoulderDirection = true;

    /// <summary>เริ่มที่มุมเยื้องจากอีกแขนกี่องศา สองแขนจะได้ไม่ทับจังหวะกัน</summary>
    [SerializeField] private float shoulderStartOffset;

    [Header("Joints (ข้อที่ 2 เป็นต้นไป หมุนเอง)")]
    /// <summary>
    /// แต่ละข้อบิดเร็วแค่ไหน องศาต่อวินาที
    ///
    /// ต้นฉบับให้ลูกกลมแต่ละลูกมีตัวนับทิศหมุนของตัวเอง (ENEMY_VAR_2)
    /// ค่าบวกหมุนตามเข็ม ค่าลบทวนเข็ม พอหมดเวลาก็สลับทิศ
    /// แขนเลยดูบิดไปมาเหมือนหนวดจริง ไม่ใช่แท่งแข็งที่แกว่งทั้งอัน
    /// </summary>
    [SerializeField] private float jointSpinSpeed = 48f;

    /// <summary>บิดออกจากรูปทรงตั้งต้นได้สุดกี่องศา กันไม่ให้แขนม้วนจนพันกัน</summary>
    [SerializeField] private float jointBendLimit = 42f;

    /// <summary>
    /// ข้อยิ่งใกล้ปลายยิ่งบิดได้มากและเร็วขึ้นตามสัดส่วน
    ///
    /// ถ้าปิด ทุกข้อบิดเท่ากัน ซึ่งพอบิดทางเดียวกันหมดแขนจะม้วนกุดเหลือครึ่งเดียว
    /// เปิดไว้แล้วโคนแขนแทบไม่ขยับ ปลายสะบัดแทน เหมือนหนวดจริง
    /// </summary>
    [SerializeField] private bool taperTowardTip = true;

    /// <summary>สลับทิศหมุนทุกกี่วินาที สุ่มในช่วงนี้ แต่ละข้อสุ่มของตัวเอง</summary>
    [SerializeField] private Vector2 flipInterval = new Vector2(0.9f, 2.6f);

    /// <summary>
    /// สุ่มทิศเริ่มต้นของแต่ละข้อ
    /// ปิดแล้วจะสลับทิศเป็นข้อเว้นข้อ ได้ลูกคลื่นเป็นระเบียบกว่า
    /// </summary>
    [SerializeField] private bool randomizeDirection = true;

    [Header("Fire")]
    [SerializeField] private GameObject fireballPrefab;

    /// <summary>ต้นฉบับตั้ง ENEMY_VAR_A กลับเป็น $90 = 144 เฟรม</summary>
    [SerializeField] private float fireInterval = 2.4f;

    /// <summary>หน่วงนัดแรก ให้สองแขนไม่ยิงพร้อมกันเป๊ะ</summary>
    [SerializeField] private float firstFireDelay = 1.2f;

    /// <summary>ความเร็วไฟ ต้นฉบับใช้ bullet speed code 5 ช้ากว่าไฟจากปาก</summary>
    [SerializeField] private float fireballSpeedScale = 0.85f;

    /// <summary>
    /// ปัดมุมเล็งเป็นขั้นละกี่องศา
    /// ศัตรูในเกมนี้ไม่เล็งเป๊ะ มันปัดเข้าช่องในตาราง 24 ทิศ
    /// </summary>
    [SerializeField] private float aimSnapDegrees = 15f;

    [Header("Hand (ลูกปลายสุด ลูกเดียวที่ยิงโดน)")]
    [SerializeField] private EnemyHealth hand;

    [Header("Death")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float orbExplosionInterval = 0.07f;

    private DragonBoss boss;
    private float fireTimer;
    private bool dead;
    private Transform player;

    /// <summary>ความยาวจริงของสไปรท์ข้อต่อ ใช้คำนวณว่าต้องยืดเท่าไหร่</summary>
    private float segmentSpriteLength = 1f;

    // สถานะการบิดของแต่ละข้อ ช่อง 0 คือหัวไหล่ ไม่ได้ใช้เพราะข้อแรกไม่บิด
    private float[] jointSwing;
    private float[] jointDirection;
    private float[] jointFlipTimer;

    // มุมกวาดของทั้งแขน สะสมไปเรื่อยๆ
    private float shoulderAngle;
    private float shoulderDirection = 1f;
    private float shoulderFlipTimer;

    public bool IsDead
    {
        get { return dead; }
    }

    public void Bind(DragonBoss owner)
    {
        boss = owner;
    }

    private void Awake()
    {
        if (orbs == null || orbs.Length == 0)
        {
            // ไม่ได้ลากใส่ไว้ก็เก็บลูกทั้งหมดจากลูกของตัวเองตามลำดับ
            orbs = new Transform[transform.childCount];

            for (int i = 0; i < transform.childCount; i++)
            {
                orbs[i] = transform.GetChild(i);
            }
        }

        if (hand == null && orbs.Length > 0)
        {
            hand = orbs[orbs.Length - 1].GetComponent<EnemyHealth>();
        }

        MeasureSegmentSprite();
        SetUpJoints();

        shoulderAngle = baseAngle + shoulderStartOffset;

        if (randomizeShoulderDirection)
        {
            shoulderDirection = Random.value < 0.5f ? -1f : 1f;
        }

        shoulderFlipTimer = Random.Range(
            shoulderFlipInterval.x, shoulderFlipInterval.y
        );
    }

    /// <summary>ตั้งทิศหมุนและจังหวะสลับทิศให้แต่ละข้อ คนละค่ากัน</summary>
    private void SetUpJoints()
    {
        int count = orbs != null ? orbs.Length : 0;

        jointSwing = new float[count];
        jointDirection = new float[count];
        jointFlipTimer = new float[count];

        for (int i = 0; i < count; i++)
        {
            jointSwing[i] = 0f;

            jointDirection[i] = randomizeDirection
                ? (Random.value < 0.5f ? -1f : 1f)
                : (i % 2 == 0 ? 1f : -1f);

            // สุ่มเวลาตั้งต้นด้วย ไม่งั้นทุกข้อจะสลับทิศพร้อมกันครั้งแรก
            jointFlipTimer[i] = Random.Range(flipInterval.x, flipInterval.y);
        }
    }

    /// <summary>
    /// อ่านความยาวสไปรท์ข้อต่อไว้ล่วงหน้า
    /// จะได้ยืดให้พอดีช่องว่างระหว่างลูก ไม่ว่าจะตั้ง segmentLength เท่าไหร่
    /// </summary>
    private void MeasureSegmentSprite()
    {
        if (segments == null)
            return;

        foreach (Transform piece in segments)
        {
            if (piece == null)
                continue;

            SpriteRenderer sprite = piece.GetComponent<SpriteRenderer>();

            if (sprite == null || sprite.sprite == null)
                continue;

            float length = sprite.sprite.bounds.size.x;

            if (length > 0.001f)
            {
                segmentSpriteLength = length;
            }

            return;
        }
    }

    private void Start()
    {
        fireTimer = firstFireDelay;

        GameObject found = GameObject.FindGameObjectWithTag("Player");

        if (found != null)
        {
            player = found.transform;
        }

        if (hand != null)
        {
            hand.OnDied += OnHandDestroyed;
        }
    }

    private void OnDestroy()
    {
        if (hand != null)
        {
            hand.OnDied -= OnHandDestroyed;
        }
    }

    private void Update()
    {
        if (dead)
            return;

        UpdateShoulder();
        UpdateJoints();
        PoseArm();
        SpinOrbs();
        UpdateFire();
    }

    /// <summary>
    /// กวาดทั้งแขนรอบหัวไหล่
    ///
    /// ไม่ตั้งขีดจำกัดก็หมุนครบรอบไปเรื่อยๆ
    /// ตั้งขีดจำกัดก็กวาดไปกลับอยู่ในช่วงนั้น ชนขอบแล้วย้อนทาง
    /// </summary>
    private void UpdateShoulder()
    {
        if (Mathf.Approximately(shoulderSpinSpeed, 0f))
            return;

        // สุ่มย้อนทางระหว่างทาง ไม่ต้องรอให้ชนขอบ
        if (shoulderFlipInterval.y > 0f)
        {
            shoulderFlipTimer -= Time.deltaTime;

            if (shoulderFlipTimer <= 0f)
            {
                shoulderDirection = -shoulderDirection;

                shoulderFlipTimer = Random.Range(
                    shoulderFlipInterval.x, shoulderFlipInterval.y
                );
            }
        }

        shoulderAngle +=
            shoulderSpinSpeed * shoulderDirection * Time.deltaTime;

        if (shoulderSweepLimit <= 0f)
        {
            // หมุนครบรอบ วนค่าไว้ไม่ให้บวกจนล้น
            shoulderAngle = Mathf.Repeat(shoulderAngle, 360f);

            return;
        }

        float offset = Mathf.DeltaAngle(baseAngle, shoulderAngle);

        if (Mathf.Abs(offset) < shoulderSweepLimit)
            return;

        shoulderAngle =
            baseAngle + Mathf.Sign(offset) * shoulderSweepLimit;

        shoulderDirection = -shoulderDirection;

        // เพิ่งเด้งขอบแล้ว ตั้งเวลาสุ่มใหม่ ไม่งั้นอาจย้อนทางซ้ำทันทีจนสั่น
        shoulderFlipTimer = Random.Range(
            shoulderFlipInterval.x, shoulderFlipInterval.y
        );
    }

    /// <summary>
    /// ขยับมุมบิดของแต่ละข้อ ข้อแรก (หัวไหล่) ไม่ยุ่ง
    ///
    /// แต่ละข้อหมุนไปทางเดิมเรื่อยๆ จนหมดเวลาแล้วสลับทิศ
    /// หรือชนขอบที่ตั้งไว้ก็สลับทันที ไม่ต้องรอหมดเวลา
    /// </summary>
    private void UpdateJoints()
    {
        if (jointSwing == null)
            return;

        for (int i = 1; i < jointSwing.Length; i++)
        {
            float reach = JointTaper(i);

            float limit = jointBendLimit * reach;
            float speed = jointSpinSpeed * reach;

            jointFlipTimer[i] -= Time.deltaTime;

            if (jointFlipTimer[i] <= 0f)
            {
                jointDirection[i] = -jointDirection[i];
                jointFlipTimer[i] =
                    Random.Range(flipInterval.x, flipInterval.y);
            }

            jointSwing[i] += speed * jointDirection[i] * Time.deltaTime;

            // ชนขอบแล้วเด้งกลับ แขนจะได้ไม่ม้วนจนพันตัวเอง
            if (Mathf.Abs(jointSwing[i]) >= limit)
            {
                jointSwing[i] = Mathf.Sign(jointSwing[i]) * limit;
                jointDirection[i] = -jointDirection[i];
            }
        }
    }

    /// <summary>0 ที่โคนแขน ไล่ขึ้นไปจนเป็น 1 ที่ปลาย</summary>
    private float JointTaper(int index)
    {
        if (!taperTowardTip || jointSwing == null || jointSwing.Length < 2)
            return 1f;

        return index / (float)(jointSwing.Length - 1);
    }

    /// <summary>
    /// หมุนลูกกลมอยู่กับที่
    /// ต้องหมุนหลัง PoseArm เพราะ PoseArm แตะแค่ตำแหน่ง ไม่แตะมุม
    /// </summary>
    private void SpinOrbs()
    {
        if (orbs == null)
            return;

        float flip = mirrorSpinPerSide && side < 0f ? -1f : 1f;

        for (int i = 0; i < orbs.Length; i++)
        {
            if (orbs[i] == null)
                continue;

            bool isHand = i == orbs.Length - 1;

            float speed = isHand ? handSpinSpeed : orbSpinSpeed;

            if (Mathf.Approximately(speed, 0f))
                continue;

            orbs[i].Rotate(0f, 0f, speed * flip * Time.deltaTime);
        }
    }

    /// <summary>
    /// วางลูกกลมเรียงไปตามส่วนโค้ง
    /// แต่ละข้อหักมุมเพิ่มทีละนิด แขนเลยดูม้วนแทนที่จะเป็นเส้นตรง
    /// </summary>
    private void PoseArm()
    {
        if (orbs == null || orbs.Length == 0)
            return;

        // มุมสะสมไปตามข้อ ข้อแรกใช้มุมหัวไหล่ที่กำลังกวาดอยู่ ยังไม่มีการบิด
        float angle = shoulderAngle;

        Vector3 cursor = transform.position;
        Vector3 previous;

        for (int i = 0; i < orbs.Length; i++)
        {
            if (orbs[i] != null)
            {
                orbs[i].position = cursor;
            }

            previous = cursor;

            // ข้อที่ 2 เป็นต้นไปบวกทั้งโค้งตั้งต้นและมุมบิดที่กำลังหมุนอยู่
            if (i > 0)
            {
                angle += curlPerSegment;

                if (jointSwing != null && i < jointSwing.Length)
                {
                    angle += jointSwing[i];
                }
            }

            Vector3 direction =
                Quaternion.Euler(0f, 0f, angle * side) * Vector3.right * side;

            cursor += direction * segmentLength;

            // ข้อต่อวางตรงกลางระหว่างลูกคู่นี้ แล้วหมุนให้ชี้ไปตามแขน
            PlaceSegment(i, previous, cursor);
        }
    }

    private void PlaceSegment(int index, Vector3 from, Vector3 to)
    {
        if (segments == null || index >= segments.Length ||
            segments[index] == null)
        {
            return;
        }

        segments[index].position = (from + to) * 0.5f;

        Vector3 along = to - from;

        if (along.sqrMagnitude < 0.0001f)
            return;

        float facing = Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg;

        segments[index].rotation = Quaternion.Euler(0f, 0f, facing);

        // ยืดให้คาบเกี่ยวลูกทั้งสองข้างเล็กน้อย จะได้ไม่เห็นรอยต่อ
        float stretch = along.magnitude / segmentSpriteLength * 1.15f;

        segments[index].localScale = new Vector3(stretch, 1f, 1f);
    }

    private void UpdateFire()
    {
        if (fireballPrefab == null || hand == null)
            return;

        fireTimer -= Time.deltaTime;

        if (fireTimer > 0f)
            return;

        fireTimer = fireInterval;

        Fire();
    }

    private void Fire()
    {
        Vector2 origin = hand.transform.position;

        Vector2 direction = Vector2.down;

        if (player != null)
        {
            Vector2 toPlayer = (Vector2)player.position - origin;

            if (toPlayer.sqrMagnitude > 0.0001f)
            {
                direction = Snap(toPlayer.normalized);
            }
        }

        GameObject shot = Instantiate(
            fireballPrefab, origin, Quaternion.identity
        );

        EnemyBullet bullet = shot.GetComponent<EnemyBullet>();

        if (bullet != null)
        {
            bullet.Initialize(direction, fireballSpeedScale);
        }
    }

    /// <summary>ปัดมุมเข้าช่องที่ใกล้ที่สุด เลียนตาราง 24 ทิศของต้นฉบับ</summary>
    private Vector2 Snap(Vector2 aim)
    {
        if (aimSnapDegrees <= 0f)
            return aim;

        float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;

        angle = Mathf.Round(angle / aimSnapDegrees) * aimSnapDegrees;

        float radians = angle * Mathf.Deg2Rad;

        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    private void OnHandDestroyed()
    {
        if (dead)
            return;

        dead = true;

        if (boss != null)
        {
            boss.NotifyArmDestroyed();
        }

        StartCoroutine(CollapseRoutine());
    }

    /// <summary>แขนขาดแล้วระเบิดไล่จากมือกลับไปหาหัวไหล่</summary>
    private System.Collections.IEnumerator CollapseRoutine()
    {
        for (int i = orbs.Length - 1; i >= 0; i--)
        {
            Vector3 spot = orbs[i] != null
                ? orbs[i].position
                : transform.position;

            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, spot, Quaternion.identity);
            }

            if (orbs[i] != null)
            {
                orbs[i].gameObject.SetActive(false);
            }

            // ข้อต่อหายตามลูกที่มันเกาะอยู่ ไม่งั้นจะเหลือหนวดลอยเป็นเส้น
            if (segments != null && i < segments.Length && segments[i] != null)
            {
                segments[i].gameObject.SetActive(false);
            }

            yield return new WaitForSeconds(orbExplosionInterval);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (orbs == null)
            return;

        // ลากเส้นให้เห็นรูปแขนตอนจัดฉาก
        for (int i = 1; i < orbs.Length; i++)
        {
            if (orbs[i] != null && orbs[i - 1] != null)
            {
                Gizmos.DrawLine(orbs[i - 1].position, orbs[i].position);
            }
        }
    }
}
