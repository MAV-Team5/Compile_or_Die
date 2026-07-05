using System;
using TMPro;
using UnityEngine;
/// <summary>
/// 데미지 텍스트 클래스.
/// </summary>
public class DamageText : MonoBehaviour
{
    [SerializeField] private TextMeshPro damageText;
    [SerializeField] private CanvasGroup canvasGroup;

    private const float MAX_LIFETIME = 1.0f;
    float lifeTime;

    Vector3 moveDirection;
    float moveSpeed;

    /// <summary>
    /// 텍스트를 받은 데미지로 수정하고 위로 이동.
    /// </summary>
    /// <param name="damage"></param>
    public void Initialize(float damage)
    {
        damageText.text = damage.ToString("F0");

        lifeTime = MAX_LIFETIME;
        moveDirection = Vector3.up;
        moveSpeed = 1.5f;

        transform.localScale = Vector3.one;
        canvasGroup.alpha = 1.0f;
    }

    /// <summary>
    /// 시간이 지나면 투명해지다 사라지게
    /// </summary>
    private void Update()
    {
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        lifeTime -= Time.deltaTime;

        float ratio = lifeTime / MAX_LIFETIME;

        transform.localScale = Vector3.one * ratio;
        canvasGroup.alpha = ratio;

        if (lifeTime <= 0)
        {
            gameObject.SetActive(false);
        }
    }
}