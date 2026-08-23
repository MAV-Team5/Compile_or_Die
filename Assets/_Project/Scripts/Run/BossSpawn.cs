using UnityEngine;

/// <summary>
/// 정해진 시각에 나오는 보스 하나.
///
/// <b>수치는 <see cref="EnemyData"/> 가 갖는다.</b> 잡몹과 같은 자리에서 밸런싱하려는 것이다 —
/// 예전에는 보스만 프리팹을 열어야 체력이 보였다.
///
/// <b>중간보스와 최종 보스를 구분하는 것은 <see cref="endsRun"/> 뿐이다.</b>
/// 목록에 여러 개를 넣고 마지막 하나만 켜면 곧 중간보스 구조가 된다 —
/// 레이어 개념이 생겨도 이 목록을 레이어별로 쪼개기만 하면 된다.
/// </summary>
[System.Serializable]
public class BossSpawn
{
    [Tooltip("어떤 보스인가. 잡몹과 같은 방식으로 EnemyData 에서 수치를 가져온다.")]
    public EnemyData enemy;

    [Tooltip("비우면 EnemyData 의 이름을 쓴다. 같은 보스를 다른 이름으로 낼 때만 채울 것.")]
    public string nameOverride;

    [Tooltip("런 시작으로부터 몇 초 뒤에 나오나.")]
    public float atSeconds = 300f;

    [Tooltip("플레이어로부터 이 거리만큼 떨어진 곳에 나온다. 0이면 플레이어 위치에 겹쳐 나온다.")]
    public float spawnDistance = 8f;

    [Tooltip("켜면 이 보스를 잡는 순간 런이 클리어된다. 최종 보스 하나에만 켤 것.")]
    public bool endsRun = true;

    [Header("이 스테이지에서 얼마나 세게 — EnemyData 기본값에 곱한다")]
    public float healthScale = 1f;
    public float speedScale = 1f;
    public float damageScale = 1f;
    public float sizeScale = 1f;

    public bool IsValid => enemy != null && enemy.prefab != null;

    /// <summary>표시용 이름. 따로 안 적었으면 EnemyData 것을 쓴다.</summary>
    public string DisplayName
        => !string.IsNullOrEmpty(nameOverride) ? nameOverride
         : enemy != null ? enemy.displayName
         : "Boss";

    /// <summary>이 보스에게 얹을 배율. 경험치는 보스에 안 쓰므로 1로 둔다.</summary>
    public EnemyScale Scale
        => EnemyScale.Of(healthScale, speedScale, damageScale, sizeScale, 1f);

    /// <summary>이미 내보냈나. RunDirector 가 관리한다.</summary>
    [System.NonSerialized] public bool Spawned;
}
