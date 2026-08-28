using UnityEngine;

/// <summary>
/// 저장된 하드웨어 레벨을 런 시작 시 한 번 주입한다.
///
/// <b>메타 진행이 게임에 닿는 유일한 지점이다.</b> 재화 → 업그레이드 → 강해짐 의
/// 마지막 화살표라, 여기가 없으면 상점에서 아무리 사도 런이 똑같다.
///
/// <code>
/// PlayerProgress   저장된 레벨
///       ↓
/// HardwareTable    레벨 → 얼마나
///       ↓
/// HardwareLoader   ← 여기
///       ├→ PlayerStats        증강 수치 (보유 증강 전부에 걸린다)
///       └→ LevelSystem · Scanner · Player   증강 밖 수치
/// </code>
///
/// <b>Start 에서 돈다.</b> PlayerStats 가 Awake 에서 자기를 등록하므로 그보다 늦어야 한다.
/// </summary>
public class HardwareLoader : MonoBehaviour
{
    [Tooltip("부품 표. 비우면 아무것도 주입하지 않고 경고만 띄운다.")]
    [SerializeField] HardwareTable table;

    [Tooltip("무엇이 얼마나 걸렸는지 콘솔에 찍는다. 밸런싱 중에만 켤 것.")]
    [SerializeField] bool logResult;

    /// <summary>상점이 표를 물려 쓸 수 있게 열어둔다.</summary>
    public HardwareTable Table => table;

    void Start() => Apply();

    void Apply()
    {
        if (table == null)
        {
            Debug.LogWarning("[HardwareLoader] 표가 비었다. 업그레이드가 런에 반영되지 않는다.", this);
            return;
        }

        PlayerStats stats = PlayerStats.Current;

        // 배율은 곱이 아니라 합으로 쌓는다. 곱으로 쌓으면 부품 두 개가 각각 +5% 일 때
        // 10.25% 가 되어, 표에 적힌 숫자와 실제가 어긋나기 시작한다
        float exp = 0f, vision = 0f, move = 0f;

        for (int i = 0; i < table.Entries.Count; i++)
        {
            HardwareTable.Entry entry = table.Entries[i];

            int level = PlayerProgress.LevelOf(entry.kind);
            if (level <= 0) continue;

            for (int e = 0; e < entry.effects.Count; e++)
            {
                HardwareEffect effect = entry.effects[e];
                float amount = effect.AmountAt(level);

                switch (effect.target)
                {
                    case HardwareTarget.Stat:
                        AddStat(stats, effect, amount);
                        break;

                    case HardwareTarget.Exp:
                        exp += Ratio(effect, amount, entry.kind);
                        break;

                    case HardwareTarget.Vision:
                        vision += Ratio(effect, amount, entry.kind);
                        break;

                    case HardwareTarget.MoveSpeed:
                        move += Ratio(effect, amount, entry.kind);
                        break;

                    // 받아 줄 시스템을 아직 안 만든 것들. 표에서 maxLevel 을 0으로 두면
                    // 상점에 LOCKED 로 떠서 살 수 없으므로 보통 여기까지 안 온다
                    case HardwareTarget.Critical:
                    case HardwareTarget.StartingAugments:
                        Debug.LogWarning($"[HardwareLoader] {entry.kind} 의 {effect.Label} 은 "
                                       + "받아 줄 시스템이 아직 없어 적용되지 않는다.", this);
                        break;
                }
            }
        }

        Push(exp, vision, move);

        if (logResult) Report(stats, exp, vision, move);
    }

    // ── 주입 ──────────────────────────────────────────────

    void AddStat(PlayerStats stats, HardwareEffect effect, float amount)
    {
        if (stats == null)
        {
            Debug.LogWarning("[HardwareLoader] PlayerStats 가 없다. 증강 수치 보정이 통째로 빠진다.\n"
                           + "Player 오브젝트에 PlayerStats 컴포넌트를 붙일 것.", this);
            return;
        }

        bool percent = effect.mode == HardwareMode.Percent;

        stats.Add(new StatModifier(effect.statKind, this,
                                   percent ? 0f : amount,
                                   percent ? amount : 0f));
    }

    /// <summary>배율 대상은 비율만 뜻이 통한다. Add 로 적어두면 그대로 비율로 읽되 알려준다.</summary>
    float Ratio(HardwareEffect effect, float amount, HardwareKind kind)
    {
        if (effect.mode == HardwareMode.Add)
            Debug.LogWarning($"[HardwareLoader] {kind} 의 {effect.Label} 은 배율 수치다. "
                           + "Add 로 적혀 있어 비율로 읽는다 — Percent 로 고칠 것.", this);

        return amount;
    }

    void Push(float exp, float vision, float move)
    {
        Player player = GameManager.instance != null ? GameManager.instance.player : null;
        LevelSystem level = GameManager.instance != null ? GameManager.instance.levelSystem : null;

        if (level != null) level.ExpMultiplier = 1f + exp;
        if (player != null) player.HardwareSpeed = 1f + move;

        Scanner scanner = player != null ? player.scanner : null;
        if (scanner != null) scanner.RangeMultiplier = 1f + vision;
    }

    // ── 확인용 ────────────────────────────────────────────

    void Report(PlayerStats stats, float exp, float vision, float move)
    {
        string statLine = stats != null ? stats.Describe() : "PlayerStats 없음";

        Debug.Log($"[하드웨어] {statLine}\n"
                + $"경험치 ×{1f + exp:0.##}  시야 ×{1f + vision:0.##}  이동 ×{1f + move:0.##}", this);
    }
}
