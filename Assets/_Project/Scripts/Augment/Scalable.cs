using UnityEngine;

/// <summary>
/// 시트 수치를 어떻게 바꿔 쓸지 적어두는 칸. 인스펙터에서 한 줄로 보인다.
///
///     [ 고정값 ]  ×  [ 배수 ]
///
/// 둘 다 0이 "손대지 않음"이다. 프로젝트의 다른 오버라이드와 같은 규칙.
///
///     0  ×  0     시트값 그대로
///     0  ×  0.5   시트값의 절반 — 레벨업하면 같이 자란다
///     3  ×  0     고정 3
///     3  ×  2     고정 6
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

    /// <summary>시트 수치를 받아 실제로 쓸 값을 돌려준다.</summary>
    public readonly float Of(float sheetValue)
    {
        float baseValue = value > 0f ? value : sheetValue;
        float factor = scale > 0f ? scale : 1f;

        return baseValue * factor;
    }

    /// <summary>아무것도 안 적힌 상태인가. 경고 표시에 쓴다.</summary>
    public readonly bool IsUntouched => value <= 0f && scale <= 0f;

    public static Scalable Fixed(float amount) => new() { value = amount };
    public static Scalable Ratio(float factor) => new() { scale = factor };
}
