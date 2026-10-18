using UnityEngine;

/// <summary>
/// ล็อกอัตราส่วนภาพให้เท่าเครื่องเกมต้นฉบับ ส่วนที่เหลือเติมแถบดำ
///
/// เครื่อง NES ส่งภาพออกมา 256x240 พิกเซล แต่ทีวียุคนั้นเป็นจอ 4:3
/// ภาพจึงถูกยืดออกข้างตอนแสดงผล พิกเซลไม่ได้เป็นสี่เหลี่ยมจัตุรัส
///
/// จอของเราเป็น 16:10 ซึ่งกว้างกว่ามาก ถ้าไม่ล็อกไว้จะเห็นฉากกว้างเกินจริง
/// และของที่ออกแบบมาให้พอดีจอ NES จะลอยอยู่กลางจอมีที่ว่างรอบๆ
///
/// ใส่สคริปต์นี้ที่กล้องหลัก แล้วมันจะย่อพื้นที่วาดให้ได้สัดส่วนที่ตั้งไว้
/// ทำงานทั้งใน Game view และในเกมที่บิลด์ออกไป ไม่ว่าหน้าต่างจะขนาดไหน
/// </summary>
[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class ScreenAspect : MonoBehaviour
{
    public enum Preset
    {
        /// <summary>4:3 แบบทีวียุคนั้น ภาพถูกยืดออกข้างเหมือนของจริง</summary>
        Television,

        /// <summary>256:240 ตามสัญญาณจริงของเครื่อง พิกเซลเป็นจัตุรัส</summary>
        NesSignal,

        /// <summary>ใส่ตัวเลขเอง</summary>
        Custom
    }

    [Header("Aspect")]
    [SerializeField] private Preset preset = Preset.Television;

    /// <summary>ใช้เมื่อเลือก Custom</summary>
    [SerializeField] private float customAspect = 4f / 3f;

    [Header("Framing")]
    /// <summary>
    /// ตั้งระยะซูมให้ความกว้างที่เห็นเท่าจอ NES พอดี
    ///
    /// เปิดแล้วฉากที่วาดมาเต็มจอ NES จะเต็มกรอบพอดีไม่มีที่ว่างข้าง
    /// แต่แลกมาด้วยการที่มองเห็นแนวตั้งน้อยลง เพราะกรอบ 4:3 เตี้ยกว่าสัดส่วนเดิม
    ///
    /// ปิดไว้ = คงระยะซูมเดิม เห็นกว้างกว่าจอ NES
    /// </summary>
    [SerializeField] private bool matchNesWidth;

    /// <summary>ความกว้างจอ NES เป็นหน่วย Unity = 256 px หาร 24 px ต่อหน่วย</summary>
    [SerializeField] private float nesWidthUnits = 256f / 24f;

    [Header("Letterbox")]
    /// <summary>
    /// สร้างกล้องพื้นหลังสีดำให้อัตโนมัติ
    ///
    /// พื้นที่นอกกรอบจะไม่ถูกวาดเลย ถ้าไม่มีอะไรล้างไว้จะเห็นภาพค้างจากเฟรมก่อน
    /// โดยเฉพาะตอนเลื่อนหน้าต่าง เปิดไว้ดีกว่า
    /// </summary>
    [SerializeField] private bool addBlackBackdrop = true;

    private Camera view;
    private int lastWidth;
    private int lastHeight;
    private Camera backdrop;

    public float TargetAspect
    {
        get
        {
            switch (preset)
            {
                case Preset.Television: return 4f / 3f;
                case Preset.NesSignal: return 256f / 240f;
                default: return Mathf.Max(0.1f, customAspect);
            }
        }
    }

    private void OnEnable()
    {
        view = GetComponent<Camera>();

        Apply();
    }

    private void Update()
    {
        // หน้าต่างเปลี่ยนขนาดเมื่อไหร่ก็คำนวณใหม่ ไม่ต้องรอรีสตาร์ต
        if (Screen.width == lastWidth && Screen.height == lastHeight)
            return;

        Apply();
    }

    private void Apply()
    {
        if (view == null)
            return;

        lastWidth = Screen.width;
        lastHeight = Screen.height;

        if (lastHeight <= 0)
            return;

        float target = TargetAspect;
        float window = lastWidth / (float)lastHeight;

        // อัตราส่วนความสูงที่ใช้ได้จริงเทียบกับทั้งหน้าต่าง
        float scaleHeight = window / target;

        if (scaleHeight < 1f)
        {
            // หน้าต่างแคบกว่าที่ต้องการ เติมแถบดำบนล่าง
            view.rect = new Rect(0f, (1f - scaleHeight) * 0.5f, 1f, scaleHeight);
        }
        else
        {
            // หน้าต่างกว้างกว่า เติมแถบดำซ้ายขวา (กรณีจอ 16:10 ของเรา)
            float scaleWidth = 1f / scaleHeight;

            view.rect = new Rect((1f - scaleWidth) * 0.5f, 0f, scaleWidth, 1f);
        }

        ApplyZoom(target);
        ApplyBackdrop();
    }

    private void ApplyZoom(float target)
    {
        if (!matchNesWidth || !view.orthographic || nesWidthUnits <= 0f)
            return;

        // orthographicSize คือครึ่งความสูง ความกว้าง = สูง x อัตราส่วน
        view.orthographicSize = nesWidthUnits / target * 0.5f;
    }

    private void ApplyBackdrop()
    {
        if (!addBlackBackdrop)
            return;

        if (backdrop == null)
        {
            Transform found = transform.Find("Letterbox Backdrop");

            backdrop = found != null ? found.GetComponent<Camera>() : null;
        }

        if (backdrop == null)
        {
            GameObject host = new GameObject("Letterbox Backdrop");

            host.transform.SetParent(transform, false);

            backdrop = host.AddComponent<Camera>();
        }

        // วาดก่อนกล้องหลัก ล้างทั้งจอเป็นสีดำ แล้วไม่วาดอะไรเลย
        backdrop.depth = view.depth - 1;
        backdrop.clearFlags = CameraClearFlags.SolidColor;
        backdrop.backgroundColor = Color.black;
        backdrop.cullingMask = 0;
        backdrop.rect = new Rect(0f, 0f, 1f, 1f);
        backdrop.orthographic = true;
    }
}
