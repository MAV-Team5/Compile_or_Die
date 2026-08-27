using UnityEngine;

/// <summary>
/// 적 한 종류의 정체. <c>Create → CoD → Enemy Data</c>
///
/// <b>"이 적은 무엇인가"만 적는다.</b> 언제 몇 마리 나오는지는 스테이지가 정한다
/// (<see cref="StageData"/>). 그래서 같은 적을 여러 스테이지가 세기만 다르게 재사용할 수 있고,
/// 기본 밸런스를 고칠 때 이 에셋 하나만 보면 된다.
/// </summary>
[CreateAssetMenu(fileName = "EnemyData", menuName = "CoD/Enemy Data")]
public class EnemyData : ScriptableObject
{
    /// <summary>적의 역할. 밸런싱할 때 묶어 보기 위한 분류다.</summary>
    public enum Rank
    {
        /// <summary>떼로 나오는 잡몹.</summary>
        Minion,

        /// <summary>가끔 섞여 나오는 강한 개체. 상자를 떨구는 자리.</summary>
        Elite,

        /// <summary>보스. 스포너가 아니라 RunDirector 가 내보낸다.</summary>
        Boss
    }

    [Header("정체")]
    [Tooltip("로그와 결과 화면에 뜰 이름.")]
    public string displayName = "Bug";

    [Tooltip("실제로 스폰될 프리팹. Enemy 컴포넌트가 붙어 있어야 한다.")]
    public GameObject prefab;

    public Rank rank = Rank.Minion;

    [Header("기본 스탯 — 스테이지가 여기에 배율을 곱한다")]
    [Tooltip("최대 체력.")]
    public float health = 10f;

    [Tooltip("이동 속도.")]
    public float speed = 2f;

    [Tooltip("플레이어와 닿아 있는 동안 초당 주는 피해.")]
    public float contactDamage = 10f;

    [Tooltip("몸집. 1이면 프리팹에 그려진 크기 그대로, 2면 두 배.\n" +
             "덩치가 곧 위협도로 읽히므로 엘리트는 여기를 올리는 것으로 충분할 때가 많다.")]
    public float scale = 1f;

    [Header("연출")]
    [Tooltip("이 적이 쓸 애니메이터 컨트롤러. 비우면 프리팹에 물려 있는 것을 그대로 쓴다.\n" +
             "적 종류마다 프리팹을 따로 만들었다면 비워두면 된다.")]
    public RuntimeAnimatorController animatorOverride;

    [Tooltip("플레이어 쪽을 보도록 좌우로 뒤집을지.\n" +
             "글자 모양 적처럼 뒤집으면 읽을 수 없게 되는 적은 꺼둘 것.")]
    public bool flipToFace = true;

    [Header("로그")]
    [Tooltip("등장했을 때 흘릴 줄. 비우면 아무 말도 안 한다.\n" +
             "{name} 이 위의 이름으로 바뀐다. 보스·엘리트에만 채우는 것이 보통이다.")]
    public string spawnMessage;

    [Tooltip("처치했을 때 흘릴 줄. 여러 개 적으면 그때그때 하나가 뽑힌다 —\n" +
             "같은 문구만 반복되면 로그 창이 금방 지겨워진다.\n\n" +
             "{name} 이 위의 이름으로 바뀐다. 비우면 기본 문구를 쓴다.")]
    [TextArea] public string[] killMessages = { "Clear {name}" };

    [Tooltip("처치 로그의 종류. 색과 말머리가 달라진다.\n" +
             "잡몹은 Combat, 보스는 System 이 어울린다.")]
    public GameLogType killLog = GameLogType.Combat;

    [Header("보상")]
    [Tooltip("처치 시 떨구는 경험치. 예전에는 Exp 프리팹에 박혀 있어 모든 적이 같았다.")]
    public int exp = 1;

    [Tooltip("처치 시 주는 비트(재화). 드랍물이 생기기 전까지는 정산에만 쓰인다.")]
    public int bits = 0;

    /// <summary>처치 로그 한 줄. 여러 개 적어뒀으면 그중 하나를 뽑는다.</summary>
    public string PickKillMessage()
    {
        if (killMessages == null || killMessages.Length == 0) return $"Clear {displayName}";

        string line = killMessages[Random.Range(0, killMessages.Length)];

        return string.IsNullOrEmpty(line) ? $"Clear {displayName}" : Fill(line);
    }

    /// <summary>등장 로그 한 줄. 비워뒀으면 null — 부르는 쪽이 건너뛴다.</summary>
    public string SpawnMessage
        => string.IsNullOrEmpty(spawnMessage) ? null : Fill(spawnMessage);

    string Fill(string line) => line.Replace("{name}", displayName);

#if UNITY_EDITOR
    void OnValidate()
    {
        if (prefab != null && prefab.GetComponent<Enemy>() == null)
            Debug.LogWarning($"[{name}] 프리팹에 Enemy 컴포넌트가 없다. 스폰해도 안 움직인다.", this);
    }
#endif
}
