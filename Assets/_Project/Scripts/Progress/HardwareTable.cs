using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하드웨어 업그레이드 표. 부품마다 레벨당 얼마나 오르고 얼마가 드는지를 기획자가 여기서 정한다.
///
/// <c>Create → CoD → Hardware Table</c> 로 만들거나, <c>CoD → 하드웨어 표 만들기</c> 로
/// 기획서의 10종이 채워진 표를 한 번에 만든다.
///
/// <b>무엇이 오르는지도 여기 적는다.</b> 부품 효과를 코드에 두면 밸런싱을 고칠 때마다
/// 컴파일을 기다려야 하고, 기획자가 손댈 수 없다. <see cref="HardwareLoader"/> 는
/// 이 표를 읽어 옮기기만 한다.
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

        [Tooltip("이 부품이 올리는 것들. 여러 개를 둘 수 있다 — GPU 는 사거리와 효과범위를 같이 올린다.")]
        public List<HardwareEffect> effects = new();

        public Sprite icon;

        /// <summary>이 레벨에서 걸리는 효과를 한 줄로. 상점 줄에 쓴다.</summary>
        public string DescribeAt(int level)
        {
            if (effects.Count == 0) return "";

            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < effects.Count; i++)
            {
                if (sb.Length > 0) sb.Append("  ");
                sb.Append(effects[i].Describe(level));
            }

            return sb.ToString();
        }
    }

    [SerializeField] List<Entry> entries = new();

    public IReadOnlyList<Entry> Entries => entries;

#if UNITY_EDITOR
    /// <summary>에디터 메뉴가 기본값을 부어 넣을 때만 쓴다. 런타임에서는 표를 안 바꾼다.</summary>
    public void EditorFill(List<Entry> filled) => entries = filled;
#endif

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

    /// <summary>잠긴 부품인가. 최대 레벨이 0이면 상점에 회색으로 뜬다.</summary>
    public bool IsLocked(HardwareKind kind)
    {
        Entry entry = Find(kind);

        return entry == null || entry.maxLevel <= 0;
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
