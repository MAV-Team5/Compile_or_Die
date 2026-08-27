using UnityEngine;

/// <summary>
/// 경험치와 레벨. 한 번에 여러 레벨이 오르면 쌓아두고,
/// 증강 선택 UI가 하나씩 꺼내 간다.
/// </summary>
public class LevelSystem : MonoBehaviour
{
    [Header("레벨 곡선 — 필요 경험치 = base + growth × (레벨-1)")]
    [Tooltip("테스트 편의를 위해 원래 값(10 / 8)의 1/10로 줄여 둔 상태.")]
    [SerializeField] int baseRequired = 1;
    [SerializeField] int growthPerLevel = 1;

    public int Level { get; private set; } = 1;
    public int CurrentExp { get; private set; }

    /// <summary>아직 증강 선택을 하지 않은 레벨업 수.</summary>
    public int PendingLevelUps { get; private set; }

    public int RequiredExp => baseRequired + growthPerLevel * (Level - 1);

    /// <summary>(레벨, 현재 경험치, 필요 경험치)</summary>
    public event System.Action<int, int, int> ExpChanged;

    /// <summary>레벨업 1회마다 새 레벨과 함께 알린다.</summary>
    public event System.Action<int> LeveledUp;

    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        // RAM 업그레이드. 올림이라 배율이 걸려 있으면 1짜리도 최소 1은 더 받는다
        amount = Mathf.CeilToInt(amount * HardwareBonus.ExpMultiplier);

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

    /// <summary>증강 선택 1회가 끝날 때 소비한다.</summary>
    public bool ConsumePendingLevelUp()
    {
        if (PendingLevelUps <= 0) return false;

        PendingLevelUps--;
        return true;
    }
}
