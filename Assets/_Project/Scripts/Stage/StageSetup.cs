using UnityEngine;

/// <summary>
/// 이 씬에서 돌 스테이지를 확정하고, 스테이지가 시작할 때 한 번 하는 준비를 맡는다 —
/// 배경 깔기와 부팅 로그.
///
/// <b>수치는 여기 없다.</b> 전부 <see cref="StageData"/> 에셋에 있다.
/// 스테이지를 바꾸는 것은 씬을 복제하는 일이 아니라 에셋을 갈아끼우는 일이다.
///
/// 씬의 [Stage] 밑에 빈 오브젝트를 만들어 붙인다.
/// </summary>
public class StageSetup : MonoBehaviour
{
    [Tooltip("스테이지 선택을 거치지 않고 이 씬을 바로 재생했을 때 쓸 스테이지.\n" +
             "선택 화면에서 넘어온 것이 있으면 그쪽이 이긴다.")]
    [SerializeField] StageData defaultStage;

    /// <summary>이 씬에서 도는 스테이지. Awake 에서 확정된다.</summary>
    public StageData Stage { get; private set; }

    void Awake()
    {
        // 읽는 쪽들은 전부 Start 에서 본다. Awake 를 다 돌린 뒤 Start 가 오므로 순서가 보장된다
        Stage = StageContext.Begin(defaultStage);

        if (Stage == null)
            Debug.LogError("[StageSetup] 스테이지가 없다. Default Stage 에 StageData 를 물릴 것.", this);
    }

    void Start()
    {
        if (Stage == null) return;

        if (Stage.backgroundPrefab != null)
            Instantiate(Stage.backgroundPrefab, Vector3.zero, Quaternion.identity);

        // 로그 창이 아직 없는 씬에서도 스테이지는 돌아야 한다
        if (LogManager.Instance == null || Stage.bootLines == null) return;

        for (int i = 0; i < Stage.bootLines.Length; i++)
            LogManager.Instance.NoneLog(Stage.bootLines[i]);
    }
}
