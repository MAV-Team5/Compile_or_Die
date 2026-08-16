using UnityEngine;

/// <summary>
/// 적에게 새겨진 탐색 표식 1개.
/// 이후 어떤 공격이든 이 적에 닿으면 추가 피해가 함께 들어간다.
/// </summary>
public class SearchMark
{
    /// <summary>이 표식을 남긴 증강. 같은 증강이 다시 탐색하면 덮어쓴다.</summary>
    public AugmentInstance Owner;

    /// <summary>추가 피해량. IsPercent 면 원래 피해의 비율.</summary>
    public float Bonus;

    public bool IsPercent;

    /// <summary>이 시각이 지나면 사라진다. 0이면 갱신될 때까지 유지.</summary>
    public float ExpireAt;

    /// <summary>띄울 표식 프리팹. 실제 생성은 MarkerHolder 가 맡는다.</summary>
    public GameObject VisualPrefab;

    /// <summary>적에 붙은 표식 오브젝트. MarkerHolder 가 채운다.</summary>
    public GameObject Visual;

    /// <summary>추가 피해가 들어갈 때마다 낼 연출. 증강 에셋이 들고 있는 것을 빌려 쓴다.</summary>
    public FxGroup BurstFx;

    /// <summary>연출 도배를 막는 최소 간격(초).</summary>
    public float BurstInterval;

    float nextBurstAt;

    public bool IsExpired => ExpireAt > 0f && Time.time >= ExpireAt;

    public float Evaluate(float baseAmount) => IsPercent ? baseAmount * Bonus : Bonus;

    /// <summary>추가 피해가 실제로 들어간 순간의 연출. 간격 안에 다시 부르면 조용히 넘어간다.</summary>
    public void PlayBurst(Vector2 position)
    {
        if (BurstFx == null || BurstFx.IsEmpty) return;
        if (Time.time < nextBurstAt) return;

        nextBurstAt = Time.time + BurstInterval;
        BurstFx.PlayAt(position);
    }
}
