using UnityEngine;
using UnityEngine.UI;

public class Item : MonoBehaviour
{
    public ItemData data;
    public int level;
    public Weapon weapon;

    Image icon;
    Text textLevel;
    Text textName;
    Text textDesc;

    void Awake()
    {
        icon      = GetComponentInChildren<Image>();
        Text[] texts = GetComponentsInChildren<Text>();
        textLevel = texts[0];
        textName  = texts[1];
        textDesc  = texts[2];
    }

    void OnEnable()
    {
        textLevel.text = string.Format("Lv.{0}", level + 1);
        textName.text  = data.itemName;
        icon.sprite    = data.itemIcon;

        switch (data.itemType)
        {
            case ItemType.Melee:
            case ItemType.Range:
                textDesc.text = string.Format(data.itemDesc,
                    data.damages[level] * 100,
                    data.counts[level]);
                break;
            case ItemType.Glove:
            case ItemType.Shoe:
                textDesc.text = string.Format(data.itemDesc,
                    data.damages[level] * 100);
                break;
            default:
                textDesc.text = string.Format(data.itemDesc);
                break;
        }
    }

    public void OnClick()
    {
        switch (data.itemType)
        {
            case ItemType.Melee:
            case ItemType.Range:
                if (level == 0)
                {
                    GameObject newWeapon = new GameObject();
                    weapon = newWeapon.AddComponent<Weapon>();
                    weapon.Init(data);
                }
                else
                {
                    float nextDamage = data.damages[level];
                    int   nextCount  = data.counts[level];
                    weapon.LevelUp(nextDamage, nextCount);
                }
                break;

            case ItemType.Glove:
            case ItemType.Shoe:
                // 기어 레벨업 로직 (Gear 스크립트와 연동)
                break;

            default:
                // 소비 아이템 (체력 회복 등)
                GameManager.instance.health = GameManager.instance.maxHealth;
                break;
        }

        level++;

        if (level == data.damages.Length)
        {
            GetComponent<Button>().interactable = false;
        }

        GameManager.instance.uiLevelUp.Hide();
    }
}
