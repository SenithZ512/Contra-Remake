using UnityEngine;

/// <summary>
/// ตัวแปลงพิกัดของด่านฐาน (Base 1 / Base 2)
///
/// ต้นฉบับ NES วางของด้วยพิกัดจอ 256x240 พิกเซล เราเลยเก็บเลขชุดนั้นไว้ทั้งดุ้น
/// แล้วค่อยแปลงเป็นหน่วย Unity ตอนสร้างฉาก
/// อยากขยับอะไรให้ตรงต้นฉบับ เปิดตารางใน ROM แล้วใส่เลขพิกเซลได้เลย
///
/// กล้องเกมนี้ orthographic size 5 = สูง 10 หน่วย
/// จอ NES สูง 240 px จึงได้ 24 พิกเซลต่อหนึ่งหน่วย พอดีเป๊ะ
/// </summary>
public static class BaseStageGeometry
{
    public const float PixelsPerUnit = 24f;

    public const float ScreenWidthPx = 256f;
    public const float ScreenHeightPx = 240f;

    /// <summary>
    /// เส้นพื้นที่ผู้เล่นยืน
    ///
    /// ต้นฉบับตรึงไว้ที่ $a8 = 168 ตายตัว แต่เลขนั้นคือขอบบนของสไปรท์
    /// ตามที่ NES เก็บค่า Y ใน OAM ส่วนจุดหมุนสไปรท์ของเราอยู่ที่เท้า
    /// เลยต้องบวกความสูงตัวละครอีกราว 32 พิกเซลถึงจะได้ระยะเท่าต้นฉบับ
    ///
    /// ถ้ายังรู้สึกว่าใกล้กำแพงไป เพิ่มเลขนี้ได้เลย ของทั้งด่านขยับตาม
    /// </summary>
    public const float PlayerPixelY = 212f;

    /// <summary>ขอบเดินในห้องทางเดิน ($30 ถึง $d0)</summary>
    public const float CorridorLeftPx = 48f;
    public const float CorridorRightPx = 208f;

    /// <summary>ขอบเดินในห้องบอส กว้างกว่าห้องทางเดิน ($20 ถึง $e0)</summary>
    public const float BossRoomLeftPx = 32f;
    public const float BossRoomRightPx = 224f;

    /// <summary>ระนาบไกลสุด ใช้คิดขนาดของที่อยู่ลึกเข้าไปในจอ</summary>
    public const float WallPixelY = 64f;

    /// <summary>ของที่อยู่ติดตัวเราขนาดเต็ม ของที่ติดผนังหลังเหลือเท่านี้</summary>
    public const float NearScale = 1f;
    public const float FarScale = 0.4f;

    public static float ToWorldX(float pixelX)
    {
        return (pixelX - ScreenWidthPx * 0.5f) / PixelsPerUnit;
    }

    /// <summary>แกน Y ของ NES นับลงล่าง ของ Unity นับขึ้นบน เลยต้องกลับทิศ</summary>
    public static float ToWorldY(float pixelY)
    {
        return (ScreenHeightPx * 0.5f - pixelY) / PixelsPerUnit;
    }

    public static Vector2 ToWorld(float pixelX, float pixelY)
    {
        return new Vector2(ToWorldX(pixelX), ToWorldY(pixelY));
    }

    public static float FloorY
    {
        get { return ToWorldY(PlayerPixelY); }
    }

    public static float WallY
    {
        get { return ToWorldY(WallPixelY); }
    }

    /// <summary>0 = อยู่ติดหน้าจอ, 1 = อยู่ติดผนังหลัง</summary>
    public static float DepthAt(float worldY)
    {
        return Mathf.Clamp01(
            Mathf.InverseLerp(FloorY, WallY, worldY)
        );
    }

    /// <summary>ขนาดที่ควรเป็นเมื่ออยู่ลึกเท่านี้ ใช้หลอกตาว่าเป็นสามมิติ</summary>
    public static float ScaleAt(float worldY)
    {
        return Mathf.Lerp(NearScale, FarScale, DepthAt(worldY));
    }
}
