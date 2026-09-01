using UnityEditor;
using UnityEngine;

/// <summary>
/// [Required] 필드를 눈에 띄게 그린다.
/// 이름 앞에 ＊ 를 붙이고, 비어 있으면 붉은 바탕으로 경고한다.
/// </summary>
[CustomPropertyDrawer(typeof(RequiredAttribute))]
public class RequiredDrawer : PropertyDrawer
{
    static readonly Color MissingTint = new(0.85f, 0.25f, 0.25f, 0.18f);

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var required = (RequiredAttribute)attribute;

        if (IsMissing(property))
            EditorGUI.DrawRect(position, MissingTint);

        var marked = new GUIContent($"＊ {label.text}", label.tooltip);

        if (!string.IsNullOrEmpty(required.Consequence))
        {
            marked.tooltip = string.IsNullOrEmpty(marked.tooltip)
                ? $"필수 — {required.Consequence}"
                : $"{marked.tooltip}\n\n필수 — {required.Consequence}";
        }

        EditorGUI.PropertyField(position, property, marked, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUI.GetPropertyHeight(property, label, true);

    static bool IsMissing(SerializedProperty p) => p.propertyType switch
    {
        SerializedPropertyType.ObjectReference => p.objectReferenceValue == null,
        SerializedPropertyType.ManagedReference => string.IsNullOrEmpty(p.managedReferenceFullTypename),
        SerializedPropertyType.String => string.IsNullOrEmpty(p.stringValue),
        SerializedPropertyType.Float => Mathf.Approximately(p.floatValue, 0f),
        SerializedPropertyType.Integer => p.intValue == 0,
        SerializedPropertyType.Generic => p.isArray && p.arraySize == 0,
        _ => false
    };
}
