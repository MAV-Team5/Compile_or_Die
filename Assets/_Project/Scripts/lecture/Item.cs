using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레벨업 창의 개별 아이템 버튼 스크립트
/// OnEnable: 활성화될 때 UI 텍스트/아이콘 초기화
/// OnClick: 버튼 클릭 시 실제 능력치 적용
/// </summary>
public class Item : MonoBehaviour
{
    [Header("# 아이템 정보")]
    public ItemData data;   // Inspector에서 ScriptableObject 연결
    public int level;       // 현재 아이템 레벨 (0부터 시작)
    public Weapon weapon;   // 무기 아이템일 때 생성된 Weapon 컴포넌트 참조

    Image icon;             // 아이템 아이콘 이미지
    Text textLevel;         // 레벨 텍스트 (예: "Lv.1")
    Text textName;          // 아이템 이름 텍스트
    Text textDesc;          // 아이템 설명 텍스트

    void Awake()
    {
        // "Icon" 이름의 자식 오브젝트에서 Image 컴포넌트 가져오기
        // GetComponentInChildren 대신 Find 사용: Hierarchy 순서에 의존하지 않게
        Transform iconTransform = transform.Find("Icon");
        if (iconTransform != null)
            icon = iconTransform.GetComponent<Image>();

        // Text 배열: Hierarchy 순서 기준 [0]=Level [1]=Name [2]=Desc
        Text[] texts = GetComponentsInChildren<Text>();
        if (texts.Length >= 3)
        {
            textLevel = texts[0];
            textName  = texts[1];
            textDesc  = texts[2];
        }
    }

    void OnEnable()
    {
        // 활성화될 때마다 현재 레벨에 맞는 UI로 갱신
        if (data == null) return;

        // 아이콘 및 이름 설정
        if (icon != null)      icon.sprite   = data.itemIcon;
        if (textName != null)  textName.text = data.itemName;
        if (textLevel != null) textLevel.text = string.Format("Lv.{0}", level + 1);

        // 설명 텍스트: 아이템 타입에 따라 string.Format 인자 다르게 전달
        if (textDesc == null) return;

        switch (data.itemType)
        {
            case ItemType.Melee:
            case ItemType.Range:
                // {0}: 데미지(%), {1}: 카운트(개)
                if (data.damages != null && level < data.damages.Length)
                    textDesc.text = string.Format(data.itemDesc,
                        data.damages[level] * 100,  // 0.5 → 50%
                        data.counts[level]);
                break;

            case ItemType.Glove:
            case ItemType.Shoe:
                // {0}: 스탯 증가율(%)
                if (data.damages != null && level < data.damages.Length)
                    textDesc.text = string.Format(data.itemDesc,
                        data.damages[level] * 100);
                break;

            default:
                // 소비 아이템: 포맷 인자 없음
                textDesc.text = data.itemDesc;
                break;
        }
    }

    /// <summary>
    /// 아이템 버튼 클릭 시 호출 (Button 컴포넌트 OnClick 이벤트에 연결)
    /// 아이템 타입에 따라 무기 생성/강화 또는 스탯 적용
    /// </summary>
    public void OnClick()
    {
        switch (data.itemType)
        {
            case ItemType.Melee:
            case ItemType.Range:
                if (level == 0)
                {
                    // 첫 획득: 새 오브젝트 생성 후 Weapon 컴포넌트 추가 → Init
                    // Init() 내부에서 GameManager.instance.player 직접 참조하므로 안전
                    GameObject newWeaponObj = new GameObject("Weapon " + data.itemId);
                    weapon = newWeaponObj.AddComponent<Weapon>();
                    weapon.Init(data);
                }
                else
                {
                    // 레벨업: 데미지·카운트 증가
                    if (data.damages != null && level < data.damages.Length)
                        weapon.LevelUp(data.damages[level], data.counts[level]);
                }
                break;

            case ItemType.Glove:
            case ItemType.Shoe:
                // TODO: Gear 스크립트 구현 후 연동
                break;

            default:
                // 소비 아이템: 체력 전체 회복
                GameManager.instance.health = GameManager.instance.maxHealth;
                break;
        }

        level++; // 레벨 증가

        // 만렙 도달 시 버튼 비활성화 (더 이상 선택 불가)
        if (data.damages != null && data.damages.Length > 0
            && level >= data.damages.Length)
        {
            Button btn = GetComponent<Button>();
            if (btn != null) btn.interactable = false;
        }

        GameManager.instance.uiLevelUp.Hide(); // 레벨업 창 닫기
    }
}
