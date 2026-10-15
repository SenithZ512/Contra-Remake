using UnityEditor;
using UnityEngine;

/// <summary>
/// หน้าตั้งค่า SpriteDrift แบบกดปุ่ม ไม่ต้องจำว่าเลขไหนแปลว่าอะไร
/// แต่ละกลุ่มแยกสีกัน และมีคำอธิบายกำกับว่าปุ่มนั้นไปแตะค่าอะไร
/// </summary>
[CustomEditor(typeof(SpriteDrift))]
[CanEditMultipleObjects]
public class SpriteDriftEditor : Editor
{
    private static readonly Color SpeedColor = new Color(0.55f, 0.78f, 1f);
    private static readonly Color RangeColor = new Color(0.6f, 1f, 0.65f);
    private static readonly Color AxisColor = new Color(1f, 0.82f, 0.5f);
    private static readonly Color RandomColor = new Color(0.85f, 0.7f, 1f);

    private SerializedProperty amplitude;
    private SerializedProperty speed;
    private SerializedProperty phase;

    private void OnEnable()
    {
        amplitude = serializedObject.FindProperty("amplitude");
        speed = serializedObject.FindProperty("speed");
        phase = serializedObject.FindProperty("phase");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        Section(
            "ความเร็ว",
            "กำหนดว่าลอยครบหนึ่งรอบใช้เวลากี่วินาที — แตะค่า Speed",
            SpeedColor
        );

        EditorGUILayout.BeginHorizontal();
        SpeedButton("ช้ามาก", 0.015f, "66 วินาทีต่อรอบ");
        SpeedButton("ช้า", 0.04f, "25 วินาทีต่อรอบ");
        SpeedButton("ปานกลาง", 0.1f, "10 วินาทีต่อรอบ");
        SpeedButton("เร็ว", 0.25f, "4 วินาทีต่อรอบ");
        EditorGUILayout.EndHorizontal();

        Section(
            "ระยะลอย",
            "กำหนดว่าลอยออกจากจุดเดิมได้ไกลแค่ไหน — แตะค่า Amplitude",
            RangeColor
        );

        EditorGUILayout.BeginHorizontal();
        RangeButton("แคบ", 0.4f, "ไหวนิดเดียว");
        RangeButton("ปานกลาง", 1.2f, "กว้างกว่าตัวละครนิดหน่อย");
        RangeButton("กว้าง", 2.5f, "ลอยข้ามจอได้ครึ่งหนึ่ง");
        EditorGUILayout.EndHorizontal();

        Section(
            "ทิศทาง",
            "เลือกว่าลอยแกนไหน — ปิดแกนที่ไม่ใช้โดยตั้งค่าเป็น 0",
            AxisColor
        );

        EditorGUILayout.BeginHorizontal();
        AxisButton("ซ้าย-ขวา", true, false);
        AxisButton("ขึ้น-ลง", false, true);
        AxisButton("ลอยวน", true, true);
        EditorGUILayout.EndHorizontal();

        Section(
            "จังหวะ",
            "สุ่มเฟสใหม่ ของที่วางใกล้กันจะได้ไม่ลอยพร้อมกันเป๊ะ",
            RandomColor
        );

        GUI.backgroundColor = RandomColor;

        if (GUILayout.Button("สุ่มจังหวะใหม่"))
        {
            foreach (Object item in targets)
            {
                Undo.RecordObject(item, "Randomize Drift Phase");
            }

            phase.vector2Value = new Vector2(
                Random.Range(0f, 6.28f),
                Random.Range(0f, 6.28f)
            );
        }

        GUI.backgroundColor = Color.white;

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

    private void SpeedButton(string label, float value, string tip)
    {
        GUI.backgroundColor = SpeedColor;

        if (GUILayout.Button(new GUIContent(label, tip)))
        {
            Vector2 current = speed.vector2Value;

            // แกนไหนปิดอยู่ (เป็น 0) ก็ปล่อยให้ปิดต่อ ไม่ไปเปิดให้เอง
            speed.vector2Value = new Vector2(
                current.x > 0f ? value : 0f,
                current.y > 0f ? value * 3f : 0f
            );
        }

        GUI.backgroundColor = Color.white;
    }

    private void RangeButton(string label, float value, string tip)
    {
        GUI.backgroundColor = RangeColor;

        if (GUILayout.Button(new GUIContent(label, tip)))
        {
            Vector2 current = amplitude.vector2Value;

            amplitude.vector2Value = new Vector2(
                current.x > 0f ? value : 0f,
                current.y > 0f ? value * 0.15f : 0f
            );
        }

        GUI.backgroundColor = Color.white;
    }

    private void AxisButton(string label, bool useX, bool useY)
    {
        GUI.backgroundColor = AxisColor;

        if (GUILayout.Button(label))
        {
            Vector2 a = amplitude.vector2Value;
            Vector2 sp = speed.vector2Value;

            // เก็บค่าที่เคยตั้งไว้ ถ้าแกนนั้นเคยปิดก็ใช้ค่ากลางๆ ให้
            float ax = a.x > 0f ? a.x : 1.2f;
            float ay = a.y > 0f ? a.y : 0.18f;
            float sx = sp.x > 0f ? sp.x : 0.04f;
            float sy = sp.y > 0f ? sp.y : 0.3f;

            amplitude.vector2Value = new Vector2(
                useX ? ax : 0f,
                useY ? ay : 0f
            );

            speed.vector2Value = new Vector2(
                useX ? sx : 0f,
                useY ? sy : 0f
            );
        }

        GUI.backgroundColor = Color.white;
    }
}
