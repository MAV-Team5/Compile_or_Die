using UnityEditor;
using UnityEngine;

/// <summary>
/// Scalable 을 한 줄로 그린다. 인스펙터 줄 수를 늘리지 않고 "고정값 × 배수"를 다 보여준다.
/// </summary>
[CustomPropertyDrawer(typeof(Scalable))]
public class ScalableDrawer : PropertyDrawer
{
    const float SignWidth = 16f;
    const float Gap = 4f;

    static GUIStyle signStyle;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        signStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
        };

        EditorGUI.BeginProperty(position, label, property);

        Rect body = EditorGUI.PrefixLabel(position, label);

        SerializedProperty value = property.FindPropertyRelative("value");
        SerializedProperty scale = property.FindPropertyRelative("scale");

        float half = (body.width - SignWidth - Gap * 2f) * 0.5f;

        var valueRect = new Rect(body.x, body.y, half, body.height);
        var signRect  = new Rect(valueRect.xMax + Gap, body.y, SignWidth, body.height);
        var scaleRect = new Rect(signRect.xMax + Gap, body.y, half, body.height);

        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        value.floatValue = EditorGUI.FloatField(valueRect, value.floatValue);
        EditorGUI.LabelField(signRect, "×", signStyle);
        scale.floatValue = EditorGUI.FloatField(scaleRect, scale.floatValue);

        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }
}
