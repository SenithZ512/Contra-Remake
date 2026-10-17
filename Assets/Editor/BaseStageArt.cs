using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// วาดสไปรท์ชั่วคราวของด่านฐานด้วยโค้ด
///
/// เป็นภาพแทนที่ไว้ให้กด Play แล้วเห็นของจริงก่อน ไม่ใช่อาร์ตตัวจบ
/// พอวาดของจริงเสร็จก็เอาไฟล์ไปทับที่เดิมได้เลย ชื่อเดิม ขนาดเดิม
/// ฉากจะหยิบไปใช้ให้เองโดยไม่ต้องแก้อะไร
///
/// ทุกภาพตั้ง 24 พิกเซลต่อหนึ่งหน่วย ให้ตรงกับ BaseStageGeometry
/// </summary>
public static class BaseStageArt
{
    public const string ArtFolder = "Assets/Asset/Base";

    /// <summary>ต้องตรงกับ BaseStageGeometry.PixelsPerUnit</summary>
    private const float PixelsPerUnit = 24f;

    // พาเลตต์แนวฐานศัตรูของ Contra - น้ำเงินเทา ตัดด้วยแดงกับเหลือง
    private static readonly Color32 Void = new Color32(16, 16, 24, 255);
    private static readonly Color32 WallDark = new Color32(32, 40, 64, 255);
    private static readonly Color32 WallMid = new Color32(56, 88, 160, 255);
    private static readonly Color32 WallLight = new Color32(104, 160, 224, 255);
    private static readonly Color32 Highlight = new Color32(216, 232, 248, 255);
    private static readonly Color32 FloorDark = new Color32(40, 48, 72, 255);
    private static readonly Color32 FloorMid = new Color32(64, 72, 104, 255);
    private static readonly Color32 FloorLine = new Color32(96, 128, 184, 255);
    private static readonly Color32 Metal = new Color32(120, 120, 128, 255);
    private static readonly Color32 MetalDark = new Color32(72, 72, 80, 255);
    private static readonly Color32 Red = new Color32(216, 48, 24, 255);
    private static readonly Color32 Yellow = new Color32(248, 184, 0, 255);
    private static readonly Color32 Skin = new Color32(248, 184, 112, 255);
    private static readonly Color32 Pants = new Color32(56, 72, 136, 255);
    private static readonly Color32 Green = new Color32(64, 144, 72, 255);
    private static readonly Color32 Boot = new Color32(96, 88, 80, 255);
    private static readonly Color32 Clear = new Color32(0, 0, 0, 0);

    [MenuItem("Tools/Contra/Base 1/1. Generate Placeholder Art")]
    public static void Run()
    {
        EnsureFolder();

        WriteBackdrop();
        WriteCoreSet();
        WriteTurretSet();
        WritePlayerSet();
        WriteSoldierSet();
        WriteRoller();
        WriteProjectiles();
        WriteBossEye();
        WriteMisc();

        AssetDatabase.Refresh();

        Debug.Log("สร้างสไปรท์ชั่วคราวของ Base 1 เสร็จแล้วที่ " + ArtFolder);
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Asset"))
        {
            AssetDatabase.CreateFolder("Assets", "Asset");
        }

        if (!AssetDatabase.IsValidFolder(ArtFolder))
        {
            AssetDatabase.CreateFolder("Assets/Asset", "Base");
        }
    }

    // ---------- ฉากหลัง ----------

    /// <summary>
    /// ทางเดินในฐาน ผนังหลังอยู่ตรงกลาง พื้นกับผนังข้างลู่เข้าหาจุดรวมสายตา
    /// ขนาด 256x240 เท่าจอ NES เป๊ะ
    /// </summary>
    // ขอบของผนังหลัง คิดแบบจอ NES คือ y นับลงล่าง
    private const int BackLeft = 48;
    private const int BackRight = 208;
    private const int BackTop = 24;
    private const int BackBottom = 140;

    // จุดรวมสายตาอยู่กลางผนังหลัง เส้นลู่ทุกเส้นพุ่งออกจากจุดนี้
    private const int VanishX = 128;
    private const int VanishY = (BackTop + BackBottom) / 2;

