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
    /// 이번 발동에 추가 피해를 얹는다. <see cref="DamageEffect"/> 가 마지막에 더한다.
    ///
    /// <b>타겟팅이 값을 정하는 증강을 위해 열어둔다.</b> 스택의 "그 좌표에 기억해둔 피해",
    /// 큐의 "기다린 시간만큼" 처럼, <b>무엇을 고르느냐</b>와 <b>얼마를 주느냐</b>가
    /// 같이 정해지는 경우가 있다. 그럴 때 효과 모듈을 새로 만들지 않고 이 값에 얹으면
    /// 기존 DamageEffect 를 그대로 쓸 수 있다.
    /// </summary>
    public void AddBonus(float amount) => BonusDamage += amount;

    /// <summary>
    /// 발동 1회를 구분하는 번호. 연쇄 단계는 최초 발동의 번호를 그대로 물려받는다.
    /// 표식 해제처럼 "이번 발동인가 지난 발동인가"를 가릴 때 쓴다.
    /// </summary>
    public int FiringId { get; private set; }

    /// <summary>
    /// 직전 피해가 <b>실제로 들어간 양</b>. <see cref="RelayDamageEffect"/> 가 "그 몇 %" 를 뗄 때 쓴다.
    ///
    /// <b>파이프라인을 통과한 뒤의 값이다.</b> 표식 추가피해와 하드웨어 배율까지 다 반영된 수치라,
    /// 간선 전이(<see cref="LinkHolder.Propagate"/>)가 쓰는 기준과 같다 —
    /// 둘이 다르면 표식 걸린 적에서 전이는 반영되고 내부 증강은 안 되는, 설명 못 할 차이가 생긴다.
    /// </summary>
    public float LastDamage;

    /// <summary>
    /// 이번 발동이 한 주기의 첫 발인가. 장탄식(BurstTrigger)에서만 갈린다.
    /// <see cref="FirstShotEffect"/> 가 이 값을 보고 강화 효과를 낼지 정한다.
    /// </summary>
    public bool FirstOfCycle = true;

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
        LastDamage = 0f;
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

    /// <summary>
    /// 내부 증강용 초기화. 뿌리가 만든 <b>상황</b>은 물려받되 <b>수치</b>는 자기 것을 쓴다.
    ///
    /// 이렇게 갈라야 내부 증강이 레벨을 탄다 — 뿌리의 ctx 를 그대로 넘기면
    /// 내부 증강의 시트가 통째로 무시되고, 반대로 스탯을 뿌리에 접어 넣으면
    /// 뿌리의 평타까지 세져서 둘 다 원하는 그림이 아니다.
    ///
    /// <see cref="BonusDamage"/> 는 물려받지 않는다. 연쇄로 쌓인 그 값은
    /// <see cref="LastDamage"/> 안에 이미 녹아 있어서, 또 더하면 두 번 세는 셈이 된다.
    /// </summary>
    public void BeginExtension(AugmentContext parent, AugmentInstance inner)
    {
        Owner = parent.Owner;

        // 여기가 핵심 — 수치와 상태 주머니가 내부 증강 것으로 바뀐다
        Instance = inner;
        Stat = inner.Stat;

        Depth = parent.Depth;
        BonusDamage = 0f;
        LastDamage = parent.LastDamage;
        FiringId = parent.FiringId;

        Heading = parent.Heading;
        BaseRange = Stat.range;
        EffectiveRange = parent.EffectiveRange;

        ChainVisited = parent.ChainVisited;
        Targets.Clear();
    }

    public T GetState<T>(AugmentModule module) where T : class, new()
        => Instance.GetState<T>(module);
}
