using UnityEngine;

/// <summary>
/// ทหารวิ่ง หยุดยืนยิงเป็นชุด แล้ววิ่งต่อ — Soldier ($05) ของต้นฉบับ
///
/// วางคู่กับ EnemyRunner ตัวนั้นคุมการวิ่งกับกระโดดข้ามหลุม
/// ตัวนี้คุมแค่จังหวะหยุดกับการยิง แล้วสั่ง Halted ให้มันหยุดชั่วคราว
///
/// วงจรตาม soldier_routine_02 กับ 03
///   วิ่ง -> ครบเวลา -> หยุด -> ตั้งท่า 8 เฟรม -> ยิงเป็นชุด -> วิ่งต่อ
///
/// **กฎกันโกงผู้เล่น** บางข้ออยู่ในต้นฉบับอยู่แล้ว บางข้อผมเพิ่มให้
///   1. ไม่ยิงถ้ากระสุนจะเกิดนอกจอ  (ต้นฉบับเช็คไว้ชัดเจนใน soldier_routine_03)
///   2. หยุดก่อนแล้วค่อยยิง มีจังหวะตั้งท่าให้เห็น ไม่ใช่ยิงทันทีที่หยุด
///   3. เล็งหยาบเป็นขั้น มีช่องให้หลบ ไม่ได้เล็งเป๊ะ
///   4. หน่วงนัดแรกแบบสุ่ม ทหารหลายตัวจะได้ไม่ยิงพร้อมกัน
///   5. ล็อกทิศตอนยิงออก ไม่ไล่ตามกระสุน
///   6. ไม่ยิงถ้าผู้เล่นอยู่ใกล้เกินจนหลบไม่ทัน
/// </summary>
[RequireComponent(typeof(EnemyRunner))]
public class EnemyStopAndShoot : MonoBehaviour
{
    private enum Phase
    {
        Running,
        WindUp,
        Firing
    }

    [Header("Cycle")]
    /// <summary>วิ่งนานแค่ไหนก่อนหยุดยิงรอบถัดไป ต้นฉบับ 128 เฟรม</summary>
    [SerializeField] private float runTimeBetweenBursts = 2.13f;

    /// <summary>
    /// หน่วงก่อนหยุดยิงครั้งแรก สุ่มจากช่วงนี้
    /// ต้นฉบับใช้ตาราง 1, 16, 32, 48 เฟรม เพื่อไม่ให้ทุกตัวยิงพร้อมกัน
    /// </summary>
    [SerializeField] private Vector2 firstBurstDelay = new Vector2(0.4f, 1.6f);

    /// <summary>
    /// หยุดแล้วตั้งท่าอยู่เท่านี้ก่อนกระสุนนัดแรกออก ต้นฉบับ 8 เฟรม
    /// นี่คือจังหวะที่คนเล่นใช้ตัดสินใจหลบ อย่าตั้งเป็น 0
    /// </summary>
    [SerializeField] private float windUp = 0.2f;

    /// <summary>ห่างระหว่างนัดในชุดเดียวกัน ต้นฉบับ 16 เฟรม</summary>
    [SerializeField] private float shotInterval = 0.267f;

    /// <summary>ยิงกี่นัดต่อชุด ต้นฉบับสุ่ม</summary>
    [SerializeField] private int minBullets = 1;

    [SerializeField] private int maxBullets = 3;

    [Header("Fair Play (กฎกันโกง)")]
    /// <summary>
    /// ต้องอยู่ในจอถึงจะยิงได้
    /// ต้นฉบับเช็คว่ากระสุนจะเกิดนอกจอไหม ถ้าใช่ก็ข้ามการยิงไปเลย
    /// ถ้าไม่มีข้อนี้ ศัตรูนอกจอจะยิงใส่เราโดยที่เรามองไม่เห็นมัน
    /// </summary>
    [SerializeField] private bool onlyFireWhenVisible = true;

    /// <summary>ต้องเข้ามาในจอลึกเท่านี้ก่อน กันยิงตั้งแต่โผล่ครึ่งตัวที่ขอบจอ</summary>
    [SerializeField] private float screenEdgeMargin = 1.2f;

    /// <summary>
    /// ใกล้กว่านี้ไม่ยิง เพราะคนเล่นหลบไม่ทันอยู่ดี
    /// ให้มันวิ่งชนเอาแทน ซึ่งเห็นมาแต่ไกลและหลบได้
    /// </summary>
    [SerializeField] private float minDistanceToPlayer = 1.6f;

