/// <summary>
/// 런이 시작될 때 씻어야 하는 전역 상태를 한자리에 모은 곳.
///
/// <b>왜 필요한가</b> — 씬을 다시 로드하면 씬 오브젝트는 전부 파괴되지만
/// <c>static</c> 목록은 그대로 남는다. 지난 런에서 죽은 적·소환물의 참조가
/// 목록에 눌러앉아, 두 번째 런부터 조용히 어긋나기 시작한다.
///
/// <b>규칙</b> — 런 사이에 남으면 안 되는 static 을 새로 만들면 여기에 한 줄 추가할 것.
/// 여기만 읽으면 무엇이 씻기는지 전부 보이는 상태를 유지한다.
/// </summary>
public static class RunLifecycle
{
    /// <summary>런 시작 직전에 한 번. RunDirector.Awake 가 부른다.</summary>
    public static void ResetStatics()
    {
        // 증강별 생존 소환물 목록. 안 비우면 지난 런의 죽은 참조가 상한을 잡아먹는다
        Summon.Clear();

        // 표식이 붙은 적 목록. 죽은 MarkerHolder 가 남으면 탐색 대상 수가 부풀려진다
        SearchRegistry.Clear();

        // "한 번만 띄운" 경고 기록. 안 비우면 에디터를 껐다 켜기 전까지 다시 안 뜬다
        ModuleWarning.Reset();

        // 증강별 피해 집계. 지난 런 수치가 이번 결과 패널에 섞이면 분석이 무의미해진다
        RunStats.Reset();

        // 지난 런에서 스택 증강을 뽑았으면 켜진 채로 남는다. static 은 씬을 넘어 산다
        AugmentDamageLog.Reset();

        // 지난 런의 성적표. 정산을 거치지 않은 경로로 결과 씬에 들어가면
        // 낡은 성적표가 이번 판 것인 양 그대로 뜬다.
        // 비워두면 그 경우 결과 패널이 "결과 없음"을 띄우므로 눈에 바로 걸린다
        RunResult.Last = null;
    }
}
