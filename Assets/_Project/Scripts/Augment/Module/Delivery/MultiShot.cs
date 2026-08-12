using UnityEngine;

/// <summary>한 타겟에 여러 발을 쏠 때의 배치 설정.</summary>
[System.Serializable]
public class MultiShot
{
    [Tooltip("타겟 1명당 몇 발. 0이면 시트의 수량(count)을 쓰고, 그것도 0이면 1발.")]
    public int shotsPerTarget = 1;

    [Tooltip("여러 발을 어떻게 배치할지. 나란히 또는 줄줄이.")]
    public ShotFormation formation = ShotFormation.Parallel;

    [Tooltip("발 사이 간격(유닛). 0이면 한 자리에서 겹쳐 나간다.")]
    public float spacing = 0.4f;

    [Tooltip("발 사이 각도(도). 0이면 완전히 평행, 값을 주면 부채꼴로 퍼진다.")]
    public float spreadPerShot = 0f;

    /// <summary>실제로 쏠 발 수. 0 규칙을 여기서 푼다.</summary>
    public int Resolve(AugmentContext ctx)
    {
        int shots = shotsPerTarget > 0 ? shotsPerTarget : ctx.Stat.count;
        return shots > 0 ? shots : 1;
    }
}
