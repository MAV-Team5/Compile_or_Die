using UnityEngine;

/// <summary>
/// 비우면 그 모듈이 아예 동작하지 않는 필드에 붙인다.
/// 인스펙터에서 이름 앞에 ＊ 가 붙고, 비어 있으면 붉은 바탕으로 표시된다.
/// </summary>
public class RequiredAttribute : PropertyAttribute
{
    /// <summary>비었을 때 무슨 일이 생기는지. 툴팁 끝에 덧붙는다.</summary>
    public readonly string Consequence;

    public RequiredAttribute(string consequence = null) => Consequence = consequence;
}
