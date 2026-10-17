using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ประกอบบอส Mechanical Alien ของด่าน Waterfall และไอเทม B กับนกเหยี่ยว
///
/// ใช้พรีแฟบที่โปรเจกต์มีอยู่แล้วต่อยอด (EnemyBullet, Explosion, WeaponPickup)
/// จะได้ไม่มีของซ้ำซ้อนและปรับที่เดียวมีผลทั้งเกม
///
/// ภาพมังกรสร้างด้วยโค้ดเป็นของชั่วคราว วาดจริงแล้วทับไฟล์ชื่อเดิมได้เลย
/// </summary>
public static class DragonBossSetup
{
    private const string ArtFolder = "Assets/Asset/Boss";
    private const string BossPrefabFolder = "Assets/Prefab/Boss";
    private const string ItemPrefabFolder = "Assets/Prefab/Item";

    private const string EnemyBulletPath = "Assets/Ref/EnemyBullet.prefab";
    private const string ExplosionPath = "Assets/Ref/Explosion.prefab";
    private const string WeaponPickupPath = "Assets/Ref/WeaponPickup.prefab";

    private const float PixelsPerUnit = 24f;

    // พาเลตต์จากภาพต้นฉบับ: ตัวเขียวมะกอก ตาชมพูแดง ลูกกลมขาวเทา
    private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
    private static readonly Color32 Outline = new Color32(16, 24, 8, 255);
    private static readonly Color32 FleshDark = new Color32(72, 104, 24, 255);
    private static readonly Color32 Flesh = new Color32(136, 176, 40, 255);
    private static readonly Color32 FleshLight = new Color32(208, 232, 72, 255);
    private static readonly Color32 Maw = new Color32(24, 16, 32, 255);
    private static readonly Color32 Teeth = new Color32(232, 240, 192, 255);
    private static readonly Color32 EyeRed = new Color32(200, 32, 88, 255);
    private static readonly Color32 EyePink = new Color32(248, 144, 184, 255);
    private static readonly Color32 Fire = new Color32(248, 160, 32, 255);
    private static readonly Color32 Hot = new Color32(255, 240, 160, 255);
    private static readonly Color32 OrbWhite = new Color32(248, 248, 248, 255);
    private static readonly Color32 OrbGrey = new Color32(152, 160, 176, 255);
    private static readonly Color32 OrbDark = new Color32(40, 40, 56, 255);
    private static readonly Color32 Red = new Color32(216, 48, 24, 255);
    private static readonly Color32 White = new Color32(255, 255, 255, 255);

    // ---------- เมนู ----------

    [MenuItem("Tools/Contra/Waterfall/1. Generate Dragon Art")]
    public static void GenerateArt()
    {
        EnsureFolder("Assets/Asset", "Boss");

        Save(Head(0), "Dragon_Head_Closed");
        Save(Head(1), "Dragon_Head_Opening");
        Save(Head(2), "Dragon_Head_Open");
        Save(Orb(OrbWhite, OrbGrey), "Dragon_Orb");
        Save(Orb(Red, new Color32(120, 16, 8, 255)), "Dragon_Hand");
        Save(Segment(), "Dragon_Segment");
        Save(Solid(8, 8, White), "Screen_White");

        AssetDatabase.Refresh();

        Debug.Log("สร้างภาพมังกรเสร็จแล้วที่ " + ArtFolder);
    }

    [MenuItem("Tools/Contra/Waterfall/2. Build Dragon Boss Prefab")]
    public static void BuildBoss()
    {
        if (LoadSprite("Dragon_Head_Closed") == null)
        {
            Debug.LogError("ยังไม่มีภาพ สั่งเมนู 1. Generate Dragon Art ก่อน");
            return;
        }

        EnsureFolder("Assets/Prefab", "Boss");

        GameObject explosion = Load(ExplosionPath);
        GameObject fireball = BuildFireball();

        GameObject root = new GameObject("Dragon Boss");

        SpriteRenderer head = root.AddComponent<SpriteRenderer>();
        head.sprite = LoadSprite("Dragon_Head_Closed");
        head.sortingOrder = 5;

        // กรอบชนครอบเฉพาะปาก ไม่ใช่ทั้งหัว จะได้ต้องเล็งปากจริงๆ
        BoxCollider2D box = root.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1.1f, 0.8f);
        box.offset = new Vector2(0f, -0.55f);

