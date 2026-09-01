using UnityEngine;

/// <summary>피해 숫자가 어떻게 보일지. 색·크기·튀는 높이.</summary>
[System.Serializable]
public struct DamageTextStyle
{
    public Color color;

    [Tooltip("글자 크기 배수. 1이면 프리팹 그대로.")]
    public float scale;

    [Tooltip("위로 떠오르는 속도. 클수록 멀리 튄다.")]
    public float riseSpeed;

    [Tooltip("뜨는 자리를 밀어낸다(유닛). 대상 머리 위가 기준.\n\n" +
             "★ 같은 순간에 두 숫자가 뜰 때 쓴다 — 추가 피해를 위로 올려두면 둘 다 읽힌다.\n" +
             "   자리에는 ±0.3 정도의 흔들림이 섞이므로 0.5 이상은 벌려야 확실히 갈라진다.")]
    public Vector2 offset;

    public static DamageTextStyle Default => new()
    {
        color = Color.white,
        scale = 1f,
        riseSpeed = 1.5f
    };

    /// <summary>0으로 남은 칸을 기본값으로 메운다. 인스펙터에서 색만 정해도 되게.</summary>
    public readonly DamageTextStyle Filled()
    {
        DamageTextStyle result = this;

        if (result.color.a <= 0.001f) result.color = Color.white;
        if (result.scale <= 0.001f) result.scale = 1f;
        if (result.riseSpeed <= 0.001f) result.riseSpeed = 1.5f;

        return result;
    }
}
