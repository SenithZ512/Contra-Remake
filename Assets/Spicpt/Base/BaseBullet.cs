using UnityEngine;

/// <summary>
/// กระสุนของผู้เล่นในด่านฐาน
///
/// ไม่เหมือนกระสุนด่านปกติเลย และนี่คือกลไกที่เครื่อง NES ใช้ปลอมความลึก
///
///   1. บินได้ระยะตายตัว ต้นฉบับให้อายุ 42 เฟรม ไม่ได้วิ่งจนชนกำแพง
///   2. ระหว่างทาง "ไม่ชนอะไรเลย" จะตรวจการชนเฉพาะตอนใกล้หมดอายุ
///      (set_indoor_bullet_delay และเงื่อนไข PLAYER_BULLET_TIMER น้อยกว่า 2)
///
/// แปลว่ากระสุนไปหยุดที่ระยะหนึ่งแล้วค่อยโดนของที่อยู่ระยะนั้นพอดี
/// ผลคือท่าที่ยิงเป็นตัวกำหนดว่าเอื้อมถึงอะไร
///
///   ก้ม    ปากกระบอกต่ำ  -> กระสุนไปจบต่ำ  เอื้อมถึงแค่ของที่ติดต่ำ
///   ยืน    ปากกระบอกปกติ -> ถึงแถวแกนกลางพอดี
///   กระโดด ปากกระบอกสูง  -> เอื้อมถึงป้อมที่ติดเหนือแกน
///
/// คู่มือต้นฉบับถึงได้เขียนว่าป้อมบนของห้อง 4 กับ 5 "ปล่อยไว้ดีกว่า"
/// เพราะยืนยิงไม่ถึงจริงๆ ต้องกระโดดเท่านั้น
/// </summary>
public class BaseBullet : MonoBehaviour
{
    [Header("Flight")]
    /// <summary>
    /// บินได้ไกลเท่าไหร่ก่อนหมดระยะ นับจากจุดที่ออกจากปากกระบอก
    /// ตั้งไว้ให้ยืนยิงแล้วถึงแถวแกนกลางพอดี ก้มจะสั้นลง กระโดดจะยาวขึ้นเอง
    /// </summary>
    [SerializeField] private float travelDistance = 3.8f;

    [SerializeField] private float speed = 8.4f;

    [SerializeField] private int damage = 1;

    /// <summary>ทะลุต่อไปหลังโดนเป้าหรือหายไปเลย</summary>
    [SerializeField] private bool pierce;

    [Header("Hit Window (ช่วงที่ชนได้)")]
    /// <summary>
    /// ชนได้เฉพาะช่วงท้ายของระยะบิน ก่อนหน้านั้นทะลุทุกอย่าง
    /// ต้นฉบับใช้ 2 เฟรมสุดท้ายจาก 42 เฟรม เราเผื่อให้กว้างขึ้นนิด
    /// เพราะเฟรมเรตของเราไม่คงที่เหมือนเครื่องเกม
    /// </summary>
    [SerializeField] private float hitWindow = 0.45f;

    [Header("Depth (ย่อขนาดตามระยะ)")]
    [SerializeField] private bool scaleWithDepth = true;

    [Header("Impact")]
    [SerializeField] private GameObject impactEffectPrefab;

    /// <summary>แสดงให้เห็นว่ากระสุนถึงระยะแล้ว กำลังชนได้</summary>
    [SerializeField] private Color armedColor = new Color(1f, 1f, 1f);

    [SerializeField] private Color travelColor = new Color(0.7f, 0.8f, 1f);

    private SpriteRenderer spriteRenderer;
    private float travelled;

    /// <summary>ถึงช่วงท้ายของระยะบินหรือยัง ก่อนหน้านี้ยิงทะลุทุกอย่าง</summary>
    private bool IsArmed
    {
        get { return travelled >= travelDistance - hitWindow; }
    }

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;

        transform.position += Vector3.up * step;

        travelled += step;

        if (scaleWithDepth)
        {
            transform.localScale = Vector3.one *
                BaseStageGeometry.ScaleAt(transform.position.y);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = IsArmed ? armedColor : travelColor;
        }

        // หมดระยะแล้วไม่โดนอะไร ก็หายไปเฉยๆ
        if (travelled >= travelDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    /// <summary>
    /// ต้องเช็คทุกเฟรมที่ยังทับกันอยู่ด้วย
    /// เพราะกระสุนมักจะทับเป้าตั้งแต่ก่อนถึงระยะ ซึ่งตอนนั้นยังชนไม่ได้
    /// ถ้าเช็คแค่ตอนเข้าครั้งแรกจะไม่มีวันโดนเลย
    /// </summary>
    private void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        if (!IsArmed)
            return;

        IBaseDamageable target = other.GetComponentInParent<IBaseDamageable>();

        if (target == null || !target.CanBeHit)
            return;

        target.TakeHit(damage, transform.position);

        if (impactEffectPrefab != null)
        {
            Instantiate(
                impactEffectPrefab,
                transform.position,
                Quaternion.identity
            );
        }

        if (!pierce)
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // วาดจุดที่กระสุนจะไปหยุด ใช้เช็คว่ายิงจากท่าไหนถึงอะไรบ้าง
        Vector3 end = transform.position + Vector3.up * travelDistance;

        Gizmos.DrawWireSphere(end, 0.2f);
        Gizmos.DrawLine(transform.position, end);
    }
}
