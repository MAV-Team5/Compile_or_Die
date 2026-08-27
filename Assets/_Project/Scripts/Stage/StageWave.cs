using UnityEngine;

/// <summary>
/// 스테이지 안의 웨이브 한 줄. "언제부터 어떤 적이 얼마나 자주, 얼마나 세게 나오는가."
///
/// <b>적의 기본 스탯은 여기 없다</b> — <see cref="EnemyData"/> 가 들고 있고,
/// 여기서는 배율만 얹는다. 그래야 적 밸런스를 고칠 때 스테이지를 전부 뒤지지 않아도 된다.
///
/// 시각이 겹치는 줄을 여러 개 두면 동시에 돈다 — 잡몹 줄과 엘리트 줄을 따로 적는 식이다.
/// </summary>
[System.Serializable]
public class StageWave
{
    [Tooltip("표에서 알아보기 위한 이름. 동작에는 영향 없다.")]
    public string label = "Wave";

    [Header("언제")]
    [Tooltip("런 시작으로부터 몇 초 뒤에 시작하나.")]
    public float startAt = 0f;

    [Tooltip("몇 초 동안 이어지나. 0이면 런이 끝날 때까지 계속.")]
    public float duration = 0f;

    [Header("무엇을")]
    public EnemyData enemy;

    [Header("얼마나")]
    [Tooltip("몇 초마다 내보내나. 작을수록 빽빽하다.")]
    public float interval = 0.6f;

    [Tooltip("한 번에 몇 마리씩. 2 이상이면 여러 지점에서 동시에 나온다.")]
    public int burst = 1;

    [Tooltip("이 웨이브가 통틀어 내보낼 최대 마릿수. 0이면 제한 없음.")]
    public int maxSpawns = 0;

    [Header("얼마나 세게 — 적의 기본 스탯에 곱한다")]
    [Tooltip("체력 배율. 1이면 EnemyData 그대로.")]
    public float healthScale = 1f;

    [Tooltip("이동 속도 배율.")]
    public float speedScale = 1f;

    [Tooltip("접촉 피해 배율.")]
    public float damageScale = 1f;

    [Tooltip("몸집 배율. EnemyData 의 크기에 곱해진다.")]
    public float sizeScale = 1f;

    [Header("경험치 — 이 웨이브가 무엇을 얼마나 떨구나")]
    [Tooltip("떨어질 경험치 오브 프리팹. 주는 값은 프리팹의 ExpMove.exp 가 갖는다.\n\n" +
             "비우면 이 웨이브의 적은 경험치를 안 떨군다.")]
    public GameObject expOrb;

    [Tooltip("적 한 마리당 평균 몇 개를 떨구나.\n\n" +
             "0.4 면 두세 마리에 하나. 1.0 이면 항상 하나. 2.7 이면 둘에 가끔 셋.\n" +
             "★ 못 준 소수는 다음 처치로 이월되므로 총량이 정확히 맞는다 —\n" +
             "   100마리 × 0.4 = 정확히 40개.")]
    [Min(0f)] public float expPerKill = 1f;

    /// <summary>이번 런에서 이미 내보낸 수. 런타임 전용이라 저장하지 않는다.</summary>
    [System.NonSerialized] public int Spawned;

    /// <summary>다음 발사까지 남은 시간.</summary>
    [System.NonSerialized] public float Timer;

    /// <summary>
    /// 아직 오브로 못 준 소수. 처치할 때마다 <see cref="expPerKill"/> 만큼 차고,
    /// 1을 넘는 만큼만 오브로 나간다.
    ///
    /// <b>웨이브가 들고 있어야 한다.</b> 적은 죽으면 사라지므로 이월할 자리가 없다.
    /// </summary>
    [System.NonSerialized] public float ExpCarry;

    public bool IsValid => enemy != null && enemy.prefab != null;

    /// <summary>이 웨이브가 적에게 얹을 배율.</summary>
    public EnemyScale Scale
        => EnemyScale.Of(healthScale, speedScale, damageScale, sizeScale);

    /// <summary>
    /// 이번 처치로 나갈 오브 개수. 남은 소수는 다음 처치로 넘긴다.
    /// <b>부를 때마다 상태가 변한다</b> — 세어 보기만 할 용도로 부르면 안 된다.
    /// </summary>
    public int TakeOrbCount()
    {
        if (expOrb == null || expPerKill <= 0f) return 0;

        ExpCarry += expPerKill;

        int count = Mathf.FloorToInt(ExpCarry);
        ExpCarry -= count;

        return count;
    }

    /// <summary>지금 이 웨이브가 도는 중인가.</summary>
    public bool IsActive(float now)
    {
        if (now < startAt) return false;
        if (duration > 0f && now >= startAt + duration) return false;

        return maxSpawns <= 0 || Spawned < maxSpawns;
    }
}
