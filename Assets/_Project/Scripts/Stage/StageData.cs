using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지 하나를 통째로 담은 에셋. <c>Create → CoD → Stage Data</c>
///
/// <b>씬은 하나면 된다.</b> 스테이지를 바꾸는 것은 이 에셋을 갈아끼우는 일이다 —
/// 씬을 복제하면 버그를 고칠 때마다 모든 사본을 똑같이 고쳐야 한다.
///
/// 밸런싱은 에셋을 나란히 놓고 비교하면 되고, 나중에 구글 시트에서 값을 부어넣기도 쉽다.
/// </summary>
[CreateAssetMenu(fileName = "StageData", menuName = "CoD/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("정체")]
    public string displayName = "Stage 01";

    [Tooltip("결과 화면·세이브에서 이 스테이지를 가리키는 값. 겹치지 않게 둘 것.")]
    public int stageId = 1;

    [Header("연출")]
    [Tooltip("바닥/배경 프리팹. 원점에 하나 깔린다. 비우면 안 깐다.")]
    public GameObject backgroundPrefab;

    [Tooltip("스테이지 시작 시 순서대로 흘릴 로그.")]
    [TextArea] public string[] bootLines =
    {
        "SYSTEM BOOT COMPLETE",
        "> kernel loaded",
        "> process attached : PLAYER_01"
    };

    [Header("적 — 시각 순서대로 적으면 읽기 쉽다")]
    [Tooltip("웨이브 목록. 시각이 겹치는 줄을 두면 동시에 돈다.")]
    public List<StageWave> waves = new();

    [Header("보스")]
    [Tooltip("정해진 시각에 나온다. endsRun 을 안 켠 항목이 곧 중간보스다.")]
    public List<BossSpawn> bosses = new();

    [Header("증강")]
    [Tooltip("이 스테이지에서 등장할 증강 목록.")]
    public AugmentPool augmentPool;

    [Tooltip("런 시작 시 들고 있는 리롤 수.")]
    public int startingRerolls = 2;

    [Header("정산")]
    [Tooltip("처치 1체당 재화.")]
    public int killValue = 1;

    [Tooltip("버틴 1초당 재화.")]
    public float timeValue = 0.5f;

    [Tooltip("클리어했을 때만 얹어 주는 재화.")]
    public int clearBonus = 100;

    /// <summary>이 스테이지가 끝날 수 있는가 — endsRun 보스가 하나라도 있는가.</summary>
    public bool HasEnding
    {
        get
        {
            for (int i = 0; i < bosses.Count; i++)
                if (bosses[i] != null && bosses[i].endsRun && bosses[i].IsValid) return true;

            return false;
        }
    }

#if UNITY_EDITOR
    // 표를 채우다 실수한 것을 저장하는 순간 알려준다
    void OnValidate()
    {
        for (int i = 0; i < waves.Count; i++)
        {
            StageWave w = waves[i];
            if (w == null) continue;

            if (w.enemy == null)
                Debug.LogWarning($"[{name}] 웨이브 {i} '{w.label}' 에 적이 없다.", this);

            // 간격이 0이면 한 프레임에 무한히 쏟아진다
            if (w.interval <= 0f)
                Debug.LogWarning($"[{name}] 웨이브 {i} '{w.label}' 의 간격이 0 이하다.", this);
        }

        if (!HasEnding)
            Debug.LogWarning($"[{name}] endsRun 보스가 없다. 죽어야만 끝나는 스테이지가 된다.", this);
    }
#endif
}
