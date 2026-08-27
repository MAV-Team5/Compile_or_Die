using UnityEngine;

/// <summary>
/// 저장된 하드웨어 레벨을 이번 런의 실제 능력치로 바꿔 넣는다.
/// 런이 시작될 때 한 번만 돈다 — 런 도중에 하드웨어가 바뀌는 일은 없다.
///
/// <b>왜 한자리에 모으나</b> — 부품마다 가는 곳이 다르다.
/// 어떤 것은 증강 수치(PlayerStats)로, 어떤 것은 최종 피해로,
/// 어떤 것은 카메라나 이동속도로 간다. 흩어놓으면 "파워를 올렸는데 왜 안 세지지"를
/// 추적할 때 온 프로젝트를 뒤져야 한다. 여기만 읽으면 전부 보이는 상태를 유지할 것.
///
/// 스테이지 씬의 아무 오브젝트에나 붙인다. HardwareTable 을 물려주지 않으면 아무 일도 안 한다.
/// </summary>
public class HardwareBonus : MonoBehaviour
{
    /// <summary>
    /// 이번 런에 걸린 파워 배율. 1이면 보정 없음.
    ///
    /// 피해는 DamagePipeline 이 표식·전이를 다 계산한 <b>뒤에</b> 이 값을 곱한다 —
    /// 먼저 곱하면 표식의 고정 추가피해가 배율을 안 받아 순서에 따라 결과가 달라진다.
    /// </summary>
    public static float DamageMultiplier { get; private set; } = 1f;

    /// <summary>경험치 배율. LevelSystem 이 읽는다.</summary>
    public static float ExpMultiplier { get; private set; } = 1f;

    /// <summary>런 시작 시 추가로 받는 증강 수. 메인보드.</summary>
    public static int ExtraStartingAugments { get; private set; }

    [Tooltip("＊ 필수 — 부품별 상승폭과 값이 적힌 표. 비우면 하드웨어가 전혀 반영되지 않는다.")]
    [SerializeField] HardwareTable table;

    [Tooltip("켜면 이번 런에 무엇이 얼마나 걸렸는지 로그로 남긴다.")]
    [SerializeField] bool logApplied = true;

    void Awake()
    {
        // 씬을 다시 시작해도 지난 런 값이 남지 않게 먼저 비운다
        DamageMultiplier = 1f;
        ExpMultiplier = 1f;
        ExtraStartingAugments = 0;

        if (table == null)
        {
            Debug.LogWarning("[HardwareBonus] HardwareTable 이 없다. 하드웨어 업그레이드가 반영되지 않는다.", this);
            return;
        }

        Apply();
    }

    void Apply()
    {
        // 증강 수치로 가는 것들. PlayerStats 를 거치면 보유 증강 전부에 한꺼번에 걸린다
        Feed(HardwareKind.Cpu, StatKind.Speed);
        Feed(HardwareKind.Ssd, StatKind.Cooldown);
        Feed(HardwareKind.Gpu, StatKind.Range);
        Feed(HardwareKind.Gpu, StatKind.EffectRange);

        // 증강 수치가 아닌 것들. 각자 자기 시스템이 읽어간다
        DamageMultiplier = 1f + BonusOf(HardwareKind.Power);
        ExpMultiplier = 1f + BonusOf(HardwareKind.Ram);
        ExtraStartingAugments = Mathf.RoundToInt(BonusOf(HardwareKind.Mainboard));

        ApplyMoveSpeed();
        ApplyView();

        // 마우스(크리티컬)·쿨러(에러율)는 받을 시스템이 아직 없다.
        // 표에서 최대 레벨 0으로 잠가두었으므로 여기서는 아무것도 안 한다

        if (logApplied) LogSummary();
    }

    /// <summary>이 부품이 지금 얹어주는 양. 적용 레벨 × 레벨당 상승폭.</summary>
    float BonusOf(HardwareKind kind)
        => table.BonusAt(kind, PlayerProgress.ActiveLevel(kind));

    /// <summary>증강 수치 하나에 비율 보정을 건다. 0이면 걸지 않는다.</summary>
    void Feed(HardwareKind kind, StatKind stat)
    {
        float bonus = BonusOf(kind);

        if (Mathf.Approximately(bonus, 0f)) return;

        if (PlayerStats.Current == null)
        {
            Debug.LogWarning($"[HardwareBonus] PlayerStats 가 없어 {kind} 를 반영하지 못했다.", this);
            return;
        }

        // Source 를 이 컴포넌트로 두면 나중에 통째로 걷어낼 수 있다
        PlayerStats.Current.Add(new StatModifier(stat, this, percent: bonus));
    }

    void ApplyMoveSpeed()
    {
        float bonus = BonusOf(HardwareKind.Keyboard);

        if (Mathf.Approximately(bonus, 0f)) return;

        Player player = GameManager.instance != null ? GameManager.instance.player : null;
        if (player == null) return;

        player.speed *= 1f + bonus;
    }

    void ApplyView()
    {
        float bonus = BonusOf(HardwareKind.Monitor);

        if (Mathf.Approximately(bonus, 0f)) return;

        Camera camera = Camera.main;

        // 2D 직교 카메라라 크기를 키우면 그만큼 넓게 보인다
        if (camera != null && camera.orthographic) camera.orthographicSize *= 1f + bonus;
    }

    void LogSummary()
    {
        var sb = new System.Text.StringBuilder();

        foreach (HardwareKind kind in System.Enum.GetValues(typeof(HardwareKind)))
        {
            int level = PlayerProgress.ActiveLevel(kind);
            if (level <= 0) continue;

            sb.Append($"{kind} Lv{level}  ");
        }

        Debug.Log(sb.Length > 0
            ? $"[HardwareBonus] 적용: {sb.ToString().TrimEnd()}"
            : "[HardwareBonus] 적용된 하드웨어 없음");
    }
}
