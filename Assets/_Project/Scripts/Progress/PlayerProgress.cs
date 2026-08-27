using UnityEngine;

/// <summary>
/// 런과 런 사이에 남는 것 — 재화와 하드웨어 업그레이드 레벨.
///
/// PlayerPrefs 한 칸에 JSON 을 통째로 넣는다. 칸을 여러 개 쓰면
/// 필드를 하나 늘릴 때마다 저장·불러오기 양쪽을 고쳐야 해서 언젠가 어긋난다.
///
/// <b>레벨을 두 벌로 두는 이유</b> — 산 것과 지금 쓰는 것을 나눈다.
/// <see cref="PurchasedLevel"/> 은 비트를 치러야 오르고 절대 내려가지 않는다.
/// <see cref="ActiveLevel"/> 은 그 범위 안에서 값 없이 오르내린다 — 되팔기는 없다.
/// 산 것을 잃지 않으면서 낮은 세팅으로도 시험해 볼 수 있게 하려는 것.
///
/// <b>주의</b> — 값을 바꾼 뒤 <see cref="Save"/> 를 부르지 않으면 남지 않는다.
/// 자동 저장은 하지 않는다. 언제 디스크에 쓰이는지가 보여야 하기 때문.
/// </summary>
public static class PlayerProgress
{
    const string Key = "CoD.Progress";

    [System.Serializable]
    class Data
    {
        public int bits;

        /// <summary>HardwareKind 순서대로 구매한 최고 레벨. 길이가 모자라면 불러올 때 늘린다.</summary>
        public int[] hardware;

        /// <summary>HardwareKind 순서대로 지금 적용 중인 레벨. 구매 레벨을 넘지 않는다.</summary>
        public int[] active;
    }

    static Data data;

    static int KindCount => System.Enum.GetValues(typeof(HardwareKind)).Length;

    /// <summary>모아둔 재화.</summary>
    public static int Bits
    {
        get { Ensure(); return data.bits; }
    }

    // ── 레벨 조회 ─────────────────────────────────────────

    /// <summary>비트를 치르고 산 최고 레벨. 되팔 수 없다.</summary>
    public static int PurchasedLevel(HardwareKind kind)
    {
        Ensure();
        return data.hardware[(int)kind];
    }

    /// <summary>지금 실제로 적용되는 레벨. 능력치 주입은 이 값을 본다.</summary>
    public static int ActiveLevel(HardwareKind kind)
    {
        Ensure();
        return data.active[(int)kind];
    }

    // ── 사고 조절하기 ─────────────────────────────────────

    public static void AddBits(int amount)
    {
        if (amount <= 0) return;

        Ensure();
        data.bits += amount;
    }

    /// <summary>
    /// 값을 치르고 구매 레벨을 한 단계 올린다. 모자라거나 최대치면 아무 일도 없이 false.
    /// 방금 산 단계는 곧바로 적용된다 — 사놓고 따로 켜야 하면 산 보람이 없다.
    /// </summary>
    public static bool TryUpgrade(HardwareTable table, HardwareKind kind)
    {
        if (table == null) return false;

        Ensure();

        int index = (int)kind;
        int level = data.hardware[index];
        int cost = table.CostToUpgrade(kind, level);

        if (cost < 0 || data.bits < cost) return false;

        data.bits -= cost;
        data.hardware[index] = level + 1;
        data.active[index] = level + 1;

        Save();

        return true;
    }

    /// <summary>
    /// 적용 레벨을 바꾼다. 0 ~ 구매 레벨 사이로 잘린다. 값은 오가지 않는다.
    /// </summary>
    public static void SetActiveLevel(HardwareKind kind, int level)
    {
        Ensure();

        int index = (int)kind;

        data.active[index] = Mathf.Clamp(level, 0, data.hardware[index]);

        Save();
    }

    // ── 저장 ──────────────────────────────────────────────

    public static void Save()
    {
        Ensure();

        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    /// <summary>전부 처음으로. 테스트용.</summary>
    public static void Wipe()
    {
        PlayerPrefs.DeleteKey(Key);
        data = null;
    }

    static void Ensure()
    {
        if (data != null) return;

        string json = PlayerPrefs.GetString(Key, "");

        data = string.IsNullOrEmpty(json) ? new Data() : JsonUtility.FromJson<Data>(json);

        // 적용 레벨이라는 개념이 없던 시절의 세이브인가. 길이를 늘리기 전에 봐둬야 한다
        bool hadActive = data.active != null && data.active.Length > 0;

        // HardwareKind 를 뒤에 늘려도 지난 세이브가 깨지지 않게 길이를 맞춰준다.
        // 앞의 값은 그대로 옮겨오므로 이미 산 업그레이드는 유지된다
        data.hardware = Grow(data.hardware);
        data.active = Grow(data.active);

        // 옛 세이브는 산 것을 전부 켜둔 상태로 옮긴다.
        // 0으로 두면 분명히 샀는데 아무것도 안 걸린 것처럼 보인다
        if (!hadActive) System.Array.Copy(data.hardware, data.active, KindCount);

        for (int i = 0; i < KindCount; i++)
            data.active[i] = Mathf.Clamp(data.active[i], 0, data.hardware[i]);
    }

    static int[] Grow(int[] source)
    {
        if (source != null && source.Length >= KindCount) return source;

        var grown = new int[KindCount];

        if (source != null) System.Array.Copy(source, grown, source.Length);

        return grown;
    }
}
