using System.Collections.Generic;

/// <summary>
/// 증강을 고를 때의 길잡이. <b>공격을 어느 정도 갖춘 플레이어를 탐색·연쇄로 이끄는 역할만</b> 한다.
///
/// 이 게임의 빌드는 공격 → 탐색 → 연쇄 순서다. 공격 증강은 누가 봐도 세 보여서 알아서 고르지만,
/// 탐색(표식만 남기고 피해가 0)과 연쇄(남의 적중을 타고 번짐)는 혼자서는 약해 보여 아무도 안 고른다.
/// 그래서 추천은 그 둘만 짚는다. 공격만 깔린 화면에는 황금 테두리가 뜨지 않는다.
///
/// <list type="number">
/// <item>공격이 아직 덜 갖춰짐(공격 2개 미만, 그리고 Lv3 이상인 공격도 없음) → 추천 없음</item>
/// <item>공격을 갖췄는데 탐색이 없음 → 탐색 카드 전부 추천</item>
/// <item>탐색이 있음 → 연쇄 카드(그래프·트리) 전부 추천</item>
/// </list>
/// 잠금 — 탐색이 일어나야만 발동하는 증강(Graph)은 탐색이 없으면 고를 수 없다.
/// </summary>
public static class AugmentAdvice
{
    public enum Role { Item, Attack, Search, Chain }

    /// <summary>공격 증강을 이만큼 가지면 "갖췄다".</summary>
    const int AttackCountReady = 2;

    /// <summary>또는 공격 하나라도 이 레벨에 닿으면 "갖췄다".</summary>
    const int AttackLevelReady = 3;

    // ── 분류 ──────────────────────────────────────────────

    static bool Has(AugmentData d, AugmentTag tag) => d != null && d.tags != null && d.tags.Contains(tag);

    static AugmentData Root(AugmentData d) => d != null && d.rootAugment != null && d.rootAugment != d ? d.rootAugment : d;

    public static Role RoleOf(AugmentData data)
    {
        AugmentData d = Root(data);
        if (d == null) return Role.Item;

        if (d.instantEffect != InstantItemEffect.None || d.category == AugmentCategory.Item) return Role.Item;

        // 표식을 남기는 것 = 탐색
        if (d.category == AugmentCategory.Search || (Has(d, AugmentTag.Mark) && !Has(d, AugmentTag.Attack))) return Role.Search;

        // 남의 적중·표식을 타고 번지는 것 = 연쇄 (Graph · Tree)
        if (!Has(d, AugmentTag.Attack) && (Has(d, AugmentTag.Synergy) || Has(d, AugmentTag.Transfer) || Has(d, AugmentTag.SearchLinked)))
            return Role.Chain;
        if (d.category == AugmentCategory.DataStruct) return Role.Chain;

        return Role.Attack;
    }

    /// <summary>
    /// 탐색 증강이 없으면 아무 일도 못 하는가. 조립을 직접 본다 —
    /// 탐색이 일어날 때만 발동(SearchTrigger)하거나, 탐색풀의 적만 노리는(SearchPoolTargeting) 증강.
    /// </summary>
    public static bool NeedsSearch(AugmentData data)
    {
        AugmentData d = Root(data);
        return d != null && (d.trigger is SearchTrigger || d.targeting is SearchPoolTargeting);
    }

    // ── 지금 빌드 ─────────────────────────────────────────

    static IEnumerable<AugmentRunner> Owned(AugmentManager owned)
    {
        if (owned == null) yield break;
        foreach (AugmentRunner r in owned.Runners)
        {
            AugmentData d = r != null && r.Instance != null ? r.Instance.Data : null;
            if (d != null && d.rootAugment == null) yield return r;   // 진화는 원본과 한 줄로 센다
        }
    }

    public static int Count(AugmentManager owned, Role role)
    {
        int n = 0;
        foreach (AugmentRunner r in Owned(owned)) if (RoleOf(r.Instance.Data) == role) n++;
        return n;
    }

    /// <summary>공격을 어느 정도 갖췄는가 — 공격 2개 이상이거나, Lv3 이상인 공격이 있다.</summary>
    public static bool AttackReady(AugmentManager owned)
    {
        int count = 0;
        foreach (AugmentRunner r in Owned(owned))
        {
            if (RoleOf(r.Instance.Data) != Role.Attack) continue;
            count++;
            if (r.Instance.Level >= AttackLevelReady) return true;
        }
        return count >= AttackCountReady;
    }

    public static bool HasSearch(AugmentManager owned) => Count(owned, Role.Search) > 0;

    public static bool Locked(AugmentData data, AugmentManager owned)
        => NeedsSearch(data) && !HasSearch(owned);

    /// <summary>지금 유도할 역할. 아직 공격을 덜 갖췄으면 Item(=유도 없음).</summary>
    public static Role Guide(AugmentManager owned)
    {
        if (!AttackReady(owned)) return Role.Item;
        return HasSearch(owned) ? Role.Chain : Role.Search;
    }

    /// <summary>이 카드를 추천하는가와 그 이유.</summary>
    public static bool Recommend(AugmentData data, AugmentManager owned, out string reason)
    {
        reason = string.Empty;
        if (data == null || Locked(data, owned)) return false;

        Role guide = Guide(owned);
        if (guide == Role.Item || RoleOf(data) != guide) return false;

        if (guide == Role.Search)
        {
            float bonus = BonusAt(data, LevelOf(data, owned) + 1);
            reason = bonus > 0f ? $"탐색 · 받는 피해 +{bonus * 100f:0}%" : "탐색 · 표식";
        }
        else
        {
            reason = NeedsSearch(data) ? "연쇄 · 탐색 대상끼리 전이" : "연쇄 · 적중을 자식에게 전이";
        }

        return true;
    }

    static int LevelOf(AugmentData data, AugmentManager owned)
    {
        AugmentRunner r = owned != null ? owned.Find(data) : null;
        return r != null && r.Instance != null ? r.Instance.Level : 0;
    }

    /// <summary>탐색 증강이 그 레벨에서 표식으로 얹는 추가 피해 비율.</summary>
    static float BonusAt(AugmentData data, int level)
    {
        if (data.levelStats == null || data.levelStats.Length == 0) return 0f;
        int i = System.Math.Clamp(level - 1, 0, data.levelStats.Length - 1);
        return data.levelStats[i].effectDamage;
    }

    public static string Label(Role role) => role switch
    {
        Role.Attack => "공격",
        Role.Search => "탐색",
        Role.Chain => "연쇄",
        _ => "아이템"
    };

    /// <summary>선택 화면 상단 요약 — 갖춘 개수와 지금 유도하는 방향.</summary>
    public static string Summary(AugmentManager owned)
    {
        string head = $"공격 {Count(owned, Role.Attack)}  ·  탐색 {Count(owned, Role.Search)}  ·  연쇄 {Count(owned, Role.Chain)}";

        return Guide(owned) switch
        {
            Role.Search => $"{head}    <color=#FFD54A>▶ 공격 준비 완료 — 이제 탐색</color>",
            Role.Chain => $"{head}    <color=#FFD54A>▶ 탐색 완료 — 이제 연쇄</color>",
            _ => head
        };
    }
}
