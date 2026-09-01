using UnityEngine;

/// <summary>
/// 경험치와 레벨. 한 번에 여러 레벨이 오르면 쌓아두고,
/// 증강 선택 UI가 하나씩 꺼내 간다.
/// </summary>
public class LevelSystem : MonoBehaviour
{
    [Header("레벨 곡선 — 필요 경험치 = base + growth × (레벨-1)")]
    [Tooltip("10 / 10 이면 10분 런에서 레벨업이 25회쯤 난다.\n" +
             "빠르게 시험하고 싶으면 둘 다 1로 줄일 것 — 대신 밸런스는 안 맞는다.")]
    [SerializeField] int baseRequired = 10;
    [SerializeField] int growthPerLevel = 10;

    public int Level { get; private set; } = 1;
    public int CurrentExp { get; private set; }

    /// <summary>아직 증강 선택을 하지 않은 레벨업 수.</summary>
    public int PendingLevelUps { get; private set; }

    public int RequiredExp => baseRequired + growthPerLevel * (Level - 1);

    // 경험치에는 배율 보정을 걸지 않는다. 적이 떨구는 값이 대개 한 자리라
    // +10% 만 걸어도 반올림 때문에 두 배로 뛴다. RAM 은 쿨타임으로 옮겼다

    /// <summary>(레벨, 현재 경험치, 필요 경험치)</summary>
    public event System.Action<int, int, int> ExpChanged;

    /// <summary>레벨업 1회마다 새 레벨과 함께 알린다.</summary>
    public event System.Action<int> LeveledUp;

    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        CurrentExp += amount;

        while (CurrentExp >= RequiredExp)
        {
            CurrentExp -= RequiredExp;
            Level++;
            PendingLevelUps++;

            if (LogManager.Instance != null)
                LogManager.Instance.System($"LEVEL UP -> Lv {Level}");

            LeveledUp?.Invoke(Level);
        }

        ExpChanged?.Invoke(Level, CurrentExp, RequiredExp);
    }

    /// <summary>
    /// 레벨과 무관하게 증강 선택 기회만 더한다. 런 시작 선택지가 쓴다.
    ///
    /// 레벨을 올리지 않는 것이 요점이다 — 시작 선택을 레벨업으로 처리하면
    /// 아직 아무것도 안 했는데 Lv 3 으로 시작하고, 다음 레벨까지 필요한 경험치도 같이 뛴다.
    /// </summary>
    public void AddPendingSelection(int count)
    {
        if (count <= 0) return;

        PendingLevelUps += count;
    }

    /// <summary>증강 선택 1회가 끝날 때 소비한다.</summary>
    public bool ConsumePendingLevelUp()
    {
        if (PendingLevelUps <= 0) return false;

        PendingLevelUps--;
        return true;
    }
}
