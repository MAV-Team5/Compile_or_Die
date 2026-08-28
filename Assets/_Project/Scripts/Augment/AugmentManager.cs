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

    void Awake()
    {
        TargetQuery.SetLayer(enemyLayer);
    }

    void Start()
    {
        foreach (AugmentData data in startingAugments)
            Grant(data);

        GrantExtraStarters();
    }

    /// <summary>
    /// 메인보드 업그레이드로 더 받는 스타트 증강.
    ///
    /// 스테이지 풀에서 평소와 같은 규칙으로 뽑으므로, 이미 들고 있는 것과
    /// 아직 안 풀린 내부 증강은 저절로 걸러진다.
    /// StageContext 는 StageSetup 이 Awake 에서 확정하므로 여기(Start)에서 읽어야 안전하다.
    /// </summary>
    void GrantExtraStarters()
    {
        int extra = HardwareBonus.ExtraStartingAugments;
        if (extra <= 0) return;

        AugmentPool pool = StageContext.Active != null ? StageContext.Active.augmentPool : null;

        if (pool == null)
        {
            Debug.LogWarning("[AugmentManager] 스테이지에 증강 풀이 없어 추가 스타트 증강을 못 준다.", this);
            return;
        }

        var picked = new List<AugmentData>();

        new AugmentDraft(pool, this).Pick(extra, picked);

        for (int i = 0; i < picked.Count; i++)
        {
            // 회복·이동속도 같은 즉시 아이템은 들고 시작할 물건이 아니다.
            // 그만큼 수가 줄지만, 이런 것으로 스타트 증강을 채우면 없느니만 못하다
            if (picked[i].instantEffect != InstantItemEffect.None) continue;

            Grant(picked[i]);
        }
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
            return existing;
        }

        // 최초 획득 — 오브젝트 생성
        string label = string.IsNullOrEmpty(data.displayName) ? data.name : data.displayName;

        var go = new GameObject(label);
        go.transform.SetParent(transform, false);

        var runner = go.AddComponent<AugmentRunner>();
        runner.Setup(new AugmentInstance(data, level));

        runners.Add(runner);
        return runner;
    }
    public AugmentRunner Find(AugmentData data)
        => runners.Find(r => r.Instance != null && r.Instance.Data == data);

    public void LevelUp(AugmentData data)
        => Find(data)?.Instance.LevelUp();
}