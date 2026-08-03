using System.Collections.Generic;
using UnityEngine;

/// <summary>보유 증강 관리. 싱글톤 아님 — 오브젝트마다 존재 가능.</summary>
public class AugmentManager : MonoBehaviour
{
    [Header("시작 시 지급 (테스트용)")]
    [SerializeField] List<AugmentData> startingAugments = new();

    readonly List<AugmentRunner> runners = new();

    public IReadOnlyList<AugmentRunner> Runners => runners;

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