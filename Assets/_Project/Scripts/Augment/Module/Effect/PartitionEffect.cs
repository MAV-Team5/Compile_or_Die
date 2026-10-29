using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 퀵 정렬. 맞은 적이 피봇이 되고, 주변 적을 <b>피봇 체력과 비교해 좌우로 가른다.</b>
/// 체력이 낮은 적은 진행 방향의 왼쪽, 높은(같은) 적은 오른쪽으로 포물선을 그리며 튕긴다.
/// 잠시 뒤 갈라진 양쪽에서 각각 새 피봇을 골라 더 좁게 다시 가른다 — 깊이만큼 반복(분할 정복).
///
/// <code>
///            ◀ 약한 적들 ── [피봇] ── 센 적들 ▶
///     ↓ stepDelay 뒤
///     ◀ ─[P]─ ▶          ◀ ─[P]─ ▶          ← 반경·거리를 줄여 양쪽을 다시
/// </code>
///
/// 피봇은 제자리에 두고 표시만 남긴다(pivotFx). 갈라진 적이 착지할 때 landEffects 가 그 적에게 적용된다.
/// </summary>
[System.Serializable]
[ModuleInfo("피봇과 비교해 좌우로 가른다 — 퀵 정렬", "깊이만큼 양쪽을 다시 가른다")]
public class PartitionEffect : EffectModule
{
    // 단계마다 둘로 갈리므로 깊이 3이면 분할이 최대 1+2+4 = 7번 일어난다
    const int HardLimit = 3;

    [Sheet("효과범위")]
    [Tooltip("피봇 주변 몇 유닛까지 가를지. 비워두면 시트의 효과 범위.")]
    public Scalable radius = Scalable.Ratio(1f);

    [Sheet("깊이")]
    [Tooltip("몇 단계까지 가를지. 0이면 시트의 깊이(depth), 그것도 0이면 1단계. 3을 넘을 수 없다.")]
    public int maxDepthOverride = 0;

    [Tooltip("좌우로 튕기는 거리(유닛).")]
    [Min(0f)] public float splitDistance = 2f;

    [Tooltip("단계마다 반경·거리에 곱하는 값. 작을수록 안쪽으로 촘촘히 갈린다.")]
    [Range(0.2f, 1f)] public float falloff = 0.6f;

    [Tooltip("다음 단계를 시작하기까지 기다리는 시간(초). 착지가 끝난 뒤부터 센다.")]
    [Min(0f)] public float stepDelay = 0.25f;

    [Min(0.05f)] public float flightTime = 0.3f;
    [Min(0f)] public float arcHeight = 1f;

    [Tooltip("보스도 가를지.")]
    public bool includeBosses = false;

    [Fx("피봇 표시", "피봇")]
    public FxGroup pivotFx = new();

    [Header("갈라진 적이 착지할 때 — 그 적에게 적용")]
    [SerializeReference] public List<EffectModule> landEffects = new();

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        if (hit.Target == null) return;

        Enemy pivot = hit.Target.GetComponentInParent<Enemy>();
        if (pivot == null || !pivot.IsAlive) return;

        Vector2 forward = ctx.HasDirection ? ctx.Heading.normalized
                        : hit.Direction.sqrMagnitude > 0.0001f ? hit.Direction.normalized
                        : Vector2.right;

        var run = new Run
        {
            Ctx = ctx,
            Credit = ctx.Credit,   // 착지 효과는 나중에 돈다. 지금의 통계 주인을 붙잡아 둔다
            Left = new Vector2(-forward.y, forward.x),
            Limit = Mathf.Min(maxDepthOverride > 0 ? maxDepthOverride : Mathf.Max(1, ctx.Stat.depth), HardLimit)
        };

        Split(run, pivot, radius.Of(ctx.Stat.effectRange), splitDistance, 0);
    }

    /// <summary>한 발동의 분할 전체가 공유하는 것.</summary>
    class Run
    {
        public AugmentContext Ctx;
        public AugmentInstance Credit;
        public Vector2 Left;
        public int Limit;
    }

    readonly List<Transform> found = new();

    void Split(Run run, Enemy pivot, float range, float distance, int level)
    {
        if (pivot == null || !pivot.IsAlive || range <= 0f) return;

        Vector2 center = pivot.transform.position;
        float pivotHealth = pivot.Health;

        pivotFx.PlayAt(center, run.Left, range, pivot.transform);

        var lower = new List<Enemy>();
        var higher = new List<Enemy>();

        TargetQuery.OverlapInto(center, range, null, found);

        foreach (Transform t in found)
        {
            Enemy enemy = t != null ? t.GetComponentInParent<Enemy>() : null;
            if (enemy == null || enemy == pivot || !EnemyLaunch.CanLaunch(enemy)) continue;
            if (!includeBosses && enemy.TryGetComponent(out BossMarker _)) continue;
            if (lower.Contains(enemy) || higher.Contains(enemy)) continue;   // 콜라이더가 여럿인 적

            (enemy.Health < pivotHealth ? lower : higher).Add(enemy);
        }

        Throw(run, lower, run.Left, distance);
        Throw(run, higher, -run.Left, distance);

        if (level + 1 >= run.Limit) return;

        // 착지가 끝난 뒤 양쪽을 각각 다시 가른다
        ArcMover.After(flightTime + stepDelay, () =>
        {
            float nextRange = range * falloff;
            float nextDistance = distance * falloff;

            Split(run, PickPivot(lower), nextRange, nextDistance, level + 1);
            Split(run, PickPivot(higher), nextRange, nextDistance, level + 1);
        });
    }

    void Throw(Run run, List<Enemy> group, Vector2 side, float distance)
    {
        foreach (Enemy enemy in group)
        {
            Vector2 to = (Vector2)enemy.transform.position + side * distance;

            EnemyLaunch.Launch(enemy, to, flightTime, arcHeight, landed => Land(run, enemy, landed, side));
        }
    }

    void Land(Run run, Enemy enemy, Vector2 landed, Vector2 side)
    {
        if (enemy == null || landEffects.Count == 0) return;

        var land = new AugmentContext();
        land.BeginChild(enemy.transform, run.Ctx, run.Ctx.BonusDamage, side);
        land.Credit = run.Credit;

        var hit = new HitInfo { Target = enemy.transform, Point = landed, Direction = side };

        for (int i = 0; i < landEffects.Count; i++) landEffects[i]?.Apply(land, hit);
    }

    /// <summary>다음 피봇. 무리 중 아무나 — 퀵 정렬의 무작위 피봇. 둘 이상 남아야 가를 의미가 있다.</summary>
    static Enemy PickPivot(List<Enemy> group)
    {
        group.RemoveAll(e => e == null || !e.IsAlive);
        return group.Count >= 2 ? group[Random.Range(0, group.Count)] : null;
    }
}
