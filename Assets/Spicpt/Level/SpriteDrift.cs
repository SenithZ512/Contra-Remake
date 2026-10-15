using UnityEngine;

/// <summary>
/// ลอยไปมาเบาๆ รอบจุดที่วางไว้ ใช้กับเมฆหรือของประดับฉากหลัง
///
/// ใช้ sine ทั้งสองแกน ไม่ใช่วิ่งไปกลับแบบเส้นตรง
/// จะได้ไม่มีจังหวะกระตุกตอนกลับทิศ เหมาะกับของที่ควรดูนุ่มๆ
///
/// ขยับแค่ตำแหน่ง ไม่แตะขนาดหรือสไปรท์ ของประดับเลยไม่มีปัญหารอยต่อแบบผืนน้ำ
/// </summary>
public class SpriteDrift : MonoBehaviour
{
    [Header("Drift")]
    /// <summary>ระยะลอยแต่ละแกน</summary>
    [SerializeField] private Vector2 amplitude = new Vector2(1.2f, 0f);

    /// <summary>รอบต่อวินาทีของแต่ละแกน ตั้งไม่เท่ากันทางลอยจะเป็นวงรีดูเป็นธรรมชาติ</summary>
    [SerializeField] private Vector2 speed = new Vector2(0.04f, 0f);

    /// <summary>เลื่อนจังหวะ ของที่วางใกล้กันตั้งต่างกันจะได้ไม่ลอยพร้อมกันเป๊ะ</summary>
    [SerializeField] private Vector2 phase;

    [Header("Shimmer")]
    [SerializeField] private bool shimmer;
    [SerializeField] private Color darkColor = new Color(0.85f, 0.88f, 1f);
    [SerializeField] private Color brightColor = Color.white;
    [SerializeField] private float shimmerSpeed = 1.1f;

    private Vector2 anchor;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        anchor = transform.position;

        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        float x = Mathf.Sin(
            Time.time * speed.x * Mathf.PI * 2f + phase.x
        ) * amplitude.x;

        float y = Mathf.Sin(
            Time.time * speed.y * Mathf.PI * 2f + phase.y
        ) * amplitude.y;

        transform.position = new Vector3(
            anchor.x + x,
            anchor.y + y,
            transform.position.z
        );

        if (!shimmer || spriteRenderer == null)
            return;

        float wave =
            (Mathf.Sin(Time.time * shimmerSpeed + phase.x) + 1f) * 0.5f;

        spriteRenderer.color = Color.Lerp(darkColor, brightColor, wave);
    }
}
