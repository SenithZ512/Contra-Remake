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
    private const string MusicFolder = "Assets/Asset/Sound";
    private const string ObjectName = "Game Audio";

    /// <summary>
    /// ชื่อฉาก -> คำที่ต้องมีในชื่อไฟล์เพลง
    ///
    /// เทียบแบบ "มีคำนี้อยู่ในชื่อ" ไม่ใช่ตรงเป๊ะ
    /// เพราะไฟล์เพลงมีเลขลำดับกับวงเล็บต่อท้ายเต็มไปหมด
    ///
    /// Base-1 ยังไม่มีเพลงของตัวเอง ต้นฉบับด่านฐานใช้เพลง Base คนละเพลงกับป่า
    /// ตอนนี้ยืมเพลงป่ามาใช้ก่อน ได้ไฟล์จริงเมื่อไหร่ค่อยเปลี่ยนคำตรงนี้
    /// </summary>
    private static readonly string[,] SceneMusicMapping =
    {
        { "Forest", "jungle" },
        { "Base-1", "jungle" },
        { "WaterFall", "waterfall" }
    };

    /// <summary>เพลงเดี่ยวที่ไม่ได้ผูกกับฉาก</summary>
    private static readonly string[,] MusicMapping =
    {
        { "bossClear", "triumphal" }
    };

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

        int music = WireMusic(data, ref missing);

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
            " เสียง และเพลง " + music +
            " เพลงแล้วที่ " + AssetPath +
            " ไม่ต้องวางอะไรในฉาก ใช้ได้ทุกฉากเลย"
        );
    }

    /// <summary>
    /// ต่อเพลงพื้นหลังกับเพลงเคลียร์บอส
    ///
    /// ตารางฉากจะ **เติมเฉพาะฉากที่ยังไม่มี** ฉากที่ตั้งไว้แล้วไม่แตะ
    /// กดเมนูซ้ำกี่รอบก็ไม่ทับของที่ปรับเอง
    /// </summary>
    private static int WireMusic(SerializedObject data, ref string missing)
    {
        int wired = 0;

        for (int i = 0; i < MusicMapping.GetLength(0); i++)
        {
            SerializedProperty slot = data.FindProperty(MusicMapping[i, 0]);

            if (slot == null)
                continue;

            AudioClip clip = FindMusic(MusicMapping[i, 1]);

            slot.objectReferenceValue = clip;

            if (clip != null)
            {
                wired++;
            }
            else
            {
                missing += MusicMapping[i, 1] + " ";
            }
        }

        SerializedProperty list = data.FindProperty("sceneMusic");

        if (list == null)
            return wired;

        for (int i = 0; i < SceneMusicMapping.GetLength(0); i++)
        {
            string scene = SceneMusicMapping[i, 0];

            if (HasScene(list, scene))
                continue;

            AudioClip clip = FindMusic(SceneMusicMapping[i, 1]);

            if (clip == null)
            {
                missing += SceneMusicMapping[i, 1] + " ";

                continue;
            }

            int index = list.arraySize;

            list.arraySize = index + 1;

            SerializedProperty entry = list.GetArrayElementAtIndex(index);

            entry.FindPropertyRelative("sceneName").stringValue = scene;
            entry.FindPropertyRelative("track").objectReferenceValue = clip;

            wired++;
        }

        return wired;
    }

    private static bool HasScene(SerializedProperty list, string scene)
    {
        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty name = list.GetArrayElementAtIndex(i)
                .FindPropertyRelative("sceneName");

            if (name != null && name.stringValue == scene)
                return true;
        }

        return false;
    }

    /// <summary>
    /// ชื่อไฟล์เพลงมีเลขลำดับกับวงเล็บปนเยอะ เทียบแบบ "มีคำนี้อยู่" พอ
    /// "07 Waterfall of Bloodshed (Stage 3)" กับ "waterfall" จึงถือว่าตรงกัน
    /// </summary>
    private static AudioClip FindMusic(string key)
    {
        string[] guids = AssetDatabase.FindAssets(
            "t:AudioClip", new[] { MusicFolder }
        );

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // FindAssets มองเข้าโฟลเดอร์ย่อยด้วย กันไม่ให้ไปโดนเสียงเอฟเฟกต์
            if (path.StartsWith(SoundFolder))
                continue;

            string name = System.IO.Path.GetFileNameWithoutExtension(path);

            if (!Simplify(name).Contains(key))
                continue;

            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        return null;
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
        // เอฟเฟกต์สั้น คลายบีบอัดทิ้งไว้ในหน่วยความจำ ยิงแล้วดังทันที
        Apply(SoundFolder, AudioClipLoadType.DecompressOnLoad, true, true);

        // เพลงยาวหลายนาที ถ้าคลายทิ้งไว้จะกิน RAM หลายสิบเมกะไบต์ต่อเพลง
        // และถ่วงเวลาโหลดฉากด้วย สตรีมจากดิสก์เอาพอ
        Apply(MusicFolder, AudioClipLoadType.Streaming, false, false);
    }

    private static void Apply(
        string folder,
        AudioClipLoadType loadType,
        bool preload,
        bool mono)
    {
        if (!AssetDatabase.IsValidFolder(folder))
            return;

        string[] guids = AssetDatabase.FindAssets(
            "t:AudioClip", new[] { folder }
        );

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // โฟลเดอร์เพลงมองเห็นโฟลเดอร์เอฟเฟกต์ที่ซ้อนอยู่ข้างใน
            // ถ้าไม่กันไว้ เอฟเฟกต์จะโดนตั้งเป็นสตรีมมิ่งตามไปด้วย
            if (folder == MusicFolder && path.StartsWith(SoundFolder))
                continue;

            AudioImporter importer =
                AssetImporter.GetAtPath(path) as AudioImporter;

            if (importer == null)
                continue;

            AudioImporterSampleSettings settings =
                importer.defaultSampleSettings;

            if (settings.loadType == loadType &&
                settings.preloadAudioData == preload &&
                importer.forceToMono == mono)
            {
                continue;
            }

            settings.loadType = loadType;

            // preloadAudioData ย้ายมาอยู่ใน SampleSettings แล้ว
            // ตัวที่อยู่บน AudioImporter ตรงๆ ถูกเลิกใช้
            settings.preloadAudioData = preload;

            importer.defaultSampleSettings = settings;
            importer.forceToMono = mono;

            importer.SaveAndReimport();
        }
    }
}
