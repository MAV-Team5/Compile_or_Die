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
/// <b>수치는 뿌리 것을 쓴다.</b> <see cref="AugmentContext"/> 를 그대로 넘기므로
/// 내부 증강의 <c>damageScale 3</c> 은 뿌리의 피해량에 곱해진다 —
/// 뿌리를 올리면 평타가 세지고, 내부를 올리면 배수만 커지는 구조.
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

        // 같은 적중에 얹기만 한다. 새로 타겟팅하면 "추가 효과" 가 아니라 별개의 공격이 된다
        for (int i = 0; i < effects.Count; i++) effects[i]?.Apply(ctx, hit);
    }
}
