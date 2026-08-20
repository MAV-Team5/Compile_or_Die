using UnityEngine;

/// <summary>
/// 연출 프리팹의 수명 관리. 지정 시간 뒤 스스로 <b>풀에 반납</b>한다.
///
/// Destroy 가 아니라 SetActive(false) 인 것이 중요하다 —
/// 파괴하면 풀 목록에 죽은 참조가 남고, 다음에 꺼내 쓸 때 터진다.
///
/// 알파를 되돌리는 것도 여기 몫이다. 페이드로 투명해진 채 반납되면
/// 재사용했을 때 안 보이는 연출이 나간다.
/// </summary>
public class FxAutoDespawn : MonoBehaviour
{
    [Tooltip("표시 시간(초). 이 시간이 지나면 사라진다.")]
    [SerializeField] float lifetime = 0.3f;

    [Tooltip("켜면 마지막 구간에 서서히 투명해지며 사라진다.")]
    [SerializeField] bool fadeOut = true;

    [Tooltip("페이드에 쓸 시간(초). lifetime 안에 포함된다.")]
    [SerializeField] float fadeDuration = 0.15f;

    SpriteRenderer[] renderers;
    float[] baseAlpha;

    float elapsed;

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseAlpha = new float[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
            baseAlpha[i] = renderers[i].color.a;
    }

    void OnEnable()
    {
        elapsed = 0f;

        SetAlpha(1f);

        // 파티클 되감기는 PooledSpawner 가 붙여주는 PooledParticles 가 맡는다
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        if (fadeOut && fadeDuration > 0f)
        {
            // 남은 시간이 fadeDuration 안으로 들어오면 알파를 줄인다
            float remain = lifetime - elapsed;

            SetAlpha(Mathf.Clamp01(remain / fadeDuration));
        }

        if (elapsed >= lifetime) gameObject.SetActive(false);
    }

    void SetAlpha(float factor)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            Color c = renderers[i].color;
            c.a = baseAlpha[i] * factor;
            renderers[i].color = c;
        }
    }
}
