using UnityEngine;

/// <summary>
/// ก้อนหินร่วงจากหน้าผา แบบด่าน Waterfall
///
/// ถอดจาก falling_rock_routine ของ ROM มีสองช่วงชัดเจน
///
///   ช่วงที่ 1 โผล่ออกมาจากรูแล้ว "ส่าย" ซ้ายขวาอยู่กับที่ราวหนึ่งวินาที
///             ช่วงนี้ยังชนไม่ได้เลย เป็นการบอกล่วงหน้าว่าเดี๋ยวจะร่วงตรงนี้
///             (ต้นฉบับเรียก enable_enemy_collision ตอนจบช่วงนี้เท่านั้น)
///
///   ช่วงที่ 2 ร่วงลงมา ชนพื้นแล้ว "เด้งเบาๆ" แล้วร่วงต่อไปพื้นชั้นถัดไป
///             เด้งด้วยความเร็วคงที่ทุกครั้ง ไม่ได้ลดหลั่นลงเรื่อยๆ แบบลูกบอลจริง
///             เด้งสูงราว 12.5 พิกเซลเท่ากันหมด
///
/// กันเด้งซ้ำที่เดิมด้วยการจำตำแหน่งพื้นที่เพิ่งชน แล้วไม่ตรวจอีกจนกว่าจะร่วงต่ำลงไป
/// 16 พิกเซล ตรงกับที่ต้นฉบับเก็บไว้ใน ENEMY_VAR_1
/// </summary>
public class FallingRock : MonoBehaviour
{
    [Header("Wobble (ช่วงส่ายก่อนร่วง)")]
    /// <summary>ส่ายอยู่กี่วินาทีก่อนร่วง ต้นฉบับ 64 เฟรม</summary>
    [SerializeField] private float wobbleDuration = 1.07f;

    /// <summary>ส่ายออกข้างละเท่าไหร่ ต้นฉบับขยับทีละ 1 พิกเซล รวมราว 2 พิกเซล</summary>
    [SerializeField] private float wobbleAmplitude = 0.085f;

    /// <summary>รอบต่อวินาที ต้นฉบับกลับทางทุก 8 เฟรม</summary>
    [SerializeField] private float wobbleRate = 3.75f;

    [Header("Fall")]
    /// <summary>ต้นฉบับบวกความเร็วตก 0.0625 พิกเซลต่อเฟรมกำลังสอง</summary>
    [SerializeField] private float gravity = 9.375f;

    /// <summary>เพดานความเร็วตก กันหินทะลุพื้นตอนร่วงจากที่สูงมาก</summary>
    [SerializeField] private float maxFallSpeed = 14f;

    [Header("Bounce")]
    /// <summary>
    /// ความเร็วเด้งขึ้นตอนชนพื้น ต้นฉบับตั้ง -1.25 พิกเซลต่อเฟรม
    /// เป็นค่าคงที่ ไม่ลดลงตามจำนวนครั้งที่เด้ง
    /// </summary>
    [SerializeField] private float bounceSpeed = 3.125f;

    /// <summary>ระยะที่หยั่งหาพื้นข้างล่าง ต้นฉบับใช้ 8 พิกเซล</summary>
    [SerializeField] private float groundProbe = 0.333f;

    /// <summary>
    /// ต้องร่วงต่ำกว่าจุดที่เพิ่งชนเท่านี้ก่อนถึงจะตรวจพื้นอีกครั้ง
    /// ต้นฉบับใช้ 16 พิกเซล กันเด้งค้างอยู่ที่ขอบพื้นเดิม
    /// </summary>
    [SerializeField] private float groundStep = 0.667f;

    [SerializeField] private LayerMask groundLayer;

    [Header("Tumble")]
    /// <summary>หมุนตอนร่วง ต้นฉบับพลิกภาพทุก 4 เฟรม เราใช้หมุนต่อเนื่องแทน</summary>
    [SerializeField] private float tumbleSpeed = 220f;

    [Header("Despawn")]
    /// <summary>ต่ำกว่าขอบจอเท่านี้แล้วหายไป</summary>
    [SerializeField] private float despawnBelowCamera = 2f;

    [SerializeField] private float lifetime = 20f;

    [Header("Impact")]
    [SerializeField] private GameObject landEffectPrefab;

