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

    [Header("죽음 연출")]
    [Tooltip("죽을 때 재생할 애니메이터 상태 이름. 비우면 재생하지 않는다.\n\n" +
             "＊ 트리거가 아니라 상태 이름을 직접 재생한다 — 풀에서 재사용되는 오브젝트라\n" +
             "  소비되지 않은 트리거가 다음 개체까지 따라가는 사고를 막으려는 것이다.")]
    public string deathState = "";

    [Tooltip("죽고 나서 사라지기까지 기다릴 시간(초). 0이면 즉시 사라진다.\n\n" +
             "＊ 애니메이션 길이와 맞출 것. 짧으면 잘리고 길면 시체가 남아 있다.\n" +
             "  기다리는 동안 판정은 이미 꺼져 있어 때릴 수도, 닿아서 아플 수도 없다.")]
    [Min(0f)] public float deathDuration = 0f;

    [Header("처치 소리")]
    [Tooltip("죽을 때 낼 소리. 여러 개 넣으면 매번 하나를 랜덤으로 고른다.\n" +
             "잡몹은 초당 수십 마리가 죽으므로 변형이 없으면 금방 귀에 박힌다.")]
    public AudioClip[] deathClips;

    [Range(0f, 1f)] public float deathVolume = 0.5f;

    [Tooltip("같은 소리를 다시 내기까지의 최소 간격(초). 0이면 기본값 0.05.\n\n" +
             "＊ 잡몹은 0.08 쯤으로 늘리는 편이 낫다 — 한꺼번에 죽을 때 소리가 뭉개진다.\n" +
             "  보스는 0으로 두어 반드시 들리게 한다.")]
    [Min(0f)] public float deathInterval = 0f;

    [Header("보상")]
    // 경험치는 여기 없다. 무엇을 얼마나 떨굴지는 StageWave 가 정한다 —
    // 같은 적이라도 웨이브마다 다른 오브를 떨궈야 하기 때문

    [Tooltip("처치 시 주는 비트(재화). 오브 없이 바로 정산에 쌓인다.")]
    public int bits = 0;

    [Tooltip("처치 수에 셀지. 상자처럼 적이 아닌 설치물은 꺼둔다 —\n" +
             "켜두면 런 정산의 처치 수와 재화가 부풀어 밸런싱 기준이 어긋난다.")]
    public bool countsAsKill = true;

    [Header("드롭")]
    [Tooltip("처치 시 떨굴 픽업 프리팹들. 가중치가 클수록 자주 나온다.\n" +
             "떨굴 것이 무엇인지는 프리팹의 ItemPickup 이 들고 있다.")]
    public DropEntry[] dropTable;

    [Range(0f, 1f)]
    [Tooltip("떨굴 확률. 1이면 반드시. 상자는 1, 엘리트는 0.3 쯤이 어울린다.")]
    public float dropChance = 1f;

    [Min(1)]
    [Tooltip("떨어뜨릴 개수. 여러 개면 조금씩 흩어져 놓인다.")]
    public int dropCount = 1;

    /// <summary>드롭 후보 하나. 가중치로 뽑는다.</summary>
    [System.Serializable]
    public struct DropEntry
    {
        public GameObject prefab;

        [Min(0f)] public float weight;
    }

    [Header("설치물")]
    [Tooltip("안 움직이는 설치물(상자 등)인가.\n\n" +
             "＊ 스포너의 규칙이 달라진다 — 화면 밖 링이 아니라 플레이어 주변에 나오고,\n" +
             "  멀어져도 앞으로 옮겨지지 않는다. 안 움직이는 것을 순간이동시키면\n" +
             "  주우러 가는 중에 눈앞에서 사라진다.")]
    public bool stationary;

    /// <summary>가중치로 하나 뽑는다. 떨굴 것이 없으면 null.</summary>
    public GameObject PickDrop()
    {
        if (dropTable == null || dropTable.Length == 0) return null;

        float total = 0f;

        for (int i = 0; i < dropTable.Length; i++)
            if (dropTable[i].prefab != null) total += Mathf.Max(0f, dropTable[i].weight);

        if (total <= 0f) return null;

        float roll = Random.Range(0f, total);

        for (int i = 0; i < dropTable.Length; i++)
        {
            if (dropTable[i].prefab == null) continue;

            roll -= Mathf.Max(0f, dropTable[i].weight);

            if (roll <= 0f) return dropTable[i].prefab;
        }

        return null;
    }

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
