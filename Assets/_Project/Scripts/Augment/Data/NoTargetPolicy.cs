public enum NoTargetPolicy
{
    Hold,     // 쿨타임 유지하고 대기 — 대상 생기면 즉시 발동
    Consume   // 대상 없어도 쿨타임 소모 (기존 Weapon 방식)
}