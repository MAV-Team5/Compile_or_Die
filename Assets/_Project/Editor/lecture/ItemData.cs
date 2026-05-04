using UnityEngine;

public enum ItemType { Melee, Range, Glove, Shoe, Heal }

[CreateAssetMenu(fileName = "Item", menuName = "ScriptableObjects/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("# Base Info")]
    public int      itemId;
    public string   itemName;
    public ItemType itemType;
    public Sprite   itemIcon;

    [Header("# Weapon Info")]
    public int      prefabId;
    public Sprite   hand;
    public float[]  damages;
    public int[]    counts;

    [Header("# Description")]
    [TextArea]
    public string itemDesc;
}
