using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 이번 런에서 증강이 각각 얼마나 일했는지 모으는 집계기.
///
/// <b>왜 파이프라인에 붙였나</b> — 모든 피해가 DamagePipeline 한 곳을 지나므로,
/// 여기 한 줄이면 증강이 몇 개로 늘어나도 집계가 저절로 따라온다.
/// 증강마다 자기 딜을 세게 하면 새 증강을 만들 때마다 빠뜨린다.
///
/// 결과 패널이 읽고, 밸런싱할 때 "어느 증강이 실제로 일하는가"의 근거가 된다.
/// </summary>
public static class RunStats
{
    /// <summary>증강 하나의 성적표.</summary>
    public class Entry
    {
        /// <summary>표시용 이름. 증강 인스턴스가 사라져도 남게 문자열로 붙잡아 둔다.</summary>
        public string Name;

        /// <summary>분류. 주인 없는 피해는 비어 있다.</summary>
        public AugmentCategory? Category;

        /// <summary>런이 끝난 시점의 레벨. 피해량을 레벨과 함께 봐야 의미가 있다.</summary>
        public int Level;

        /// <summary>이 증강이 낸 총 피해. 표식·전이로 불어난 몫까지 포함한다.</summary>
        public float TotalDamage;

        /// <summary>적중 횟수. 총 피해를 나누면 한 방의 무게가 나온다.</summary>
        public int Hits;

        /// <summary>한 방 평균.</summary>
        public float PerHit => Hits > 0 ? TotalDamage / Hits : 0f;
    }

    /// <summary>증강 인스턴스로 찾는다. 같은 증강을 여러 레벨로 올려도 한 줄로 합쳐진다.</summary>
    static readonly Dictionary<AugmentInstance, Entry> byAugment = new();

    /// <summary>주인을 모르는 피해(옛 무기 경로 등). 합계에서 빠지면 비중이 틀어진다.</summary>
    static readonly Entry unattributed = new() { Name = "기타" };

    static float total;

    /// <summary>이번 런의 총 피해. 비중 계산의 분모.</summary>
    public static float TotalDamage => total;

    /// <summary>
    /// 피해 1건 기록. DamagePipeline 이 실제로 피해를 적용한 뒤에 부른다.
    /// augment 가 null 이면 "기타"로 모은다 — 버리면 비중 합이 100%가 안 된다.
    /// </summary>
    public static void Record(AugmentInstance augment, float amount)
    {
        if (amount <= 0f) return;

        Entry entry = Resolve(augment);

        entry.TotalDamage += amount;
        entry.Hits++;

        total += amount;
    }

    static Entry Resolve(AugmentInstance augment)
    {
        if (augment == null || augment.Data == null) return unattributed;

        if (byAugment.TryGetValue(augment, out Entry found))
        {
            // 런 도중 레벨업하면 마지막 값이 남게 매번 덮는다
            found.Level = augment.Level;
            return found;
        }

        // 이름은 지금 붙잡아 둔다. 런이 끝나면 인스턴스가 살아있다는 보장이 없다
        var created = new Entry
        {
            Name = augment.Data.displayName,
            Category = augment.Data.category,
            Level = augment.Level
        };

        byAugment[augment] = created;

        return created;
    }

    /// <summary>피해 내림차순 성적표. 결과 패널이 그대로 그린다.</summary>
    public static List<Entry> Ranked()
    {
        var list = new List<Entry>(byAugment.Count + 1);

        foreach (Entry entry in byAugment.Values) list.Add(entry);

        // 주인 없는 피해가 있을 때만 줄을 만든다. 없으면 표에 빈 줄이 생긴다
        if (unattributed.Hits > 0) list.Add(unattributed);

        list.Sort((a, b) => b.TotalDamage.CompareTo(a.TotalDamage));

        return list;
    }

    /// <summary>전체 대비 비중(0~1). 분모가 0이면 0.</summary>
    public static float ShareOf(Entry entry)
        => total > 0f ? entry.TotalDamage / total : 0f;

    /// <summary>런 시작 시 비운다. RunLifecycle 이 부른다.</summary>
    public static void Reset()
    {
        byAugment.Clear();

        unattributed.TotalDamage = 0f;
        unattributed.Hits = 0;

        total = 0f;
    }
}
