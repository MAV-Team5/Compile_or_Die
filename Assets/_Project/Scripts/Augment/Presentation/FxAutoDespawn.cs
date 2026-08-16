using UnityEngine;

/// <summary>
/// 연출 프리팹의 수명 관리. 지정 시간 뒤 스스로 사라진다.
/// VfxSpawner 는 낳기만 하고 죽는 것은 프리팹 책임이므로,
/// 파티클이 아닌 스프라이트 연출은 이걸 붙여야 씬에 안 쌓인다.
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
    float elapsed;

    void Awake() => renderers = GetComponentsInChildren<SpriteRenderer>();

    void OnEnable() => elapsed = 0f;

    void Update()
    {
        elapsed += Time.deltaTime;

        if (fadeOut && renderers != null)
        {
            // 남은 시간이 fadeDuration 안으로 들어오면 알파를 줄인다
            float remain = lifetime - elapsed;
            float alpha = Mathf.Clamp01(remain / fadeDuration);

            for (int i = 0; i < renderers.Length; i++)
            {
                Color c = renderers[i].color;
                c.a = alpha;
                renderers[i].color = c;
            }
        }

        if (elapsed >= lifetime) Destroy(gameObject);
    }
}