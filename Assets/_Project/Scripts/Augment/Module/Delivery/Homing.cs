using UnityEngine;

/// <summary>
/// 투사체가 날아가는 동안 목표를 쫓는 설정.
/// 발사 방식(겨냥·방사)과 무관한 "비행 성질"이라 투사체를 쏘는 모든 전달이 공유한다.
/// </summary>
[System.Serializable]
public class Homing
{
    [Tooltip("초당 회전 각도(도). 0이면 유도하지 않고 직진한다.\n" +
             "90 정도가 자연스럽고, 360이면 거의 즉시 꺾인다.")]
    public float turnSpeed = 0f;

    [Tooltip("날아가면서 목표를 찾을 반경(유닛). 0이면 발사 때 정해진 대상만 쫓는다.\n" +
             "Radial 은 발사 대상이 없으므로 이 값을 줘야 유도가 걸린다.")]
    public float seekRadius = 0f;

    [Tooltip("목표를 다시 고르는 주기(초). 짧을수록 정확하지만 그만큼 자주 검색한다.")]
    public float retargetInterval = 0.15f;

    public bool Enabled => turnSpeed > 0f;
}
