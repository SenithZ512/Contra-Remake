using UnityEngine;

/// <summary>
/// จอวาบสั้นๆ ใช้ตอนไอเทมนกเหยี่ยวทำงาน
///
/// ต้นฉบับตั้ง FALCON_FLASH_TIMER ไว้ที่ $20 = 32 เฟรม = ราวครึ่งวินาที
/// แล้วสลับพาเลตต์ทั้งจอระหว่างนั้น
/// เราใช้แผ่นสีทับหน้ากล้องแทน ได้ผลใกล้เคียงโดยไม่ต้องแตะเชเดอร์
/// </summary>
public class ScreenFlash : MonoBehaviour
{
    [Header("Flash")]
    /// <summary>32 เฟรมของต้นฉบับ</summary>
    [SerializeField] private float duration = 0.53f;

    /// <summary>สลับสีกี่ครั้งต่อวินาที ยิ่งถี่ยิ่งเหมือนเครื่องเก่า</summary>
    [SerializeField] private float blinkRate = 16f;

    [SerializeField] private Color[] colors =
    {
        new Color(1f, 1f, 1f, 0.55f),
        new Color(1f, 0.8f, 0.3f, 0.35f),
        new Color(0.6f, 0.9f, 1f, 0.45f),
        new Color(0f, 0f, 0f, 0f)
    };

    [Header("Look")]
    [SerializeField] private SpriteRenderer sheet;
    [SerializeField] private int sortingOrder = 200;

    private float elapsed;

    private void Start()
    {
        if (sheet == null)
        {
            sheet = GetComponentInChildren<SpriteRenderer>();
        }

        FitToCamera();
    }

    /// <summary>ยืดแผ่นสีให้คลุมจอ แล้วเกาะไปกับกล้องเผื่อกล้องเลื่อน</summary>
    private void FitToCamera()
    {
        Camera cam = Camera.main;

        if (cam == null || sheet == null)
            return;

        transform.SetParent(cam.transform, false);
        transform.localPosition = new Vector3(0f, 0f, 1f);

        sheet.sortingOrder = sortingOrder;

        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;

        Vector2 size = sheet.sprite != null
            ? sheet.sprite.bounds.size
            : Vector2.one;

        if (size.x <= 0f || size.y <= 0f)
            return;

        // เผื่อขอบไว้นิดหน่อย กันเห็นขอบแผ่นตอนอัตราส่วนจอแปลกๆ
        transform.localScale = new Vector3(
            width / size.x * 1.2f,
            height / size.y * 1.2f,
            1f
        );
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        if (elapsed >= duration)
        {
            Destroy(gameObject);
            return;
        }

        if (sheet == null || colors == null || colors.Length == 0)
            return;

        int index = Mathf.FloorToInt(elapsed * blinkRate) % colors.Length;

        sheet.color = colors[index];
    }
}
