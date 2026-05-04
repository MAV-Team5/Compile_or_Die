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
        // Image는 직접 이름으로 찾기 (GetComponentInChildren은 자기 자신도 포함)
        icon = transform.Find("Icon").GetComponent<Image>();

        // Text 배열: Hierarchy 순서 [0]=TextLevel [1]=TextName [2]=TextDesc
        Text[] texts = GetComponentsInChildren<Text>();
        textLevel = texts[0];
        textName  = texts[1];
        textDesc  = texts[2];
    }

    void OnEnable()
    {
        if (data == null) return;

        textLevel.text = string.Format("Lv.{0}", level + 1);
        textName.text  = data.itemName;
        icon.sprite    = data.itemIcon;

        switch (data.itemType)
        {
            case ItemType.Melee:
            case ItemType.Range:
                // damages/counts 배열 범위 체크
                if (data.damages != null && level < data.damages.Length)
                    textDesc.text = string.Format(data.itemDesc,
                        data.damages[level] * 100,
                        data.counts[level]);
                break;

            case ItemType.Glove:
            case ItemType.Shoe:
                if (data.damages != null && level < data.damages.Length)
                    textDesc.text = string.Format(data.itemDesc,
                        data.damages[level] * 100);
                break;

            default:
                // Heal 등 소비 아이템: 포맷 인자 없음
                textDesc.text = data.itemDesc;
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
                    // 새 오브젝트 생성 → Weapon 추가 → Init
                    // Init() 내부에서 player 참조를 GameManager에서 직접 받으므로 순서 무관
                    GameObject newWeaponObj = new GameObject("Weapon " + data.itemId);
                    weapon = newWeaponObj.AddComponent<Weapon>();
                    weapon.Init(data);
                }
                else
                {
                    if (data.damages != null && level < data.damages.Length)
                    {
                        float nextDamage = data.damages[level];
                        int   nextCount  = data.counts[level];
                        weapon.LevelUp(nextDamage, nextCount);
                    }
                }
                break;

            case ItemType.Glove:
            case ItemType.Shoe:
                // TODO: Gear 스크립트 구현 후 연동
                break;

            default:
                // 소비 아이템 — 체력 전체 회복
                GameManager.instance.health = GameManager.instance.maxHealth;
                break;
        }

        level++;

        // 만렙 체크 (damages 배열이 있는 아이템만)
        if (data.damages != null && data.damages.Length > 0
            && level >= data.damages.Length)
        {
            GetComponent<Button>().interactable = false;
        }

        GameManager.instance.uiLevelUp.Hide();
    }
}