    /// <summary>เอฟเฟกต์ตอนชนพื้นครั้งแรกเท่านั้น ครั้งถัดไปไม่ต้องซ้ำ</summary>
    [SerializeField] private bool effectOnFirstBounceOnly = true;

    [Header("References")]
    [SerializeField] private Collider2D body;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private float startX;
    private float wobbleTimer;
    private float verticalSpeed;
    private float nextProbeY;
    private float life;
    private bool falling;
    private int bounces;

    private void Awake()
    {
        if (body == null)
        {
            body = GetComponent<Collider2D>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Start()
    {
        startX = transform.position.x;

        // ยังชนอะไรไม่ได้ระหว่างส่าย ตรงกับต้นฉบับที่เปิดการชนทีหลัง
        if (body != null)
        {
            body.enabled = false;
        }

        nextProbeY = transform.position.y;
    }

    private void Update()
    {
        life += Time.deltaTime;

        if (life >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (!falling)
        {
            UpdateWobble();
            return;
        }

        UpdateFall();
        Tumble();
        DespawnIfBelowCamera();
    }

    /// <summary>โผล่มาส่ายอยู่กับที่ บอกคนเล่นล่วงหน้าว่าเดี๋ยวหินจะลงตรงนี้</summary>
    private void UpdateWobble()
    {
        wobbleTimer += Time.deltaTime;

        float sway = Mathf.Sin(wobbleTimer * wobbleRate * Mathf.PI * 2f);

        transform.position = new Vector3(
            startX + sway * wobbleAmplitude,
            transform.position.y,
            transform.position.z
        );

        if (wobbleTimer < wobbleDuration)
            return;

        BeginFall();
    }

    private void BeginFall()
    {
        falling = true;

        transform.position = new Vector3(
            startX, transform.position.y, transform.position.z
        );

        if (body != null)
        {
            body.enabled = true;
        }
    }

    private void UpdateFall()
    {
        verticalSpeed -= gravity * Time.deltaTime;

        verticalSpeed = Mathf.Max(verticalSpeed, -maxFallSpeed);

        transform.position += Vector3.up * (verticalSpeed * Time.deltaTime);

        // ยังไม่ร่วงต่ำพอจากจุดที่เพิ่งชน ก็ยังไม่ต้องตรวจพื้น
        if (transform.position.y > nextProbeY)
            return;

        // กำลังลอยขึ้นจากการเด้งอยู่ ไม่ต้องตรวจ
        if (verticalSpeed > 0f)
            return;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, Vector2.down, groundProbe, groundLayer
        );

        if (hit.collider == null)
            return;

        Bounce();
    }

    private void Bounce()
    {
        // ความเร็วเด้งคงที่ทุกครั้ง หินจึงเด้งสูงเท่ากันทุกชั้น ไม่ค่อยๆ เตี้ยลง
        verticalSpeed = bounceSpeed;

        // ตรวจพื้นอีกทีเมื่อร่วงต่ำกว่าจุดนี้ กันเด้งซ้ำที่ขอบพื้นเดิม
        nextProbeY = transform.position.y - groundStep;

        bool showEffect = !effectOnFirstBounceOnly || bounces == 0;

        bounces++;

        if (showEffect && landEffectPrefab != null)
        {
            Instantiate(
                landEffectPrefab, transform.position, Quaternion.identity
            );
        }
    }

    private void Tumble()
    {
        if (Mathf.Approximately(tumbleSpeed, 0f) || spriteRenderer == null)
            return;

        spriteRenderer.transform.Rotate(
            0f, 0f, -tumbleSpeed * Time.deltaTime
        );
    }

    private void DespawnIfBelowCamera()
    {
        Camera view = Camera.main;

        if (view == null || !view.orthographic)
            return;

        float floor = view.transform.position.y -
            view.orthographicSize - despawnBelowCamera;

        if (transform.position.y < floor)
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // เส้นหยั่งพื้นกับระยะที่ต้องร่วงก่อนตรวจซ้ำ
        Gizmos.DrawLine(
            transform.position,
            transform.position + Vector3.down * groundProbe
        );

        Gizmos.DrawWireSphere(
            transform.position + Vector3.down * groundStep, 0.08f
        );
    }
}
