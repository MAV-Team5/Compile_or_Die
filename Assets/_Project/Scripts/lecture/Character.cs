using UnityEngine;

// MonoBehaviour 미상속 — 씬에 배치 불필요
// GameManager.instance가 null이면 속성값 기본 1 반환
public class Character
{
    // 이동 속도 배율 (캐릭터 0: 10% 증가)
    public static float Speed
    {
        get
        {
            if (GameManager.instance == null) return 1f;
            return GameManager.instance.playerId == 0 ? 1.1f : 1f;
        }
    }

    // 원거리 무기 발사 간격 배율 (캐릭터 1: 10% 단축)
    public static float WeaponSpeed
    {
        get
        {
            if (GameManager.instance == null) return 1f;
            return GameManager.instance.playerId == 1 ? 0.9f : 1f;
        }
    }

    // 무기 데미지 배율 (캐릭터 2: 20% 증가)
    public static float WeaponDamage
    {
        get
        {
            if (GameManager.instance == null) return 1f;
            return GameManager.instance.playerId == 2 ? 1.2f : 1f;
        }
    }

    // 무기 카운트 보정 — 곱하기 아닌 더하기 (캐릭터 3: +1)
    public static int WeaponCount
    {
        get
        {
            if (GameManager.instance == null) return 0;
            return GameManager.instance.playerId == 3 ? 1 : 0;
        }
    }
}
