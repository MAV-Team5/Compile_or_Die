using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하드웨어 업그레이드 표. 부품마다 레벨당 얼마나 오르고 얼마가 드는지를 기획자가 여기서 정한다.
///
/// <c>Create → CoD → Hardware Table</c> 로 만든다.
///
/// <b>지금은 값을 담아두기만 한다.</b> 실제 능력치 주입(PlayerStats 등)은 아직 연결하지 않았다 —
/// 어떤 부품이 어느 수치로 갈지 확정되면 그때 배선한다.
/// </summary>
[CreateAssetMenu(fileName = "HardwareTable", menuName = "CoD/Hardware Table")]
public class HardwareTable : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public HardwareKind kind;

        [Tooltip("상점에 뜰 이름.")]
        public string displayName;

        [Tooltip("한 줄 설명. 무엇이 좋아지는가.")]
        [TextArea(1, 3)] public string description;

        [Tooltip("올릴 수 있는 최대 레벨. 0이면 잠긴 부품이다.")]
        public int maxLevel = 10;

        [Tooltip("레벨 1을 살 때의 값.")]
        public int baseCost = 50;

        [Tooltip("레벨이 오를 때마다 값에 곱해지는 비율. 1.5 면 레벨마다 1.5배씩 비싸진다.")]
        public float costGrowth = 1.5f;

        [Tooltip("레벨 1당 오르는 양. 비율 수치면 0.05 가 +5% 다.")]
        public float perLevel = 0.05f;

        public Sprite icon;
    }

    [SerializeField] List<Entry> entries = new();

    public IReadOnlyList<Entry> Entries => entries;

    public Entry Find(HardwareKind kind)
    {
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].kind == kind) return entries[i];

        return null;
    }

    /// <summary>다음 레벨을 사는 데 드는 값. 최대치면 -1.</summary>
    public int CostToUpgrade(HardwareKind kind, int currentLevel)
    {
        Entry entry = Find(kind);

        if (entry == null || currentLevel >= entry.maxLevel) return -1;

        return Mathf.RoundToInt(entry.baseCost * Mathf.Pow(entry.costGrowth, currentLevel));
    }

    /// <summary>이 레벨에서 실제로 얹히는 보너스 총량.</summary>
    public float BonusAt(HardwareKind kind, int level)
    {
        Entry entry = Find(kind);

        return entry == null ? 0f : entry.perLevel * level;
    }

#if UNITY_EDITOR
    // 같은 부품을 두 줄 적으면 Find 가 앞줄만 보게 되어 뒷줄이 조용히 무시된다
    void OnValidate()
    {
        var seen = new HashSet<HardwareKind>();

        for (int i = 0; i < entries.Count; i++)
            if (!seen.Add(entries[i].kind))
                Debug.LogWarning($"[HardwareTable] {entries[i].kind} 가 두 번 있다. 앞줄만 쓰인다.", this);
    }
#endif
}
