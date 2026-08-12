using UnityEngine;

/// <summary>
/// 레벨 1칸분 수치. 시트가 채우는 유일한 영역이다.
///
/// 거리는 둘로 나뉜다.
///   range        발동 사거리 — 대상을 찾고 거기까지 도달하는 거리
///   effectRange  효과 범위   — 닿은 뒤 퍼지는 크기
///
/// 모듈 필드가 0이면 여기 값을 쓴다. 안 쓰는 항목은 0으로 비워두면 된다.
/// </summary>
[System.Serializable]
public struct AugmentLevelData
{
    [Tooltip("피해량 — 직접 때리는 피해.\n" +
             "Damage 효과가 이 값에 damageScale 을 곱해 넣는다.")]
    public float damage;

    [Tooltip("효과 피해 — 직접 때리지 않는 피해.\n" +
             "탐색 표식의 추가 피해, 트리의 전이 피해 등이 여기서 온다.\n" +
             "탐색 계열은 피해량보다 이쪽이 주력이다.")]
    public float effectDamage;

    [Tooltip("쿨타임(초) — 발동 간격.\n" +
             "0이면 아예 발동하지 않는다. 반드시 채울 것.")]
    public float cooldown;

    [Tooltip("사거리(유닛) — 대상에게 '닿기까지'의 거리.\n" +
             "타겟팅이 적을 찾는 범위이자 투사체가 날아가는 거리다.\n" +
             "닿은 뒤 퍼지는 크기는 효과 범위가 맡는다.")]
    public float range;

    [Tooltip("효과 범위(유닛) — 닿은 '뒤' 퍼지는 크기.\n" +
             "폭발 반경, 탐색 전파 반경이 여기서 온다.\n" +
             "하위 파이프라인(SubPipeline·Chain 안)은 사거리 대신 이 값을 기준으로 삼는다.\n" +
             "0이면 사거리로 대신한다.")]
    public float effectRange;

    [Tooltip("수량 — '몇 개'.\n" +
             "타겟 수 · 투사체 수 · 좌표 수 · 스택량.\n" +
             "거리나 단계가 아니다. 깊이와 섞지 말 것.")]
    public int count;

    [Tooltip("관통력 — 하나가 뚫고 지나가는 적 수.\n" +
             "투사체 관통 수, 레이저의 최대 적중 수.\n" +
             "1이면 첫 적중에서 멈춘다.")]
    public int pierce;

    [Tooltip("지속시간(초) — 효과가 남아있는 시간.\n" +
             "탐색 표식 유지 시간 등. 0이면 무기한이거나 즉시 끝난다(모듈마다 다름).")]
    public float duration;

    [Tooltip("속도(초당 유닛) — 투사체 비행 속도.\n" +
             "즉발·폭발·레이저 계열은 쓰지 않는다.")]
    public float speed;

    [Tooltip("깊이 — '몇 단계'.\n" +
             "연쇄가 이어지는 단계 수, 트리 계층.\n" +
             "거리가 아니다. 최대 8까지만 유효하다.")]
    public int depth;
}
