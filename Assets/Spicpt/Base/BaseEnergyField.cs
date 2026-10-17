using UnityEngine;

/// <summary>
/// รั้วไฟฟ้า Energy Field ที่กั้นไม่ให้เดินเข้าห้องถัดไป
///
/// ไม่ได้ทำดาเมจ และไม่ได้เป็นคอลไลเดอร์ด้วยซ้ำ
/// มันแค่เป็นธงว่า "ห้องยังไม่เคลียร์" ตอนผู้เล่นกดขึ้น BasePlayer จะไปเช็คเอง
/// ตัวนี้มีหน้าที่แค่ทำให้เห็นภาพว่ารั้วยังอยู่
///
/// พอแกนกลางพังครบ กำแพงจะระเบิดแล้วรั้วดับตามในไม่กี่วินาที
/// </summary>
public class BaseEnergyField : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private SpriteRenderer[] bars;

    [SerializeField] private Color colorA = new Color(0.55f, 0.95f, 1f);
    [SerializeField] private Color colorB = new Color(1f, 1f, 1f);

    /// <summary>สลับสีไปมาเร็วแค่ไหน ต้นฉบับกะพริบถี่มาก</summary>
    [SerializeField] private float flickerSpeed = 14f;

    /// <summary>ยืดหดตามแนวตั้งนิดหน่อย ให้ดูเหมือนกระแสไฟวิ่ง</summary>
    [SerializeField] private float pulseAmount = 0.25f;

    [SerializeField] private float pulseSpeed = 9f;

    [Header("Fade Out")]
    /// <summary>แกนพังแล้วรั้วค่อยๆ ดับ ไม่ได้หายทันที</summary>
    [SerializeField] private float fadeDuration = 0.4f;

    private bool on = true;
    private float fade = 1f;
    private Vector3[] baseScales;

    private void Awake()
    {
        if (bars == null || bars.Length == 0)
        {
            bars = GetComponentsInChildren<SpriteRenderer>();
        }

        baseScales = new Vector3[bars.Length];

        for (int i = 0; i < bars.Length; i++)
        {
            if (bars[i] != null)
            {
                baseScales[i] = bars[i].transform.localScale;
            }
        }
    }

    public void SetActive(bool value)
    {
        on = value;

        if (value)
        {
            fade = 1f;
        }
    }

    private void Update()
    {
        if (!on && fade > 0f)
        {
            fade -= fadeDuration > 0f
                ? Time.deltaTime / fadeDuration
                : 1f;

            fade = Mathf.Max(0f, fade);
        }

        for (int i = 0; i < bars.Length; i++)
        {
            SpriteRenderer bar = bars[i];

            if (bar == null)
                continue;

            if (fade <= 0f)
            {
                bar.enabled = false;
                continue;
            }

            bar.enabled = true;

            // เฟสต่างกันทีละแถบ กระแสไฟจะได้ดูไหลไปตามแนว ไม่กะพริบพร้อมกันหมด
            float phase = i * 0.7f;

            float wave = Mathf.Repeat(Time.time * flickerSpeed + phase, 2f) < 1f
                ? 0f
                : 1f;

            Color color = Color.Lerp(colorA, colorB, wave);
            color.a = fade;

            bar.color = color;

            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed + phase) *
                pulseAmount;

            bar.transform.localScale = new Vector3(
                baseScales[i].x,
                baseScales[i].y * pulse,
                baseScales[i].z
            );
        }
    }
}
