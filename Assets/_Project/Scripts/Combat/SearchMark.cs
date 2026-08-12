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

    /// <summary>적 위에 붙은 표식 오브젝트.</summary>
    public GameObject Visual;

    public bool IsExpired => ExpireAt > 0f && Time.time >= ExpireAt;

    public float Evaluate(float baseAmount) => IsPercent ? baseAmount * Bonus : Bonus;
}
