using UnityEngine;

/// <summary>
/// 네온 연출이 켜졌을 때만 이 프리팹의 스프라이트·색을 네온용으로 바꾼다.
/// 프리팹에는 원래 모습을 두고, 네온 모습은 여기에 적어 둔다.
/// <see cref="BeamVisual"/> 처럼 Awake 에서 원래 색을 기억하는 컴포넌트보다 먼저 돌아야 해서 실행 순서를 앞당겼다.
/// </summary>
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(SpriteRenderer))]
public class NeonLook : MonoBehaviour
{
    [Tooltip("네온일 때 쓸 스프라이트. 비우면 원래 스프라이트 그대로.")]
    [SerializeField] Sprite neonSprite;

    [Tooltip("네온일 때 색. 증강 고유색이 재질로 덧칠되므로 보통 흰색.")]
    [SerializeField] Color neonColor = Color.white;

    void Awake()
    {
        if (!PresentationSettings.Neon) return;

        var body = GetComponent<SpriteRenderer>();
        if (neonSprite != null) body.sprite = neonSprite;
        body.color = neonColor;
    }
}
