using UnityEngine;

/// <summary>
/// กระสุนของศัตรูในด่านฐาน พุ่งออกจากจอมาหาผู้เล่น
///
/// ใช้ได้ทั้งกระสุนป้อมปืน กระสุนแกนกลาง และวงแหวนไฟของบอส
/// ต่างกันแค่ค่าใน Inspector
///
/// ตัวไหนตั้ง hitPoints ไว้มากกว่า 0 จะยิงแตกได้ (วงแหวนไฟของบอสมี 2 หน่วย)
/// </summary>
public class BaseProjectile : MonoBehaviour, IBaseDamageable
{
    [Header("Flight")]
    [SerializeField] private float speed = 7f;

    /// <summary>เล็งผู้เล่นตอนเกิด ถ้าปิดจะพุ่งตรงลงมาเฉยๆ</summary>
    [SerializeField] private bool aimAtPlayer = true;

    /// <summary>
    /// ปัดมุมเล็งเป็นขั้นละกี่องศา
    ///
    /// ต้นฉบับไม่ได้เล็งเป๊ะ มันใช้ตาราง quadrant_aim_dir_01 ที่มี
    /// 6 ทิศต่อควอดแรนต์ รวม 24 ทิศ = ห่างกันทิศละ 15 องศา
    /// ช่องว่างระหว่างทิศนี่แหละคือที่ที่คนเล่นใช้ยืนหลบ
    /// ตั้ง 0 ถ้าอยากให้เล็งเป๊ะแบบเกมยุคใหม่
    /// </summary>
    [SerializeField] private float aimSnapDegrees = 15f;

    /// <summary>บินออกนอกจอเกินเส้นนี้ก็หายไป</summary>
    [SerializeField] private float despawnBelowFloor = 1.5f;

    [SerializeField] private float lifetime = 6f;

    [Header("Arc (วิถีโค้งแบบระเบิดมือ)")]
    /// <summary>แรงดึงลง 0 = วิ่งเป็นเส้นตรง ระเบิดมือของต้นฉบับใช้วิถีโค้ง</summary>
    [SerializeField] private float gravity;

    /// <summary>แรงส่งขึ้นตอนออกตัว ใช้คู่กับ gravity ถึงจะได้เส้นโค้งสวย</summary>
    [SerializeField] private float launchUpBoost;

    [Header("Depth (ขยายขนาดตามที่เข้ามาใกล้)")]
    [SerializeField] private bool scaleWithDepth = true;

    [Header("Destructible (ยิงแตกได้ไหม)")]
    /// <summary>0 = ยิงไม่แตก วงแหวนไฟของบอสใช้ 2</summary>
    [SerializeField] private int hitPoints;

    [SerializeField] private GameObject destroyEffectPrefab;

    [Header("Spin")]
    [SerializeField] private float spinSpeed;

    [Header("Sound")]
    /// <summary>ไม่เล่นเสียงปืนตอนเกิด ลูกกลิ้งกับวงแหวนไฟไม่ใช่กระสุนปืน</summary>
    [SerializeField] private bool silentSpawn;

    [Header("Crouch")]
    /// <summary>
    /// หมอบแล้วลอดได้ไหม
    /// เปิดไว้สำหรับกระสุนปืน ต้นฉบับหมอบแล้วกันกระสุนได้ทุกนัด
    /// ปิดสำหรับลูกกลิ้งกับวงแหวนไฟ สองอย่างนั้นหมอบก็ยังโดนอยู่ดี
    /// </summary>
    [SerializeField] private bool blockedByCrouch = true;

    private Vector2 velocity = Vector2.down;
    private float despawnY;
    private float life;

    public bool CanBeHit
    {
        get { return hitPoints > 0; }
    }

    /// <summary>
    /// ป้อมที่ยิงเป็นแพตเทิร์นตายตัวเรียกอันนี้ทันทีหลัง Instantiate
    /// ต้องเรียกก่อน Start ซึ่ง Unity จะรันให้ทีหลังอยู่แล้ว
    /// </summary>
    public void OverrideAim(bool aim)
    {
        aimAtPlayer = aim;
    }