    /// <summary>ไกลเกินนี้ก็ไม่ยิง ไม่งั้นโดนยิงจากมุมที่แทบมองไม่เห็นตัว</summary>
    [SerializeField] private float maxDistanceToPlayer = 11f;

    /// <summary>
    /// ปัดมุมเล็งเป็นขั้นละกี่องศา
    ///
    /// ศัตรูกลางแจ้งของต้นฉบับใช้ตาราง quadrant_aim_dir_00 ซึ่งมีแค่ 11 ทิศ
    /// หยาบกว่าศัตรูในฐานที่มี 24 ทิศ ราวเท่าตัว
    /// ช่องว่างระหว่างมุมคือที่ที่คนเล่นใช้ยืนหลบ ตั้ง 0 เมื่อไหร่เกมจะโหดทันที
    /// </summary>
    [SerializeField] private float aimSnapDegrees = 30f;

    [Header("Telegraph (บอกล่วงหน้าว่าจะยิง)")]
    /// <summary>
    /// เปลี่ยนสีตอนตั้งท่า ให้คนเล่นเห็นว่าตัวนี้กำลังจะยิง
    /// ข้อนี้ต้นฉบับไม่มี ผมเพิ่มให้เพราะสไปรท์ของเรายังไม่มีท่ายิงแยก
    /// </summary>
    [SerializeField] private bool tintWhileAiming = true;

    [SerializeField] private Color aimTint = new Color(1f, 0.75f, 0.5f);

    [Header("Bullet")]
    [SerializeField] private GameObject bulletPrefab;

    /// <summary>ปากกระบอกปืน วัดจากจุดหมุนของศัตรู</summary>
    [SerializeField] private Vector2 muzzleOffset = new Vector2(0.45f, 0.95f);

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private string playerTag = "Player";

    private EnemyRunner runner;
    private Transform player;
    private Camera view;

    private Phase phase = Phase.Running;
    private float timer;
    private float shotTimer;
    private int bulletsLeft;
    private Vector2 lockedAim = Vector2.left;

    /// <summary>
    /// ให้จุดเกิดสั่งพฤติกรรมเฉพาะตัว
    ///
    /// ต้นฉบับเก็บ "ยิงได้ไหม" ไว้ในบิต 2 ของ ENEMY_ATTRIBUTES ของทหารแต่ละตัว
    /// ทหารตัวเดียวกันเป๊ะ แต่จุดเกิดต่างกันทำให้บางตัวยิง บางตัววิ่งผ่านเฉยๆ
    ///
    /// เรียกหลัง Instantiate ได้เลย จะทับค่าใน prefab
    /// </summary>
    public void Configure(bool shoots, int bulletsMin, int bulletsMax,
        float runTime)
    {
        minBullets = Mathf.Max(1, bulletsMin);
        maxBullets = Mathf.Max(minBullets, bulletsMax);

        if (runTime > 0f)
        {
            runTimeBetweenBursts = runTime;
        }

        if (shoots)
            return;

        // ไม่ยิงก็ปิดตัวเองทิ้ง เหลือแต่ EnemyRunner ที่วิ่งอย่างเดียว
        // OnDisable ปลด Halted ให้อยู่แล้ว ตัวจะไม่ค้างยืนนิ่ง
        enabled = false;
    }

