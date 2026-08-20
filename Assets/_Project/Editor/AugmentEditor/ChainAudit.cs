using System.Collections.Generic;
using System.Text;

/// <summary>
/// 연쇄가 지수로 불어나는 조합을 만들기 전에 잡아낸다.
///
/// 연쇄는 선이 아니라 나무다. 한 단계에서 b개로 갈라지면 총 실행 수가 b^깊이 가 된다.
/// 분기 1(Nearest 1체)일 때만 깊이 8이 안전하고, 관통·방사를 물리면 즉시 프레임이 멈춘다.
/// </summary>
public static class ChainAudit
{
    /// <summary>이 수를 넘으면 경고한다. 한 발동에 수백 번 도는 것은 정상이 아니다.</summary>
    const int SafeNodes = 64;

    /// <summary>분기 수를 미리 알 수 없다는 뜻. 주변 적 수에 따라 결정되는 경우.</summary>
    const int Unbounded = -1;

    /// <summary>문제가 없으면 null. 있으면 인스펙터에 띄울 문구.</summary>
    public static string Inspect(AugmentData data)
    {
        if (data == null || data.effects == null) return null;

        var chains = new List<ChainEffect>();
        Collect(data.effects, chains, new HashSet<object>());

        if (chains.Count == 0) return null;

        var report = new StringBuilder();

        for (int i = 0; i < chains.Count; i++)
        {
            string line = Describe(chains[i], data);
            if (line != null) report.AppendLine(line);
        }

        return report.Length > 0 ? report.ToString().TrimEnd() : null;
    }

    static string Describe(ChainEffect chain, AugmentData data)
    {
        int depth = ResolveDepth(chain, data);
        if (depth <= 0) return null;

        int branch = Branch(chain, data);

        // 분기 1이면 대상이 깊이만큼만 늘어난다. 원래 의도한 선형 연쇄
        if (branch == 1) return null;

        if (branch == Unbounded)
        {
            return $"연쇄 분기가 주변 적 수에 따라 정해진다 (범위형 타겟팅·폭발). " +
                   $"깊이 {depth} 라 적이 몰리면 한 프레임에 수천 번 실행될 수 있다.";
        }

        long nodes = Power(branch, depth);
        if (nodes <= SafeNodes) return null;

        return $"연쇄가 지수로 불어난다 — 분기 {branch} × 깊이 {depth} = 최대 {nodes:N0}회 실행. " +
               $"하위 타겟 수나 관통·발사 수를 줄이거나 깊이를 낮출 것.";
    }

    // ── 분기 수 ────────────────────────────────────────────

    /// <summary>한 단계에서 몇 갈래로 번지는가. 타겟 수 × 전달이 만드는 적중 수.</summary>
    static int Branch(ChainEffect chain, AugmentData data)
    {
        int targets = TargetBranch(chain.targeting, data);
        if (targets == Unbounded) return Unbounded;
        if (targets <= 0) return 0;

        if (chain.deliveries == null || chain.deliveries.Count == 0) return targets;

        int hits = 0;

        for (int i = 0; i < chain.deliveries.Count; i++)
        {
            int per = DeliveryBranch(chain.deliveries[i], data);
            if (per == Unbounded) return Unbounded;

            hits += per;
        }

        return targets * hits;
    }

    static int TargetBranch(TargetingModule targeting, AugmentData data) => targeting switch
    {
        null => 0,
        NearestTargeting => 1,
        OwnerPointTargeting => 1,
        DirectionPointTargeting => 1,
        // 타겟 수는 시트를 안 본다. 모듈에 적힌 수가 전부
        RandomTargeting r => r.targetCount > 0 ? r.targetCount : 1,
        RandomPointTargeting p => p.pointCount > 0 ? p.pointCount : 1,

        // 상한을 안 걸면 반경 안 전원이라 미리 알 수 없다
        AllInRangeTargeting a => a.targetLimit > 0 ? a.targetLimit : Unbounded,

        _ => 1
    };

    static int DeliveryBranch(DeliveryModule delivery, AugmentData data) => delivery switch
    {
        null => 0,
        InstantDelivery => 1,

        ProjectileDelivery p
            => p.multiShot.shotsPerTarget.IntOf(Sheet(data).count)
               * p.pierce.IntOf(Sheet(data).pierce),

        RadialDelivery r
            => r.projectileCount.IntOf(Sheet(data).count)
               * r.pierce.IntOf(Sheet(data).pierce),

        // 관통을 안 걸면 선상 전부라 미리 셀 수 없다
        LineDelivery l => l.maxHits.IntOf(Sheet(data).pierce, Unbounded),

        // 폭발은 범위 안에 있는 만큼 맞는다
        AreaDelivery => Unbounded,

        _ => 1
    };

    // ── 도우미 ────────────────────────────────────────────

    /// <summary>최악을 봐야 하므로 마지막 레벨 수치를 쓴다.</summary>
    static AugmentLevelData Sheet(AugmentData data)
        => data.levelStats != null && data.levelStats.Length > 0
            ? data.levelStats[data.levelStats.Length - 1]
            : default;

    static int ResolveDepth(ChainEffect chain, AugmentData data)
    {
        int depth = chain.maxDepthOverride > 0 ? chain.maxDepthOverride : Sheet(data).depth;
        return depth > 8 ? 8 : depth;
    }

    static long Power(int b, int exponent)
    {
        long result = 1;

        for (int i = 0; i < exponent; i++)
        {
            result *= b;

            // 어차피 화면이 멈추는 수준이라 정확한 값이 의미가 없다
            if (result > 99_999_999L) return 99_999_999L;
        }

        return result;
    }

    // ── 수집 ──────────────────────────────────────────────

    /// <summary>중첩된 파이프라인 안에 숨은 Chain 까지 찾는다.</summary>
    static void Collect(List<EffectModule> effects, List<ChainEffect> found, HashSet<object> seen)
    {
        if (effects == null) return;

        for (int i = 0; i < effects.Count; i++)
        {
            EffectModule effect = effects[i];
            if (effect == null || !seen.Add(effect)) continue;

            switch (effect)
            {
                case ChainEffect chain:
                    found.Add(chain);
                    Collect(chain.effects, found, seen);
                    Collect(chain.finalEffects, found, seen);
                    break;

                case SubPipelineEffect sub:
                    Collect(sub.effects, found, seen);
                    break;
            }
        }
    }
}