    private static void WriteBackdrop()
    {
        Canvas canvas = new Canvas(256, 240);

        canvas.Fill(Void);

        DrawCorridorSurfaces(canvas);
        DrawBackWall(canvas);
        DrawPerspectiveLines(canvas);
        DrawFloorRungs(canvas);

        Save(canvas, "Base_Backdrop", new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// ระบายเพดาน พื้น และผนังสองข้าง
    /// ทั้งหมดเป็นกล่องเพอร์สเปกทีฟจุดเดียว มุมของผนังหลังลากไปหามุมจอ
    /// </summary>
    private static void DrawCorridorSurfaces(Canvas canvas)
    {
        for (int y = 0; y < 240; y++)
        {
            int leftEdge;
            int rightEdge;
            Color32 middle;

            if (y < BackTop)
            {
                // ช่วงเพดาน ขอบลู่เข้าหามุมบนของผนังหลัง
                float t = y / (float)BackTop;

                leftEdge = Mathf.RoundToInt(Mathf.Lerp(0f, BackLeft, t));
                rightEdge = Mathf.RoundToInt(Mathf.Lerp(256f, BackRight, t));
                middle = WallDark;
            }
            else if (y <= BackBottom)
            {
                // ช่วงที่เห็นผนังหลังเต็มๆ ผนังข้างเป็นแถบตรงสองฝั่ง
                leftEdge = BackLeft;
                rightEdge = BackRight;
                middle = WallMid;
            }
            else
            {
                // ช่วงพื้น ขอบบานออกหามุมล่างของจอ
                float t = (y - BackBottom) / (float)(239 - BackBottom);

                leftEdge = Mathf.RoundToInt(Mathf.Lerp(BackLeft, 0f, t));
                rightEdge = Mathf.RoundToInt(Mathf.Lerp(BackRight, 256f, t));

                // พื้นใกล้ตัวสว่างกว่าพื้นไกล
                middle = t < 0.45f ? FloorDark : FloorMid;
            }

            canvas.RectNes(0, y, leftEdge, 1, WallDark);
            canvas.RectNes(leftEdge, y, rightEdge - leftEdge, 1, middle);
            canvas.RectNes(rightEdge, y, 256 - rightEdge, 1, WallDark);
        }
    }

    private static void DrawBackWall(Canvas canvas)
    {
        int w = BackRight - BackLeft;
        int h = BackBottom - BackTop;

        // แผงโลหะเรียงเป็นตาราง ให้ผนังไม่เรียบโล่ง
        for (int y = BackTop + 6; y < BackBottom - 10; y += 16)
        {
            for (int x = BackLeft + 8; x < BackRight - 10; x += 16)
            {
                canvas.RectNes(x, y, 14, 14, WallDark);
                canvas.RectNes(x + 1, y + 1, 12, 12, WallMid);
                canvas.RectNes(x + 1, y + 1, 12, 1, WallLight);
            }
        }

        // กรอบประตูบานใหญ่ที่แกนพลังงานไปเกาะ
        canvas.FrameNes(BackLeft, BackTop, w, h, 2, Metal);

        // ร่องแบ่งบานประตูตรงกลาง
        canvas.RectNes(VanishX - 1, BackTop, 2, h, MetalDark);
    }

    /// <summary>เส้นลู่บนทั้งสี่ผิว ลากจากขอบผนังหลังออกไปหาขอบจอ</summary>
    private static void DrawPerspectiveLines(Canvas canvas)
    {
        // พื้นกับเพดาน ไล่ตามแนวนอนของผนังหลัง
        for (int x = BackLeft; x <= BackRight; x += 20)
        {
            canvas.LineNes(x, BackBottom, ProjectX(x, BackBottom, 239),
                239, FloorLine);

            canvas.LineNes(x, BackTop, ProjectX(x, BackTop, 0), 0, WallMid);
        }

        // ผนังสองข้าง ไล่ตามแนวตั้งของผนังหลัง
        for (int y = BackTop; y <= BackBottom; y += 20)
        {
            canvas.LineNes(BackLeft, y, 0, ProjectY(BackLeft, y, 0), FloorLine);

            canvas.LineNes(BackRight, y, 255, ProjectY(BackRight, y, 255),
                FloorLine);
        }
    }

    /// <summary>ยืดเส้นจากจุดรวมสายตาผ่านจุดบนผนังหลัง ไปจนถึงแถว targetY</summary>
    private static int ProjectX(int x, int y, int targetY)
    {
        if (y == VanishY)
            return x;

        float scale = (targetY - VanishY) / (float)(y - VanishY);

        return Mathf.RoundToInt(VanishX + (x - VanishX) * scale);
    }

    private static int ProjectY(int x, int y, int targetX)
    {
        if (x == VanishX)
            return y;

        float scale = (targetX - VanishX) / (float)(x - VanishX);

        return Mathf.RoundToInt(VanishY + (y - VanishY) * scale);
    }

    /// <summary>เส้นขวางบนพื้น ยิ่งเข้าใกล้ยิ่งห่าง ตาจะอ่านเป็นระยะลึก</summary>
    private static void DrawFloorRungs(Canvas canvas)
    {
        float spacing = 4f;
        float lineY = BackBottom + spacing;

        while (lineY < 240f)
        {
            int y = Mathf.RoundToInt(lineY);
            float t = (y - BackBottom) / (float)(239 - BackBottom);

            int leftEdge = Mathf.RoundToInt(Mathf.Lerp(BackLeft, 0f, t));
            int rightEdge = Mathf.RoundToInt(Mathf.Lerp(BackRight, 256f, t));

            canvas.RectNes(leftEdge, y, rightEdge - leftEdge, 1, FloorLine);

            spacing *= 1.45f;
            lineY += spacing;
        }
    }

    // ---------- แกนพลังงาน ----------

    private static void WriteCoreSet()
    {
        // แผงเกราะคลุมอีกชั้น (ใช้ในด่าน 4)
        Canvas plate = NewCoreBase();
        plate.Rect(4, 4, 16, 16, Metal);
        plate.Line(4, 4, 19, 19, MetalDark);
        plate.Line(4, 19, 19, 4, MetalDark);
        Save(plate, "Base_Core_Plate", Vector2.one * 0.5f);

        // ปิดฝาอยู่ ยิงไม่เข้า
        Canvas closed = NewCoreBase();
        closed.Rect(4, 4, 8, 8, Metal);
        closed.Rect(12, 4, 8, 8, MetalDark);
        closed.Rect(4, 12, 8, 8, MetalDark);
        closed.Rect(12, 12, 8, 8, Metal);
        Save(closed, "Base_Core_Closed", Vector2.one * 0.5f);

        // เปิดแล้ว เห็นแกนแดงข้างใน
        Canvas open = NewCoreBase();
        open.Rect(4, 4, 16, 16, Void);
        open.Disc(12, 12, 6, Red);
        open.Disc(12, 12, 3, Yellow);
        open.Disc(11, 13, 1, Highlight);
        Save(open, "Base_Core_Open", Vector2.one * 0.5f);

        // พังแล้ว
        Canvas broken = NewCoreBase();
        broken.Rect(4, 4, 16, 16, Void);
        broken.Rect(6, 6, 4, 3, MetalDark);
        broken.Rect(14, 15, 5, 3, MetalDark);
        broken.Rect(10, 11, 3, 3, MetalDark);
        Save(broken, "Base_Core_Broken", Vector2.one * 0.5f);
    }

    private static Canvas NewCoreBase()
    {
        Canvas canvas = new Canvas(24, 24);

        canvas.Fill(Clear);
        canvas.Rect(0, 0, 24, 24, MetalDark);
        canvas.Frame(0, 0, 24, 24, 2, Metal);

        return canvas;
    }

    // ---------- ป้อมปืน ----------

    private static void WriteTurretSet()
    {
        Canvas closed = NewTurretBase();
        closed.Rect(4, 4, 12, 12, Metal);
        closed.Rect(4, 9, 12, 2, MetalDark);
        Save(closed, "Base_Turret_Closed", Vector2.one * 0.5f);

        Canvas open = NewTurretBase();
        open.Rect(4, 4, 12, 12, Void);
        open.Disc(10, 10, 5, MetalDark);
        open.Disc(10, 10, 3, Void);
        open.Disc(10, 10, 1, Red);
        Save(open, "Base_Turret_Open", Vector2.one * 0.5f);

        Canvas broken = NewTurretBase();
        broken.Rect(4, 4, 12, 12, Void);
        broken.Rect(5, 6, 4, 3, MetalDark);
        broken.Rect(11, 11, 4, 3, MetalDark);
        Save(broken, "Base_Turret_Broken", Vector2.one * 0.5f);
    }

    private static Canvas NewTurretBase()
    {
        Canvas canvas = new Canvas(20, 20);

        canvas.Fill(Clear);
        canvas.Rect(0, 0, 20, 20, MetalDark);
        canvas.Frame(0, 0, 20, 20, 2, Metal);
        canvas.Rect(8, 17, 4, 2, Red);

        return canvas;
    }

    // ---------- ผู้เล่น (มองจากหลัง) ----------

    private static void WritePlayerSet()
    {
        Save(PlayerBody(0, false), "Base_Player_Stand", BottomPivot());
        Save(PlayerBody(-1, false), "Base_Player_Walk_0", BottomPivot());
        Save(PlayerBody(1, false), "Base_Player_Walk_1", BottomPivot());
        Save(PlayerBody(0, true), "Base_Player_Shock", BottomPivot());

        // กระโดด เก็บขาขึ้นมาใต้ลำตัว เห็นปุ๊บรู้ว่าลอยอยู่
        Canvas jump = new Canvas(16, 28);
        jump.Fill(Clear);
        jump.Rect(3, 6, 4, 3, Boot);
        jump.Rect(9, 6, 4, 3, Boot);
        jump.Rect(4, 9, 3, 4, Pants);
        jump.Rect(9, 9, 3, 4, Pants);
        jump.Rect(4, 12, 8, 2, Pants);
        jump.Rect(4, 14, 8, 8, Skin);
        jump.Line(4, 21, 11, 15, Yellow);
        jump.Rect(1, 19, 3, 2, Skin);
        jump.Rect(12, 19, 3, 2, Skin);
        jump.Rect(5, 22, 6, 4, Skin);
        jump.Rect(5, 24, 6, 3, Yellow);
        jump.Rect(3, 23, 2, 1, Red);
        jump.Rect(13, 17, 2, 3, MetalDark);
        Save(jump, "Base_Player_Jump", BottomPivot());

        // หมอบ ย่อทุกอย่างลงมาอยู่ครึ่งล่าง
        Canvas crouch = new Canvas(16, 28);
        crouch.Fill(Clear);
        crouch.Rect(3, 0, 4, 3, Boot);
        crouch.Rect(9, 0, 4, 3, Boot);
        crouch.Rect(3, 3, 10, 5, Pants);
        crouch.Rect(4, 8, 8, 5, Skin);
        crouch.Rect(5, 13, 6, 4, Skin);
        crouch.Rect(5, 16, 6, 2, Yellow);
        crouch.Rect(4, 16, 2, 1, Red);
        crouch.Rect(11, 9, 3, 2, MetalDark);
        Save(crouch, "Base_Player_Crouch", BottomPivot());

        // ตาย กระจายเป็นชิ้น
        Canvas dead = new Canvas(16, 28);
        dead.Fill(Clear);
        dead.Rect(2, 0, 5, 3, Pants);
        dead.Rect(9, 1, 4, 2, Boot);
        dead.Rect(5, 5, 4, 3, Skin);
        dead.Rect(10, 7, 3, 2, Red);
        Save(dead, "Base_Player_Dead", BottomPivot());
    }

    /// <summary>legShift บอกว่าขาไหนก้าวนำ, arms คือยกแขนขึ้น (ท่าโดนช็อต)</summary>
    private static Canvas PlayerBody(int legShift, bool arms)
    {
        Canvas canvas = new Canvas(16, 28);

        canvas.Fill(Clear);

        // รองเท้ากับขา
        canvas.Rect(3 + legShift, 0, 4, 4, Boot);
        canvas.Rect(9 - legShift, 0, 4, 4, Boot);
        canvas.Rect(4 + legShift, 4, 3, 7, Pants);
        canvas.Rect(9 - legShift, 4, 3, 7, Pants);
        canvas.Rect(4, 10, 8, 2, Pants);

        // ลำตัวเปลือย เห็นสายกระสุนพาดหลัง
        canvas.Rect(4, 12, 8, 9, Skin);
        canvas.Line(4, 20, 11, 13, Yellow);

        // แขน ปกติแนบลำตัว ท่าช็อตยกขึ้นทั้งสองข้าง
        if (arms)
        {
            canvas.Rect(2, 18, 2, 7, Skin);
            canvas.Rect(12, 18, 2, 7, Skin);
        }
        else
        {
            canvas.Rect(2, 14, 2, 6, Skin);
            canvas.Rect(12, 14, 2, 6, Skin);
        }

        // หัว ผมเหลือง ผ้าคาดหัวแดงปลิวไปข้างหลัง
        canvas.Rect(5, 21, 6, 5, Skin);
        canvas.Rect(5, 24, 6, 3, Yellow);
        canvas.Rect(4, 24, 1, 2, Red);
        canvas.Rect(2, 23, 2, 1, Red);

        // ปืนที่ชี้เข้าจอ มองจากหลังเลยเห็นแค่ท้ายปืนสั้นๆ
        if (!arms)
        {
            canvas.Rect(12, 17, 3, 2, MetalDark);
            canvas.Rect(13, 19, 2, 3, Metal);
        }

        return canvas;
    }

    // ---------- ทหาร ----------

    private static void WriteSoldierSet()
    {
        Save(SoldierBody(-1), "Base_Soldier_0", BottomPivot());
        Save(SoldierBody(1), "Base_Soldier_1", BottomPivot());
    }

    private static Canvas SoldierBody(int legShift)
    {
        Canvas canvas = new Canvas(14, 22);

        canvas.Fill(Clear);

        canvas.Rect(2 + legShift, 0, 4, 3, Boot);
        canvas.Rect(8 - legShift, 0, 4, 3, Boot);
        canvas.Rect(3 + legShift, 3, 3, 6, Green);
        canvas.Rect(8 - legShift, 3, 3, 6, Green);
        canvas.Rect(3, 9, 8, 7, Green);
        canvas.Rect(1, 10, 2, 5, Green);
        canvas.Rect(11, 10, 2, 5, Green);
        canvas.Rect(4, 16, 6, 5, Skin);
        canvas.Rect(4, 19, 6, 2, MetalDark);

        return canvas;
    }

    // ---------- ลูกกลิ้ง Darr ----------

    private static void WriteRoller()
    {
        Canvas canvas = new Canvas(16, 16);

        canvas.Fill(Clear);
        canvas.Disc(8, 8, 7, MetalDark);
        canvas.Disc(8, 8, 6, Metal);
        canvas.Disc(8, 8, 4, Yellow);
        canvas.Disc(8, 8, 2, Red);

        Save(canvas, "Base_Roller", Vector2.one * 0.5f);
    }

    // ---------- กระสุนและวงแหวนไฟ ----------

    private static void WriteProjectiles()
    {
        Canvas bullet = new Canvas(6, 6);
        bullet.Fill(Clear);
        bullet.Disc(3, 3, 3, Yellow);
        bullet.Disc(3, 3, 1, Highlight);
        Save(bullet, "Base_Bullet", Vector2.one * 0.5f);

        Canvas shot = new Canvas(8, 8);
        shot.Fill(Clear);
        shot.Disc(4, 4, 4, Red);
        shot.Disc(4, 4, 2, Yellow);
        Save(shot, "Base_EnemyShot", Vector2.one * 0.5f);

        // วงแหวนไฟของบอส กลวงตรงกลาง แบบต้นฉบับ
        Canvas ring = new Canvas(20, 20);
        ring.Fill(Clear);
        ring.Ring(10, 10, 9, 3, WallLight);
        ring.Ring(10, 10, 7, 1, Highlight);
        Save(ring, "Base_FireRing", Vector2.one * 0.5f);
    }

    // ---------- บอส Garmakilma ----------

    private static void WriteBossEye()
    {
        Save(BossFrame(0), "Base_BossEye_0", Vector2.one * 0.5f);
        Save(BossFrame(1), "Base_BossEye_1", Vector2.one * 0.5f);
    }

    private static Canvas BossFrame(int frame)
    {
        Canvas canvas = new Canvas(48, 44);

        canvas.Fill(Clear);

        // เปลือกโลหะทรงกลม
        canvas.Disc(24, 22, 22, MetalDark);
        canvas.Disc(24, 22, 20, Metal);

        // เบ้าตา
        canvas.Disc(24, 22, 15, Void);

        // ม่านตาสลับสีสองเฟรม ให้ดูเหมือนกำลังสะสมพลัง
        Color32 iris = frame == 0 ? WallMid : Red;
        Color32 pupil = frame == 0 ? WallLight : Yellow;

        canvas.Disc(24, 22, 12, iris);
        canvas.Disc(24, 22, 6, pupil);
        canvas.Disc(22, 25, 2, Highlight);

        // หมุดยึดรอบเปลือก
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 2f / 8f;

            canvas.Rect(
                24 + Mathf.RoundToInt(Mathf.Cos(angle) * 19f) - 1,
                22 + Mathf.RoundToInt(Mathf.Sin(angle) * 19f) - 1,
                3,
                3,
                Highlight
            );
        }

        return canvas;
    }

    // ---------- เบ็ดเตล็ด ----------

    private static void WriteMisc()
    {
        // แถบรั้วไฟฟ้า ต่อกันหลายอันเป็นแนวยาว
        Canvas field = new Canvas(24, 8);
        field.Fill(Clear);
        field.Rect(0, 3, 24, 2, Highlight);

        for (int x = 0; x < 24; x += 4)
        {
            field.Rect(x, 1, 2, 6, WallLight);
        }

        Save(field, "Base_Field", Vector2.one * 0.5f);

        // แคปซูลอาวุธ
        Canvas item = new Canvas(18, 14);
        item.Fill(Clear);
        item.Rect(0, 0, 18, 14, MetalDark);
        item.Rect(1, 1, 16, 12, Red);
        item.Rect(3, 3, 12, 8, Yellow);
        item.Rect(7, 5, 4, 4, MetalDark);
        Save(item, "Base_Item", Vector2.one * 0.5f);

        // แผ่นดำไว้คั่นจอตอนเปลี่ยนห้อง
        Canvas black = new Canvas(8, 8);
        black.Fill(new Color32(0, 0, 0, 255));
        Save(black, "Base_Black", Vector2.one * 0.5f);
    }

    private static Vector2 BottomPivot()
    {
        return new Vector2(0.5f, 0f);
    }

    // ---------- เขียนไฟล์และตั้งค่านำเข้า ----------

    private static void Save(Canvas canvas, string name, Vector2 pivot)
    {
        string path = ArtFolder + "/" + name + ".png";

        File.WriteAllBytes(path, canvas.ToTexture().EncodeToPNG());

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer =
            AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;

        // พิกเซลอาร์ตต้องเป็น Point กับไม่บีบอัด ไม่งั้นขอบเบลอและสีเพี้ยน
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);

        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;

        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    /// <summary>
    /// ผืนผ้าใบพิกเซลเล็กๆ
    /// ภายในใช้ y นับขึ้นบนแบบ Unity ส่วนเมธอดลงท้าย Nes ใช้ y นับลงล่างแบบจอ NES
    /// </summary>
    private class Canvas
    {
        private readonly int width;
        private readonly int height;
        private readonly Color32[] pixels;

        public Canvas(int w, int h)
        {
            width = w;
            height = h;
            pixels = new Color32[w * h];
        }

        public void Fill(Color32 color)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
        }

        public void Px(int x, int y, Color32 color)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
                return;

            pixels[y * width + x] = color;
        }

