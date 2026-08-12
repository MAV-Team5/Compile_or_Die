using UnityEditor;
using UnityEngine;

/// <summary>
/// 증강 에셋 인스펙터. 레벨별 수치를 세로로 늘어놓지 않고 표로 그린다.
/// 나머지 필드는 기본 방식 그대로 둔다.
/// </summary>
[CustomEditor(typeof(AugmentData))]
public class AugmentDataEditor : Editor
{
    const string TableField = "levelStats";

    // 시트 열 순서와 맞춘다. 헤더는 한글, 실제 필드는 영문
    static readonly string[] Fields =
        { "damage", "effectDamage", "cooldown", "range", "effectRange",
          "count", "pierce", "duration", "speed", "depth" };

    static readonly string[] Heads =
        { "피해", "효과피해", "쿨타임", "사거리", "효과범위",
          "수량", "관통", "지속", "속도", "깊이" };

    const float LevelColumn = 40f;
    const float Padding = 2f;

    static GUIStyle headStyle;
    static GUIStyle levelStyle;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        BuildStyles();

        SerializedProperty it = serializedObject.GetIterator();
        bool enter = true;

        while (it.NextVisible(enter))
        {
            enter = false;

            if (it.propertyPath == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(it);
                continue;
            }

            if (it.propertyPath == TableField)
            {
                DrawTable(serializedObject.FindProperty(TableField));
                continue;
            }

            EditorGUILayout.PropertyField(it, true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    static void BuildStyles()
    {
        headStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(0, 0, 0, 0)
        };

        levelStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };
    }

    void DrawTable(SerializedProperty array)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("레벨별 수치", EditorStyles.boldLabel);

        if (array.arraySize == 0)
        {
            EditorGUILayout.HelpBox("레벨이 하나도 없습니다. 증강이 동작하지 않습니다.", MessageType.Error);
        }
        else
        {
            // 열 이름에 마우스를 올리면 그 수치가 뭔지 뜬다
            DrawHeaderRow(array.GetArrayElementAtIndex(0));

            for (int level = 0; level < array.arraySize; level++)
                DrawLevelRow(array.GetArrayElementAtIndex(level), level + 1);
        }

        DrawButtons(array);
        EditorGUILayout.Space(4);
    }

    static void DrawHeaderRow(SerializedProperty sample)
    {
        Rect row = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
        float width = ColumnWidth(row);

        EditorGUI.LabelField(new Rect(row.x, row.y, LevelColumn, row.height),
                             new GUIContent("Lv", "레벨. 1부터 시작한다."), headStyle);

        for (int i = 0; i < Heads.Length; i++)
        {
            var cell = new Rect(row.x + LevelColumn + i * width, row.y, width - Padding, row.height);

            // 설명은 AugmentLevelData 의 [Tooltip] 에서 그대로 가져온다. 두 곳에 안 적어도 된다
            SerializedProperty field = sample.FindPropertyRelative(Fields[i]);
            string tip = field != null ? field.tooltip : null;

            EditorGUI.LabelField(cell, new GUIContent(Heads[i], $"{Heads[i]} ({Fields[i]})\n\n{tip}"),
                                 headStyle);
        }
    }

    static void DrawLevelRow(SerializedProperty element, int level)
    {
        Rect row = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
        float width = ColumnWidth(row);

        EditorGUI.LabelField(new Rect(row.x, row.y, LevelColumn, row.height), $"{level}", levelStyle);

        // 좁은 칸이라 라벨을 빼야 숫자가 보인다
        float previousLabel = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 1f;

        for (int i = 0; i < Fields.Length; i++)
        {
            SerializedProperty field = element.FindPropertyRelative(Fields[i]);
            if (field == null) continue;

            var cell = new Rect(row.x + LevelColumn + i * width, row.y, width - Padding, row.height);
            EditorGUI.PropertyField(cell, field, GUIContent.none);
        }

        EditorGUIUtility.labelWidth = previousLabel;
    }

    static float ColumnWidth(Rect row) => (row.width - LevelColumn) / Heads.Length;

    static void DrawButtons(SerializedProperty array)
    {
        EditorGUILayout.Space(2);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("＋ 레벨 추가"))
            {
                int last = array.arraySize;
                array.InsertArrayElementAtIndex(last);

                // 마지막 레벨을 복사해두면 조금씩 올리기만 하면 된다
                if (last > 0) CopyRow(array.GetArrayElementAtIndex(last - 1),
                                      array.GetArrayElementAtIndex(last));
            }

            using (new EditorGUI.DisabledScope(array.arraySize == 0))
                if (GUILayout.Button("－ 마지막 삭제"))
                    array.DeleteArrayElementAtIndex(array.arraySize - 1);
        }
    }

    static void CopyRow(SerializedProperty from, SerializedProperty to)
    {
        foreach (string name in Fields)
        {
            SerializedProperty source = from.FindPropertyRelative(name);
            SerializedProperty target = to.FindPropertyRelative(name);

            if (source == null || target == null) continue;

            if (source.propertyType == SerializedPropertyType.Float)
                target.floatValue = source.floatValue;
            else
                target.intValue = source.intValue;
        }
    }
}
