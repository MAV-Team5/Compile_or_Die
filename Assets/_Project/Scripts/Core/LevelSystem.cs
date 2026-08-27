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

    /// <summary>하드웨어(RAM)가 올리는 경험치 배율. 1이면 보정 없음. HardwareLoader 가 채운다.</summary>
    public float ExpMultiplier { get; set; } = 1f;

    /// <summary>
    /// 배율을 곱하고 남은 소수. 매번 버리면 <b>+5% 가 영영 한 번도 안 붙는다</b> —
    /// 경험치 1짜리 적을 1.05 로 받아도 내림하면 계속 1이기 때문이다.
    /// </summary>
    float expCarry;

    /// <summary>(레벨, 현재 경험치, 필요 경험치)</summary>
    public event System.Action<int, int, int> ExpChanged;

    /// <summary>레벨업 1회마다 새 레벨과 함께 알린다.</summary>
    public event System.Action<int> LeveledUp;

    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        CurrentExp += Boosted(amount);

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

    /// <summary>배율을 먹인 실제 획득량. 남은 소수는 다음 획득으로 넘긴다.</summary>
    int Boosted(int amount)
    {
        if (Mathf.Approximately(ExpMultiplier, 1f)) return amount;

        float scaled = amount * ExpMultiplier + expCarry;
        int whole = Mathf.FloorToInt(scaled);

        expCarry = scaled - whole;

        return whole;
    }

    /// <summary>증강 선택 1회가 끝날 때 소비한다.</summary>
    public bool ConsumePendingLevelUp()
    {
        if (PendingLevelUps <= 0) return false;

        PendingLevelUps--;
        return true;
    }
}
