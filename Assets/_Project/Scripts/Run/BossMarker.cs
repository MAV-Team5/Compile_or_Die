using UnityEngine;

/// <summary>
/// "이 적은 보스다" 표시. 보스 프리팹에 붙인다.
///
/// <b>왜 따로 두나</b> — Enemy.Dead() 는 죽었다는 사실을 아무에게도 알리지 않고
/// SetActive(false) 만 한다. 여기서 OnDisable 을 받아 RunDirector 에 전하면
/// <b>Enemy.cs 를 한 줄도 안 고치고</b> 보스 처치를 잡을 수 있다.
///
/// 보스는 풀을 쓰지 않는다 — 풀 반납도 OnDisable 이라 처치와 구별되지 않기 때문이다.
/// </summary>
public class BossMarker : MonoBehaviour
{
    /// <summary>어느 스케줄 항목에서 나왔나. RunDirector 가 채운다.</summary>
    public BossSpawn Origin { get; private set; }

    RunDirector director;

    /// <summary>등장 직후 RunDirector 가 한 번 부른다.</summary>
    public void Bind(RunDirector owner, BossSpawn origin)
    {
        director = owner;
        Origin = origin;
    }

    void OnDisable()
    {
        // 씬이 통째로 내려가는 중이면 알릴 필요도, 알릴 대상도 없다
        if (director == null) return;

        director.OnBossDown(this);
        director = null;
    }
}
