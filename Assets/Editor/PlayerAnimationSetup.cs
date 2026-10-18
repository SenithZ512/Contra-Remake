using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// สร้างคลิปอนิเมชั่นจากโฟลเดอร์รูปใน Assets/Asset/Main Player
/// แล้วต่อเข้า Animator Controller ให้อัตโนมัติ
///
/// ชื่อคลิปกับชื่อ State = ชื่อโฟลเดอร์เป๊ะๆ
/// อยากเพิ่มท่าใหม่ก็แค่สร้างโฟลเดอร์รูปเพิ่ม แล้วสั่งเมนูนี้ซ้ำ
/// </summary>
public static class PlayerAnimationSetup
{
    private const string SpriteRoot = "Assets/Asset/Main Player";
    private const string ClipFolder = "Assets/Spicpt/Player/Animation";
    private const string ControllerPath =
        "Assets/Spicpt/Player/Animation/PlayerAnimatorController.controller";

    // ชื่อโฟลเดอร์ที่ตั้งไว้ -> หน้าที่ในเกม
    private const string FolderIdle = "PlayerIdle";
    private const string FolderRun = "PlayerRun";
    private const string FolderJump = "PlayerJump";
    private const string FolderRunFire = "Run And Fire";
    private const string FolderAimUp = "Running and aiming diagonally up";
    private const string FolderAimDown = "Running shot, angled downwards_";
    private const string FolderAimUpStraight = "Shoot straight up";
    private const string FolderCrouchFire = "Crawl-firing";
    private const string FolderStandFire = "Standing shot";

    /// <summary>ไม่มีโฟลเดอร์ท่าหมอบเฉยๆ ใช้เฟรมแรกของท่าคลานยิงแทน</summary>
    private const string FolderCrouch = "PlayerCrouch";

    /// <summary>
    /// ท่ายืนนิ่งยิงขึ้นฟ้า มีโฟลเดอร์ของตัวเองแล้ว
    /// ถ้าหาโฟลเดอร์นี้เจอจะใช้อันนี้ ถ้าไม่เจอค่อยถอยไปใช้เฟรมตรึงข้างล่าง
    /// </summary>
    private const string FolderAimUpStand = "Stand still and fire into the sky_";

    /// <summary>
    /// ท่ายืนนิ่งยิงขึ้นตรงๆ ไม่มีโฟลเดอร์ของตัวเอง
    ///
    /// รูปใน "Shoot straight up" เป็นท่าวิ่งทุกเฟรม ขาก้าวหมด
    /// ถ้าเอาไปเล่นตอนยืนนิ่งขาจะวิ่งอยู่กับที่
    /// เลยหยิบมาเฟรมเดียวแล้วตรึงไว้ ขาจะได้หยุดนิ่ง
    /// </summary>
    private const string ClipAimUpStand = "Shoot straight up (stand)";

    /// <summary>เอาเฟรมไหนมาตรึง นับจาก 1 เลือกเฟรมที่เท้าแตะพื้นทั้งสองข้าง</summary>
    private const int AimUpStandFrame = 1;

    private const int CycleFrameRate = 16;

    // ลูกของ Player: Visual (จุดหมุนอยู่ระดับเอว) -> Sprite (ตัว SpriteRenderer)
    private const string VisualPath = "Visual";
    private const string SpritePath = "Visual/Sprite";

    /// <summary>ตีลังกา 1 รอบใช้กี่วินาที ยิ่งมากยิ่งหมุนช้า กระโดดครั้งนึงลอยราว 1 วิ</summary>
    private const float JumpSpinDuration = 0.7f;

