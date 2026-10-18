using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ประกอบพรีแฟบหินร่วงของด่าน Waterfall พร้อมวาดภาพหินให้
///
/// ภาพเป็นของชั่วคราว วาดจริงแล้วทับไฟล์ชื่อเดิมได้เลย
/// </summary>
public static class FallingRockSetup
{
    private const string ArtFolder = "Assets/Asset/Level";
    private const string PrefabFolder = "Assets/Prefab/Level";
    private const string ExplosionPath = "Assets/Ref/Explosion.prefab";

    private const float PixelsPerUnit = 24f;

    private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
    private static readonly Color32 Edge = new Color32(24, 24, 32, 255);
    private static readonly Color32 Shade = new Color32(112, 112, 128, 255);
    private static readonly Color32 Stone = new Color32(200, 200, 208, 255);
    private static readonly Color32 Lit = new Color32(248, 248, 248, 255);

    [MenuItem("Tools/Contra/Waterfall/3. Build Falling Rock")]
    public static void Run()
    {
        EnsureFolder("Assets/Asset", "Level");
        EnsureFolder("Assets/Prefab", "Level");

        SaveBoulder();

        AssetDatabase.Refresh();

        GameObject rock = BuildRock();

        Debug.Log(
            "สร้างหินร่วงเสร็จแล้ว " + AssetDatabase.GetAssetPath(rock) +
            " — ลาก RockCave ใส่จุดที่อยากให้หินร่วงในฉาก WaterFall"
        );
    }

    private static GameObject BuildRock()
    {
        GameObject root = new GameObject("Falling Rock");

        GameObject visual = new GameObject("Sprite");
        visual.transform.SetParent(root.transform, false);

        SpriteRenderer sprite = visual.AddComponent<SpriteRenderer>();
        sprite.sprite = LoadSprite("Boulder");
        sprite.sortingOrder = 3;

        // หมุนที่ลูก ไม่ใช่ที่ราก คอลไลเดอร์จะได้ไม่หมุนตาม
        CircleCollider2D hit = root.AddComponent<CircleCollider2D>();
        hit.radius = 0.42f;
        hit.isTrigger = true;

        // ต้องมี Rigidbody2D ไม่งั้นคอลไลเดอร์นับเป็น static
        // พอขยับด้วย transform ทุกเฟรม ตัวแทนในระบบฟิสิกส์จะตามไม่ทัน
        // bounds ที่ EnemyContactDamage อ่านก็เป็นค่าเก่า หินเลยผ่านตัวไปเฉยๆ
        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.simulated = true;

        GameObject explosion =
            AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionPath);

        // ใช้ Require แทน AddComponent ตรงๆ
        // รอบก่อน EnemyHealth หลุดหายไปจากพรีแฟบโดยไม่มีอะไรฟ้อง
        // แบบนี้ถ้าใส่ไม่ติดจะได้รู้ทันทีตอนกดเมนู ไม่ใช่ไปงงตอนเล่น
        EnemyHealth health = Require<EnemyHealth>(root);

        if (health != null)
        {
            SetInt(health, "maxHealth", 1);
            SetObject(health, "deathEffectPrefab", explosion);
        }

        EnemyContactDamage touch = Require<EnemyContactDamage>(root);

        if (touch != null)
        {
            // หินเป็นก้อนหิน ไม่ใช่คน เกราะของผู้เล่นไม่ควรทำให้มันแตก
            SetBool(touch, "diesToBarrier", false);
        }

        FallingRock rock = Require<FallingRock>(root);

        SerializedObject data = new SerializedObject(rock);

        data.FindProperty("body").objectReferenceValue = hit;
        data.FindProperty("spriteRenderer").objectReferenceValue = sprite;
        data.FindProperty("landEffectPrefab").objectReferenceValue = explosion;

        // พื้นของเกมนี้อยู่เลเยอร์ groundLayer
        data.FindProperty("groundLayer").intValue =
            1 << LayerMask.NameToLayer("groundLayer");

        data.ApplyModifiedPropertiesWithoutUndo();

