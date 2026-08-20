using UnityEngine;

/// <summary>
/// 이 칸이 비었을 때 어느 시트 컬럼에서 값을 가져오는지 라벨 옆에 적어준다.
///
/// 툴팁에만 적어두면 마우스를 올려야 보여서 팀원이 못 찾는다.
/// 인스펙터에 항상 보이면 "이건 시트가 정한다"가 한눈에 읽힌다.
///
/// <b>PropertyDrawer 로 만들지 않았다.</b> 어트리뷰트 드로어는 타입 드로어(Scalable 등)를
/// 밀어내서 조립 버튼이 사라지는 사고가 난다 — AugmentModuleDrawer 가 라벨만 갈아끼운다.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Field)]
public class SheetAttribute : System.Attribute
{
    /// <summary>시트 컬럼의 한글 이름. 예) 수량 · 관통력 · 효과범위</summary>
    public readonly string Column;

    public SheetAttribute(string column) => Column = column;
}

/// <summary>
/// 자주 안 만지는 칸. 인스펙터에서 "세부" 접이 안으로 들어간다.
///
/// 지우는 대신 접는 이유 — 지우면 표현력이 사라진다.
/// 쓸 일이 드물 뿐 필요할 때는 반드시 있어야 하는 값들이다.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Field)]
public class DetailAttribute : System.Attribute { }
