/// <summary>여러 발을 쏠 때 어떻게 배치할지.</summary>
public enum ShotFormation
{
    /// <summary>진행 방향에 수직으로 나란히. 좌우 대칭으로 벌어진다.</summary>
    Parallel,

    /// <summary>진행 방향으로 앞뒤. 원점보다 앞에 늘어서고 마지막 발이 원점에서 출발한다.</summary>
    Column
}
