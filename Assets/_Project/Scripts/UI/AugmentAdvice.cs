using UnityEngine;
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
        foreach (AugmentRunner r in Owned(owned)) if (GuideRoleOf(r.Instance.Data) == role) n++;
        return n;
    }

    /// <summary>공격을 어느 정도 갖췄는가 — 공격 2개 이상이거나, Lv3 이상인 공격이 있다.</summary>
    public static bool AttackReady(AugmentManager owned)
    {
        int count = 0;
        foreach (AugmentRunner r in Owned(owned))
        {
            if (GuideRoleOf(r.Instance.Data) != Role.Attack) continue;
            count++;
            if (r.Instance.Level >= AttackLevelReady) return true;
        }
        return count >= AttackCountReady;
    }

    public static bool HasSearch(AugmentManager owned) => Count(owned, Role.Search) > 0;

    public static bool Locked(AugmentData data, AugmentManager owned)
        => NeedsSearch(data) && !HasSearch(owned);

    // ── 중요도 ────────────────────────────────────────────
    //
    // 공격 > 탐색 > 연쇄. 처음엔 단계대로(공격 갖추기 → 탐색 하나 → 연쇄 하나) 길을 잡고,
    // 셋 다 갖춘 뒤에는 "레벨 합 ÷ 가중치"가 가장 낮은 역할을 올리게 한다.
    // 가중치 3 : 2 : 1 — 연쇄만 끝없이 올리는 일 없이, 공격이 탐색의 1.5배·연쇄의 3배 레벨을 유지하는 쪽으로 이끈다.

    static readonly System.Collections.Generic.HashSet<string> CoreAttackIds = new() { "BASH", "BRUTE_FORCE", "C_LAN", "ITERATION", "PYTHON" };

    /// <summary>"공격"으로 치는 증강(나머지 공격형은 보조). 진화형은 원본을 따른다.</summary>
    public static bool IsCoreAttack(AugmentData data)
    {
        AugmentData d = Root(data);
        return d != null && !string.IsNullOrEmpty(d.id) && CoreAttackIds.Contains(d.id.ToUpperInvariant());
    }

    /// <summary>추천에서 보는 역할. 핵심 공격이 아닌 공격형은 보조(Item 취급 = 추천 안 함).</summary>
    static Role GuideRoleOf(AugmentData data)
    {
        Role r = RoleOf(data);
        if (r == Role.Attack && !IsCoreAttack(data)) return Role.Item;
        return r;
    }

    static float Weight(Role r) => r switch { Role.Attack => 3f, Role.Search => 2f, Role.Chain => 1f, _ => 0f };

    /// <summary>역할별 투자량 = 가진 증강의 레벨 합.</summary>
    static int Invested(AugmentManager owned, Role role)
    {
        int sum = 0;
        foreach (AugmentRunner r in Owned(owned))
            if (GuideRoleOf(r.Instance.Data) == role) sum += r.Instance.Level;
        return sum;
    }

    /// <summary>지금 올려야 할 역할.</summary>
    public static Role Guide(AugmentManager owned)
    {
        if (!AttackReady(owned)) return Role.Attack;
        if (!HasSearch(owned)) return Role.Search;
        if (Count(owned, Role.Chain) == 0) return Role.Chain;

        // 셋 다 갖췄다 — 가중치 대비 가장 덜 키운 역할. 같으면 중요도 순
        Role best = Role.Attack;
        float bestRatio = float.MaxValue;
        foreach (Role r in new[] { Role.Attack, Role.Search, Role.Chain })
        {
            float ratio = Invested(owned, r) / Weight(r);
            if (ratio < bestRatio - 0.001f) { bestRatio = ratio; best = r; }
        }
        return best;
    }

    /// <summary>
    /// 이 카드의 추천 점수(0 = 추천 안 함). 선택 화면은 가장 높은 카드 한 장에만 추천을 붙인다.
    /// 지금 올려야 할 역할이면 크게, 아니면 중요도·부족한 정도로 작게 — 원하는 역할이 안 깔렸을 때 차선을 고른다.
    /// </summary>
    public static float Score(AugmentData data, AugmentManager owned, out string reason)
    {
        reason = string.Empty;
        if (data == null || Locked(data, owned)) return 0f;

        Role role = GuideRoleOf(data);
        if (role == Role.Item) return 0f;

        // 단계 조건 — 공격을 갖추기 전엔 탐색·연쇄를, 탐색 없이는 연쇄를 권하지 않는다
        if (role != Role.Attack && !AttackReady(owned)) return 0f;
        if (role == Role.Chain && !HasSearch(owned)) return 0f;

        Role guide = Guide(owned);
        int lv = LevelOf(data, owned);

        float score = role == guide ? 100f : 0f;
                score += role switch { Role.Attack => 3f, Role.Search => 2f, _ => 1f };   // 중요도
        score -= Invested(owned, role) / Weight(role);               // 덜 키운 역할일수록 위
        score += lv > 0 ? 0.5f : 0f;                                  // 같은 역할이면 가진 것을 올리는 쪽

        reason = role switch
        {
            Role.Attack => lv > 0 ? "공격 · 화력 강화" : "공격 · 화력 확보",
            Role.Search => BonusAt(data, lv + 1) is float b && b > 0f ? $"탐색 · 받는 피해 +{b * 100f:0}%" : "탐색 · 표식",
            _ => NeedsSearch(data) ? "연쇄 · 탐색 대상끼리 전이" : "연쇄 · 적중을 자식에게 전이"
        };

        return Mathf.Max(0.01f, score);
    }

    /// <summary>이 카드를 추천하는가(지금 올려야 할 역할인가)와 그 이유.</summary>
    public static bool Recommend(AugmentData data, AugmentManager owned, out string reason)
        => Score(data, owned, out reason) >= 100f;

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
            Role.Attack => $"{head}    <color=#FFD54A>▶ 공격 강화</color>",
            Role.Search => $"{head}    <color=#FFD54A>▶ 탐색 강화</color>",
            Role.Chain => $"{head}    <color=#FFD54A>▶ 연쇄 강화</color>",
            _ => head
        };
    }
}
