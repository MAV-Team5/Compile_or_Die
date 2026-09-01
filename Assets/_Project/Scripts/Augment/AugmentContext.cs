using System.Collections.Generic;
using UnityEngine;

/// <summary>증강 1회 발동분. 모듈들이 돌려가며 채운다.</summary>
public class AugmentContext
{
    /// <summary>이 파이프라인의 원점. 최초는 플레이어, 연쇄 단계는 적중한 적.</summary>
    public Transform Owner;

    public AugmentInstance Instance;
    public AugmentLevelData Stat;

    public readonly TargetSet Targets = new();

    /// <summary>
    /// 이번 발동에서 이미 효과를 받은 대상. 연쇄가 같은 적을 되짚는 것만 막는다.
    /// 타겟팅 단계에서만 참조할 것 — 전달 단계에서 보면 동시 발사끼리 서로 방해한다.
    /// </summary>
    public HashSet<Transform> ChainVisited { get; private set; } = new();

    /// <summary>연쇄 깊이. 최초 발동은 0.</summary>
    public int Depth { get; private set; }

    /// <summary>
    /// 연쇄 단계마다 쌓이는 추가 피해. DamageEffect 가 마지막에 더한다.
    ///
    /// 곱하지 않고 더하는 이유 — 배율은 지수로 불어나 몇 단계만 지나도 손을 못 댄다.
    /// 덧셈이면 "깊이 3이니 세 번 더해졌다"가 눈으로 검산된다.
    /// </summary>
    public float BonusDamage { get; private set; }

    /// <summary>
    /// 발동 1회를 구분하는 번호. 연쇄 단계는 최초 발동의 번호를 그대로 물려받는다.
    /// 표식 해제처럼 "이번 발동인가 지난 발동인가"를 가릴 때 쓴다.
    /// </summary>
    public int FiringId { get; private set; }

    /// <summary>
    /// 이 단계의 기본 사거리. 최초 발동은 사거리(range), 하위 파이프라인은 효과 범위(effectRange).
    /// 타겟팅의 rangeOverride 가 0일 때 이 값을 쓴다.
    /// </summary>
    public float BaseRange;

    /// <summary>
    /// 이 단계가 향하는 방향(정규화).
    /// 최초 발동은 시전자가 바라보는 쪽, 하위 파이프라인은 여기까지 날아온 쪽.
    /// 부채꼴 타겟팅과 방사 발사가 참조한다.
    /// </summary>
    public Vector2 Heading;

    /// <summary>방향을 알고 있는가. 시전자가 방향을 안 알려주면 false.</summary>
    public bool HasDirection => Heading.sqrMagnitude > 0.0001f;

    /// <summary>
    /// 이번 단계가 실제로 쓴 사거리. 타겟팅이 채우고 전달이 읽는다.
    /// 타겟팅에서 반경을 좁혔는데 투사체만 멀리 날아가는 어긋남을 막는다.
    /// </summary>
    public float EffectiveRange;

    static int nextFiringId = 1;

    /// <summary>최초 발동용 초기화.</summary>
    public void Begin(Transform owner, AugmentInstance instance)
    {
        Owner = owner;
        Instance = instance;
        Stat = instance.Stat;

        Depth = 0;
        BonusDamage = 0f;
        FiringId = nextFiringId++;

        // 최초 발동은 "적을 찾아 도달하는 거리"가 기준이다
        BaseRange = instance.Stat.range;
        EffectiveRange = BaseRange;

        // 최초 발동의 방향은 시전자가 바라보는 쪽.
        // Runner 는 Player 의 손자라 부모 체인을 타야 찾힌다. 안 알려주면 방향 없음
        var facing = owner != null ? owner.GetComponentInParent<IFacingProvider>() : null;
        Heading = facing != null ? facing.Facing : Vector2.zero;

        ChainVisited = new HashSet<Transform>();
        Targets.Clear();
    }

    /// <summary>연쇄 단계용 초기화. 제외 목록은 상위와 공유한다.</summary>
    public void BeginChild(Transform owner, AugmentContext parent, float bonusDamage,
                           Vector2 heading)
    {
        Heading = heading;

        Owner = owner;
        Instance = parent.Instance;
        Stat = parent.Stat;

        Depth = parent.Depth + 1;
        BonusDamage = bonusDamage;
        FiringId = parent.FiringId;

        // 이미 도달한 뒤이므로 여기서부터는 "퍼지는 크기"가 기준이 된다
        BaseRange = Stat.effectRange > 0f ? Stat.effectRange : Stat.range;
        EffectiveRange = BaseRange;

        ChainVisited = parent.ChainVisited;
        Targets.Clear();
    }

    public T GetState<T>(AugmentModule module) where T : class, new()
        => Instance.GetState<T>(module);
}
