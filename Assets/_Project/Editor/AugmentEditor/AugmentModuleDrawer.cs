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

    /// <summary>4축은 아니지만 모듈로 끼워 쓰는 것들 (지속 효과 등).</summary>
    static readonly Color StatusColor    = new(0.95f, 0.80f, 0.35f);
    static readonly Color UnknownColor   = new(0.55f, 0.55f, 0.55f);

    /// <summary>None 인 칸의 버튼 배경. 비워두면 그 단계가 통째로 무시된다.</summary>
    static readonly Color MissingColor   = new(1.00f, 0.55f, 0.55f);

    /// <summary>
    /// 이 칸이 비어 있는 게 정상인가.
    ///
    /// <b>내부 증강은 3축을 대부분 비운다.</b> 스스로 발동하지 않고 뿌리에 얹히기만 하므로
    /// 트리거·타겟팅이 없는 것이 맞는 상태다. 그런데도 붉게 칠하면 늘 붉어서
    /// 진짜 실수를 놓치게 된다 — 경고는 드물어야 눈에 들어온다.
    ///
    /// 덮어쓰겠다고 해놓고(Patch ≠ None) 정작 비워둔 칸은 실수이므로 그대로 칠한다.
    /// </summary>
    static bool IsExpectedEmpty(SerializedProperty property)
    {
        if (property.serializedObject.targetObject is not AugmentData data) return false;

        // 뿌리 증강이면 예전처럼 전부 필요하다
        if (data.rootAugment == null) return false;

        return property.propertyPath switch
        {
            "trigger" => true,
            "targeting" => data.targetingPatch == BuildPatch.None,
            _ => false
        };
    }

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

        // 비어 있는 모듈 칸은 조용히 무시되므로 붉게 칠해 눈에 띄게 한다.
        // 다만 내부 증강은 비우는 것이 정상이라 칠하지 않는다 — 늘 붉으면 경고가 안 읽힌다
        Color previous = GUI.backgroundColor;
        if (!hasValue && !IsExpectedEmpty(property)) GUI.backgroundColor = MissingColor;

        if (GUI.Button(buttonRect, DisplayName(property), EditorStyles.popup))
            ShowTypeMenu(property);

        GUI.backgroundColor = previous;

        float y = position.y + line + Gap;

        // 고른 뒤에도 무슨 모듈인지 잊지 않도록 설명 한 줄을 남긴다
        ModuleInfoAttribute info = hasValue ? InfoOf(property) : null;

        if (info != null)
        {
            EditorGUI.LabelField(new Rect(bodyX, y, bodyWidth, line), Describe(info), NoteStyle);
            y += line + Gap;
        }

        if (hasValue && property.isExpanded)
        {
            EditorGUI.indentLevel++;
            DrawChildren(new Rect(bodyX, y, bodyWidth, 0), property);
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;
        float h = line;

        if (string.IsNullOrEmpty(property.managedReferenceFullTypename)) return h;

        if (InfoOf(property) != null) h += line + Gap;

        if (!property.isExpanded) return h;

        var end = property.GetEndProperty();
        var it = property.Copy();
        bool enter = true;
        bool hasDetail = false;

        while (it.NextVisible(enter) && !SerializedProperty.EqualContents(it, end))
        {
            enter = false;

            if (ModuleFieldInfo.Of(it).Detail)
            {
                hasDetail = true;

                // 접혀 있으면 자리를 안 먹는다
                if (!DetailOpen(property)) continue;
            }

            h += Gap + EditorGUI.GetPropertyHeight(it, true);
        }

        // "세부" 접이 줄
        if (hasDetail) h += Gap + line;

        return h;
    }

    /// <summary>세부 접이가 펼쳐져 있나. 모듈 칸마다 따로 기억한다.</summary>
    static bool DetailOpen(SerializedProperty property)
        => EditorPrefs.GetBool(DetailKey(property), false);

    static void SetDetailOpen(SerializedProperty property, bool open)
        => EditorPrefs.SetBool(DetailKey(property), open);

    static string DetailKey(SerializedProperty property)
        => $"CoD.Detail.{property.serializedObject.targetObject.GetInstanceID()}.{property.propertyPath}";

    static GUIStyle noteStyle;
    static GUIStyle NoteStyle => noteStyle ??= new GUIStyle(EditorStyles.miniLabel)
    {
        fontStyle = FontStyle.Italic,
        normal = { textColor = new Color(0.62f, 0.62f, 0.62f) },
        wordWrap = false
    };

    static string Describe(ModuleInfoAttribute info)
        => string.IsNullOrEmpty(info.Note) ? info.Summary : $"{info.Summary}   ·   {info.Note}";

    static void DrawChildren(Rect rect, SerializedProperty property)
    {
        var end = property.GetEndProperty();
        var it = property.Copy();
        bool enter = true;
        float y = rect.y;

        bool open = DetailOpen(property);
        bool hasDetail = false;

        // 자주 만지는 칸 먼저, 세부는 접이 아래로
        while (it.NextVisible(enter) && !SerializedProperty.EqualContents(it, end))
        {
            enter = false;

            ModuleFieldInfo.Marks marks = ModuleFieldInfo.Of(it);

            if (marks.Detail)
            {
                hasDetail = true;
                continue;
            }

            y = DrawField(rect, y, it, marks);
        }

        if (!hasDetail) return;

        float line = EditorGUIUtility.singleLineHeight;

        // 옅은 바탕을 깔아 구분선처럼 보이게 한다. 그냥 글씨만 두면 필드 사이에 묻힌다
        EditorGUI.DrawRect(new Rect(rect.x, y, rect.width, line), new Color(1f, 1f, 1f, 0.05f));

        bool next = EditorGUI.Foldout(
            new Rect(rect.x + 4f, y, rect.width - 4f, line),
            open, "상세", true, EditorStyles.boldLabel);

        if (next != open) SetDetailOpen(property, next);

        y += line + Gap;

        if (!next) return;

        it = property.Copy();
        enter = true;

        while (it.NextVisible(enter) && !SerializedProperty.EqualContents(it, end))
        {
            enter = false;

            ModuleFieldInfo.Marks marks = ModuleFieldInfo.Of(it);
            if (!marks.Detail) continue;

            y = DrawField(rect, y, it, marks);
        }
    }

    /// <summary>
    /// 칸 하나를 그린다. 시트 컬럼이 지정돼 있으면 라벨 뒤에 붙여
    /// "이건 시트가 정한다"가 마우스를 안 올려도 보이게 한다.
    /// </summary>
    static float DrawField(Rect rect, float y, SerializedProperty property,
                          ModuleFieldInfo.Marks marks)
    {
        float h = EditorGUI.GetPropertyHeight(property, true);
        var area = new Rect(rect.x, y, rect.width, h);

        if (string.IsNullOrEmpty(marks.Sheet))
        {
            EditorGUI.PropertyField(area, property, true);
        }
        else
        {
            var label = new GUIContent(
                $"{property.displayName}  ← {marks.Sheet}",
                property.tooltip);

            EditorGUI.PropertyField(area, property, label, true);
        }

        return y + h + Gap;
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
                var info = InfoOf(captured);

                // 이름만으로는 뭘 하는 모듈인지 모르므로 설명을 항목에 함께 띄운다
                string text = info == null
                    ? ShortName(captured.Name)
                    : $"{ShortName(captured.Name)}   —   {info.Summary}";

                menu.AddItem(new GUIContent(text), false,
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

    // 리플렉션은 매 프레임 돌리기엔 비싸므로 타입별로 한 번만 읽고 담아둔다
    static readonly System.Collections.Generic.Dictionary<Type, ModuleInfoAttribute> infoCache = new();

    static ModuleInfoAttribute InfoOf(SerializedProperty property)
    {
        Type type = ResolveType(property.managedReferenceFullTypename);
        return type == null ? null : InfoOf(type);
    }

    static ModuleInfoAttribute InfoOf(Type type)
    {
        if (infoCache.TryGetValue(type, out var cached)) return cached;

        var found = (ModuleInfoAttribute)Attribute.GetCustomAttribute(type, typeof(ModuleInfoAttribute));
        infoCache[type] = found;

        return found;
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
        foreach (string suffix in new[] { "Targeting", "Delivery", "Effect", "Trigger", "Status" })
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
        if (cls.EndsWith("Status"))    return StatusColor;

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
