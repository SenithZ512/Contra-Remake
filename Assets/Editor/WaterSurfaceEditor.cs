using UnityEditor;
using UnityEngine;

/// <summary>
/// หน้าตั้งค่าผิวน้ำแบบกดปุ่ม แยกสีตามกลุ่มและบอกว่าปุ่มไหนแตะค่าอะไร
/// </summary>
[CustomEditor(typeof(WaterSurface))]
[CanEditMultipleObjects]
public class WaterSurfaceEditor : Editor
{
    private static readonly Color FlowColor = new Color(0.5f, 0.85f, 1f);
    private static readonly Color ShineColor = new Color(1f, 0.95f, 0.55f);
    private static readonly Color ToggleColor = new Color(0.8f, 0.8f, 0.9f);

    private SerializedProperty scrollTexture;
    private SerializedProperty scrollSpeed;
    private SerializedProperty shimmer;
    private SerializedProperty shimmerSpeed;
    private SerializedProperty darkColor;

    private void OnEnable()
    {
        scrollTexture = serializedObject.FindProperty("scrollTexture");
        scrollSpeed = serializedObject.FindProperty("scrollSpeed");
        shimmer = serializedObject.FindProperty("shimmer");
        shimmerSpeed = serializedObject.FindProperty("shimmerSpeed");
        darkColor = serializedObject.FindProperty("darkColor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        Section(
            "กระแสน้ำ",
            "ลายน้ำไหลเร็วแค่ไหน ตัวผืนน้ำไม่ขยับ เลื่อนแค่ลายข้างใน",
            FlowColor
        );

        EditorGUILayout.BeginHorizontal();
        FlowButton("นิ่ง", 0f, "น้ำนิ่งสนิท");
        FlowButton("เอื่อย", -0.04f, "ลายวนครบรอบใน 25 วินาที");
        FlowButton("ปกติ", -0.08f, "12 วินาทีต่อรอบ");
        FlowButton("เชี่ยว", -0.2f, "5 วินาทีต่อรอบ");
        EditorGUILayout.EndHorizontal();

        GUI.backgroundColor = FlowColor;

        if (GUILayout.Button("สลับทิศไหล"))
        {
            scrollSpeed.floatValue = -scrollSpeed.floatValue;
        }

        GUI.backgroundColor = Color.white;

        Section(
            "ประกายน้ำ",
            "ไล่เฉดสีไปมาแบบ palette cycling ของ NES ยิ่งเข้มยิ่งเห็นชัด",
            ShineColor
        );

        EditorGUILayout.BeginHorizontal();
        ShineButton("จาง", 2.2f, new Color(0.9f, 0.95f, 1f), "แทบไม่เห็น");
        ShineButton("ปกติ", 2.2f, new Color(0.78f, 0.88f, 1f), "ค่าที่ตั้งไว้ตอนแรก");
        ShineButton("ชัด", 3.5f, new Color(0.6f, 0.8f, 1f), "วิบวับแบบ NES แท้");
        EditorGUILayout.EndHorizontal();

        Section(
            "เปิด-ปิด",
            "ปิดอันไหนก็ได้ถ้าไม่ชอบ อีกอันยังทำงานต่อ",
            ToggleColor
        );

        EditorGUILayout.BeginHorizontal();
        ToggleButton(scrollTexture, "ลายไหล");
        ToggleButton(shimmer, "ประกายน้ำ");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("ค่าดิบ", EditorStyles.boldLabel);

        DrawPropertiesExcluding(serializedObject, "m_Script");

        serializedObject.ApplyModifiedProperties();
    }

    private void Section(string title, string help, Color color)
    {
        EditorGUILayout.Space(8);

        Color old = GUI.color;
        GUI.color = color;

        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        GUI.color = old;

        EditorGUILayout.HelpBox(help, MessageType.None);
    }

    private void FlowButton(string label, float value, string tip)
    {
        GUI.backgroundColor = FlowColor;

        if (GUILayout.Button(new GUIContent(label, tip)))
        {
            scrollSpeed.floatValue = value;
            scrollTexture.boolValue = value != 0f;
        }

        GUI.backgroundColor = Color.white;
    }

    private void ShineButton(string label, float speed, Color dark, string tip)
    {
        GUI.backgroundColor = ShineColor;

        if (GUILayout.Button(new GUIContent(label, tip)))
        {
            shimmerSpeed.floatValue = speed;
            darkColor.colorValue = dark;
            shimmer.boolValue = true;
        }

        GUI.backgroundColor = Color.white;
    }

    private void ToggleButton(SerializedProperty flag, string label)
    {
        GUI.backgroundColor = flag.boolValue
            ? new Color(0.6f, 1f, 0.6f)
            : new Color(1f, 0.6f, 0.6f);

        string state = flag.boolValue ? "เปิด" : "ปิด";

        if (GUILayout.Button(label + " : " + state))
        {
            flag.boolValue = !flag.boolValue;
        }

        GUI.backgroundColor = Color.white;
    }
}
