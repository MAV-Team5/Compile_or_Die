using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SerializeReference 모듈 필드에 타입 선택 드롭다운을 그린다.
/// 축마다 색 띠를 붙이고 이름에서 축 접미사를 떼어 중첩 파이프라인을 읽기 쉽게 한다.
/// </summary>
[CustomPropertyDrawer(typeof(AugmentModule), true)]
public class AugmentModuleDrawer : PropertyDrawer
{
    const float Gap = 2f;
    const float StripeWidth = 3f;
    const float StripeGap = 4f;

    // 축별 색. 중첩이 깊어져도 지금 보는 게 어느 축인지 바로 알 수 있게 한다
    static readonly Color TriggerColor   = new(0.95f, 0.55f, 0.25f);
    static readonly Color TargetingColor = new(0.35f, 0.65f, 0.95f);
    static readonly Color DeliveryColor  = new(0.70f, 0.50f, 0.95f);
    static readonly Color EffectColor    = new(0.35f, 0.80f, 0.45f);
    static readonly Color UnknownColor   = new(0.55f, 0.55f, 0.55f);

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        bool hasValue = !string.IsNullOrEmpty(property.managedReferenceFullTypename);
        float line = EditorGUIUtility.singleLineHeight;

        if (hasValue)
        {
            var stripe = new Rect(position.x, position.y, StripeWidth, position.height);
            EditorGUI.DrawRect(stripe, AxisColor(property));
        }

        // 색 띠만큼 본문을 오른쪽으로 밀어 겹치지 않게 한다
        float indent = hasValue ? StripeWidth + StripeGap : 0f;
        float bodyX = position.x + indent;
        float bodyWidth = position.width - indent;

        var labelRect  = new Rect(bodyX, position.y, EditorGUIUtility.labelWidth - indent, line);
        var buttonRect = new Rect(bodyX + labelRect.width, position.y,
                                  bodyWidth - labelRect.width, line);

        if (hasValue)
            property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label, true);
        else
            EditorGUI.LabelField(labelRect, label);

        if (GUI.Button(buttonRect, DisplayName(property), EditorStyles.popup))
            ShowTypeMenu(property);

        if (hasValue && property.isExpanded)
        {
            EditorGUI.indentLevel++;
            DrawChildren(new Rect(bodyX, position.y + line + Gap, bodyWidth, 0), property);
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float h = EditorGUIUtility.singleLineHeight;

        if (string.IsNullOrEmpty(property.managedReferenceFullTypename) || !property.isExpanded)
            return h;

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

    static void ShowTypeMenu(SerializedProperty property)
    {
        var so = property.serializedObject;
        string path = property.propertyPath;
        Type baseType = ResolveType(property.managedReferenceFieldTypename);

        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("None"), false, () => Assign(so, path, null));

        if (baseType != null)
        {
            menu.AddSeparator("");

            var types = TypeCache.GetTypesDerivedFrom(baseType)
                                 .Where(t => !t.IsAbstract && !t.IsGenericType)
                                 .OrderBy(t => t.Name);

            foreach (var type in types)
            {
                var captured = type;
                menu.AddItem(new GUIContent(ShortName(captured.Name)), false,
                             () => Assign(so, path, Activator.CreateInstance(captured)));
            }
        }
        menu.ShowAsContext();
    }

    static void Assign(SerializedObject so, string path, object value)
    {
        so.Update();
        var p = so.FindProperty(path);
        p.managedReferenceValue = value;
        p.isExpanded = value != null;
        so.ApplyModifiedProperties();
    }

    static Type ResolveType(string typename)
    {
        if (string.IsNullOrEmpty(typename)) return null;
        int space = typename.IndexOf(' ');
        if (space < 0) return null;
        return Type.GetType($"{typename.Substring(space + 1)}, {typename.Substring(0, space)}");
    }

    /// <summary>필드 위치가 이미 축을 말해주므로 접미사는 빼서 줄을 짧게 만든다.</summary>
    static string ShortName(string className)
    {
        foreach (string suffix in new[] { "Targeting", "Delivery", "Effect", "Trigger" })
        {
            if (className.Length > suffix.Length && className.EndsWith(suffix))
                return ObjectNames.NicifyVariableName(className[..^suffix.Length]);
        }
        return ObjectNames.NicifyVariableName(className);
    }

    static string DisplayName(SerializedProperty property)
    {
        string cls = ClassName(property.managedReferenceFullTypename);
        return cls == null ? "None" : ShortName(cls);
    }

    static Color AxisColor(SerializedProperty property)
    {
        string cls = ClassName(property.managedReferenceFullTypename);
        if (cls == null) return UnknownColor;

        if (cls.EndsWith("Trigger"))   return TriggerColor;
        if (cls.EndsWith("Targeting")) return TargetingColor;
        if (cls.EndsWith("Delivery"))  return DeliveryColor;
        if (cls.EndsWith("Effect"))    return EffectColor;

        return UnknownColor;
    }

    static string ClassName(string fullTypename)
    {
        if (string.IsNullOrEmpty(fullTypename)) return null;

        int space = fullTypename.IndexOf(' ');
        string cls = space < 0 ? fullTypename : fullTypename[(space + 1)..];

        int dot = cls.LastIndexOf('.');
        return dot >= 0 ? cls[(dot + 1)..] : cls;
    }
}
