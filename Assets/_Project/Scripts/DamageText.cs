using System;
using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    [SerializeField] private TextMeshPro damageText;
    [SerializeField] private CanvasGroup canvasGroup;

    float lifeTime = 0.8f;

    Vector3 moveDirection;
    float moveSpeed;

    public void Initialize(float damage)
    {
        damageText.text = damage.ToString();

        moveDirection = Vector3.up;
        moveSpeed = 1.5f;
    }

    private void Update()
    {
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        lifeTime -= Time.deltaTime;

        float ratio = lifeTime / 0.8f;

        transform.localScale = Vector3.one * ratio;
        canvasGroup.alpha = ratio;

        if (lifeTime <= 0)
        {
            Destroy(gameObject);
        }
    }
}