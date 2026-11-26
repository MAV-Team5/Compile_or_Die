using UnityEngine;

/// <summary>
/// 세이렌 2페이즈 효과음. 파일 없이 코드로 파형을 만든다(시작 연출의 지지직 소리와 같은 방식).
///
/// <list type="bullet">
/// <item><see cref="LaserHum"/> — 레이저가 켜져 있는 동안 도는 "지이이잉" 웅웅거림. 1초짜리 무한 반복용.
///       모든 주파수를 1Hz 의 정수배로 맞춰 1초 끝과 처음이 정확히 이어진다(반복 이음새에서 "틱" 소리가 안 난다).</item>
/// <item><see cref="LaserIgnite"/> — 경고선이 진짜 레이저로 바뀌는 순간의 "지잉—" 하고 치솟는 소리.</item>
/// <item><see cref="WaveBoom"/> — 물결 띠가 타격으로 바뀌는 순간의 "팡!". 짧은 파열음 + 낮게 떨어지는 쿵.</item>
/// </list>
///
/// 만든 소리는 한 번만 만들고 재사용한다.
/// </summary>
public static class HalloweenBossSfx
{
    const int Rate = 22050;

    static AudioClip hum, ignite, boom;

    public static AudioClip LaserHum => hum != null ? hum : hum = MakeHum();
    public static AudioClip LaserIgnite => ignite != null ? ignite : ignite = MakeIgnite();
    public static AudioClip WaveBoom => boom != null ? boom : boom = MakeBoom();

    /// <summary>지이이잉 — 톱니파 같은 낮은 웅웅 + 위에서 떨리는 전기 지글거림.</summary>
    static AudioClip MakeHum()
    {
        int n = Rate;                       // 정확히 1초
        var d = new float[n];

        const float f0 = 110f;              // 기본 웅웅
        const float buzz = 440f;            // 지글거리는 윗소리
        const float wobble = 30f;           // 지글거림이 떨리는 속도 — 전기 느낌

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;

            // 톱니파 근사(배음 8개) — 거친 "지이" 소리
            float saw = 0f;
            for (int k = 1; k <= 8; k++) saw += Mathf.Sin(2f * Mathf.PI * f0 * k * t) / k;

            float trem = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * wobble * t);
            float high = Mathf.Sin(2f * Mathf.PI * buzz * t) * trem
                       + 0.4f * Mathf.Sin(2f * Mathf.PI * buzz * 2f * t) * trem;

            // 천천히 숨 쉬는 세기 — 2Hz
            float swell = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 2f * t);

            d[i] = (saw * 0.35f + high * 0.25f) * swell;
        }

        return Finish("SirenLaserHum", d, 0.6f);
    }

    /// <summary>지잉— 낮은 데서 높게 치솟는 소리 + 짧은 스파크.</summary>
    static AudioClip MakeIgnite()
    {
        const float seconds = 0.55f;
        int n = (int)(Rate * seconds);
        var d = new float[n];

        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float k = t / seconds;

            // 250Hz → 1400Hz 로 휘어 오르는 주파수
            float freq = 250f * Mathf.Pow(1400f / 250f, Mathf.Sqrt(k));
            phase += 2f * Mathf.PI * freq / Rate;

            float tone = Mathf.Sin(phase) + 0.5f * Mathf.Sin(phase * 2f) + 0.25f * Mathf.Sin(phase * 3f);

            float attack = Mathf.Clamp01(t / 0.015f);
            float decay = Mathf.Exp(-k * 2.2f);

            // 처음 0.12초에 지직거리는 스파크
            float spark = t < 0.12f ? Random.Range(-1f, 1f) * (1f - t / 0.12f) * 0.6f : 0f;

            d[i] = (tone * 0.5f + spark) * attack * decay;
        }

        return Finish("SirenLaserIgnite", d, 0.8f);
    }

    /// <summary>팡! — 날카로운 파열 노이즈 + 낮게 떨어지는 쿵 + 짧은 쨍.</summary>
    static AudioClip MakeBoom()
    {
        const float seconds = 0.6f;
        int n = (int)(Rate * seconds);
        var d = new float[n];

        float bodyPhase = 0f;
        float lp = 0f;     // 노이즈를 살짝 뭉갠다 — 너무 쨍하지 않게

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;

            // 파열 — 아주 빨리 사라지는 노이즈
            lp += (Random.Range(-1f, 1f) - lp) * 0.45f;
            float crack = lp * Mathf.Exp(-t * 38f);

            // 쿵 — 95Hz 에서 40Hz 로 떨어지는 낮은 음
            float freq = 40f + 55f * Mathf.Exp(-t * 9f);
            bodyPhase += 2f * Mathf.PI * freq / Rate;
            float body = Mathf.Sin(bodyPhase) * Mathf.Exp(-t * 7f);

            // 쨍 — 네온 느낌의 짧은 고음
            float ping = Mathf.Sin(2f * Mathf.PI * 1250f * t) * Mathf.Exp(-t * 30f) * 0.35f;

            float attack = Mathf.Clamp01(t / 0.002f);
            d[i] = (crack * 0.9f + body * 0.9f + ping) * attack;
        }

        return Finish("SirenWaveBoom", d, 0.9f);
    }

    /// <summary>최댓값을 <paramref name="peak"/> 로 맞추고 클립으로 만든다.</summary>
    static AudioClip Finish(string name, float[] d, float peak)
    {
        float max = 0.0001f;
        foreach (float v in d) max = Mathf.Max(max, Mathf.Abs(v));
        float g = peak / max;
        for (int i = 0; i < d.Length; i++) d[i] *= g;

        AudioClip clip = AudioClip.Create(name, d.Length, 1, Rate, false);
        clip.SetData(d, 0);
        return clip;
    }
}
