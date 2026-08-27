using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하드웨어 업그레이드 표. 부품마다 레벨당 얼마나 오르고 얼마가 드는지를 기획자가 여기서 정한다.
///
/// <c>Create → CoD → Hardware Table</c> 로 만든다.
///
/// <b>값은 레벨마다 직접 적는다.</b> 예전에는 기본값에 배수를 거듭 곱하는 식이었는데,
/// 그러면 100·200·400 처럼 배수가 일정한 곡선만 만들 수 있다.
/// 메인보드처럼 500·2000·5000 으로 확 뛰는 곡선을 적을 수 없어서 표로 바꿨다.
/// 칸 수가 곧 최대 레벨이라, 최대 레벨과 값 목록이 서로 어긋날 일도 없다.
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

        [Tooltip("무엇이 좋아지는가. 값은 뒤에 자동으로 붙으므로 '투사체 속도'처럼 짧게.")]
        public string effectName;

        [Tooltip("레벨 1당 오르는 양. 비율이면 0.1 이 +10%, 개수면 1 이 +1개.")]
        public float perLevel = 0.1f;

        [Tooltip("켜면 % 로, 끄면 개수로 표시한다. 메인보드처럼 개수를 주는 부품만 끈다.")]
        public bool percent = true;

        [Tooltip("레벨별 값(bit). 첫 칸이 레벨 1을 살 때 드는 값이다.\n" +
                 "＊ 칸 수가 곧 최대 레벨이다. 비워두면 잠긴 부품이 된다.")]
        public int[] costs = { 100, 200, 400, 800, 1600 };

        public Sprite icon;

        public int MaxLevel => costs != null ? costs.Length : 0;

        /// <summary>아직 받을 시스템이 없어 살 수 없는 부품인가.</summary>
        public bool Locked => MaxLevel == 0;
    }

    [SerializeField] List<Entry> entries = new();

    public IReadOnlyList<Entry> Entries => entries;

    public Entry Find(HardwareKind kind)
    {
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].kind == kind) return entries[i];

        return null;
    }

    /// <summary>올릴 수 있는 최대 레벨. 잠긴 부품은 0.</summary>
    public int MaxLevelOf(HardwareKind kind)
    {
        Entry entry = Find(kind);
        return entry != null ? entry.MaxLevel : 0;
    }

    /// <summary>다음 레벨을 사는 데 드는 값. 최대치이거나 잠겼으면 -1.</summary>
    public int CostToUpgrade(HardwareKind kind, int currentLevel)
    {
        Entry entry = Find(kind);

        if (entry == null || currentLevel < 0 || currentLevel >= entry.MaxLevel) return -1;

        return entry.costs[currentLevel];
    }

    /// <summary>이 레벨에서 실제로 얹히는 보너스 총량.</summary>
    public float BonusAt(HardwareKind kind, int level)
    {
        Entry entry = Find(kind);

        return entry == null ? 0f : entry.perLevel * level;
    }

    /// <summary>"투사체 속도 +30%" 처럼 화면에 그대로 쓸 문구.</summary>
    public string DescribeAt(HardwareKind kind, int level)
    {
        Entry entry = Find(kind);
        if (entry == null) return "";

        float bonus = entry.perLevel * level;

        return entry.percent
            ? $"{entry.effectName} +{bonus:P0}"
            : $"{entry.effectName} +{bonus:0}";
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        var seen = new HashSet<HardwareKind>();

        for (int i = 0; i < entries.Count; i++)
        {
            // 같은 부품을 두 줄 적으면 Find 가 앞줄만 보게 되어 뒷줄이 조용히 무시된다
            if (!seen.Add(entries[i].kind))
                Debug.LogWarning($"[HardwareTable] {entries[i].kind} 가 두 번 있다. 앞줄만 쓰인다.", this);

            // 값이 0 이하면 공짜로 살 수 있게 되어버린다
            int[] costs = entries[i].costs;
            if (costs == null) continue;

            for (int c = 0; c < costs.Length; c++)
                if (costs[c] <= 0)
                    Debug.LogWarning($"[HardwareTable] {entries[i].kind} 레벨 {c + 1} 의 값이 0 이하다.", this);
        }
    }
#endif
}
