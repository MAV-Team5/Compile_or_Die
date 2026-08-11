/// <summary>
/// 레벨 1칸분 수치. 시트가 채우는 유일한 영역이다.
/// count 는 "몇 개", depth 는 "몇 단계". 둘을 섞지 말 것.
/// </summary>
[System.Serializable]
public struct AugmentLevelData
{
    public float damage;        // 직접 피해량
    public float effectDamage;  // 부가 피해 — 탐색 추가피해 / 전이
    public float cooldown;      // 쿨타임 (초)
    public float range;         // 사거리
    public int   count;         // 수량 — 타겟 수 / 투사체 수 / 좌표 수 / 스택량
    public float duration;      // 유지 시간 (초)
    public int   depth;         // 깊이 — 연쇄 단계 / 탐색 전파 / 트리 깊이
}