        EnemyHealth hp = root.AddComponent<EnemyHealth>();
        SetInt(hp, "maxHealth", 32);
        SetObject(hp, "deathEffectPrefab", explosion);

        GameObject muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(root.transform, false);

        // ปากอยู่ครึ่งล่างของหัว ไฟต้องพ่นออกจากตรงนั้น ไม่ใช่กลางตัว
        muzzle.transform.localPosition = new Vector3(0f, -0.95f, 0f);

        // มุมเริ่มต้นเท่ากันทั้งคู่ สองแขนจะกวาดสมมาตรกันแบบในภาพต้นฉบับ
        // ส่วนรายละเอียดการบิดทีละข้อสุ่มคนละชุดอยู่แล้ว เลยไม่ซ้ำกันเป๊ะ
        DragonArm left = BuildArm(root.transform, "Arm Left", -1f, fireball,
            explosion, 0f);

        DragonArm right = BuildArm(root.transform, "Arm Right", 1f, fireball,
            explosion, 0f);

        DragonBoss boss = root.AddComponent<DragonBoss>();

        SerializedObject data = new SerializedObject(boss);

        data.FindProperty("fireballPrefab").objectReferenceValue = fireball;
        data.FindProperty("muzzle").objectReferenceValue = muzzle.transform;
        data.FindProperty("explosionPrefab").objectReferenceValue = explosion;
        data.FindProperty("spriteRenderer").objectReferenceValue = head;

        data.FindProperty("closedSprite").objectReferenceValue =
            LoadSprite("Dragon_Head_Closed");
        data.FindProperty("openingSprite").objectReferenceValue =
            LoadSprite("Dragon_Head_Opening");
        data.FindProperty("openSprite").objectReferenceValue =
            LoadSprite("Dragon_Head_Open");

        SerializedProperty arms = data.FindProperty("arms");
        arms.arraySize = 2;
        arms.GetArrayElementAtIndex(0).objectReferenceValue = left;
        arms.GetArrayElementAtIndex(1).objectReferenceValue = right;

        data.ApplyModifiedPropertiesWithoutUndo();

        string path = BossPrefabFolder + "/Dragon.prefab";

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();

