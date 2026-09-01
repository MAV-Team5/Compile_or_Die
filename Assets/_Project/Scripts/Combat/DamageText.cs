using TMPro;
using UnityEngine;

/// <summary>
/// 데미지 텍스트 클래스. 색·크기·튀는 속도는 띄우는 쪽에서 정해 넘긴다.
/// </summary>
public class DamageText : MonoBehaviour
{
    [SerializeField] private TextMeshPro damageText;
    [SerializeField] private CanvasGroup canvasGroup;

    private const float MAX_LIFETIME = 1.0f;
    float lifeTime;

    Vector3 moveDirection;
    float moveSpeed;
    float baseScale;

    /// <summary>텍스트를 받은 데미지로 수정하고 위로 이동.</summary>
    public void Initialize(float damage, DamageTextStyle style)
    {
        damageText.text = damage.ToString("F0");
        damageText.color = style.color;

        lifeTime = MAX_LIFETIME;
        moveDirection = Vector3.up;
        moveSpeed = style.riseSpeed;
        baseScale = style.scale;

        transform.localScale = Vector3.one * baseScale;
        canvasGroup.alpha = 1.0f;
    }

    public void Initialize(float damage) => Initialize(damage, DamageTextStyle.Default);

    /// <summary>시간이 지나면 작아지고 투명해지다 사라진다.</summary>
    private void Update()
    {
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        lifeTime -= Time.deltaTime;

        float ratio = lifeTime / MAX_LIFETIME;

        // 스타일 크기를 유지한 채 줄어들어야 큰 숫자가 처음부터 작아 보이지 않는다
        transform.localScale = Vector3.one * (baseScale * ratio);
        canvasGroup.alpha = ratio;

        if (lifeTime <= 0)
        {
            gameObject.SetActive(false);
        }
    }
}
