using UnityEditor;
using UnityEngine;

/// <summary>
/// Scalable 을 한 줄로 그린다. 인스펙터 줄 수를 늘리지 않고
/// "고정값 × 배수 + 가감" 을 다 보여준다.
/// </summary>
[CustomPropertyDrawer(typeof(Scalable))]
public class ScalableDrawer : PropertyDrawer
{
    const float SignWidth = 14f;
    const float Gap = 3f;

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
        SerializedProperty offset = property.FindPropertyRelative("offset");

        // 칸 셋 + 기호 둘. 좁은 인스펙터에서도 숫자가 잘리지 않게 최소 폭을 준다
        float third = Mathf.Max(28f, (body.width - SignWidth * 2f - Gap * 4f) / 3f);

        var valueRect  = new Rect(body.x, body.y, third, body.height);
        var mulRect    = new Rect(valueRect.xMax + Gap, body.y, SignWidth, body.height);
        var scaleRect  = new Rect(mulRect.xMax + Gap, body.y, third, body.height);
        var plusRect   = new Rect(scaleRect.xMax + Gap, body.y, SignWidth, body.height);
        var offsetRect = new Rect(plusRect.xMax + Gap, body.y, third, body.height);

        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        value.floatValue = EditorGUI.FloatField(valueRect, value.floatValue);
        EditorGUI.LabelField(mulRect, "×", signStyle);
        scale.floatValue = EditorGUI.FloatField(scaleRect, scale.floatValue);
        EditorGUI.LabelField(plusRect, "+", signStyle);
        offset.floatValue = EditorGUI.FloatField(offsetRect, offset.floatValue);

        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }
}