    private void Start()
    {
        despawnY = BaseStageGeometry.FloorY - despawnBelowFloor;

        Vector2 direction = Vector2.down;

        BasePlayer player = BaseStage.Instance != null
            ? BaseStage.Instance.Player
            : Object.FindObjectOfType<BasePlayer>();

        if (aimAtPlayer && player != null)
        {
            // เล็งไปที่ตำแหน่งผู้เล่นตอนยิงออก ไม่ตามต่อหลังจากนั้น
            // ของต้นฉบับก็ล็อกทิศตอนเกิดเหมือนกัน หลบได้ด้วยการขยับ
            Vector2 toPlayer =
                (Vector2)player.transform.position + Vector2.up * 0.6f -
                (Vector2)transform.position;

            if (toPlayer.sqrMagnitude > 0.0001f)
            {
                direction = SnapAim(toPlayer.normalized);
            }
        }

        // ป้อม Zark หมุนกระสุนทีละนัดเพื่อกางเป็นพัด เอามุมนั้นมาบิดทิศต่อ
        float tilt = transform.eulerAngles.z;

        if (!Mathf.Approximately(tilt, 0f))
        {
            direction = Quaternion.Euler(0f, 0f, tilt) * direction;
        }

        velocity = direction * speed + Vector2.up * launchUpBoost;

        if (!silentSpawn)
        {
            GameAudio.EnemyShot();
        }
    }

    /// <summary>
    /// ปัดมุมเข้าหาช่องที่ใกล้ที่สุด เลียนตาราง 24 ทิศของต้นฉบับ
    /// ผลคือกระสุนมักจะเฉียดแทนที่จะเข้าเป้าเสมอ ซึ่งเป็นช่องให้คนเล่นหลบ
    /// </summary>
    private Vector2 SnapAim(Vector2 aim)
    {
        if (aimSnapDegrees <= 0f)
            return aim;

        float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;

        angle = Mathf.Round(angle / aimSnapDegrees) * aimSnapDegrees;

        float radians = angle * Mathf.Deg2Rad;

        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    private void Update()
    {
        life += Time.deltaTime;

        if (life >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // ระเบิดมือสะสมความเร็วตกทุกเฟรม เหมือน set_enemy_falling_arc_pos
        if (gravity != 0f)
        {
            velocity += Vector2.down * (gravity * Time.deltaTime);
        }

        transform.position += (Vector3)(velocity * Time.deltaTime);

        if (spinSpeed != 0f)
        {
            transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
        }

        if (scaleWithDepth)
        {
            float scale = BaseStageGeometry.ScaleAt(transform.position.y);

            transform.localScale = new Vector3(scale, scale, 1f);
        }

        if (transform.position.y <= despawnY)
        {
            Destroy(gameObject);
        }
    }

    public void TakeHit(int damage, Vector2 from)
    {
        hitPoints -= damage;

        if (hitPoints > 0)
            return;

        if (destroyEffectPrefab != null)
        {
            Instantiate(
                destroyEffectPrefab,
                transform.position,
                Quaternion.identity
            );
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHitPlayer(other);
    }

    /// <summary>
    /// ต้องเช็คทุกเฟรมที่ยังทับกันอยู่ ไม่ใช่แค่ตอนเข้ามาครั้งแรก
    /// ไม่งั้นหมอบหลบตอนกระสุนเข้ามาแล้วรีบยืน จะรอดฟรีทั้งที่ยังทับกันอยู่
    /// </summary>
    private void OnTriggerStay2D(Collider2D other)
    {
        TryHitPlayer(other);
    }

    private void TryHitPlayer(Collider2D other)
    {
        BasePlayer player = other.GetComponentInParent<BasePlayer>();

        if (player == null)
            return;

        // หมอบอยู่ก็ปล่อยผ่านไป ไม่ทำลายกระสุนด้วย
        // จะได้เห็นมันลอยข้ามหัวไปจริงๆ ไม่ใช่หายวับตรงหน้า
        if (blockedByCrouch && player.IsCrouching)
            return;

        player.Kill();

        Destroy(gameObject);
    }
}
