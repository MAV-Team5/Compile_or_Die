using UnityEngine;

/// <summary>
/// 모든 피해가 통과하는 단일 관문.
/// 탐색 태그·비트·크리티컬 같은 보정이 전부 여기에 끼어든다.
/// </summary>
public static class DamagePipeline
{
    public static void Process(DamageContext dmg)
    {
        if (dmg == null || dmg.Target == null) return;

        // 1. 기본값에서 시작
        dmg.Amount = dmg.BaseAmount;

        // 2. 탐색 마커 보너스 — 마커 시스템 도입 시 여기에 연결
        // 3. 비트 마커 보너스 — Bitwise 도입 시 여기에 연결
        // 4. 크리티컬 판정 — 하드웨어 업그레이드 도입 시 여기에 연결

        if (dmg.Amount <= 0f) return;

        // 5. 적용
        dmg.Target.TakeDamage(dmg.Amount);

        // 6. 링크 전이 — Graph·Tree 도입 시 여기에 연결
    }
}