    private void Awake()
    {
        runner = GetComponent<EnemyRunner>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Start()
    {
        // สุ่มเวลานัดแรก ทหารที่ออกมาพร้อมกันจะได้ไม่ยิงพร้อมกัน
        timer = Random.Range(firstBurstDelay.x, firstBurstDelay.y);
    }

    private void Update()
    {
        switch (phase)
        {
            case Phase.Running:
                UpdateRunning();
                break;

            case Phase.WindUp:
                UpdateWindUp();
                break;

            case Phase.Firing:
                UpdateFiring();
                break;
        }
    }

    private void UpdateRunning()
    {
        timer -= Time.deltaTime;

        if (timer > 0f)
            return;

        // ถึงคิวยิงแล้ว แต่ถ้าเงื่อนไขยังไม่พร้อมก็วิ่งต่อ ไม่ฝืนยิง
        if (!CanFire())
        {
            // ลองใหม่เร็วกว่ารอบปกติ แต่ไม่ถี่จนเช็คทุกเฟรม
            timer = 0.25f;

            return;
        }

        EnterWindUp();
    }

    private void EnterWindUp()
    {
        phase = Phase.WindUp;
        timer = windUp;

        runner.Halted = true;

        if (tintWhileAiming && spriteRenderer != null)
        {
            spriteRenderer.color = aimTint;
        }
    }

    private void UpdateWindUp()
    {
        timer -= Time.deltaTime;

        if (timer > 0f)
            return;

        // ล็อกทิศตอนนี้ครั้งเดียว ยิงทั้งชุดไปทางเดิม ไม่ไล่ตามผู้เล่น
        lockedAim = GetAim();

        bulletsLeft = Random.Range(minBullets, maxBullets + 1);

        phase = Phase.Firing;
        shotTimer = 0f;
    }

    private void UpdateFiring()
    {
        shotTimer -= Time.deltaTime;

        if (shotTimer > 0f)
            return;

        Fire();

        bulletsLeft--;

        if (bulletsLeft > 0)
        {
            shotTimer = shotInterval;

            return;
        }

        EndBurst();
    }

    private void EndBurst()
    {
        phase = Phase.Running;
        timer = runTimeBetweenBursts;

        runner.Halted = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
    }

    /// <summary>กฎกันโกงทั้งหมดรวมอยู่ตรงนี้</summary>
    private bool CanFire()
    {
        if (bulletPrefab == null)
            return false;

        if (!FindPlayer())
            return false;

        float distance = Vector2.Distance(
            transform.position, player.position
        );

        if (distance < minDistanceToPlayer || distance > maxDistanceToPlayer)
            return false;

        if (!onlyFireWhenVisible)
            return true;

        return IsWellInsideScreen();
    }

    /// <summary>
    /// ต้องอยู่ในจอ และลึกเข้ามาพอสมควร
    /// ต้นฉบับเช็คจุดเกิดกระสุนว่าหลุดขอบจอไหม ผมเช็คตัวศัตรูแทน ซึ่งเข้มกว่า
    /// </summary>
    private bool IsWellInsideScreen()
    {
        if (view == null)
        {
            view = Camera.main;
        }

        if (view == null || !view.orthographic)
            return true;

        Vector2 center = view.transform.position;

        float halfHeight = view.orthographicSize - screenEdgeMargin;
        float halfWidth =
            view.orthographicSize * view.aspect - screenEdgeMargin;

        Vector3 spot = transform.position;

        return Mathf.Abs(spot.x - center.x) <= halfWidth &&
            Mathf.Abs(spot.y - center.y) <= halfHeight;
    }

    private bool FindPlayer()
    {
        if (player != null)
            return true;

        GameObject found = GameObject.FindGameObjectWithTag(playerTag);

        if (found == null)
            return false;

        player = found.transform;

        return true;
    }

    private Vector2 GetAim()
    {
        if (player == null)
            return transform.localScale.x >= 0f ? Vector2.right : Vector2.left;

        Vector2 origin = (Vector2)transform.position + GetMuzzleOffset();

        Vector2 toPlayer = (Vector2)player.position + Vector2.up * 0.6f - origin;

        if (toPlayer.sqrMagnitude < 0.0001f)
            return Vector2.left;

        return Snap(toPlayer.normalized);
    }

    /// <summary>ปัดมุมเข้าช่องที่ใกล้ที่สุด เลียนตารางทิศของต้นฉบับ</summary>
    private Vector2 Snap(Vector2 aim)
    {
        if (aimSnapDegrees <= 0f)
            return aim;

        float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;

        angle = Mathf.Round(angle / aimSnapDegrees) * aimSnapDegrees;

        float radians = angle * Mathf.Deg2Rad;

        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    /// <summary>ปากกระบอกพลิกตามทางที่ศัตรูหันอยู่</summary>
    private Vector2 GetMuzzleOffset()
    {
        float facing = transform.localScale.x >= 0f ? 1f : -1f;

        return new Vector2(muzzleOffset.x * facing, muzzleOffset.y);
    }

    private void Fire()
    {
        Vector2 origin = (Vector2)transform.position + GetMuzzleOffset();

        GameObject shot = Instantiate(
            bulletPrefab, origin, Quaternion.identity
        );

        EnemyBullet bullet = shot.GetComponent<EnemyBullet>();

        if (bullet != null)
        {
            bullet.Initialize(lockedAim);
        }
    }

    private void OnDisable()
    {
        // ตายหรือถูกปิดกลางคัน อย่าทิ้งให้ตัววิ่งค้างอยู่ในสถานะหยุด
        if (runner != null)
        {
            runner.Halted = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // วงในคือใกล้เกินไปไม่ยิง วงนอกคือไกลเกินไปไม่ยิง
        Gizmos.DrawWireSphere(transform.position, minDistanceToPlayer);
        Gizmos.DrawWireSphere(transform.position, maxDistanceToPlayer);
    }
}
