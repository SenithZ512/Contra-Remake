using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// หน้า Inspector กลางของสคริปต์ทุกตัวในโฟลเดอร์ Spicpt
///
///   - จัดกลุ่มตาม [Header] แล้วแยกสีให้แต่ละกลุ่ม สีคงที่ตามชื่อกลุ่ม
///   - ตัวเลขมีปุ่มกด ลด/เพิ่ม/คูณสอง/หารสอง ไม่ต้องพิมพ์เอง
///   - true/false เป็นปุ่มสี เขียว = เปิด แดง = ปิด
///   - ชี้เมาส์ค้างที่ปุ่มจะบอกว่ากดแล้วค่าไปเป็นเท่าไหร่
///
/// สคริปต์ของ Unity เองหรือของแพ็กเกจไม่โดนแตะ วาดแบบเดิมทุกอย่าง
/// </summary>
[CustomEditor(typeof(MonoBehaviour), true)]
[CanEditMultipleObjects]
public class ContraInspector : Editor
{
    /// <summary>แตะเฉพาะสคริปต์ที่อยู่ใต้โฟลเดอร์นี้</summary>
    private const string OwnFolder = "Assets/Spicpt/";

    private static readonly Color[] GroupColors =
    {
        new Color(0.55f, 0.80f, 1.00f),   // ฟ้า
        new Color(0.60f, 1.00f, 0.65f),   // เขียว
        new Color(1.00f, 0.82f, 0.50f),   // ส้ม
        new Color(0.85f, 0.70f, 1.00f),   // ม่วง
        new Color(1.00f, 0.70f, 0.70f),   // ชมพู
        new Color(0.70f, 0.95f, 0.95f)    // ฟ้าอมเขียว
    };

    private static readonly Color OnColor = new Color(0.55f, 0.95f, 0.55f);
    private static readonly Color OffColor = new Color(1f, 0.6f, 0.6f);

    private bool isOwnScript;
    private Dictionary<string, string> headers;
    private Dictionary<string, string> tooltips;

    private void OnEnable()
    {
        isOwnScript = false;

        MonoBehaviour behaviour = target as MonoBehaviour;

        if (behaviour == null)
            return;

        MonoScript script = MonoScript.FromMonoBehaviour(behaviour);

        if (script == null)
            return;

        string path = AssetDatabase.GetAssetPath(script);

        isOwnScript = !string.IsNullOrEmpty(path) && path.StartsWith(OwnFolder);

        if (isOwnScript)
        {
            ReadAttributes(behaviour.GetType());
        }
    }

    /// <summary>อ่าน [Header] กับ [Tooltip] จากตัวคลาส ผ่าน reflection</summary>
    private void ReadAttributes(System.Type type)
    {
        headers = new Dictionary<string, string>();
        tooltips = new Dictionary<string, string>();

        FieldInfo[] fields = type.GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic
        );

        foreach (FieldInfo field in fields)
        {
            HeaderAttribute header = field.GetCustomAttribute<HeaderAttribute>();

            if (header != null)
            {
                headers[field.Name] = header.header;
            }

            TooltipAttribute tip = field.GetCustomAttribute<TooltipAttribute>();

            if (tip != null)
            {
                tooltips[field.Name] = tip.tooltip;
            }
        }
    }

    public override void OnInspectorGUI()
    {
        if (!isOwnScript)
        {
            // สคริปต์ที่ไม่ใช่ของเรา วาดแบบมาตรฐานไปตามเดิม
            DrawDefaultInspector();
            return;
        }

        serializedObject.Update();

        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        int groupIndex = -1;

        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (property.propertyPath == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(property);
                }

                continue;
            }

            string headerText;

            if (headers != null &&
                headers.TryGetValue(property.name, out headerText))
            {
                groupIndex++;

                DrawGroupHeader(headerText, GroupColor(groupIndex));
            }

            DrawProperty(property);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static Color GroupColor(int index)
    {
        if (index < 0)
            return Color.white;

        return GroupColors[index % GroupColors.Length];
    }

    private void DrawGroupHeader(string text, Color color)
    {
        EditorGUILayout.Space(8);

        Rect bar = GUILayoutUtility.GetRect(0f, 3f, GUILayout.ExpandWidth(true));

        EditorGUI.DrawRect(bar, color);

        Color old = GUI.color;
        GUI.color = color;

        EditorGUILayout.LabelField(text, EditorStyles.boldLabel);

        GUI.color = old;
    }

    private void DrawProperty(SerializedProperty property)
    {
        string tip;

        if (tooltips == null || !tooltips.TryGetValue(property.name, out tip))
        {
            tip = string.Empty;
        }

        GUIContent label = new GUIContent(property.displayName, tip);

        switch (property.propertyType)
        {
            case SerializedPropertyType.Boolean:
                DrawToggleButton(property, label);
                return;

            case SerializedPropertyType.Float:
                DrawFloatRow(property, label);
                return;

            case SerializedPropertyType.Integer:
                DrawIntRow(property, label);
                return;

            default:
                EditorGUILayout.PropertyField(property, label, true);
                return;
        }
    }

    private void DrawToggleButton(SerializedProperty property, GUIContent label)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth));

        bool on = property.boolValue;

        GUI.backgroundColor = on ? OnColor : OffColor;

        string caption = on ? "เปิด" : "ปิด";

        if (GUILayout.Button(new GUIContent(caption, "กดเพื่อสลับเปิด/ปิด")))
        {
            property.boolValue = !on;
        }

        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawFloatRow(SerializedProperty property, GUIContent label)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.PropertyField(property, label);

        float value = property.floatValue;

        // ก้าวทีละ 10% ของค่าปัจจุบัน ค่าเล็กก็ขยับละเอียด ค่าใหญ่ก็ขยับหยาบ
        float step = Mathf.Max(0.01f, Mathf.Abs(value) * 0.1f);

        if (StepButton("−", value - step, "ลดลงเหลือ"))
        {
            property.floatValue = Round(value - step);
        }

        if (StepButton("+", value + step, "เพิ่มเป็น"))
        {
            property.floatValue = Round(value + step);
        }

        if (StepButton("÷2", value * 0.5f, "ครึ่งหนึ่งคือ"))
        {
            property.floatValue = Round(value * 0.5f);
        }

        if (StepButton("×2", value * 2f, "สองเท่าคือ"))
        {
            property.floatValue = Round(value * 2f);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawIntRow(SerializedProperty property, GUIContent label)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.PropertyField(property, label);

        int value = property.intValue;

        if (StepButton("−", value - 1, "ลดลงเหลือ"))
        {
            property.intValue = value - 1;
        }

        if (StepButton("+", value + 1, "เพิ่มเป็น"))
        {
            property.intValue = value + 1;
        }

        if (StepButton("÷2", value / 2, "ครึ่งหนึ่งคือ"))
        {
            property.intValue = value / 2;
        }

        if (StepButton("×2", value * 2, "สองเท่าคือ"))
        {
            property.intValue = value * 2;
        }

        EditorGUILayout.EndHorizontal();
    }

    private static bool StepButton(string caption, float preview, string prefix)
    {
        GUI.backgroundColor = new Color(0.82f, 0.88f, 1f);

        bool pressed = GUILayout.Button(
            new GUIContent(caption, prefix + " " + Round(preview)),
            GUILayout.Width(28f)
        );

        GUI.backgroundColor = Color.white;

        return pressed;
    }

    private static float Round(float value)
    {
        return Mathf.Round(value * 1000f) / 1000f;
    }
}