    [MenuItem("Tools/Contra/Setup Player Animation")]
    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder(SpriteRoot))
        {
            Debug.LogError(
                "หาโฟลเดอร์รูปไม่เจอ: " + SpriteRoot
            );

            return;
        }

        NormalizeImportSettings();

        Dictionary<string, AnimationClip> clips = BuildAllClips();

        if (clips.Count == 0)
        {
            Debug.LogError("ไม่พบรูปในโฟลเดอร์ย่อยเลย");
            return;
        }

        WireController(clips);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "เชื่อมอนิเมชั่นเสร็จแล้ว รวม " + clips.Count + " คลิป"
        );
    }

    // ---------- ตั้งค่านำเข้ารูป ----------

    /// <summary>ขนาดมาตรฐานของสไปรท์ตัวละครหลัก</summary>
    private const float SpritePixelsPerUnit = 410f;

    /// <summary>
    /// ค่า PPU ที่ Unity ตั้งให้เองตอนลากรูปเข้ามาใหม่
    /// เจอค่านี้เมื่อไหร่แปลว่ายังไม่เคยตั้ง ต้องจัดให้
    /// </summary>
    private const float DefaultPixelsPerUnit = 100f;

    /// <summary>
    /// จัดค่านำเข้าให้รูปที่เพิ่งลากเข้ามาใหม่
    ///
    /// ปัญหานี้เกิดซ้ำทุกครั้งที่เพิ่มท่าใหม่ — รูปเข้ามาด้วย PPU 100
    /// และจุดหมุนกลางภาพ ตัวละครเลยตัวใหญ่ผิดขนาดและลอยเหนือพื้น
    ///
    /// ตรงนี้หาขอบล่างของตัวละครในรูปจริงๆ แล้วตั้งจุดหมุนไว้ที่เท้า
    /// ไม่ต้องไปวัดเอง และไม่แตะโฟลเดอร์ที่ตั้งค่าไว้แล้ว
    /// </summary>
    private static void NormalizeImportSettings()
    {
        List<string> pending = new List<string>();

        foreach (string folder in AssetDatabase.GetSubFolders(SpriteRoot))
        {
            foreach (string file in Directory.GetFiles(folder))
            {
                if (!IsImage(file))
                    continue;

                string path = file.Replace("\\", "/");

                if (NeedsSetup(path))
                {
                    pending.Add(path);
                }
            }
        }

        if (pending.Count == 0)
            return;

        // รวบนำเข้าใหม่เป็นชุดเดียว ไม่งั้น Unity จะนำเข้าทีละไฟล์แล้วช้ามาก
        AssetDatabase.StartAssetEditing();

        try
        {
            for (int i = 0; i < pending.Count; i++)
            {
                EditorUtility.DisplayProgressBar(
                    "ตั้งค่ารูปตัวละคร",
                    Path.GetFileName(pending[i]),
                    i / (float)pending.Count
                );

                NormalizeOne(pending[i]);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();

            AssetDatabase.StopAssetEditing();
        }

        Debug.Log(
            "ตั้งค่านำเข้าให้รูปใหม่ " + pending.Count + " ไฟล์ " +
            "(PPU " + SpritePixelsPerUnit + " และจุดหมุนที่เท้า)"
        );
    }

    /// <summary>ยังเป็นค่า default อยู่ไหม ถ้าตั้งแล้วก็ไม่ต้องไปยุ่ง</summary>
    private static bool NeedsSetup(string path)
    {
        TextureImporter importer =
            AssetImporter.GetAtPath(path) as TextureImporter;

        return importer != null &&
            Mathf.Approximately(
                importer.spritePixelsPerUnit, DefaultPixelsPerUnit);
    }

    /// <summary>ตั้งค่าให้ไฟล์เดียว นำเข้าใหม่รอบเดียวจบ</summary>
    private static void NormalizeOne(string path)
    {
        TextureImporter importer =
            AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = SpritePixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);

        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, FindFeetPivot(path));

        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
    }

    /// <summary>
    /// หาแถวล่างสุดที่ยังมีสีอยู่ แล้วคิดเป็นสัดส่วนความสูง
    /// ได้จุดหมุนที่อยู่ระดับเท้าพอดี ไม่ว่าตัวละครจะวางอยู่ตรงไหนของรูป
    ///
    /// อ่านไฟล์ PNG จากดิสก์ตรงๆ ไม่ยุ่งกับตัวนำเข้า
    /// วิธีเดิมเปิด isReadable แล้วสั่งนำเข้าใหม่สองรอบต่อไฟล์
    /// ซึ่งกับรูป 1920x1080 แบบไม่บีบอัด ทำเอา Editor ค้างเป็นนาที
    /// </summary>
    private static float FindFeetPivot(string path)
    {
        Texture2D probe = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        try
        {
            if (!probe.LoadImage(File.ReadAllBytes(path)))
                return 0f;

            Color32[] pixels = probe.GetPixels32();

            // GetPixels32 เรียงจากล่างขึ้นบน แถว 0 คือขอบล่างของรูป
            // ตัวละครเท้าอยู่ล่าง เลยเจอตั้งแต่แถวแรกๆ ไม่ต้องไล่ทั้งรูป
            for (int row = 0; row < probe.height; row++)
            {
                int start = row * probe.width;

                for (int column = 0; column < probe.width; column++)
                {
                    if (pixels[start + column].a > 16)
                        return row / (float)probe.height;
                }
            }

            return 0f;
        }
        catch (System.Exception error)
        {
            Debug.LogWarning(
                "อ่านรูปไม่ได้ ใช้จุดหมุนที่ขอบล่างแทน: " + path +
                " (" + error.Message + ")"
            );

            return 0f;
        }
        finally
        {
            Object.DestroyImmediate(probe);
        }
    }

    // ---------- สร้างคลิป ----------

    private static Dictionary<string, AnimationClip> BuildAllClips()
    {
        Dictionary<string, AnimationClip> result =
            new Dictionary<string, AnimationClip>();

        string[] folders = AssetDatabase.GetSubFolders(SpriteRoot);

        foreach (string folder in folders)
        {
            string name = Path.GetFileName(folder);

            List<Sprite> sprites = LoadSpritesSorted(folder);

            if (sprites.Count == 0)
            {
                Debug.LogWarning(
                    "ข้าม \"" + name + "\" เพราะไม่มีรูปข้างใน"
                );

                continue;
            }

            result[name] = WriteClip(
                name,
                sprites,
                name == FolderJump
            );

            Debug.Log(
                "สร้างคลิป \"" + name + "\" " +
                sprites.Count + " เฟรม"
            );
        }

        // ไม่มีโฟลเดอร์ยืนเฉยๆ ก็หยิบเฟรมแรกของท่าวิ่งมาใช้แทน
        if (!result.ContainsKey(FolderIdle))
        {
            // ท่ายืนเฉยๆ เอาจากท่ายืนยิงก่อน เพราะเป็นท่ายืนจริงๆ
            // ถ้าไม่มีค่อยถอยไปใช้เฟรมแรกของท่าวิ่ง ซึ่งเป็นท่าก้าวขากลางคัน
            string source = result.ContainsKey(FolderStandFire)
                ? FolderStandFire
                : FolderRun;

            if (result.ContainsKey(source))
            {
                List<Sprite> sourceSprites = LoadSpritesSorted(
                    SpriteRoot + "/" + source
                );

                result[FolderIdle] = WriteClip(
                    FolderIdle,
                    new List<Sprite> { sourceSprites[0] },
                    false
                );

                Debug.Log(
                    "ยังไม่มีโฟลเดอร์ \"" + FolderIdle +
                    "\" ใช้เฟรมแรกของ \"" + source + "\" แทน"
                );
            }
        }

        // มีโฟลเดอร์ท่ายืนยิงขึ้นจริงแล้วก็ไม่ต้องตัดเฟรมมาทำเอง
        if (result.ContainsKey(FolderAimUpStand))
        {
            Debug.Log(
                "ใช้โฟลเดอร์ \"" + FolderAimUpStand + "\" เป็นท่ายืนยิงขึ้น"
            );
        }
        else if (result.ContainsKey(FolderAimUpStraight))
        {
            List<Sprite> upSprites = LoadSpritesSorted(
                SpriteRoot + "/" + FolderAimUpStraight
            );

            int index = Mathf.Clamp(
                AimUpStandFrame - 1, 0, upSprites.Count - 1
            );

            result[ClipAimUpStand] = WriteClip(
                ClipAimUpStand,
                new List<Sprite> { upSprites[index] },
                false
            );

            Debug.Log(
                "ทำท่ายืนยิงขึ้นจากเฟรมที่ " + (index + 1) +
                " ของ \"" + FolderAimUpStraight + "\""
            );
        }

        if (!result.ContainsKey(FolderCrouch) &&
            result.ContainsKey(FolderCrouchFire))
        {
            List<Sprite> crouchSprites = LoadSpritesSorted(
                SpriteRoot + "/" + FolderCrouchFire
            );

            result[FolderCrouch] = WriteClip(
                FolderCrouch,
                new List<Sprite> { crouchSprites[0] },
                false
            );

            Debug.Log(
                "ไม่มีโฟลเดอร์ \"" + FolderCrouch +
                "\" ใช้เฟรมแรกของท่าคลานยิงไปก่อน"
            );
        }

        return result;
    }

    private static List<Sprite> LoadSpritesSorted(string folder)
    {
        string[] files = Directory.GetFiles(folder)
            .Where(IsImage)
            .ToArray();

        // เรียงตามตัวเลขท้ายชื่อไฟล์ (1, 2, ... 10, 11) ไม่ใช่เรียงตัวอักษร
        System.Array.Sort(
            files,
            (a, b) => TrailingNumber(a).CompareTo(TrailingNumber(b))
        );

        List<Sprite> sprites = new List<Sprite>();

        foreach (string file in files)
        {
            string path = file.Replace("\\", "/");

            Sprite sprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite != null)
            {
                sprites.Add(sprite);
            }
        }

        return sprites;
    }

    private static bool IsImage(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();

        return ext == ".png" || ext == ".jpg" || ext == ".jpeg";
    }

    /// <summary>
    /// ดึงเลขลำดับจากชื่อไฟล์
    ///
    /// รองรับสองแบบที่เจอในโปรเจกต์นี้
    ///   "Shoot straight up 11"                    เลขต่อท้ายตรงๆ
    ///   "Stand still and fire into the sky (11)"  เลขในวงเล็บ แบบที่ Windows ตั้งให้ตอนก๊อป
    ///
    /// วิธีเดิมจับเฉพาะเลขที่อยู่ท้ายสุดจริงๆ พอเจอวงเล็บปิดคั่นก็อ่านไม่ออก
    /// ได้ 0 หมดทุกไฟล์ แล้วอนิเมชั่นจะเล่นเรียงตามตัวอักษรแทน ซึ่งสลับลำดับ
    /// ตอนนี้หยิบกลุ่มตัวเลข "กลุ่มสุดท้าย" ที่เจอในชื่อ ไม่สนว่ามีอะไรตามหลัง
    /// </summary>
    private static int TrailingNumber(string path)
    {
        MatchCollection numbers = Regex.Matches(
            Path.GetFileNameWithoutExtension(path),
            @"\d+"
        );

        if (numbers.Count == 0)
            return 0;

        return int.Parse(numbers[numbers.Count - 1].Value);
    }

    private static AnimationClip WriteClip(
        string name,
        List<Sprite> sprites,
        bool spin)
    {
        string path = ClipFolder + "/" + name + ".anim";

        AnimationClip clip =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

        bool isNew = clip == null;

        if (isNew)
        {
            clip = new AnimationClip();
        }
        else
        {
            clip.ClearCurves();

            AnimationUtility.SetAnimationEvents(
                clip,
                new AnimationEvent[0]
            );
        }

        int frameRate = sprites.Count > 1 ? CycleFrameRate : 1;

        clip.frameRate = spin ? 60 : frameRate;

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(
            SpritePath,
            typeof(SpriteRenderer),
            "m_Sprite"
        );

        ObjectReferenceKeyframe[] keys =
            new ObjectReferenceKeyframe[sprites.Count];

        for (int i = 0; i < sprites.Count; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i / (float)frameRate,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        WriteSpinCurves(clip, spin);

        AnimationClipSettings settings =
            AnimationUtility.GetAnimationClipSettings(clip);

        settings.loopTime = spin || sprites.Count > 1;

        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (isNew)
        {
            AssetDatabase.CreateAsset(clip, path);
        }
        else
        {
            EditorUtility.SetDirty(clip);
        }

        return clip;
    }

    /// <summary>
    /// ท่ากระโดดหมุนตีลังกา ส่วนท่าอื่นตรึงมุมไว้ที่ 0
    /// ต้องใส่เส้นมุมหมุนให้ครบทุกคลิป เพราะ Write Defaults ปิดอยู่
    /// ถ้าคลิปไหนไม่มีเส้นนี้ ตัวจะค้างเอียงหลังลงพื้น
    /// </summary>
    private static void WriteSpinCurves(AnimationClip clip, bool spin)
    {
        float length = spin ? JumpSpinDuration : 1f / clip.frameRate;

        // ตีลังกาไปข้างหน้า = หมุนตามเข็ม = ค่า z ติดลบ
        // ตอนหันซ้าย Player พลิกด้วย localScale.x ติดลบ ภาพเลยหมุนกลับทางให้เอง
        AnimationCurve z = spin
            ? AnimationCurve.Linear(0f, 0f, length, -360f)
            : AnimationCurve.Linear(0f, 0f, length, 0f);

        clip.SetCurve(
            VisualPath, typeof(Transform), "localEulerAnglesRaw.x",
            AnimationCurve.Linear(0f, 0f, length, 0f));

        clip.SetCurve(
            VisualPath, typeof(Transform), "localEulerAnglesRaw.y",
            AnimationCurve.Linear(0f, 0f, length, 0f));

        clip.SetCurve(
            VisualPath, typeof(Transform), "localEulerAnglesRaw.z", z);
    }

    // ---------- ต่อเข้า Animator ----------

    private static void WireController(
        Dictionary<string, AnimationClip> clips)
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(
                ControllerPath
            );

        if (controller == null)
        {
            Debug.LogError(
                "หา Animator Controller ไม่เจอ: " + ControllerPath
            );

            return;
        }

        EnsureParameter(
            controller, "Speed", AnimatorControllerParameterType.Float);

        EnsureParameter(
            controller, "IsGrounded", AnimatorControllerParameterType.Bool);

        EnsureParameter(
            controller, "IsFiring", AnimatorControllerParameterType.Bool);

        EnsureParameter(
            controller, "AimY", AnimatorControllerParameterType.Float);

        EnsureParameter(
            controller, "IsCrouching", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine machine =
            controller.layers[0].stateMachine;

        ClearMachine(machine);

        AnimatorState idle = AddState(
            machine, clips, FolderIdle, new Vector3(280, 0, 0));

        AnimatorState run = AddState(
            machine, clips, FolderRun, new Vector3(280, 70, 0));

        AnimatorState jump = AddState(
            machine, clips, FolderJump, new Vector3(280, 140, 0));

        AnimatorState runFire = AddState(
            machine, clips, FolderRunFire, new Vector3(280, 210, 0));

        AnimatorState aimUp = AddState(
            machine, clips, FolderAimUp, new Vector3(280, 280, 0));

        AnimatorState aimDown = AddState(
            machine, clips, FolderAimDown, new Vector3(280, 350, 0));

        AnimatorState aimUpStraight = AddState(
            machine, clips, FolderAimUpStraight, new Vector3(280, 420, 0));

        AnimatorState crouchFire = AddState(
            machine, clips, FolderCrouchFire, new Vector3(280, 490, 0));

        AnimatorState crouch = AddState(
            machine, clips, FolderCrouch, new Vector3(280, 560, 0));

        AnimatorState standFire = AddState(
            machine, clips, FolderStandFire, new Vector3(280, 630, 0));

        // มีโฟลเดอร์จริงก็ใช้อันนั้น ไม่มีค่อยใช้เฟรมที่ตัดมาตรึงไว้
        string standUpClip = clips.ContainsKey(FolderAimUpStand)
            ? FolderAimUpStand
            : ClipAimUpStand;

        AnimatorState aimUpStand = AddState(
            machine, clips, standUpClip, new Vector3(280, 700, 0));

        if (idle != null)
        {
            machine.defaultState = idle;
        }

        // ลำดับสำคัญ Any State ไล่เช็คจากบนลงล่าง เจออันแรกที่เงื่อนไขผ่านก็ใช้เลย

        // 1. ลอยอยู่กลางอากาศ = ท่ากระโดดเสมอ
        Link(machine, jump, t =>
        {
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
        });

        // 2. หมอบอยู่ -> ท่าคลาน ไม่สนมุมเล็ง เพราะคลานยิงได้แค่แนวนอน
        Link(machine, crouchFire, t =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsCrouching");
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsFiring");
        });

        Link(machine, crouch, t =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsCrouching");
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsFiring");
        });

        // 3. ยิงโดยเล็งเฉียงหรือตรงขึ้น -> ใช้ท่าตามมุมเล็ง ไม่สนว่ายืนหรือวิ่ง
        // กลุ่มนี้ต้องมาก่อนท่ายืนยิง ไม่งั้นยืนนิ่งกดยิงขึ้นจะถูกท่ายืนยิงคว้าไปก่อน
        // เพราะท่ายืนยิงไม่ได้ดูมุมเล็ง ท่า "ยิงขึ้นตรงๆ" เลยไม่ได้เล่นเลย
        // ยืนนิ่งยิงขึ้น ใช้เฟรมตรึง ไม่งั้นขาจะวิ่งอยู่กับที่
        Link(machine, aimUpStand, t =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsCrouching");
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsFiring");
            t.AddCondition(AnimatorConditionMode.Greater, 0.9f, "AimY");
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        });

        Link(machine, aimUpStraight, t =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsCrouching");
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsFiring");
            t.AddCondition(AnimatorConditionMode.Greater, 0.9f, "AimY");
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        });

        Link(machine, aimUp, t =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsCrouching");
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsFiring");
            t.AddCondition(AnimatorConditionMode.Greater, 0.5f, "AimY");
            t.AddCondition(AnimatorConditionMode.Less, 0.9f, "AimY");
        });

        Link(machine, aimDown, t =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsCrouching");
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsFiring");
            t.AddCondition(AnimatorConditionMode.Less, -0.5f, "AimY");
        });

        // 4. เล็งแนวนอน -> แยกตามว่าขากำลังขยับหรือไม่
        // ใส่ขอบ AimY ไว้ด้วยทั้งที่ลำดับจัดให้ถูกแล้ว
        // วันหลังสลับลำดับไปมาก็ยังไม่พัง เพราะเงื่อนไขไม่ทับกันเองเลย
        Link(machine, standFire, t =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsCrouching");
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsFiring");
            t.AddCondition(AnimatorConditionMode.Less, 0.5f, "AimY");
            t.AddCondition(AnimatorConditionMode.Greater, -0.5f, "AimY");
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        });

        Link(machine, runFire, t =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsCrouching");
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsFiring");
            t.AddCondition(AnimatorConditionMode.Less, 0.5f, "AimY");
            t.AddCondition(AnimatorConditionMode.Greater, -0.5f, "AimY");
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        });


        // 5. อยู่บนพื้น ไม่ได้ยิง -> วิ่งหรือยืนเฉยๆ
        Link(machine, run, t =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsCrouching");
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsFiring");
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        });

        Link(machine, idle, t =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsCrouching");
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsFiring");
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        });

        EditorUtility.SetDirty(controller);

        List<string> unused = clips.Keys
            .Where(k =>
                k != FolderIdle && k != FolderRun && k != FolderJump &&
                k != FolderRunFire && k != FolderAimUp && k != FolderAimDown &&
                k != FolderAimUpStraight && k != FolderCrouchFire &&
                k != FolderCrouch && k != FolderStandFire &&
                k != ClipAimUpStand && k != FolderAimUpStand)
            .ToList();

        if (unused.Count > 0)
        {
            Debug.LogWarning(
                "คลิปพวกนี้สร้างไว้แล้วแต่ยังไม่ได้ต่อเข้า Animator: " +
                string.Join(", ", unused.ToArray())
            );
        }
    }

    private static void ClearMachine(AnimatorStateMachine machine)
    {
        foreach (AnimatorStateTransition transition in
            machine.anyStateTransitions.ToArray())
        {
            machine.RemoveAnyStateTransition(transition);
        }

        foreach (AnimatorTransition transition in
            machine.entryTransitions.ToArray())
        {
            machine.RemoveEntryTransition(transition);
        }

        foreach (ChildAnimatorState child in machine.states.ToArray())
        {
            machine.RemoveState(child.state);
        }
    }

    private static AnimatorState AddState(
        AnimatorStateMachine machine,
        Dictionary<string, AnimationClip> clips,
        string name,
        Vector3 position)
    {
        AnimationClip clip;

        if (!clips.TryGetValue(name, out clip))
        {
            Debug.LogWarning(
                "ยังไม่มีโฟลเดอร์ \"" + name + "\" เลยข้าม State นี้ไป"
            );

            return null;
        }

        AnimatorState state = machine.AddState(name, position);

        state.motion = clip;
        state.writeDefaultValues = false;

        return state;
    }

    private static void Link(
        AnimatorStateMachine machine,
        AnimatorState target,
        System.Action<AnimatorStateTransition> setup)
    {
        if (target == null)
            return;

        AnimatorStateTransition transition =
            machine.AddAnyStateTransition(target);

        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = 0f;
        transition.canTransitionToSelf = false;

        setup(transition);
    }

    private static void EnsureParameter(
        AnimatorController controller,
        string name,
        AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter parameter in
            controller.parameters)
        {
            if (parameter.name == name)
            {
                if (parameter.type != type)
                {
                    Debug.LogWarning(
                        "พารามิเตอร์ \"" + name + "\" ชนิดไม่ตรง"
                    );
                }

                return;
            }
        }

        controller.AddParameter(name, type);
    }
}
