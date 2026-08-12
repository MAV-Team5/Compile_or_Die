using UnityEditor;
using UnityEngine;

/// <summary>
/// [Fold] · [Fx] 가 붙은 묶음 필드를 한 줄로 접어 그린다.
/// 접힌 상태에서도 오른쪽 안내로 무엇이 들었는지 알 수 있게 한다.
/// </summary>
[CustomPropertyDrawer(typeof(FoldAttribute), true)]
public class FoldDrawer : PropertyDrawer
{
    const float Gap = 2f;

    static readonly Color FilledColor = new(0.55f, 0.80f, 0.95f);
    static readonly Color PlainColor = new(0.55f, 0.55f, 0.55f);

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var fold = (FoldAttribute)attribute;
        float line = EditorGUIUtility.singleLineHeight;

        EditorGUI.BeginProperty(position, label, property);

        var titleRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, line);
        var noteRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y,
                                position.width - EditorGUIUtility.labelWidth, line);

        property.isExpanded = EditorGUI.Foldout(titleRect, property.isExpanded, fold.Title, true);

        DrawNote(noteRect, property, fold);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            DrawChildren(new Rect(position.x, position.y + line + Gap, position.width, 0), property);
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float h = EditorGUIUtility.singleLineHeight;
        if (!property.isExpanded) return h;

        var end = property.GetEndProperty();
        var it = property.Copy();
        bool enter = true;

        while (it.NextVisible(enter) && !SerializedProperty.EqualContents(it, end))
        {
            enter = false;
            h += Gap + EditorGUI.GetPropertyHeight(it, true);
        }
        return h;
    }

    /// <summary>접힌 줄 오른쪽 안내. 연출 묶음은 채움 여부까지 보여준다.</summary>
    static void DrawNote(Rect rect, SerializedProperty property, FoldAttribute fold)
    {
        string text = fold.Note;
        bool highlight = false;

        if (fold is FxAttribute)
        {
            var vfx = property.FindPropertyRelative("vfx");
            var sfx = property.FindPropertyRelative("sfx");

            bool hasVfx = vfx != null && vfx.objectReferenceValue != null;
            bool hasSfx = sfx != null && sfx.objectReferenceValue != null;

            string filled = (hasVfx, hasSfx) switch
            {
                (true, true) => "이펙트 · 효과음",
                (true, false) => "이펙트",
                (false, true) => "효과음",
                _ => "비어 있음"
            };

            text = $"{fold.Note}   ·   {filled}";
            highlight = hasVfx || hasSfx;
        }

        if (string.IsNullOrEmpty(text)) return;

        var style = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = highlight ? FilledColor : PlainColor }
        };

        EditorGUI.LabelField(rect, text, style);
    }

    static void DrawChildren(Rect rect, SerializedProperty property)
    {
        var end = property.GetEndProperty();
        var it = property.Copy();
        bool enter = true;
        float y = rect.y;

        while (it.NextVisible(enter) && !SerializedProperty.EqualContents(it, end))
        {
            enter = false;
            float h = EditorGUI.GetPropertyHeight(it, true);
            EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, h), it, true);
            y += h + Gap;
        }
    }
}
