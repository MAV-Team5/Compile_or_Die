using UnityEngine;

/// <summary>
/// 적중한 자리에 소환물을 놓는다. 터렛 · 드론 · 지배 계열 전용.
///
/// 소환물은 <see cref="AugmentRunner"/> 를 달고 <b>자기 자리를 원점으로</b> 파이프라인을 돌린다.
/// 그래서 소환물의 공격도 증강 에셋으로 그대로 조립하면 된다 — 코드를 새로 쓸 일이 없다.
/// 레벨과 플레이어 전역 보정도 그대로 따라가므로 하드웨어 업그레이드가 소환물에도 먹는다.
/// </summary>
[System.Serializable]
[ModuleInfo("적중 자리에 소환물을 놓는다", "소환물의 공격은 증강 에셋으로 따로 짠다")]
public class SummonEffect : EffectModule
{
    [Header("소환물")]
    [Required("아무것도 소환되지 않는다")]
    [Tooltip("소환물 몸통 프리팹. 스프라이트·콜라이더·이동 등 겉모습만 담으면 된다.\n" +
             "행동은 아래 증강이 맡으므로 여기에 공격 코드를 넣지 말 것.")]
    public GameObject summonPrefab;

    [Required("소환은 되지만 아무 짓도 안 한다")]
    [Tooltip("소환물이 실행할 증강. 이것도 평범한 증강 에셋이다 —\n" +
             "타겟팅이 소환물 자리를 기준으로 돌기 때문에 '터렛 주변 적'이 저절로 된다.")]
    public AugmentData behaviour;

    [Header("수명과 수")]
    [Sheet("지속시간")]
    [Tooltip("소환물이 남아있는 시간(초). 비워두면 시트의 지속시간(duration)을 쓴다.\n" +
             "결과가 0이면 사라지지 않는다 — 상한을 꼭 걸 것.")]
    public Scalable duration = Scalable.Ratio(1f);

    [Sheet("수량")]
    [Tooltip("동시에 살아있을 수 있는 수. 0이면 시트의 수량(count)을 쓰고, 그것도 0이면 무제한.\n" +
             "넘치면 가장 오래된 것부터 사라진다.")]
    public int maxAliveOverride = 0;

    [Tooltip("켜면 적중한 적 위치에, 끄면 이 파이프라인의 원점에 놓는다.\n" +
             "제자리에 터렛을 세우려면 꺼둘 것.")]
    public bool placeAtTarget = true;

    [Fx("소환 연출", "소환된 자리")]
    public FxGroup summonFx = new();

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        if (summonPrefab == null || behaviour == null) return;

        // 소환물이 자기 자신을 또 부르면 화면이 순식간에 덮인다
        if (behaviour == ctx.Instance.Data)
        {
            ModuleWarning.Once(ctx, "소환물의 행동이 자기 자신입니다. 무한 소환이 되므로 막았습니다");
            return;
        }

        Vector3 at = placeAtTarget && hit.Target != null
            ? hit.Target.position
            : ctx.Owner.position;

        int maxAlive = maxAliveOverride > 0 ? maxAliveOverride : ctx.Stat.count;

        Summon placed = Summon.Place(summonPrefab, at, ctx.Instance, behaviour,
                                     duration.Of(ctx.Stat.duration), maxAlive);

        // 소환물 자신을 넘긴다 — 등장 모션을 여기서 시킬 수 있다
        if (placed != null) summonFx.PlayAt(at, ctx.Heading, 0f, placed.transform);
    }
}
