/// <summary>
/// 적 하나에 얹을 배율 묶음. <see cref="EnemyData"/> 의 기본 수치에 곱해진다.
///
/// <b>왜 구조체로 묶나</b> — 잡몹은 웨이브가, 보스는 보스 일정이 배율을 준다.
/// 매개변수를 다섯 개 늘어놓으면 부르는 쪽에서 순서를 헷갈리고,
/// <see cref="Enemy"/> 가 StageWave 를 직접 알게 되면 적이 스테이지에 묶여 버린다.
/// </summary>
public struct EnemyScale
{
    public float Health;
    public float Speed;
    public float Damage;
    public float Size;
    public float Exp;

    /// <summary>전부 1배. 아무 배율도 안 얹은 기본 상태.</summary>
    public static EnemyScale One => new()
    {
        Health = 1f,
        Speed = 1f,
        Damage = 1f,
        Size = 1f,
        Exp = 1f
    };

    public static EnemyScale Of(float health, float speed, float damage, float size, float exp)
        => new() { Health = health, Speed = speed, Damage = damage, Size = size, Exp = exp };
}
