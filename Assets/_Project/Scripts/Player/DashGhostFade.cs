using UnityEngine;

/// <summary>대시 잔상을 서서히 투명하게 지우고 없앤다. <see cref="DashGhost"/> 가 붙인다.</summary>
public class DashGhostFade : MonoBehaviour
{
    SpriteRenderer[] parts;
    float[] startAlpha;
    float life;
    float age;

    public void Begin(float seconds)
    {
        life = Mathf.Max(0.01f, seconds);
        parts = GetComponentsInChildren<SpriteRenderer>();
        startAlpha = new float[parts.Length];
        for (int i = 0; i < parts.Length; i++) startAlpha[i] = parts[i].color.a;
    }

    void Update()
    {
        // 시간이 멈춘 동안(증강 선택)에도 잔상은 사라지게 실제 시간으로 센다
        age += Time.unscaledDeltaTime;
        float k = 1f - Mathf.Clamp01(age / life);

        for (int i = 0; i < parts.Length; i++)
        {
            Color c = parts[i].color;
            c.a = startAlpha[i] * k * k;
            parts[i].color = c;
        }

        if (age >= life) Destroy(gameObject);
    }
}
