using System.Collections.Generic;

/// <summary>내부 증강이 뿌리의 조립 한 축을 어떻게 건드리나.</summary>
public enum BuildPatch
{
    /// <summary>안 건드린다. 뿌리 것을 그대로 쓴다.</summary>
    None,

    /// <summary>뿌리 것을 버리고 이 증강 것으로 갈아끼운다.</summary>
    Replace,

    /// <summary>뿌리 것 뒤에 이어 붙인다. 목록인 축(전달·효과)에서만 뜻이 있다.</summary>
    Add
}

/// <summary>
/// 이번 발동에 실제로 쓸 3축. 뿌리 조립에 내부 증강이 덮거나 더한 결과다.
///
/// <b>뿌리 에셋을 절대 수정하지 않는다.</b> ScriptableObject 를 런타임에 고치면
/// 플레이를 끝낸 뒤에도 그 값이 에셋에 남는다 — 조용히 밸런스가 변해 있게 된다.
/// 그래서 합쳐진 결과를 여기 따로 담고, 원본은 읽기만 한다.
///
/// 증강을 뽑거나 레벨업할 때만 다시 만든다. 매 프레임 만들면 목록 복사가 낭비다.
/// </summary>
public struct AugmentBuild
{
    public TargetingModule Targeting;
    public List<DeliveryModule> Deliveries;
    public List<EffectModule> Effects;

    /// <summary>아무도 안 덮은 상태. 뿌리 조립 그대로.</summary>
    public static AugmentBuild Of(AugmentData data) => new()
    {
        Targeting = data.targeting,
        Deliveries = data.deliveries,
        Effects = data.effects
    };
}
