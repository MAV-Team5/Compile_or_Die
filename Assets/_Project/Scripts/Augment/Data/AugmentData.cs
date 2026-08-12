using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 증강 1종의 설계도. 런타임에 변하지 않는다.
/// 투사체·이펙트 프리팹은 각 모듈이 직접 들고 있다.
/// </summary>
[CreateAssetMenu(fileName = "Augment", menuName = "CoD/Augment")]
public class AugmentData : ScriptableObject
{
    [Header("정체성")]
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
    [Tooltip("레벨별 수치. 시트 임포터가 덮어쓰는 유일한 영역.")]
    public AugmentLevelData[] levelStats;

    [Header("모듈 조립")]
    [SerializeReference] public TriggerModule trigger;
    [SerializeReference] public TargetingModule targeting;
    [SerializeReference] public List<DeliveryModule> deliveries = new();
    [SerializeReference] public List<EffectModule> effects = new();

    [Tooltip("대상이 없을 때 쿨타임을 유지할지 버릴지.")]
    public NoTargetPolicy noTargetPolicy = NoTargetPolicy.Hold;
}
