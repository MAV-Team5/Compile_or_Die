using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <b>한 주기의 첫 발에만</b> 안에 든 효과를 낸다. 나머지 발에서는 아무 일도 안 한다.
///
/// <code>
///   do { 강화된 첫 발 } while (조건)
///        탕!  탕 · 탕 · 탕      ← 첫 발만 굵다
/// </code>
///
/// <b>왜 별도 모듈인가</b> — "첫 발인가" 는 트리거만 아는 사실이고(남은 탄),
/// 효과 목록은 그걸 볼 방법이 없다. 조건을 감싸는 모듈을 하나 두면
/// 안에 무엇을 넣든(피해·상태이상·연출) 같은 규칙으로 걸러진다.
///
/// 주기 개념이 없는 트리거(주기 발동·탐색 반응형)에서는 항상 첫 발이라 늘 실행된다 —
/// 그런 증강에 달면 조건이 없는 것과 같아지므로 장탄식에만 쓸 것.
/// </summary>
[System.Serializable]
[ModuleInfo("한 주기의 첫 발에만 실행", "장탄식(Burst) 트리거에서만 뜻이 있다")]
public class FirstShotEffect : EffectModule
{
    [Tooltip("뒤집으면 <b>첫 발을 뺀 나머지</b>에만 실행된다.\n" +
             "\"첫 발은 약하고 뒤로 갈수록 세진다\" 같은 반대 설계에 쓴다.")]
    public bool invert = false;

    [Tooltip("첫 발일 때 실행할 효과들. 여기 넣은 것이 그대로 순서대로 돈다.")]
    [SerializeReference] public List<EffectModule> effects = new();

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        if (hit.Target == null) return;

        if (ctx.FirstOfCycle == invert) return;

        for (int i = 0; i < effects.Count; i++) effects[i]?.Apply(ctx, hit);
    }
}
