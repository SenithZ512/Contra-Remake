using UnityEngine;

/// <summary>
/// ระเบิดสไตล์ Contra NES
///
/// หัวใจอยู่ที่ "เดินเป็นเฟรม" ไม่ใช่ไหลลื่น — เครื่อง NES วาดอนิเมชั่น
/// ราว 12-15 ภาพต่อวินาที ทุกอย่างเลยกระตุกเป็นขั้นๆ
/// ถ้าใช้ Lerp ไหลต่อเนื่องจะได้ความรู้สึกแบบเกมยุคใหม่ ไม่ใช่ NES
///
/// และสีเปลี่ยนเป็นขั้น ขาว -> เหลือง -> ส้ม -> แดง ไม่ค่อยๆ จางลง
/// เพราะ NES สลับพาเลตต์ทีละสี ทำสีไล่เฉดไม่ได้
/// </summary>
public class Explosion : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float duration = 0.35f;

    /// <summary>ภาพต่อวินาที ยิ่งต่ำยิ่งกระตุกแบบเครื่องเก่า</summary>
    [SerializeField] private int frameRate = 14;

    [Header("Palette (ไล่สีเป็นขั้น ไม่ไล่เฉด)")]
    [SerializeField] private Color[] palette =
    {
        new Color(1f, 1f, 1f),
        new Color(1f, 0.95f, 0.4f),
        new Color(1f, 0.6f, 0.1f),
        new Color(0.9f, 0.2f, 0.1f)
    };

    [Header("Core (ลูกไฟกลาง)")]
    [SerializeField] private float coreStartScale = 0.5f;
    [SerializeField] private float coreEndScale = 1.6f;

    [Header("Puff (ก้อนที่กระจายออก)")]
    [SerializeField] private int puffCount = 6;
    [SerializeField] private float puffDistance = 1.2f;
    [SerializeField] private float puffScale = 0.55f;

    /// <summary>บิดมุมเริ่มต้นแบบสุ่ม ระเบิดแต่ละลูกจะได้ไม่เหมือนกันเป๊ะ</summary>
    [SerializeField] private bool randomizeRotation = true;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Transform[] puffs;
    private SpriteRenderer[] puffRenderers;
    private float elapsed;
    private int lastFrame = -1;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        BuildPuffs();

        Draw(0f);
    }

    private void BuildPuffs()
    {
        if (spriteRenderer == null || puffCount <= 0)
            return;

        puffs = new Transform[puffCount];
        puffRenderers = new SpriteRenderer[puffCount];

        float spin = randomizeRotation ? Random.Range(0f, 360f) : 0f;

        for (int i = 0; i < puffCount; i++)
        {
            GameObject puff = new GameObject("Puff");

            puff.transform.SetParent(transform, false);

            SpriteRenderer renderer = puff.AddComponent<SpriteRenderer>();

            renderer.sprite = spriteRenderer.sprite;
            renderer.sortingLayerID = spriteRenderer.sortingLayerID;

            // วางไว้หลังลูกไฟกลางหนึ่งชั้น แกนกลางจะได้เด่นสุด
            renderer.sortingOrder = spriteRenderer.sortingOrder - 1;

            float angle = (360f / puffCount) * i + spin;

            puff.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            puffs[i] = puff.transform;
            puffRenderers[i] = renderer;
        }
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        if (elapsed >= duration)
        {
            Destroy(gameObject);
            return;
        }

        // ปัดเวลาลงเป็นเฟรม ค่าระหว่างเฟรมไม่ขยับเลย ได้ความกระตุกแบบ NES
        int frame = Mathf.FloorToInt(elapsed * frameRate);

        if (frame == lastFrame)
            return;

        lastFrame = frame;

        int totalFrames = Mathf.Max(1, Mathf.RoundToInt(duration * frameRate));

        Draw(Mathf.Clamp01((float)frame / totalFrames));
    }

    private void Draw(float progress)
    {
        Color tint = PickColor(progress);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = tint;

            transform.localScale = Vector3.one *
                Mathf.Lerp(coreStartScale, coreEndScale, progress);
        }

        if (puffs == null)
            return;

        for (int i = 0; i < puffs.Length; i++)
        {
            // ก้อนพุ่งออกแล้วหดเล็กลง ตรงข้ามกับแกนกลางที่พองขึ้น
            puffs[i].localPosition =
                puffs[i].localRotation * Vector3.right *
                (puffDistance * progress);

            puffs[i].localScale =
                Vector3.one * puffScale * (1f - progress * 0.6f);

            puffRenderers[i].color = tint;
        }
    }

    /// <summary>เลือกสีจากพาเลตต์ตรงๆ ไม่ผสมระหว่างสี</summary>
    private Color PickColor(float progress)
    {
        if (palette == null || palette.Length == 0)
            return Color.white;

        int index = Mathf.Clamp(
            Mathf.FloorToInt(progress * palette.Length),
            0,
            palette.Length - 1
        );

        return palette[index];
    }
}
