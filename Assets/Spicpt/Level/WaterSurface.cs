using UnityEngine;

/// <summary>
/// ผิวน้ำไหวแบบ Contra - ขยับแค่ "ภาพ" ตัวออบเจกต์ไม่ขยับเลย
///
/// ตำแหน่งอยู่กับที่ ขนาดเท่าเดิม จึงไม่มีทางเกิดรอยต่อระหว่างผืน
/// และไม่กระทบ WaterZone / WaterFloor ที่ยึดกับตำแหน่งเดิม
///
/// ทำสองอย่างซ้อนกัน
///   1. เลื่อนลายในเนื้อภาพ (UV) ต้องตั้ง Wrap Mode ของรูปเป็น Repeat
///   2. ไล่เฉดสีไปมา เลียนแบบ palette cycling ของเครื่อง NES
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class WaterSurface : MonoBehaviour
{
    [Header("Scroll (เลื่อนลายในภาพ)")]
    [SerializeField] private bool scrollTexture = true;

    /// <summary>รอบของลายต่อวินาที ค่าลบ = ไหลไปทางซ้าย</summary>
    [SerializeField] private float scrollSpeed = -0.08f;

    [Header("Shimmer (ไล่เฉดสีแบบ NES)")]
    [SerializeField] private bool shimmer = true;

    [SerializeField] private Color darkColor = new Color(0.78f, 0.88f, 1f);
    [SerializeField] private Color brightColor = Color.white;
    [SerializeField] private float shimmerSpeed = 2.2f;

    /// <summary>
    /// เลื่อนจังหวะกะพริบ ผืนที่วางติดกันต้องตั้งเท่ากัน
    /// ถ้าตั้งต่างกันจะเห็นเป็นแถบสีคนละเฉดตรงรอยต่อ
    /// </summary>
    [SerializeField] private float shimmerPhase;

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock block;

    /// <summary>ค่า tiling/offset อยู่ที่ _MainTex_ST ไม่ใช่ _MainTex</summary>
    private static readonly int MainTexStId =
        Shader.PropertyToID("_MainTex_ST");

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        block = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (spriteRenderer == null)
            return;

        if (scrollTexture)
        {
            ScrollTexture();
        }

        if (shimmer)
        {
            float wave =
                (Mathf.Sin(Time.time * shimmerSpeed + shimmerPhase) + 1f) * 0.5f;

            spriteRenderer.color = Color.Lerp(darkColor, brightColor, wave);
        }
    }

    private void ScrollTexture()
    {
        // ใช้ MaterialPropertyBlock ไม่ใช่ renderer.material
        // การแตะ .material จะโคลน material ใหม่ให้ทุกผืน กินหน่วยความจำฟรีๆ
        spriteRenderer.GetPropertyBlock(block);

        float offset = Mathf.Repeat(scrollSpeed * Time.time, 1f);

        block.SetVector(
            MainTexStId,
            new Vector4(1f, 1f, offset, 0f)
        );

        spriteRenderer.SetPropertyBlock(block);
    }
}
