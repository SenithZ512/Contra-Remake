using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// สร้าง state Jump และเส้นเชื่อมทั้งหมดให้ PlayerAnimatorController
/// ใช้ API ของ Unity เอง เพื่อให้ไฟล์ถูกเขียนอย่างถูกต้อง 100%
/// กดซ้ำได้ ของที่มีอยู่แล้วจะไม่ถูกสร้างซ้ำ
/// </summary>
public static class PlayerAnimatorSetup
{
    private const string ControllerPath =
        "Assets/Spicpt/Player/Animation/PlayerAnimatorController.controller";

    private const string JumpClipPath =
        "Assets/Spicpt/Player/Animation/Player_Jump.anim";

    private const string SpeedParam = "Speed";
    private const string GroundedParam = "IsGrounded";
    private const float SpeedThreshold = 0.1f;

    [MenuItem("Tools/Contra/Setup Player Animator")]
    public static void Setup()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

        if (controller == null)
        {
            Debug.LogError("หา Controller ไม่เจอที่ " + ControllerPath);
            return;
        }

        AnimationClip jumpClip =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(JumpClipPath);

        if (jumpClip == null)
        {
            Debug.LogError("หาคลิป Jump ไม่เจอที่ " + JumpClipPath);
            return;
        }

        EnsureBoolParameter(controller, GroundedParam, true);

        AnimatorStateMachine stateMachine =
            controller.layers[0].stateMachine;

        CleanUp(stateMachine);

        AnimatorState idle = FindState(stateMachine, "Idle");
        AnimatorState run = FindState(stateMachine, "Run");

        if (idle == null || run == null)
        {
            Debug.LogError("ไม่พบ state Idle หรือ Run ใน Controller");
            return;
        }

        // ถ้า state เริ่มต้นถูกลบไปตอนล้างของส่วนเกิน ต้องตั้งกลับเป็น Idle
        stateMachine.defaultState = idle;

        AnimatorState jump = FindState(stateMachine, "Jump");

        if (jump == null)
        {
            jump = stateMachine.AddState("Jump", new Vector3(300f, 240f, 0f));
        }

        jump.motion = jumpClip;
        jump.writeDefaultValues = true;

        // ลอยตัวเมื่อไหร่ = เข้า Jump ทันที ไม่ว่ากำลังยืนหรือวิ่งอยู่
        AddTransition(idle, jump, t =>
        {
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, GroundedParam);
        });

        AddTransition(run, jump, t =>
        {
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, GroundedParam);
        });

        // แตะพื้นแล้วแยกทางชัดเจน ไม่ต้องแวะ Idle ก่อนถ้ากำลังวิ่ง
        AddTransition(jump, run, t =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, GroundedParam);
            t.AddCondition(AnimatorConditionMode.Greater, SpeedThreshold, SpeedParam);
        });

        AddTransition(jump, idle, t =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, GroundedParam);
            t.AddCondition(AnimatorConditionMode.Less, SpeedThreshold, SpeedParam);
        });

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log("ตั้งค่า Animator ของ Player เรียบร้อย: เพิ่ม state Jump และเส้นเชื่อมครบแล้ว");
    }

    /// <summary>
    /// ล้างของส่วนเกินก่อนตั้งค่าใหม่ กันปัญหาที่เผลอลากคลิปเข้ากราฟจนได้ state ซ้ำ
    /// และเส้นเชื่อมที่ไม่มีเงื่อนไข (ซึ่งจะทำงานทันทีโดยไม่รออะไร)
    /// </summary>
    private static void CleanUp(AnimatorStateMachine stateMachine)
    {
        string[] keep = { "Idle", "Run", "Jump" };

        // ลบ state ที่ไม่ได้อยู่ในรายการ (เส้นเชื่อมที่ชี้หามันจะถูกลบตามไปเอง)
        foreach (ChildAnimatorState child in stateMachine.states.ToArray())
        {
            if (child.state == null)
                continue;

            if (System.Array.IndexOf(keep, child.state.name) >= 0)
                continue;

            Debug.Log("ลบ state ส่วนเกิน: " + child.state.name);

            stateMachine.RemoveState(child.state);
        }

        // ลบเส้นเชื่อมที่ไม่มีเงื่อนไข
        foreach (ChildAnimatorState child in stateMachine.states)
        {
            if (child.state == null)
                continue;

            foreach (AnimatorStateTransition transition in child.state.transitions.ToArray())
            {
                if (transition.conditions != null && transition.conditions.Length > 0)
                    continue;

                Debug.Log(
                    "ลบเส้นเชื่อมที่ไม่มีเงื่อนไข: ออกจาก " + child.state.name
                );

                child.state.RemoveTransition(transition);
            }
        }

        foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
        {
            if (transition.conditions != null && transition.conditions.Length > 0)
                continue;

            Debug.Log("ลบเส้นเชื่อมที่ไม่มีเงื่อนไขจาก Any State");

            stateMachine.RemoveAnyStateTransition(transition);
        }
    }

    private static void EnsureBoolParameter(
        AnimatorController controller,
        string name,
        bool defaultValue)
    {
        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.name == name)
                return;
        }

        controller.AddParameter(name, AnimatorControllerParameterType.Bool);

        // AddParameter ตั้งค่าเริ่มต้นไม่ได้ตรงๆ ต้องเขียนกลับผ่านอาเรย์
        AnimatorControllerParameter[] parameters = controller.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == name)
            {
                parameters[i].defaultBool = defaultValue;
            }
        }

        controller.parameters = parameters;
    }

    private static AnimatorState FindState(
        AnimatorStateMachine stateMachine,
        string name)
    {
        foreach (ChildAnimatorState child in stateMachine.states)
        {
            if (child.state != null && child.state.name == name)
                return child.state;
        }

        return null;
    }

    private static void AddTransition(
        AnimatorState from,
        AnimatorState to,
        System.Action<AnimatorStateTransition> configure)
    {
        // มีเส้นนี้อยู่แล้วก็ข้าม กันสร้างซ้ำเวลากดปุ่มหลายครั้ง
        foreach (AnimatorStateTransition existing in from.transitions)
        {
            if (existing.destinationState == to)
                return;
        }

        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0f;

        configure(transition);
    }
}
