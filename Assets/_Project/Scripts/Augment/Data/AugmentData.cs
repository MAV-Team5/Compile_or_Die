using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Augment", menuName = "CoD/Augment")]
public class AugmentData : ScriptableObject
{
    [Header("Augment")]
    public string id;
    public string displayName;
    public AugmentCategory category;
    public Sprite icon;

    [Header("description")]
    [TextArea(2,4)]
    public string descriptionTemplate;

    [Header("Inner Augment")]
    public AugmentData rootAugment;
    public int requiredRootLevel;

    [Header("Level Stat")]
    public AugmentLevelData[] levelStats;

    [Header("Module")]
    [SerializeReference] public TriggerModule trigger;
    [SerializeReference] public TargetingModule targeting;
    [SerializeReference] public List<DeliveryModule> deliveries = new();
    [SerializeReference] public List<EffectModule> effects = new();

    [Header("Visual")]
    public GameObject projectilePrefab;
    public GameObject vfxPrefab;
}