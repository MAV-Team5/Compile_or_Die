/// <summary>
/// 모듈 클래스에 붙이는 한 줄 설명. 인스펙터 드롭다운과 선택 후 설명 줄에 쓰인다.
/// 팀원이 코드를 안 열고도 무슨 모듈인지 알 수 있게 하는 것이 목적이다.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Class)]
public class ModuleInfoAttribute : System.Attribute
{
    /// <summary>이 모듈이 하는 일. 드롭다운에 이름과 함께 뜬다.</summary>
    public readonly string Summary;

    /// <summary>헷갈리기 쉬운 이웃 모듈과의 차이. 없으면 생략된다.</summary>
    public readonly string Note;

    public ModuleInfoAttribute(string summary, string note = null)
    {
        Summary = summary;
        Note = note;
    }
}
