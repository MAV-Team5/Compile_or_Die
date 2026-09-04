using UnityEngine;

/// <summary>
/// 시트 수치를 어떻게 바꿔 쓸지 적어두는 칸. 인스펙터에서 한 줄로 보인다.
///
///     [ 고정값 ]  ×  [ 배수 ]  +  [ 가감 ]
///
/// 값·배수는 0이 "손대지 않음"이다. 프로젝트의 다른 오버라이드와 같은 규칙.
/// 가감은 0이 그냥 0인데, 안 더하는 것과 0을 더하는 것이 같은 뜻이라 헷갈리지 않는다.
///
///     0  ×  0     +  0     시트값 그대로
///     0  ×  0.5   +  0     시트값의 절반 — 레벨업하면 같이 자란다
///     0  ×  1     + -1     시트값보다 하나 적게
///     0  ×  0.5   +  1     절반보다 하나 많게
///     3  ×  0     +  0     고정 3
///     3  ×  2     +  0     고정 6
///
/// <b>1차식까지만 적는다.</b> 제곱이나 조건 분기가 필요해지면 그건 수치가 아니라 동작이므로
/// 모듈로 만들어야 한다 — 수식을 이 칸에 밀어 넣으면 인스펙터만 봐서는 결과를 알 수 없게 된다.
///
/// 효과 범위 하나를 폭발·전파·표식이 나눠 쓸 때, 각자 배수만 달리 주면
/// 전부 레벨을 따라 같이 자란다.
/// </summary>
[System.Serializable]
public struct Scalable
{
    [Tooltip("고정값. 0이면 시트 수치를 쓴다.")]
    public float value;

    [Tooltip("곱할 배수. 0이면 배수 없음(1배).")]
    public float scale;

    [Tooltip("곱한 뒤 더할 값. 음수면 뺀다.\n" +
             "＊ 정수 수치(수량·관통·깊이)는 아무리 빼도 1 아래로 안 내려간다 —\n" +
             "  0이 되면 그 모듈이 통째로 아무 일도 안 하게 되기 때문.")]
    public float offset;

    /// <summary>시트 수치를 받아 실제로 쓸 값을 돌려준다.</summary>
    public readonly float Of(float sheetValue)
    {
        float baseValue = value > 0f ? value : sheetValue;
        float factor = scale > 0f ? scale : 1f;

        return baseValue * factor + offset;
    }

    /// <summary>
    /// 정수로 쓰는 값. 발사 수·관통·깊이처럼 반개가 없는 것들.
    /// 반올림하되 0으로는 안 떨어뜨린다 — 0.4발을 쏘려던 것이 0발이 되면 조용히 사라진다.
    /// </summary>
    public readonly int IntOf(int sheetValue, int fallback = 1)
    {
        float raw = Of(sheetValue);

        if (raw <= 0f) return fallback;

        return Mathf.Max(1, Mathf.RoundToInt(raw));
    }

    /// <summary>아무것도 안 적힌 상태인가. 경고 표시에 쓴다.</summary>
    public readonly bool IsUntouched
        => value <= 0f && scale <= 0f && Mathf.Approximately(offset, 0f);

    public static Scalable Fixed(float amount) => new() { value = amount };
    public static Scalable Ratio(float factor) => new() { scale = factor };

    /// <summary>시트값에 더하거나 뺀다. "본체보다 하나 적게" 같은 것.</summary>
    public static Scalable Shift(float amount) => new() { offset = amount };
}