        Debug.Log("ประกอบบอสมังกรเสร็จแล้วที่ " + path +
            " ลากเข้าฉาก WaterFall ได้เลย");
    }

    [MenuItem("Tools/Contra/Items/Build Barrier and Falcon")]
    public static void BuildItems()
    {
        if (LoadSprite("Screen_White") == null)
        {
            Debug.LogError("ยังไม่มีภาพ สั่งเมนู Waterfall > 1. Generate Dragon Art ก่อน");
            return;
        }

        EnsureFolder("Assets/Prefab", "Item");

        GameObject flash = BuildFlash();

        BuildPickup("BarrierPickup", 2, null, 0f);
        BuildPickup("FalconPickup", 3, flash, 0f);

        AssetDatabase.SaveAssets();

        Debug.Log("สร้างไอเทม B และนกเหยี่ยวเสร็จแล้วที่ " + ItemPrefabFolder);
    }

    // ---------- ชิ้นส่วน ----------

    private static GameObject BuildFireball()
    {
        GameObject source = Load(EnemyBulletPath);

        if (source == null)
        {
            Debug.LogWarning("หา EnemyBullet ไม่เจอ บอสจะไม่มีกระสุน");
            return null;
        }

        GameObject copy =
            (GameObject)PrefabUtility.InstantiatePrefab(source);

        copy.name = "Dragon_Fireball";

        PrefabUtility.UnpackPrefabInstance(
            copy, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction
        );

        EnemyBullet bullet = copy.GetComponent<EnemyBullet>();

        if (bullet != null)
        {
            SerializedObject data = new SerializedObject(bullet);

            // ไฟมังกรช้ากว่ากระสุนทหารและบินได้นานกว่า เพราะยิงจากที่สูง
            data.FindProperty("speed").floatValue = 5f;
            data.FindProperty("lifetime").floatValue = 5f;
            data.FindProperty("rotateAlongVelocity").boolValue = true;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        SpriteRenderer sprite = copy.GetComponent<SpriteRenderer>();

        if (sprite != null)
        {
            sprite.color = new Color(1f, 0.62f, 0.15f);
            copy.transform.localScale = Vector3.one * 1.3f;
        }

        string path = BossPrefabFolder + "/Dragon_Fireball.prefab";

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(copy, path);

        Object.DestroyImmediate(copy);

        return saved;
    }

    /// <summary>
    /// แขนหนึ่งข้าง 5 ลูกตามต้นฉบับ
    /// ลูกสุดท้ายคือ "มือ" ลูกเดียวที่มีคอลไลเดอร์และเลือด
    /// </summary>
    private static DragonArm BuildArm(Transform parent, string name,
        float side, GameObject fireball, GameObject explosion, float phase)
    {
        GameObject armRoot = new GameObject(name);
        armRoot.transform.SetParent(parent, false);

        // หัวไหล่เยื้องออกจากกลางหัวพอสมควร แขนจะได้ไม่ทับตัวบอส
        armRoot.transform.localPosition = new Vector3(side * 1.3f, -0.2f, 0f);

        const int orbCount = 5;

        Transform[] orbs = new Transform[orbCount];
        Transform[] segments = new Transform[orbCount];
        EnemyHealth hand = null;

        // ข้อต่อสีเขียววาดใต้ลูกกลม ให้ลูกกลมเด่นอยู่ข้างหน้า
        for (int i = 0; i < orbCount; i++)
        {
            GameObject piece = new GameObject("Segment " + i);
            piece.transform.SetParent(armRoot.transform, false);

            SpriteRenderer sprite = piece.AddComponent<SpriteRenderer>();
            sprite.sprite = LoadSprite("Dragon_Segment");
            sprite.sortingOrder = 3;

            segments[i] = piece.transform;
        }

        for (int i = 0; i < orbCount; i++)
        {
            bool isHand = i == orbCount - 1;

            GameObject orb = new GameObject(isHand ? "Hand" : "Orb " + i);
            orb.transform.SetParent(armRoot.transform, false);

            SpriteRenderer sprite = orb.AddComponent<SpriteRenderer>();
            sprite.sprite = LoadSprite(isHand ? "Dragon_Hand" : "Dragon_Orb");
            sprite.sortingOrder = isHand ? 7 : 6;

            orbs[i] = orb.transform;

            if (!isHand)
                continue;

            // มือเท่านั้นที่ยิงโดน ลูกอื่นเป็นแค่ข้อต่อให้กระสุนทะลุผ่าน
            CircleCollider2D hit = orb.AddComponent<CircleCollider2D>();
            hit.radius = 0.45f;

            hand = orb.AddComponent<EnemyHealth>();
            SetInt(hand, "maxHealth", 16);
            SetObject(hand, "deathEffectPrefab", explosion);
        }

        DragonArm arm = armRoot.AddComponent<DragonArm>();

        SerializedObject data = new SerializedObject(arm);

        data.FindProperty("side").floatValue = side;

        // เยื้องมุมเริ่มต้นของสองแขน ใส่ 0 ทั้งคู่จะกวาดสมมาตรกันเป๊ะ
        data.FindProperty("shoulderStartOffset").floatValue = phase;
        data.FindProperty("fireballPrefab").objectReferenceValue = fireball;
        data.FindProperty("explosionPrefab").objectReferenceValue = explosion;
        data.FindProperty("hand").objectReferenceValue = hand;

        SerializedProperty list = data.FindProperty("orbs");
        list.arraySize = orbCount;

        SerializedProperty joints = data.FindProperty("segments");
        joints.arraySize = orbCount;

        for (int i = 0; i < orbCount; i++)
        {
            list.GetArrayElementAtIndex(i).objectReferenceValue = orbs[i];
            joints.GetArrayElementAtIndex(i).objectReferenceValue = segments[i];
        }

        data.ApplyModifiedPropertiesWithoutUndo();

        return arm;
    }

    private static GameObject BuildFlash()
    {
        GameObject root = new GameObject("Falcon_Flash");

        GameObject sheet = new GameObject("Sheet");
        sheet.transform.SetParent(root.transform, false);

        SpriteRenderer sprite = sheet.AddComponent<SpriteRenderer>();
        sprite.sprite = LoadSprite("Screen_White");
        sprite.sortingOrder = 200;

        ScreenFlash flash = root.AddComponent<ScreenFlash>();

        SerializedObject data = new SerializedObject(flash);
        data.FindProperty("sheet").objectReferenceValue = sprite;
        data.ApplyModifiedPropertiesWithoutUndo();

        string path = ItemPrefabFolder + "/Falcon_Flash.prefab";

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);

        Object.DestroyImmediate(root);

        return saved;
    }

    /// <summary>kind: 2 = Barrier, 3 = Falcon (ตามลำดับใน PickupKind)</summary>
    private static void BuildPickup(string name, int kind, GameObject flash,
        float barrierDuration)
    {
        GameObject source = Load(WeaponPickupPath);

        if (source == null)
        {
            Debug.LogWarning("หา WeaponPickup ไม่เจอ ข้าม " + name);
            return;
        }

        GameObject copy =
            (GameObject)PrefabUtility.InstantiatePrefab(source);

        copy.name = name;

        PrefabUtility.UnpackPrefabInstance(
            copy, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction
        );

        WeaponPickup pickup = copy.GetComponent<WeaponPickup>();

        if (pickup != null)
        {
            SerializedObject data = new SerializedObject(pickup);

            data.FindProperty("kind").enumValueIndex = kind;

            // 0 = ให้ PlayerHealth เลือกตามด่านเอง (ด่าน Hangar ได้นานกว่า)
            data.FindProperty("barrierDuration").floatValue = barrierDuration;

            data.FindProperty("falconFlashPrefab").objectReferenceValue = flash;

            data.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(
            copy, ItemPrefabFolder + "/" + name + ".prefab"
        );

        Object.DestroyImmediate(copy);
    }

    // ---------- วาดภาพ ----------

    /// <summary>
    /// หัวบอส วาดตามภาพต้นฉบับ
    /// ครีบกางสองข้างด้านบน ตาแดงเรืองสองดวง ปากกว้างเต็มฟัน
    /// state 0 ปิด 1 อ้าครึ่ง 2 อ้าเต็ม
    /// </summary>
    private static Color32[] Head(int state)
    {
        const int w = 72;
        const int h = 56;

        Color32[] px = Fill(w, h, Clear);

        // ครีบกางสองข้างด้านบน เป็นเอกลักษณ์ของบอสตัวนี้
        Fin(px, w, h, 14, 44, -1);
        Fin(px, w, h, 58, 44, 1);

        // กะโหลกหลัก ทรงป้านกว้างกว่าสูง
        Disc(px, w, h, 36, 34, 21, Outline);
        Disc(px, w, h, 36, 34, 19, FleshDark);
        Disc(px, w, h, 36, 38, 15, Flesh);
        Disc(px, w, h, 36, 44, 9, FleshLight);

        // ขากรรไกรล่างยื่นออกมา
        Rect(px, w, h, 20, 10, 32, 16, Outline);
        Rect(px, w, h, 22, 12, 28, 14, FleshDark);
        Rect(px, w, h, 25, 14, 22, 9, Flesh);

        // ตาเรืองสองดวง เอียงเข้าหากันให้ดูดุ
        Eye(px, w, h, 25, 40);
        Eye(px, w, h, 47, 40);

        // ปากอยู่กลางขากรรไกร อ้ากว้างขึ้นตามสถานะ
        int gap = state == 0 ? 3 : (state == 1 ? 8 : 14);
        int mouthY = 19 - gap / 2;

        Rect(px, w, h, 24, mouthY, 24, gap, Maw);

        if (state > 0)
        {
            // เห็นไฟในลำคอตอนอ้า
            Rect(px, w, h, 28, mouthY + 2, 16, Mathf.Max(1, gap - 4), Fire);
            Rect(px, w, h, 32, mouthY + 4, 8, Mathf.Max(1, gap - 8), Hot);
        }

        // เขี้ยวบนล่าง สลับฟันปลาแบบภาพต้นฉบับ
        for (int x = 25; x < 47; x += 4)
        {
            Rect(px, w, h, x, mouthY + gap - 2, 3, 3, Teeth);
            Rect(px, w, h, x + 2, mouthY - 1, 3, 3, Teeth);
        }

        return Pack(px, w, h);
    }

    /// <summary>ครีบสามเหลี่ยมที่กางออกด้านข้างหัว</summary>
    private static void Fin(Color32[] px, int w, int h, int x, int y, int dir)
    {
        for (int i = 0; i < 14; i++)
        {
            int height = 10 - i / 2;

            Rect(px, w, h, x + dir * i - (dir < 0 ? 1 : 0), y - height / 2,
                1, height, i < 2 ? FleshDark : (i < 9 ? Flesh : FleshDark));
        }
    }

    private static void Eye(Color32[] px, int w, int h, int cx, int cy)
    {
        Disc(px, w, h, cx, cy, 5, Outline);
        Disc(px, w, h, cx, cy, 4, EyeRed);
        Disc(px, w, h, cx - 1, cy + 1, 2, EyePink);
    }

    /// <summary>
    /// ลูกกลมบนแขน ตามภาพคือทรงกลมขาวเทามีปุ่มสีเข้มเป็นลาย
    /// ลูกมือปลายสุดใช้สีแดงแทน จะได้รู้ว่าอันไหนคือจุดที่ยิงเข้า
    /// </summary>
    private static Color32[] Orb(Color32 main, Color32 dark)
    {
        const int w = 20;
        const int h = 20;

        Color32[] px = Fill(w, h, Clear);

        Disc(px, w, h, 10, 10, 9, Outline);
        Disc(px, w, h, 10, 10, 8, dark);
        Disc(px, w, h, 10, 10, 7, main);
        Disc(px, w, h, 8, 12, 3, main.Equals(OrbWhite) ? OrbWhite : main);

        // ปุ่มสี่ทิศรอบลูก
        Rect(px, w, h, 9, 2, 2, 3, OrbDark);
        Rect(px, w, h, 9, 15, 2, 3, OrbDark);
        Rect(px, w, h, 2, 9, 3, 2, OrbDark);
        Rect(px, w, h, 15, 9, 3, 2, OrbDark);

        // วงกลางแบบลายดุม
        Disc(px, w, h, 10, 10, 4, OrbDark);
        Disc(px, w, h, 10, 10, 2, main);

        return Pack(px, w, h);
    }

    /// <summary>
    /// ข้อต่อสีเขียวที่คั่นระหว่างลูกกลม
    /// วาดให้ชี้ไปทางขวา โค้งเรียวปลาย แบบหนวดในภาพต้นฉบับ
    /// </summary>
    private static Color32[] Segment()
    {
        // ยาวหน่อยเพื่อให้ตอนถูกยืดเต็มช่องว่าง พิกเซลไม่บานจนเห็นชัด
        const int w = 32;
        const int h = 14;

        Color32[] px = Fill(w, h, Clear);

        for (int x = 0; x < w; x++)
        {
            // หนาตรงกลาง เรียวสองปลาย และโก่งขึ้นเล็กน้อย
            float t = x / (float)(w - 1);
            int thick = 2 + Mathf.RoundToInt(Mathf.Sin(t * Mathf.PI) * 4f);
            int lift = Mathf.RoundToInt(Mathf.Sin(t * Mathf.PI) * 2f);
            int y = h / 2 - thick / 2 + lift - 1;

            Rect(px, w, h, x, y - 1, 1, thick + 2, Outline);
            Rect(px, w, h, x, y, 1, thick, Flesh);

            if (thick > 3)
            {
                Rect(px, w, h, x, y + thick - 2, 1, 1, FleshLight);
            }
        }

        return Pack(px, w, h);
    }

    private static Color32[] Solid(int w, int h, Color32 color)
    {
        return Pack(Fill(w, h, color), w, h);
    }

    // ---------- ตัวช่วยวาด ----------
    // ช่องแรกของอาเรย์เก็บความกว้าง/สูงไว้ตอน Pack เพื่อส่งต่อให้ Save

    private static int packedWidth;
    private static int packedHeight;

    private static Color32[] Pack(Color32[] px, int w, int h)
    {
        packedWidth = w;
        packedHeight = h;

        return px;
    }

    private static Color32[] Fill(int w, int h, Color32 color)
    {
        Color32[] px = new Color32[w * h];

        for (int i = 0; i < px.Length; i++)
        {
            px[i] = color;
        }

        return px;
    }

    private static void Rect(Color32[] px, int w, int h, int x, int y,
        int rw, int rh, Color32 color)
    {
        for (int j = 0; j < rh; j++)
        {
            for (int i = 0; i < rw; i++)
            {
                Plot(px, w, h, x + i, y + j, color);
            }
        }
    }

    private static void Disc(Color32[] px, int w, int h, int cx, int cy,
        int radius, Color32 color)
    {
        for (int y = cy - radius; y <= cy + radius; y++)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                int dx = x - cx;
                int dy = y - cy;

                if (dx * dx + dy * dy <= radius * radius)
                {
                    Plot(px, w, h, x, y, color);
                }
            }
        }
    }

    private static void Plot(Color32[] px, int w, int h, int x, int y,
        Color32 color)
    {
        if (x < 0 || y < 0 || x >= w || y >= h)
            return;

        px[y * w + x] = color;
    }

    // ---------- บันทึกไฟล์ ----------

    private static void Save(Color32[] px, string name)
    {
        Texture2D texture = new Texture2D(
            packedWidth, packedHeight, TextureFormat.RGBA32, false
        );

        texture.SetPixels32(px);
        texture.Apply();

        string path = ArtFolder + "/" + name + ".png";

        File.WriteAllBytes(path, texture.EncodeToPNG());

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer =
            AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
    }

    // ---------- ตัวช่วยทั่วไป ----------

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent))
        {
            string grand = Path.GetDirectoryName(parent).Replace("\\", "/");

            AssetDatabase.CreateFolder(grand, Path.GetFileName(parent));
        }

        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static GameObject Load(string path)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(
            ArtFolder + "/" + name + ".png"
        );
    }

    private static void SetInt(Object target, string field, int value)
    {
        SerializedObject data = new SerializedObject(target);

        data.FindProperty(field).intValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObject(Object target, string field, Object value)
    {
        SerializedObject data = new SerializedObject(target);

        data.FindProperty(field).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
