using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 내부 증강이 끼어들 자리. 뿌리 증강이 <b>"여기에 꽂힌다"를 자기 조립에 미리 표시</b>한다.
///
/// <code>
/// Bash.asset
///   Chain
///     Final Effects[]
///       ExtensionEffect(slot = "kill")   ← 자리만 비워둔다
///
/// Bash_kill.asset
///   Root Augment     Bash
///   Extension Slot   kill               ← 이 자리에 꽂힌다
///   Effects[]        DamageEffect ×3
/// </code>
///
/// <b>런타임에 붙지만 자리는 에셋에 보인다.</b> 아무 데나 끼어드는 방식이면
/// 무엇이 언제 실행되는지 추적할 수가 없다 — 뿌리가 허락한 자리에만 꽂히게 한 이유다.
///
/// <b>수치는 내부 증강 자기 것을 쓴다.</b> 뿌리의 컨텍스트를 그대로 넘기면
/// 내부 증강의 시트가 통째로 무시되어 <b>레벨을 올려도 아무것도 안 바뀐다</b> —
/// 예전에는 그렇게 돌았고, 그래서 내부 증강에 성장이 없었다.
///
/// 그렇다고 스탯을 뿌리에 접어 넣으면 뿌리의 평타까지 세진다. 둘 다 아니라서,
/// <see cref="AugmentContext.BeginExtension"/> 로 <b>상황은 물려주고 수치만 갈아끼운다.</b>
///
/// 뿌리가 방금 준 피해는 <see cref="AugmentContext.LastDamage"/> 로 전해지므로,
/// <see cref="RelayDamageEffect"/> 를 쓰면 "그 피해의 30%" 같은 것이 된다.
/// </summary>
[System.Serializable]
[ModuleInfo("내부 증강이 꽂히는 자리", "뽑지 않았으면 아무 일도 안 한다")]
public class ExtensionEffect : EffectModule
{
    [Tooltip("이 자리의 이름. 내부 증강의 Extension Slot 과 글자가 같아야 한다.\n\n" +
             "한 뿌리에 자리가 여럿일 수 있어 이름으로 구분한다. 예) kill · redirect")]
    public string slot = "";

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        if (hit.Target == null || ctx.Instance == null) return;

        if (string.IsNullOrEmpty(slot))
        {
            ModuleWarning.Once(ctx, "확장 슬롯 이름이 비어 있다. 내부 증강이 절대 안 꽂힌다.");
            return;
        }

        AugmentManager manager = AugmentManager.Current;
        if (manager == null) return;

        AugmentRunner inner = manager.FindExtension(ctx.Instance.Data, slot);

        // 아직 안 뽑았다. 흔한 상태이므로 조용히 넘어간다
        if (inner == null || inner.Instance == null) return;

        List<EffectModule> effects = inner.Instance.Data.effects;
        if (effects == null) return;

        // 수치만 내부 증강 것으로 갈아끼운 컨텍스트. 상황(대상·방향·직전 피해)은 그대로 물려받는다
        var sub = new AugmentContext();
        sub.BeginExtension(ctx, inner.Instance);

        // 같은 적중에 얹기만 한다. 새로 타겟팅하면 "추가 효과" 가 아니라 별개의 공격이 된다
        for (int i = 0; i < effects.Count; i++) effects[i]?.Apply(sub, hit);

        // 내부 증강이 또 때렸으면 그 값이 최신이다. 뿌리 쪽으로 돌려줘야
        // 뒤에 오는 효과가 "직전 피해" 를 물었을 때 어긋나지 않는다
        ctx.LastDamage = sub.LastDamage;
    }
}
