using UnityEngine;

/// <summary>
/// 씬의 공용 참조 창구. 어디서든 <c>GameManager.instance</c> 로 플레이어·풀·레벨에 닿는다.
///
/// <b>여기서 규칙을 정하지 않는다.</b> 담당이 전부 따로 있다:
/// 런의 진행과 끝은 <see cref="RunDirector"/>, 스테이지 준비는 <see cref="StageSetup"/>,
/// 경험치와 레벨은 <see cref="LevelSystem"/>, 화면은 <see cref="UIManager"/>,
/// 시간이 흐르는지는 <see cref="TimeControl"/>.
///
/// 이 파일이 다시 두꺼워지고 있다면 그건 새 담당자가 필요하다는 신호다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("씬 참조")]
    public Player player;
    public PoolManager poolManager;

    [Tooltip("경험치와 레벨. 보통 [Game] 오브젝트에 같이 붙인다.")]
    [SerializeField] LevelSystem level;

    /// <summary>경험치와 레벨.</summary>
    public LevelSystem levelSystem => level;

    void Awake()
    {
        instance = this;

        // 앞 씬에서 멈춰둔 채로 넘어왔을 수 있다. 붙잡던 오브젝트는 이미 파괴됐다
        TimeControl.ReleaseAll();

        // 씬에서 안 물렸으면 찾아본다. 어느 오브젝트에 있든 상관없게
        if (level == null) level = FindAnyObjectByType<LevelSystem>();

        if (level == null)
            Debug.LogError("[GameManager] LevelSystem 이 씬에 없다. 레벨업이 안 된다.", this);

        // 씬에 PlayerHealth 를 붙이지 않았어도 체력 시스템이 돌게 한다
        if (player != null && player.GetComponent<PlayerHealth>() == null)
            player.gameObject.AddComponent<PlayerHealth>();
    }
}
