using Unity.Cinemachine;   // LensSettings
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

    /// <summary>
    /// 런 시작 시 추가로 도는 증강 선택 횟수. 메인보드.
    ///
    /// 예전에는 여기서 몰래 뽑아 그냥 줬는데, 그러면 무엇을 받았는지 모른 채 시작한다.
    /// 지금은 레벨업과 똑같은 3택 화면이 이 횟수만큼 뜬다 —
    /// 업그레이드한 보람이 화면에 보여야 상점에 돌아올 이유가 생긴다.
    /// </summary>
    public static int ExtraStartRounds { get; private set; }

    [Tooltip("＊ 필수 — 부품별 상승폭과 값이 적힌 표. 비우면 하드웨어가 전혀 반영되지 않는다.")]
    [SerializeField] HardwareTable table;

    [Tooltip("켜면 이번 런에 무엇이 얼마나 걸렸는지 로그로 남긴다.")]
    [SerializeField] bool logApplied = true;

    /// <summary>
    /// 남이 읽어가는 값만 먼저 정한다. 표와 세이브만 보면 되는 계산이라 씬 순서를 안 탄다.
    ///
    /// AugmentSelectUI 가 Start 에서 <see cref="ExtraStartRounds"/> 를 읽으므로
    /// 이 몫은 반드시 Awake 에 있어야 한다 — Start 끼리는 순서가 보장되지 않는다.
    /// </summary>
    void Awake()
    {
        // 씬을 다시 시작해도 지난 런 값이 남지 않게 먼저 비운다
        DamageMultiplier = 1f;
        ExtraStartRounds = 0;

        if (table == null)
        {
            Debug.LogWarning("[HardwareBonus] HardwareTable 이 없다. 하드웨어 업그레이드가 반영되지 않는다.", this);
            return;
        }

        DamageMultiplier = 1f + BonusOf(HardwareKind.Power);
        ExtraStartRounds = Mathf.RoundToInt(BonusOf(HardwareKind.Mainboard));
    }

    /// <summary>
    /// 씬의 다른 컴포넌트를 건드리는 몫. Awake 가 아니라 여기여야 한다 —
    /// PlayerStats.Current 는 그쪽 Awake 가 채우는데 Awake 끼리는 순서가 없다.
    /// 모든 Awake 가 끝난 뒤에 Start 가 돌므로 여기서는 이미 준비돼 있다.
    /// </summary>
    void Start()
    {
        if (table == null) return;

        // 증강 수치로 가는 것들. PlayerStats 를 거치면 보유 증강 전부에 한꺼번에 걸린다
        Feed(HardwareKind.Cpu, StatKind.Speed);
        Feed(HardwareKind.Ram, StatKind.Cooldown);
        Feed(HardwareKind.Gpu, StatKind.Range);
        Feed(HardwareKind.Gpu, StatKind.EffectRange);

        ApplyMaxHealth();
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

    void ApplyMaxHealth()
    {
        float bonus = BonusOf(HardwareKind.Ssd);

        if (Mathf.Approximately(bonus, 0f)) return;

        Player player = GameManager.instance != null ? GameManager.instance.player : null;

        if (player == null || !player.TryGetComponent(out PlayerHealth health)) return;

        health.ScaleMaxHealth(1f + bonus);
    }

    void ApplyMoveSpeed()
    {
        float bonus = BonusOf(HardwareKind.Keyboard);

        if (Mathf.Approximately(bonus, 0f)) return;

        Player player = GameManager.instance != null ? GameManager.instance.player : null;
        if (player == null) return;

        player.speed *= 1f + bonus;
    }

    /// <summary>
    /// 시야를 넓힌다.
    ///
    /// <b>Camera 를 직접 건드리면 안 된다.</b> 이 씬은 Cinemachine 이 카메라를 몰고 있어서,
    /// CinemachineBrain 이 매 프레임 가상 카메라의 렌즈 값으로 덮어쓴다.
    /// 그래서 Camera.orthographicSize 를 키워봐야 그 프레임에 바로 되돌아간다.
    /// 몰고 있는 쪽(CinemachineCamera)의 렌즈를 고쳐야 실제로 넓어진다.
    /// </summary>
    void ApplyView()
    {
        float bonus = BonusOf(HardwareKind.Monitor);

        if (Mathf.Approximately(bonus, 0f)) return;

        float scale = 1f + bonus;

        Camera camera = Camera.main;

        // ＊ lens.Orthographic 을 믿으면 안 된다.
        //   ModeOverride 가 None 이면 그 값은 브레인이 카메라를 스냅샷할 때 채워지는
        //   내부 플래그를 따르는데, Start 시점에는 아직 비어 있어 원근으로 나온다.
        //   그대로 믿고 갈라지면 직교 화면에서 화각만 바꿔 아무 일도 일어나지 않는다.
        //   진짜 카메라에 물어보는 것이 확실하다
        bool orthographic = camera == null || camera.orthographic;

        var vcam = FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();

        if (vcam != null)
        {
            LensSettings lens = vcam.Lens;

            if (orthographic) lens.OrthographicSize *= scale;
            else lens.FieldOfView = Mathf.Min(179f, lens.FieldOfView * scale);

            vcam.Lens = lens;

            if (logApplied)
                Debug.Log($"[HardwareBonus] 시야 x{scale:0.##} → " +
                          (orthographic ? $"직교 {lens.OrthographicSize:0.##}"
                                        : $"화각 {lens.FieldOfView:0.##}"), this);
            return;
        }

        // Cinemachine 이 없는 씬(증강 테스트 등)에서는 카메라를 직접 만져도 안 밀린다
        if (camera != null && camera.orthographic) camera.orthographicSize *= scale;
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