        public void Rect(int x, int y, int w, int h, Color32 color)
        {
            for (int j = 0; j < h; j++)
            {
                for (int i = 0; i < w; i++)
                {
                    Px(x + i, y + j, color);
                }
            }
        }

        public void Frame(int x, int y, int w, int h, int thickness, Color32 color)
        {
            Rect(x, y, w, thickness, color);
            Rect(x, y + h - thickness, w, thickness, color);
            Rect(x, y, thickness, h, color);
            Rect(x + w - thickness, y, thickness, h, color);
        }

        public void Line(int x0, int y0, int x1, int y1, Color32 color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                Px(x0, y0, color);

                if (x0 == x1 && y0 == y1)
                    break;

                int err2 = err * 2;

                if (err2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }

                if (err2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        public void Disc(int cx, int cy, int radius, Color32 color)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    int dx = x - cx;
                    int dy = y - cy;

                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        Px(x, y, color);
                    }
                }
            }
        }

        public void Ring(int cx, int cy, int radius, int thickness, Color32 color)
        {
            int inner = Mathf.Max(0, radius - thickness);

            for (int y = cy - radius; y <= cy + radius; y++)
            {
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    int dx = x - cx;
                    int dy = y - cy;
                    int d = dx * dx + dy * dy;

                    if (d <= radius * radius && d >= inner * inner)
                    {
                        Px(x, y, color);
                    }
                }
            }
        }

        // ---------- รุ่นที่คิดพิกัดแบบจอ NES (y นับลงล่าง) ----------

        public void RectNes(int x, int nesY, int w, int h, Color32 color)
        {
            Rect(x, height - nesY - h, w, h, color);
        }

        public void FrameNes(int x, int nesY, int w, int h, int thickness,
            Color32 color)
        {
            Frame(x, height - nesY - h, w, h, thickness, color);
        }

        public void LineNes(int x0, int nesY0, int x1, int nesY1, Color32 color)
        {
            Line(x0, height - 1 - nesY0, x1, height - 1 - nesY1, color);
        }

        public Texture2D ToTexture()
        {
            Texture2D texture = new Texture2D(
                width, height, TextureFormat.RGBA32, false
            );

            texture.SetPixels32(pixels);
            texture.Apply();

            return texture;
        }
    }
}
