using UnityEngine;

/// <summary>
/// 노드 사이를 잇는 간선 1개.
///
/// 탐색 표식과 수명이 독립적이다 — 표식이 풀려도 간선은 남고, 다음 탐색이 시작돼도 끊기지 않는다.
/// 화면에 계속 보이는 독립 개체로 다룬다.
///
/// 값은 만들 때 복사해 들고 있는다. 증강 레벨이 올라도 이미 놓인 간선은 그대로다 —
/// 이미 화면에 그려진 것이 나중에 조용히 세지면 플레이어가 인과를 못 읽는다.
/// </summary>
public class Link
{
    /// <summary>이 간선이 이어진 반대쪽.</summary>
    public LinkHolder Other;

    /// <summary>
    /// 반대쪽이 내 자식인가. 간선은 양쪽에 하나씩 저장되고 이 값만 반대다.
    /// 방향이 있어야 트리를 타고 내려가 잎을 찾을 수 있다 —
    /// 대칭이면 어디가 뿌리이고 어디가 끝인지 코드가 알 수 없다.
    /// </summary>
    public bool ToChild;

    /// <summary>누가 이었나. 피해 숫자 색과 해제에 쓴다.</summary>
    public AugmentInstance Owner;

    /// <summary>전이량. IsPercent 면 들어온 피해의 비율.</summary>
    public float Amount;

    public bool IsPercent;

    /// <summary>여기서 몇 번 더 번질 수 있나. 0이면 이 간선을 타고 끝.</summary>
    public int Hops;

    /// <summary>
    /// 노드 하나가 가질 수 있는 간선 상한.
    /// 완전 그래프는 노드 N개면 각자 N-1개를 가지므로, 낮으면 잇는 도중에 밀려난다.
    /// </summary>
    public int MaxPerNode;

    /// <summary>이 시각이 지나면 끊긴다. 0이면 노드가 사라질 때까지 유지.</summary>
    public float ExpireAt;

    /// <summary>
    /// 이 길이를 넘으면 끊긴다. 0이면 제한 없음.
    ///
    /// 이을 때의 거리 제한과 별개로 필요하다 — 이어진 뒤에 두 적이 멀어지면
    /// 선이 화면을 가로지른다. 특히 멀어진 적을 앞쪽으로 회수할 때 눈에 띈다.
    /// </summary>
    public float MaxLength;

    /// <summary>선 오브젝트. 한 쌍 중 만든 쪽만 들고 있다 — 안 그러면 선이 두 겹으로 그려진다.</summary>
    public GameObject Visual;

    /// <summary>선을 그리는 컴포넌트. 만들 때 한 번만 찾아둔다.</summary>
    public LineRenderer Line;

    /// <summary>
    /// 있으면 그리기를 이쪽에 맡긴다. <b>선을 만든 쪽만</b> 들고 있다 —
    /// 양쪽이 다 그리면 0번 점이 매 프레임 부모와 자식 사이를 오가며 뒤집힌다.
    /// </summary>
    public LinkPulse Pulse;

    /// <summary>
    /// 꿀렁임을 울릴 대상. Pulse 와 같은 것을 가리키지만 <b>양쪽 간선이 함께</b> 들고 있다.
    ///
    /// 그리기는 한쪽만 맡아야 하지만, 피해는 자식에서 부모로도 흐른다.
    /// 나눠두지 않으면 그래프처럼 사방으로 번지는 구조에서 절반이 조용히 지나간다.
    /// </summary>
    public LinkPulse Echo;

    /// <summary>이 간선을 타고 뭔가 지나갔다고 알린다. 연출이 없으면 조용히 넘어간다.</summary>
    public void Ripple(float strength)
    {
        if (Echo != null) Echo.Pulse(strength);
    }

    public bool IsExpired => ExpireAt > 0f && Time.time >= ExpireAt;

    /// <summary>양 끝이 너무 벌어졌는가.</summary>
    public bool IsStretched(Vector3 from)
        => MaxLength > 0f && Other != null
        && (Other.transform.position - from).sqrMagnitude > MaxLength * MaxLength;

    /// <summary>반대쪽이 죽었거나 풀로 돌아갔는지.</summary>
    public bool IsBroken => Other == null || !Other.isActiveAndEnabled;

    public float Transfer(float incoming) => IsPercent ? incoming * Amount : Amount;
}
