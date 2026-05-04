using UnityEngine;

/// <summary>
/// 캐릭터별 특성 수치를 제공하는 정적 클래스
/// MonoBehaviour 미상속 → 씬에 배치 불필요
/// Character.Speed 처럼 어디서든 바로 접근
/// </summary>
public class Character
{
    /// <summary>
    /// 이동 속도 배율
    /// 캐릭터 0(쌀농부): 1.1 (10% 증가)
    /// 나머지: 1.0 (변화 없음)
    /// </summary>
    public static float Speed
    {
        get
        {
            // GameManager가 아직 초기화 안 됐을 때 안전하게 기본값 반환
            if (GameManager.instance == null) return 1f;
            return GameManager.instance.playerId == 0 ? 1.1f : 1f;
        }
    }

    /// <summary>
    /// 원거리 무기 발사 간격 배율 (낮을수록 빠름)
    /// 캐릭터 1(보리농부): 0.9 (10% 단축)
    /// 나머지: 1.0
    /// </summary>
    public static float WeaponSpeed
    {
        get
        {
            if (GameManager.instance == null) return 1f;
            return GameManager.instance.playerId == 1 ? 0.9f : 1f;
        }
    }

    /// <summary>
    /// 무기 데미지 배율
    /// 캐릭터 2(감자농부): 1.2 (20% 증가)
    /// 나머지: 1.0
    /// </summary>
    public static float WeaponDamage
    {
        get
        {
            if (GameManager.instance == null) return 1f;
            return GameManager.instance.playerId == 2 ? 1.2f : 1f;
        }
    }

    /// <summary>
    /// 무기 카운트 보정 (곱하기 아닌 더하기로 적용)
    /// 캐릭터 3(콩농부): +1 (회전체/관통력 1개 추가)
    /// 나머지: 0
    /// </summary>
    public static int WeaponCount
    {
        get
        {
            if (GameManager.instance == null) return 0;
            return GameManager.instance.playerId == 3 ? 1 : 0;
        }
    }
}
