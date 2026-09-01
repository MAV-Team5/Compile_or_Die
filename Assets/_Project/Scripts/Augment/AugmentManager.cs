using System.Collections.Generic;
using UnityEngine;

/// <summary>보유 증강 관리. 싱글톤 아님 — 오브젝트마다 존재 가능.</summary>
public class AugmentManager : MonoBehaviour
{
    [Header("시작 시 지급 (테스트용)")]
    [SerializeField] List<AugmentData> startingAugments = new();

    [Header("Targeting Layer")]
    [SerializeField] LayerMask enemyLayer;

    readonly List<AugmentRunner> runners = new();

    public IReadOnlyList<AugmentRunner> Runners => runners;

    /// <summary>
    /// 플레이어의 증강 관리자. 확장 슬롯이 "나를 뿌리로 삼는 증강" 을 찾아올 통로다.
    ///
    /// 소환물처럼 관리자가 여럿 있을 수 있으므로 <b>가장 마지막에 깨어난 것</b>이 잡힌다 —
    /// 플레이어 것이 씬에 먼저 있으므로 실제로는 플레이어 것이다.
    /// </summary>
    public static AugmentManager Current { get; private set; }

    void Awake()
    {
        Current = this;

        TargetQuery.SetLayer(enemyLayer);
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    /// <summary>
    /// 씬에 물려둔 것만 조용히 지급한다. 시험용 통로다.
    ///
    /// 캐릭터 고정 증강과 메인보드 추가 선택은 여기서 처리하지 않는다 —
    /// 그것들은 <see cref="AugmentSelectUI"/> 가 카드로 보여주고 플레이어가 누른다.
    /// </summary>
    void Start()
    {
        foreach (AugmentData data in startingAugments)
            Grant(data);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        for (int i = 0; i < runners.Count; i++)
            runners[i].Tick(dt);
    }

    public AugmentRunner Grant(AugmentData data, int level = 1)
    {
        if (data == null) return null;

        if (data.levelStats == null || data.levelStats.Length == 0)
        {
            Debug.LogError($"[{data.name}] levelStats 가 비어있습니다", this);
            return null;
        }

        // 이미 보유 중이면 레벨업
        AugmentRunner existing = Find(data);
        if (existing != null)
        {
            existing.Instance.LevelUp();

            // 내부 증강이 올랐으면 뿌리의 보정도 같이 커진다
            RebuildDerived();
            return existing;
        }

        // 최초 획득 — 오브젝트 생성
        string label = string.IsNullOrEmpty(data.displayName) ? data.name : data.displayName;

        var go = new GameObject(label);
        go.transform.SetParent(transform, false);

        var runner = go.AddComponent<AugmentRunner>();
        runner.Setup(new AugmentInstance(data, level));

        runners.Add(runner);

        RebuildDerived();
        return runner;
    }
    public AugmentRunner Find(AugmentData data)
        => runners.Find(r => r.Instance != null && r.Instance.Data == data);

    /// <summary>
    /// <paramref name="root"/> 의 <paramref name="slot"/> 자리에 꽂힌 내부 증강. 없으면 null.
    ///
    /// <b>없는 것이 정상이다</b> — 아직 안 뽑았다는 뜻이므로 부르는 쪽은 조용히 넘어간다.
    /// 보유 증강이 열 몇 개뿐이라 매번 훑어도 비용이 없다.
    /// </summary>
    public AugmentRunner FindExtension(AugmentData root, string slot)
    {
        if (root == null || string.IsNullOrEmpty(slot)) return null;

        return runners.Find(r => r.Instance != null
                              && r.Instance.Data != null
                              && r.Instance.Data.rootAugment == root
                              && r.Instance.Data.extensionSlot == slot);
    }

    // ── 내부 증강 반영 ────────────────────────────────────

    /// <summary>
    /// 보유 증강 전체를 훑어 내부 증강의 보정과 조립 덮어쓰기를 다시 접는다.
    ///
    /// <b>증강이 늘거나 레벨업할 때만 부른다.</b> 그때 말고는 결과가 안 변하는데,
    /// AugmentInstance.Stat 은 매 프레임 여러 번 읽히므로 거기서 계산하면 낭비다.
    /// </summary>
    void RebuildDerived()
    {
        // 먼저 전부 원래대로 되돌린다. 지난번에 덮어쓴 것이 남으면
        // 내부 증강을 잃어도 효과가 계속 붙어 있게 된다
        for (int i = 0; i < runners.Count; i++)
        {
            AugmentInstance inst = runners[i].Instance;
            if (inst == null) continue;

            StatMath.Clear(inst.BonusAdd);
            StatMath.Clear(inst.BonusPercent);
            inst.Build = AugmentBuild.Of(inst.Data);
        }

        for (int i = 0; i < runners.Count; i++)
        {
            AugmentInstance inner = runners[i].Instance;
            if (inner?.Data == null || inner.Data.rootAugment == null) continue;

            AugmentRunner rootRunner = Find(inner.Data.rootAugment);
            if (rootRunner?.Instance == null) continue;

            ApplyBonus(rootRunner.Instance, inner);
            ApplyPatch(rootRunner.Instance, inner);
        }
    }

    /// <summary>내부 증강의 레벨 수치가 곧 뿌리의 보정치다.</summary>
    static void ApplyBonus(AugmentInstance root, AugmentInstance inner)
    {
        if (inner.Data.levelStats == null || inner.Data.levelStats.Length == 0) return;

        StatMath.Accumulate(inner.BaseStat, inner.Data.bonusIsPercent,
                            root.BonusAdd, root.BonusPercent);
    }

    /// <summary>내부 증강이 뿌리의 3축을 덮거나 이어 붙인다.</summary>
    static void ApplyPatch(AugmentInstance root, AugmentInstance inner)
    {
        AugmentData d = inner.Data;

        if (d.targetingPatch == BuildPatch.Replace && d.targeting != null)
            root.Build.Targeting = d.targeting;

        root.Build.Deliveries = Merge(root.Build.Deliveries, d.deliveries, d.deliveryPatch);
        root.Build.Effects = Merge(root.Build.Effects, d.effects, d.effectPatch);
    }

    /// <summary>
    /// 목록 축 합치기. <b>뿌리 목록을 직접 안 건드린다</b> —
    /// Add 는 새 리스트를 만든다. 뿌리 에셋의 리스트에 넣으면 에셋이 오염된다.
    /// </summary>
    static List<T> Merge<T>(List<T> current, List<T> patch, BuildPatch mode)
    {
        if (mode == BuildPatch.None || patch == null || patch.Count == 0) return current;

        if (mode == BuildPatch.Replace) return patch;

        var merged = new List<T>(current ?? new List<T>());
        merged.AddRange(patch);

        return merged;
    }

    public void LevelUp(AugmentData data)
    {
        AugmentRunner runner = Find(data);
        if (runner?.Instance == null) return;

        runner.Instance.LevelUp();

        RebuildDerived();
    }
}