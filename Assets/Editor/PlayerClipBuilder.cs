using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// แปลงคลิปของผู้เล่นให้เป็นแบบมาตรฐานของ Unity
/// คือให้คลิปขยับ property Sprite ของ SpriteRenderer ตรงๆ
/// แทนการยิง Animation Event ไปเรียกสคริปต์
///
/// ผลคือเปิดแก้ใน Animation window ได้ตามปกติ และไม่ต้องใช้ PlayerAnimationFrames อีก
/// </summary>
public static class PlayerClipBuilder
{
    private const string RunClipPath =
        "Assets/Spicpt/Player/Animation/Player_Run.anim";

    private const string JumpClipPath =
        "Assets/Spicpt/Player/Animation/Player_Jump.anim";

    private const string RunSheetPath = "Assets/Asset/1.png";
    private const string JumpFolder = "Assets/Asset/PlayerJump";
    private const string JumpPrefix = "Jump_Spin_";

    private const float RunFrameRate = 24f;
    private const float JumpFrameRate = 16f;

    [MenuItem("Tools/Contra/Rebuild Player Clips")]
    public static void RebuildClips()
    {
        Sprite[] runSprites = LoadSheetSprites(RunSheetPath);
        Sprite[] jumpSprites = LoadNumberedSprites(JumpFolder, JumpPrefix);

        if (runSprites.Length == 0)
        {
            Debug.LogError("ไม่พบสไปรท์ในชีต " + RunSheetPath + " (สไลซ์แล้วหรือยัง?)");
            return;
        }

        if (jumpSprites.Length == 0)
        {
            Debug.LogError("ไม่พบสไปรท์ " + JumpPrefix + "* ใน " + JumpFolder);
            return;
        }

        bool runOk = WriteSpriteCurve(RunClipPath, runSprites, RunFrameRate);
        bool jumpOk = WriteSpriteCurve(JumpClipPath, jumpSprites, JumpFrameRate);

        if (!runOk || !jumpOk)
            return;

        AssetDatabase.SaveAssets();

        Debug.Log(
            "แปลงคลิปเรียบร้อย: Run " + runSprites.Length + " เฟรม, " +
            "Jump " + jumpSprites.Length + " เฟรม " +
            "— ตอนนี้ลบ component PlayerAnimationFrames ออกจาก Player ได้แล้ว"
        );
    }

    /// <summary>เขียน keyframe ของ Sprite ลงคลิปเดิม (คง guid ไว้ Animator จึงยังชี้ถูก)</summary>
    private static bool WriteSpriteCurve(
        string clipPath,
        Sprite[] sprites,
        float frameRate)
    {
        AnimationClip clip =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

        if (clip == null)
        {
            Debug.LogError("หาคลิปไม่เจอที่ " + clipPath);
            return false;
        }

        clip.frameRate = frameRate;

        // ล้าง Animation Event ของวิธีเดิมทิ้ง ไม่งั้นจะเรียกสคริปต์ซ้อนกับ curve
        AnimationUtility.SetAnimationEvents(clip, new AnimationEvent[0]);

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(
            string.Empty,
            typeof(SpriteRenderer),
            "m_Sprite"
        );

        ObjectReferenceKeyframe[] keyframes =
            new ObjectReferenceKeyframe[sprites.Length];

        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        AnimationClipSettings settings =
            AnimationUtility.GetAnimationClipSettings(clip);

        settings.loopTime = true;

        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);

        return true;
    }

    /// <summary>ดึงสไปรท์ย่อยทั้งหมดในชีต แล้วเรียงตามเลขท้ายชื่อ (1_2 ต้องมาก่อน 1_10)</summary>
    private static Sprite[] LoadSheetSprites(string sheetPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(sheetPath)
            .OfType<Sprite>()
            .OrderBy(GetTrailingNumber)
            .ToArray();
    }

    /// <summary>ดึงไฟล์สไปรท์เดี่ยวที่ชื่อขึ้นต้นเหมือนกัน แล้วเรียงตามเลขท้ายชื่อ</summary>
    private static Sprite[] LoadNumberedSprites(string folder, string prefix)
    {
        List<Sprite> sprites = new List<Sprite>();

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite != null && sprite.name.StartsWith(prefix))
            {
                sprites.Add(sprite);
            }
        }

        return sprites.OrderBy(GetTrailingNumber).ToArray();
    }

    private static int GetTrailingNumber(Sprite sprite)
    {
        string name = sprite.name;
        int index = name.Length;

        while (index > 0 && char.IsDigit(name[index - 1]))
        {
            index--;
        }

        if (index >= name.Length)
            return 0;

        return int.Parse(name.Substring(index));
    }
}
