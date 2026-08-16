using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레벨/보유 개념 없이 선택 즉시 1회 적용되는 아이템 효과.
/// None이 아니면 AugmentManager에 등록되지 않고, 선택 즉시 소비된 뒤 사라진다 —
/// 그래서 항상 "새 증강"으로 취급되어 몇 번이고 다시 뜰 수 있다.
/// </summary>
public enum InstantItemEffect
{
    None,

    /// <summary>최대 체력의 instantValue 비율만큼 즉시 회복.</summary>
    Heal,

    /// <summary>instantDuration초 동안 이동속도를 (1 + instantValue)배로.</summary>
    SpeedBoost
}

/// <summary>
/// 증강 1종의 설계도. 런타임에 변하지 않는다.
/// 투사체·이펙트 프리팹은 각 모듈이 직접 들고 있다.
/// </summary>
[CreateAssetMenu(fileName = "Augment", menuName = "CoD/Augment")]
public class AugmentData : ScriptableObject
{
    [Header("정체성")]
    [Required("시트 임포터가 이 증강을 찾지 못한다")]
    [Tooltip("시트와 잇는 유일 키. 한번 정하면 바꾸지 말 것.")]
    public string id;

    public string displayName;
    public AugmentCategory category;

    [Tooltip("HUD 슬롯에 표시할 아이콘.")]
    public Sprite icon;

    [Header("설명")]
    [Tooltip("{damage} {count} 같은 토큰이 현재 레벨 수치로 치환된다.")]
    [TextArea(2, 4)]
    public string descriptionTemplate;

    [Header("내부 증강")]
    [Tooltip("뿌리 증강. 비어있으면 이 증강 자체가 뿌리다.")]
    public AugmentData rootAugment;

    [Tooltip("뿌리가 이 레벨 이상일 때 해금된다.")]
    public int requiredRootLevel;

    [Header("성장")]
    [Tooltip("＊ 필수 — 레벨별 수치. 비면 증강이 전혀 동작하지 않는다.\n" +
             "시트 임포터가 덮어쓰는 유일한 영역.")]
    public AugmentLevelData[] levelStats;

    [Header("모듈 조립")]
    [SerializeReference] public TriggerModule trigger;
    [SerializeReference] public TargetingModule targeting;
    [SerializeReference] public List<DeliveryModule> deliveries = new();
    [SerializeReference] public List<EffectModule> effects = new();

    [Tooltip("대상이 없을 때 쿨타임을 유지할지 버릴지.")]
    public NoTargetPolicy noTargetPolicy = NoTargetPolicy.Hold;

    [Header("즉시 효과 (레벨 없는 소모성 아이템 전용)")]
    [Tooltip("None이 아니면 이 증강은 무기로 등록되지 않고 선택 즉시 효과만 적용된다.\n" +
             "levelStats·모듈 조립은 쓰지 않는다.")]
    public InstantItemEffect instantEffect = InstantItemEffect.None;

    [Tooltip("Heal: 최대 체력에 곱할 비율(0.2 = 20%). SpeedBoost: 배율에 더할 비율(0.2 = 120%).")]
    public float instantValue;

    [Tooltip("SpeedBoost 지속시간(초). Heal은 쓰지 않는다.")]
    public float instantDuration;
}
