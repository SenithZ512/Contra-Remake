using UnityEngine;

/// <summary>
/// พื้นเลื่อนที่พาผู้เล่นไปด้วย
///
/// ใช้คู่กับ PathMover (หรือจะขยับด้วยอะไรก็ได้) — ตัวนี้แค่ดูว่า
/// เฟรมนี้พื้นขยับไปเท่าไหร่ แล้วดันคนที่ยืนอยู่ข้างบนไปเท่านั้น
///
/// ต้องทำเองเพราะฟิสิกส์ 2D ของ Unity ไม่พาวัตถุไปกับพื้นที่เคลื่อนที่ให้
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MovingPlatform : MonoBehaviour
{
    [Header("Rider")]
    [SerializeField] private LayerMask riderLayer = ~0;

    /// <summary>ความสูงของโซนตรวจเหนือผิวพื้น ที่ถือว่า "ยืนอยู่บนนี้"</summary>
    [SerializeField] private float checkHeight = 0.25f;

    /// <summary>หดความกว้างโซนตรวจเข้ามา กันจับคนที่แค่เดินเฉียดขอบ</summary>
    [SerializeField] private float widthPadding = 0.05f;

    private Collider2D surface;
    private Vector2 lastPosition;

    private void Awake()
    {
        surface = GetComponent<Collider2D>();

        lastPosition = transform.position;
    }

    // ต้องอยู่ใน FixedUpdate จังหวะเดียวกับที่ PathMover ขยับ
    // ถ้าทำใน LateUpdate ระยะจะไม่ตรงกับสเต็ปฟิสิกส์ ตัวละครจะไถลหลุดพื้น
    private void FixedUpdate()
    {
        Vector2 delta = (Vector2)transform.position - lastPosition;

        lastPosition = transform.position;

        if (delta.sqrMagnitude < 0.0000001f)
            return;

        CarryRiders(delta);
    }

    private void CarryRiders(Vector2 delta)
    {
        if (surface == null)
            return;

        Bounds area = surface.bounds;

        Vector2 center = new Vector2(
            area.center.x,
            area.max.y + checkHeight * 0.5f
        );

        Vector2 size = new Vector2(
            Mathf.Max(0.05f, area.size.x - widthPadding * 2f),
            checkHeight
        );

        Collider2D[] riders = Physics2D.OverlapBoxAll(
            center,
            size,
            0f,
            riderLayer
        );

        foreach (Collider2D rider in riders)
        {
            if (rider == surface)
                continue;

            Rigidbody2D body = rider.attachedRigidbody;

            if (body == null)
                continue;

            // บวกเข้า position ตรงๆ ไม่ใช้ MovePosition
            // MovePosition ออกแบบมาสำหรับ Kinematic ใช้กับ Dynamic อย่างผู้เล่น
            // จะตีกับแรงโน้มถ่วงจนตัวสั่นและหลุดพื้น
            body.position += delta;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D preview = GetComponent<Collider2D>();

        if (preview == null)
            return;

        Bounds area = preview.bounds;

        Gizmos.DrawWireCube(
            new Vector3(area.center.x, area.max.y + checkHeight * 0.5f, 0f),
            new Vector3(
                Mathf.Max(0.05f, area.size.x - widthPadding * 2f),
                checkHeight,
                0f
            )
        );
    }
}