        string path = PrefabFolder + "/Falling_Rock.prefab";

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);

        Object.DestroyImmediate(root);

        Verify(saved);

        BuildCave(saved);

        return saved;
    }

    /// <summary>ใส่คอมโพเนนต์แล้วฟ้องทันทีถ้าใส่ไม่ติด</summary>
    private static T Require<T>(GameObject host) where T : Component
    {
        T existing = host.GetComponent<T>();

        if (existing != null)
            return existing;

        T added = host.AddComponent<T>();

        if (added == null)
        {
            Debug.LogError(
                "ใส่ " + typeof(T).Name + " ไม่ได้ — " +
                "เช็คว่าสคริปต์คอมไพล์ผ่านแล้วหรือยัง แล้วกดเมนูใหม่"
            );
        }

        return added;
    }

    /// <summary>ตรวจพรีแฟบที่เซฟแล้วว่าได้ของครบจริง</summary>
    private static void Verify(GameObject prefab)
    {
        if (prefab == null)
            return;

        string missing = string.Empty;

        if (prefab.GetComponent<EnemyHealth>() == null)
            missing += "EnemyHealth ";

        if (prefab.GetComponent<EnemyContactDamage>() == null)
            missing += "EnemyContactDamage ";

        if (prefab.GetComponent<FallingRock>() == null)
            missing += "FallingRock ";

        if (prefab.GetComponent<Collider2D>() == null)
            missing += "Collider2D ";

        if (string.IsNullOrEmpty(missing))
        {
            Debug.Log("ตรวจพรีแฟบหินแล้ว คอมโพเนนต์ครบ");
            return;
        }

        Debug.LogError("พรีแฟบหินขาด: " + missing + "— กดเมนูใหม่อีกครั้ง");
    }

    /// <summary>รูบนหน้าผา ลากอันนี้ไปวางในฉากได้เลย</summary>
    private static void BuildCave(GameObject rockPrefab)
    {
        GameObject root = new GameObject("Rock Cave");

        RockCave cave = root.AddComponent<RockCave>();

        SerializedObject data = new SerializedObject(cave);

        data.FindProperty("rockPrefab").objectReferenceValue = rockPrefab;

        data.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(
            root, PrefabFolder + "/Rock_Cave.prefab"
        );

        Object.DestroyImmediate(root);
    }

    // ---------- ภาพ ----------

    /// <summary>ก้อนหินกลมๆ ขาวเทา แบบในภาพต้นฉบับ</summary>
    private static void SaveBoulder()
    {
        const int size = 20;

        Color32[] px = new Color32[size * size];

        for (int i = 0; i < px.Length; i++)
        {
            px[i] = Clear;
        }

        Disc(px, size, 10, 10, 9, Edge);
        Disc(px, size, 10, 10, 8, Shade);
        Disc(px, size, 10, 11, 7, Stone);
        Disc(px, size, 8, 13, 3, Lit);

        // รอยแตกให้เห็นว่าเป็นหิน ไม่ใช่ลูกบอล
        Rect(px, size, 5, 8, 4, 1, Shade);
        Rect(px, size, 12, 6, 3, 1, Shade);
        Rect(px, size, 9, 4, 1, 3, Shade);

        Save(px, size, size, "Boulder");
    }

    private static void Disc(Color32[] px, int w, int cx, int cy, int r,
        Color32 color)
    {
        for (int y = cy - r; y <= cy + r; y++)
        {
            for (int x = cx - r; x <= cx + r; x++)
            {
                int dx = x - cx;
                int dy = y - cy;

                if (dx * dx + dy * dy <= r * r)
                {
                    Plot(px, w, x, y, color);
                }
            }
        }
    }

    private static void Rect(Color32[] px, int w, int x, int y, int rw,
        int rh, Color32 color)
    {
        for (int j = 0; j < rh; j++)
        {
            for (int i = 0; i < rw; i++)
            {
                Plot(px, w, x + i, y + j, color);
            }
        }
    }

    private static void Plot(Color32[] px, int w, int x, int y, Color32 color)
    {
        if (x < 0 || y < 0 || x >= w || y * w + x >= px.Length)
            return;

        px[y * w + x] = color;
    }

    private static void Save(Color32[] px, int w, int h, string name)
    {
        Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, false);

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

    // ---------- ตัวช่วย ----------

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
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

    private static void SetBool(Object target, string field, bool value)
    {
        SerializedObject data = new SerializedObject(target);
        data.FindProperty(field).boolValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObject(Object target, string field, Object value)
    {
        SerializedObject data = new SerializedObject(target);
        data.FindProperty(field).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
