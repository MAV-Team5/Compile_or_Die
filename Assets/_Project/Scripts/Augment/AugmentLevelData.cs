[System.Serializable]
public struct AugmentLevelData
{
    public float damage;        // 직접 피해량
    public float effectDamage;  // 부가 피해 — 탐색 추가피해 / 전이 / 체인 증폭
    public float cooldown;      // 쿨타임 (초)
    public float range;         // 범위
    public int   count;         // 수량 — 투사체 수 / 최대 스택량 / 연쇄 횟수
    public float duration;      // 유지 시간 (초)
    public int   depth;         // 깊이 — 탐색 전파 / 트리 깊이
}