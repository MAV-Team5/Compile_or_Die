using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>SerializeReference 모듈 필드에 타입 선택 드롭다운을 그린다.</summary>
[CustomPropertyDrawer(typeof(AugmentModule), true)]
public class AugmentModuleDrawer : PropertyDrawer
{
    const float Gap = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float line = EditorGUIUtility.singleLineHeight;
        var labelRect  = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, line);
        var buttonRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y,
                                  position.width - EditorGUIUtility.labelWidth, line);

        bool hasValue = !string.IsNullOrEmpty(property.managedReferenceFullTypename);

        if (hasValue)
            property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label, true);
        else
            EditorGUI.LabelField(labelRect, label);

        if (GUI.Button(buttonRect, DisplayName(property), EditorStyles.popup))
            ShowTypeMenu(property);

        if (hasValue && property.isExpanded)
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
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(captured.Name)), false,
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

    static string DisplayName(SerializedProperty property)
    {
        string full = property.managedReferenceFullTypename;
        if (string.IsNullOrEmpty(full)) return "None";

        int space = full.IndexOf(' ');
        string cls = space < 0 ? full : full.Substring(space + 1);
        int dot = cls.LastIndexOf('.');
        return ObjectNames.NicifyVariableName(dot >= 0 ? cls.Substring(dot + 1) : cls);
    }
}