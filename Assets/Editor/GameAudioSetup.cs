using UnityEditor;
using UnityEngine;

/// <summary>
/// สร้างตัวเล่นเสียงกลางแล้วจับคู่ไฟล์เสียงตามชื่อให้เอง
///
/// จับคู่จากชื่อไฟล์แบบไม่สนตัวพิมพ์เล็กใหญ่และขีดกลาง
/// เพิ่มไฟล์เสียงใหม่ที่ตั้งชื่อตามตารางข้างล่าง แล้วกดเมนูซ้ำก็ติดเอง
///
/// ตั้งค่านำเข้าเสียงให้ด้วย เสียงเอฟเฟกต์สั้นๆ ควรคลายบีบอัดตอนโหลด
/// และบังคับเป็นโมโน เพราะเล่นแบบ 2D อยู่แล้วไม่ต้องใช้สองช่อง
/// </summary>
public static class GameAudioSetup
{
    private const string SoundFolder = "Assets/Asset/Sound VFX";
    private const string ObjectName = "Game Audio";

    /// <summary>ชื่อฟิลด์ในสคริปต์ -> ชื่อไฟล์ที่ต้องมองหา</summary>
    private static readonly string[,] Mapping =
    {
        { "playerNormal", "normal" },
        { "playerMachineGun", "machinegun" },
        { "playerSpread", "spreadgun" },
        { "playerLaser", "laser" },
        { "playerFireball", "fireball" },
        { "enemyGun", "enemygun" },
        { "enemyHit", "enemyhit" }
    };

    private const string ResourceFolder = "Assets/Resources";
    private const string AssetPath =
        "Assets/Resources/GameAudioLibrary.asset";

    [MenuItem("Tools/Contra/Audio/Setup Game Audio")]
    public static void Run()
    {
        ImportSettings();

        GameAudioLibrary library = LoadOrCreate();

        SerializedObject data = new SerializedObject(library);

        int wired = 0;
        string missing = string.Empty;

        for (int i = 0; i < Mapping.GetLength(0); i++)
        {
            AudioClip clip = FindClip(Mapping[i, 1]);

            SerializedProperty slot = data.FindProperty(Mapping[i, 0]);

            if (slot == null)
                continue;

            slot.objectReferenceValue = clip;

            if (clip != null)
            {
                wired++;
            }
            else
            {
                missing += Mapping[i, 1] + " ";
            }
        }

        data.ApplyModifiedProperties();

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();

        Selection.activeObject = library;
        EditorGUIUtility.PingObject(library);

        if (!string.IsNullOrEmpty(missing))
        {
            Debug.LogWarning(
                "ต่อเสียงได้ " + wired +
                " เสียง แต่หาไม่เจอ: " + missing +
                "— เช็คชื่อไฟล์ใน " + SoundFolder
            );

            return;
        }

        Debug.Log(
            "ต่อเสียงครบ " + wired +
            " เสียงแล้วที่ " + AssetPath +
            " ไม่ต้องวางอะไรในฉาก ใช้ได้ทุกฉากเลย"
        );
    }

    /// <summary>
    /// หาไฟล์คลังเสียง ไม่มีก็สร้างใหม่
    /// ต้องอยู่ใน Resources ถึงจะโหลดตอนรันได้
    /// </summary>
    private static GameAudioLibrary LoadOrCreate()
    {
        GameAudioLibrary existing =
            AssetDatabase.LoadAssetAtPath<GameAudioLibrary>(AssetPath);

        if (existing != null)
            return existing;

        if (!AssetDatabase.IsValidFolder(ResourceFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        GameAudioLibrary created =
            ScriptableObject.CreateInstance<GameAudioLibrary>();

        AssetDatabase.CreateAsset(created, AssetPath);

        return created;
    }

    /// <summary>
    /// หาไฟล์ที่ชื่อตรงกับคีย์ โดยตัดขีดกลาง เว้นวรรค และตัวพิมพ์ทิ้งก่อนเทียบ
    /// "Spread-Gun.mp3" กับ "spreadgun" จึงถือว่าตรงกัน
    /// </summary>
    private static AudioClip FindClip(string key)
    {
        string[] guids = AssetDatabase.FindAssets(
            "t:AudioClip", new[] { SoundFolder }
        );

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            string name = System.IO.Path.GetFileNameWithoutExtension(path);

            if (Simplify(name) != key)
                continue;

            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        return null;
    }

    private static string Simplify(string text)
    {
        return text
            .Replace("-", string.Empty)
            .Replace("_", string.Empty)
            .Replace(" ", string.Empty)
            .ToLowerInvariant();
    }

    /// <summary>
    /// เสียงเอฟเฟกต์สั้นควรคลายบีบอัดตอนโหลด จะได้ไม่มีดีเลย์ตอนเล่น
    /// และบีบเป็นโมโน เพราะเราเล่นแบบ 2D อยู่แล้ว เก็บสองช่องไว้ก็เปลืองเปล่า
    /// </summary>
    private static void ImportSettings()
    {
        string[] guids = AssetDatabase.FindAssets(
            "t:AudioClip", new[] { SoundFolder }
        );

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            AudioImporter importer =
                AssetImporter.GetAtPath(path) as AudioImporter;

            if (importer == null)
                continue;

            AudioImporterSampleSettings settings =
                importer.defaultSampleSettings;

            if (settings.loadType == AudioClipLoadType.DecompressOnLoad &&
                settings.preloadAudioData &&
                importer.forceToMono)
            {
                continue;
            }

            settings.loadType = AudioClipLoadType.DecompressOnLoad;

            // preloadAudioData ย้ายมาอยู่ใน SampleSettings แล้ว
            // ตัวที่อยู่บน AudioImporter ตรงๆ ถูกเลิกใช้
            settings.preloadAudioData = true;

            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;

            importer.SaveAndReimport();
        }
    }
}
