using UnityEngine;

// 씬에 오브젝트로 배치 불필요 - static 클래스로 어디서든 접근
public class Character
{
    // 이동 속도 배율
    public static float Speed
    {
        get { return GameManager.instance.playerId == 0 ? 1.1f : 1f; }
    }

    // 원거리 무기 발사 간격 배율 (낮을수록 빠름)
    public static float WeaponSpeed
    {
        get { return GameManager.instance.playerId == 1 ? 0.9f : 1f; }
    }

    // 무기 데미지 배율
    public static float WeaponDamage
    {
        get { return GameManager.instance.playerId == 2 ? 1.2f : 1f; }
    }

    // 무기 카운트 보정 (더하기)
    public static int WeaponCount
    {
        get { return GameManager.instance.playerId == 3 ? 1 : 0; }
    }
}
