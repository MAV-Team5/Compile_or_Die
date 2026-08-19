using UnityEngine;

/// <summary>
/// 대상에게 얼마간 붙어 있는 효과. 지속 피해 · 둔화 같은 버프/디버프의 공통 부모.
///
/// 이 객체는 증강 에셋에 하나만 있고 모든 대상이 공유한다 — 여기에 상태를 담으면 안 된다.
/// 개체별 남은 시간·세기·타이머는 StatusHolder.Active 가 들고 있으니 그걸 받아 쓴다.
///
/// 탐색 표식(SearchMark)은 여기 속하지 않는다. 전역 탐색풀에 등록되고
/// 다른 증강이 그 목록을 조회하는, 성격이 다른 물건이라 따로 산다.
/// </summary>
[System.Serializable]
public abstract class Status : AugmentModule
{
    [Tooltip("이 상태가 살아있는 동안 대상에 붙일 오브젝트. 비워도 효과는 그대로 동작한다.\n" +
             "적 몸에 붙는 파티클·틴트를 쓴다. 머리 위 아이콘은 탐색 표식의 자리다.")]
    public GameObject statusVfx;

    [Tooltip("켜면 같은 증강이 다시 걸 때 지속시간만 갱신한다. 끄면 하나 더 쌓인다.")]
    public bool refreshInsteadOfStack = true;

    /// <summary>매 프레임 호출. 지속 피해처럼 스스로 뭔가 하는 상태가 쓴다.</summary>
    public virtual void Tick(StatusHolder holder, StatusHolder.Active active, float deltaTime) { }

    /// <summary>이동속도에 곱할 배율. 1이면 영향 없음.</summary>
    public virtual float SpeedMultiplier(StatusHolder.Active active) => 1f;

    public virtual void OnApplied(StatusHolder holder, StatusHolder.Active active) { }
    public virtual void OnRemoved(StatusHolder holder, StatusHolder.Active active) { }
}
