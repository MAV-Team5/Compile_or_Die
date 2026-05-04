using UnityEngine;

/// <summary>
/// 아이템 종류 열거형
/// ItemData.itemType에 사용
/// int 값이 Player.hands[] 배열 인덱스와 일치 (Melee=0=왼손, Range=1=오른손)
/// </summary>
public enum ItemType
{
    Melee,  // 0: 근접무기 (왼손)
    Range,  // 1: 원거리무기 (오른손)
    Glove,  // 2: 장갑 (연사속도)
    Shoe,   // 3: 신발 (이동속도)
    Heal    // 4: 소비 아이템 (체력 회복)
}

/// <summary>
/// 아이템 데이터 ScriptableObject
/// Assets/Data/ 폴더에 *.asset 파일로 생성
/// Create > ScriptableObjects > ItemData 메뉴 사용
/// </summary>
[CreateAssetMenu(fileName = "Item", menuName = "ScriptableObjects/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("# 기본 정보")]
    public int      itemId;     // 아이템 고유 ID (Weapon.id, PoolManager.prefabId와 일치)
    public string   itemName;   // 아이템 이름 (UI 표시용)
    public ItemType itemType;   // 아이템 종류

    [Header("# 무기 설정 (Melee/Range만 사용)")]
    public int    prefabId;     // PoolManager.prefabs 배열 인덱스
    public Sprite hand;         // 손에 표시할 무기 스프라이트
    public float[] damages;     // 레벨별 데미지 배율 [레벨0, 레벨1, ...]
    public int[]   counts;      // 레벨별 카운트 [근접=배치수, 원거리=관통력]

    [Header("# 아이콘")]
    public Sprite itemIcon;     // 레벨업 창에 표시할 아이템 아이콘

    [Header("# 설명")]
    [TextArea]                  // Inspector에서 여러 줄 입력 가능
    // string.Format 포맷 사용 가능
    // 예: "Damage {0}% 증가\n회전체 {1}개 추가"
    // {0}: damages[level]*100, {1}: counts[level]
    public string itemDesc;
}
