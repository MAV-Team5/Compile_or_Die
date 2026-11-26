using UnityEngine;

/// <summary>
/// 투사체가 진행 방향의 좌우로 흔들리며 S자로 나는 설정.
/// 유도·사거리는 흔들림 없는 중심선 기준이라, 흔들려도 목표를 놓치지 않는다.
/// </summary>
[System.Serializable]
public class Wave
{
    [Tooltip("좌우로 흔들리는 폭(유닛). 0이면 직진한다.")]
    public float amplitude = 0f;

    [Tooltip("초당 S 반복 횟수.")]
    public float frequency = 2.5f;

    [Tooltip("발사 직후 이 시간(초) 동안 흔들림을 0에서 키운다. 생성 위치에서 튀는 것을 막는다.")]
    public float rampIn = 0.15f;

    [Tooltip("켜면 함께 쏜 발 중 홀수 번째가 반대로 흔들린다 — 두 발이 서로 꼬이며 난다.\n" +
             "다중 발사 간격을 0으로 두면 한 점에서 출발해 교차한다.")]
    public bool mirrorPerShot = true;

    [Tooltip("켜면 발마다 흔들림 시작 위치가 무작위. 미러와 같이 켜면 꼬임이 어긋난다.")]
    public bool randomPhase = false;

    public bool Enabled => amplitude > 0f && frequency > 0f;
}
