using UnityEngine;

/// <summary>
/// 런과 런 사이에 남는 것 — 재화와 하드웨어 업그레이드 레벨.
///
/// PlayerPrefs 한 칸에 JSON 을 통째로 넣는다. 칸을 여러 개 쓰면
/// 필드를 하나 늘릴 때마다 저장·불러오기 양쪽을 고쳐야 해서 언젠가 어긋난다.
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

        /// <summary>HardwareKind 순서대로의 레벨. 길이가 모자라면 불러올 때 늘린다.</summary>
        public int[] hardware;
    }

    static Data data;

    static int KindCount => System.Enum.GetValues(typeof(HardwareKind)).Length;

    /// <summary>모아둔 재화.</summary>
    public static int Bits
    {
        get { Ensure(); return data.bits; }
    }

    public static int LevelOf(HardwareKind kind)
    {
        Ensure();
        return data.hardware[(int)kind];
    }

    public static void AddBits(int amount)
    {
        if (amount <= 0) return;

        Ensure();
        data.bits += amount;
    }

    /// <summary>값을 치르고 한 단계 올린다. 모자라거나 최대치면 아무 일도 없이 false.</summary>
    public static bool TryUpgrade(HardwareTable table, HardwareKind kind)
    {
        if (table == null) return false;

        Ensure();

        int level = data.hardware[(int)kind];
        int cost = table.CostToUpgrade(kind, level);

        if (cost < 0 || data.bits < cost) return false;

        data.bits -= cost;
        data.hardware[(int)kind] = level + 1;

        Save();

        return true;
    }

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

        // HardwareKind 를 뒤에 늘려도 지난 세이브가 깨지지 않게 길이를 맞춰준다.
        // 앞의 값은 그대로 옮겨오므로 이미 산 업그레이드는 유지된다
        if (data.hardware == null || data.hardware.Length < KindCount)
        {
            var grown = new int[KindCount];

            if (data.hardware != null)
                System.Array.Copy(data.hardware, grown, data.hardware.Length);

            data.hardware = grown;
        }
    }
}
