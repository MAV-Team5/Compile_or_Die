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

    /// <summary>누가 이었나. 피해 숫자 색과 해제에 쓴다.</summary>
    public AugmentInstance Owner;

    /// <summary>전이량. IsPercent 면 들어온 피해의 비율.</summary>
    public float Amount;

    public bool IsPercent;

    /// <summary>여기서 몇 번 더 번질 수 있나. 0이면 이 간선을 타고 끝.</summary>
    public int Hops;

    /// <summary>이 시각이 지나면 끊긴다. 0이면 노드가 사라질 때까지 유지.</summary>
    public float ExpireAt;

    /// <summary>선 오브젝트. 한 쌍 중 만든 쪽만 들고 있다 — 안 그러면 선이 두 겹으로 그려진다.</summary>
    public GameObject Visual;

    public bool IsExpired => ExpireAt > 0f && Time.time >= ExpireAt;

    /// <summary>반대쪽이 죽었거나 풀로 돌아갔는지.</summary>
    public bool IsBroken => Other == null || !Other.isActiveAndEnabled;

    public float Transfer(float incoming) => IsPercent ? incoming * Amount : Amount;
}
