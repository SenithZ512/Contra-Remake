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

    private const int CycleFrameRate = 16;

    // ลูกของ Player: Visual (จุดหมุนอยู่ระดับเอว) -> Sprite (ตัว SpriteRenderer)
    private const string VisualPath = "Visual";
    private const string SpritePath = "Visual/Sprite";

    /// <summary>ตีลังกา 1 รอบใช้กี่วินาที ยิ่งมากยิ่งหมุนช้า กระโดดครั้งนึงลอยราว 1 วิ</summary>
    private const float JumpSpinDuration = 0.2f;

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
        if (!result.ContainsKey(FolderIdle) &&
            result.ContainsKey(FolderRun))
        {
            List<Sprite> runSprites = LoadSpritesSorted(
                SpriteRoot + "/" + FolderRun
            );

            result[FolderIdle] = WriteClip(
                FolderIdle,
                new List<Sprite> { runSprites[0] },
                false
            );

            Debug.Log(
                "ยังไม่มีโฟลเดอร์ \"" + FolderIdle +
                "\" ใช้เฟรมแรกของท่าวิ่งไปก่อน"
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

    private static int TrailingNumber(string path)
    {
        Match match = Regex.Match(
            Path.GetFileNameWithoutExtension(path),
            @"(\d+)\s*$"
        );

        return match.Success
            ? int.Parse(match.Groups[1].Value)
            : 0;
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

        // 2. อยู่บนพื้น + กำลังยิง -> เลือกท่าตามมุมเล็ง
        Link(machine, aimUp, t =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsFiring");
            t.AddCondition(AnimatorConditionMode.Greater, 0.5f, "AimY");
        });

        Link(machine, aimDown, t =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsFiring");
            t.AddCondition(AnimatorConditionMode.Less, -0.5f, "AimY");
        });

        Link(machine, runFire, t =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsFiring");
            t.AddCondition(AnimatorConditionMode.Less, 0.5f, "AimY");
            t.AddCondition(AnimatorConditionMode.Greater, -0.5f, "AimY");
        });

        // 3. อยู่บนพื้น ไม่ได้ยิง -> วิ่งหรือยืนเฉยๆ
        Link(machine, run, t =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsFiring");
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        });

        Link(machine, idle, t =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsFiring");
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        });

        EditorUtility.SetDirty(controller);

        List<string> unused = clips.Keys
            .Where(k =>
                k != FolderIdle && k != FolderRun && k != FolderJump &&
                k != FolderRunFire && k != FolderAimUp && k != FolderAimDown)
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
