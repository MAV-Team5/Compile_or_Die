using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적중 시 게임 내 로그창에 문구를 띄운다.
/// effects 에 넣으면 적중 때, ChainEffect.finalEffects 에 넣으면 연쇄가 끝날 때 나온다.
/// </summary>
[System.Serializable]
[ModuleInfo("게임 내 로그창에 문구 출력", "토큰으로 수치를 끼워 넣을 수 있다")]
public class LogEffect : EffectModule
{
    [Required("아무 로그도 뜨지 않는다")]
    [Tooltip("출력할 문구. 토큰 사용 가능 — {target} {damage} {depth} {level} {name} {count} {range} {index}")]
    [TextArea(1, 3)]
    public string message = "> {name} → {target}";

    [Tooltip("로그 분류. 색과 필터에 쓰인다.")]
    public GameLogType logType = GameLogType.Skill;

    [Tooltip("같은 문구를 다시 띄우기까지의 최소 간격(초). 폭발이 여럿을 맞힐 때 도배를 막는다.")]
    public float minInterval = 0.5f;

    // 모듈은 SO에 저장되므로 시간 기록은 static 으로 둔다. 문구 단위라 증강이 달라도 안전
    static readonly Dictionary<string, float> lastShown = new();

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        if (string.IsNullOrEmpty(message)) return;

        string text = AugmentText.Format(message, ctx, hit);

        if (minInterval > 0f)
        {
            float now = Time.unscaledTime;

            if (lastShown.TryGetValue(text, out float last) && now - last < minInterval)
                return;

            lastShown[text] = now;
        }

        if (LogManager.Instance != null) LogManager.Instance.AddLog(logType, text);
        else Debug.Log(text);
    }
}
