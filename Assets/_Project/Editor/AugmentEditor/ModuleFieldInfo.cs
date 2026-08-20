using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

/// <summary>
/// 모듈 필드에 붙은 <see cref="SheetAttribute"/> · <see cref="DetailAttribute"/> 를 읽어둔다.
///
/// 리플렉션은 매 프레임 돌기엔 비싸므로 (타입, 필드명) 으로 한 번만 캐시한다.
/// 어트리뷰트를 PropertyDrawer 로 만들지 않은 것은 타입 드로어(Scalable 등)를
/// 밀어내는 사고를 피하기 위해서다 — 그리는 쪽에서 읽어 쓰는 편이 안전하다.
/// </summary>
public static class ModuleFieldInfo
{
    public readonly struct Marks
    {
        /// <summary>시트 컬럼 이름. 없으면 null.</summary>
        public readonly string Sheet;

        /// <summary>세부 접이로 내릴 칸인가.</summary>
        public readonly bool Detail;

        public Marks(string sheet, bool detail)
        {
            Sheet = sheet;
            Detail = detail;
        }
    }

    static readonly Dictionary<(Type, string), Marks> cache = new();

    /// <summary>이 프로퍼티가 속한 필드의 표시 정보. 못 찾으면 빈 값.</summary>
    public static Marks Of(SerializedProperty property)
    {
        object owner = OwnerOf(property);
        if (owner == null) return default;

        Type type = owner.GetType();
        string name = LastSegment(property.propertyPath);

        var key = (type, name);
        if (cache.TryGetValue(key, out Marks cached)) return cached;

        Marks marks = Read(type, name);
        cache[key] = marks;

        return marks;
    }

    static Marks Read(Type type, string name)
    {
        // 필드는 상속 계층 어디에나 있을 수 있다 (ProjectileDeliveryBase 등)
        for (Type t = type; t != null; t = t.BaseType)
        {
            FieldInfo field = t.GetField(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly);

            if (field == null) continue;

            var sheet = field.GetCustomAttribute<SheetAttribute>();
            bool detail = field.GetCustomAttribute<DetailAttribute>() != null;

            return new Marks(sheet?.Column, detail);
        }

        return default;
    }

    /// <summary>이 프로퍼티를 들고 있는 객체. SerializeReference 모듈이 대상이다.</summary>
    static object OwnerOf(SerializedProperty property)
    {
        int cut = property.propertyPath.LastIndexOf('.');
        if (cut < 0) return null;

        string parentPath = property.propertyPath[..cut];

        SerializedProperty parent = property.serializedObject.FindProperty(parentPath);

        return parent is { propertyType: SerializedPropertyType.ManagedReference }
            ? parent.managedReferenceValue
            : null;
    }

    static string LastSegment(string path)
    {
        int cut = path.LastIndexOf('.');
        return cut < 0 ? path : path[(cut + 1)..];
    }
}
