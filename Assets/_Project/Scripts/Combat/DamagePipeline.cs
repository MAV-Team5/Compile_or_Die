using UnityEngine;

/// <summary>
/// 모든 피해가 통과하는 단일 관문.
/// 탐색 표식·비트·크리티컬 같은 보정이 전부 여기에 끼어든다.
/// 피해 숫자를 띄우는 것도 여기 몫이다 — 맥락(누가·크리티컬인지)을 아는 유일한 자리라서.
/// </summary>
public static class DamagePipeline
{
    public static void Process(DamageContext dmg)
    {
        if (dmg == null || dmg.Target == null) return;

        // 무적이면 여기서 끝낸다. 표식 소비도 숫자 표시도 하지 않아야
        // "막혔다" 가 화면에 정직하게 보인다
        if (!dmg.Target.AcceptsDamage) return;

        // 1. 기본값에서 시작
        dmg.Amount = dmg.BaseAmount;

        // 2. 탐색 표식 보너스. 여러 표식이 겹치면 각각 다 더해진다
        if (dmg.TargetTransform != null &&
            dmg.TargetTransform.TryGetComponent(out MarkerHolder marks))
        {
            dmg.Amount += marks.Consume(dmg.BaseAmount);
        }

        // 3. 비트 표식 보너스 — Bitwise 도입 시 여기에 연결
        // 4. 크리티컬 판정 — 마우스 업그레이드 도입 시 여기에 연결

        // 5. 하드웨어(파워) 최종 배율.
        //
        //    ＊ 반드시 표식·비트 보정을 다 더한 뒤에 곱한다.
        //      먼저 곱하면 표식의 고정 추가피해가 배율을 못 받아
        //      (10×1.2)+5 = 17 과 (10+5)×1.2 = 18 로 결과가 갈린다.
        //
        //    전이된 피해는 이미 곱해진 값을 물려받으므로 여기서 다시 곱하면 두 번 먹는다.
        //    간선을 다섯 번만 타도 2.5배가 되어 파워가 기하급수로 불어난다
        if (!dmg.HardwareApplied)
        {
            dmg.Amount *= HardwareBonus.DamageMultiplier;
            dmg.HardwareApplied = true;
        }

        if (dmg.Amount <= 0f) return;

        // 5. 링크 전이. 이어진 이웃에게 번진다 —
        //    전이된 피해도 이 파이프라인을 다시 통과하므로 표식 보정과 분류 색이 그대로 붙는다.
        //
        //    ＊ 반드시 피해를 적용하기 전에 흘려보낸다. 이 피해로 대상이 죽으면
        //      OnDisable 이 간선을 전부 끊어서, 센 공격일수록 전이가 안 되는 거꾸로가 된다.
        //      전이량은 4단계까지 이미 확정돼 있으므로 순서를 앞당겨도 값은 같다.
        if (dmg.TargetTransform != null &&
            dmg.TargetTransform.TryGetComponent(out LinkHolder links))
        {
            links.Propagate(dmg);
        }

        // 6. 적용
        dmg.Target.TakeDamage(dmg.Amount);

        // 7. 증강별 집계. 모든 피해가 여기를 지나므로 이 한 줄이면 증강이 늘어도 따라온다
        RunStats.Record(dmg.SourceAugment, dmg.Amount);

        // 7-2. 피해를 모아두는 증강(스택)에게도 같은 이유로 여기서 흘려준다
        AugmentDamageLog.Record(dmg);

        // 8. 숫자 표시. 여기서 해야 크리티컬·증강 분류에 따라 색을 고를 수 있다
        ShowNumber(dmg);
    }

    static void ShowNumber(DamageContext dmg)
    {
        if (dmg.TargetTransform == null) return;

        DamageTextPalette palette = DamageTextSpawner.Palette;

        DamageTextStyle style = palette != null
            ? palette.Resolve(dmg)
            : DamageTextStyle.Default;

        DamageTextSpawner.Show(dmg.Amount, dmg.TargetTransform, style);
    }
}
