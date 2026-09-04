using UnityEngine;

/// <summary>
/// 판정 반경에 맞춰 크기를 잡아주는 가장 단순한 연출. 장판·사거리 표시에 쓴다.
///
/// <b>스프라이트 원본 크기를 재서 맞춘다.</b> 512픽셀이든 64픽셀이든, PPU 가 몇이든
/// 결과가 같은 반경으로 나온다 — 이게 없으면 <see cref="AreaDelivery"/> 가
/// <c>localScale *= radius</c> 로 곱해버려서, 지름 16유닛짜리 원에 반경 2를 곱하면
/// 32유닛이 되어 <b>화면 전체가 덮인다.</b>
///
/// 회전·부채꼴이 필요하면 <see cref="SwingArc"/>, 훑는 느낌이면 <see cref="RadarSweep"/>.
/// 이건 "그 크기로 잠깐 뜨기" 만 한다.
/// </summary>
public class SizedRing : MonoBehaviour, ISizedVisual
{
    [Tooltip("크기를 잴 스프라이트. 비우면 자식에서 찾는다.")]
    [SerializeField] SpriteRenderer body;

    [Tooltip("반경에 곱할 값. 1이면 판정과 정확히 같은 크기.\n\n" +
             "＊ 1에서 크게 벗어나게 두지 말 것 — 그림과 판정이 어긋나면\n" +
             "  플레이어가 \"닿았는데 왜 안 맞지\" 를 겪는다. 1.05 정도가 한계.")]
    [Min(0.01f)] public float scaleFactor = 1f;

    void Awake()
    {
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
    }

    public void Resize(float radius)
    {
        float unit = UnitRadius();

        if (unit <= 0.0001f)
        {
            // 스프라이트를 못 찾으면 예전처럼 곱하는 수밖에 없다
            transform.localScale = Vector3.one * (radius * scaleFactor);
            return;
        }

        transform.localScale = Vector3.one * (radius * scaleFactor / unit);
    }

    /// <summary>
    /// 스케일 1일 때 이 그림의 반지름(월드 유닛).
    ///
    /// <c>sprite.bounds</c> 는 에셋 자체의 크기라 현재 스케일에 영향받지 않는다 —
    /// <c>renderer.bounds</c> 를 쓰면 이미 커진 상태를 재서 매번 더 커진다.
    /// </summary>
    float UnitRadius()
    {
        if (body == null || body.sprite == null) return 0f;

        Vector2 size = body.sprite.bounds.size;

        return Mathf.Max(size.x, size.y) * 0.5f;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        float unit = UnitRadius();
        if (unit <= 0.0001f) return;

        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, unit * transform.lossyScale.x);
    }
#endif
}
