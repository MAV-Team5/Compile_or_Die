using UnityEngine;

/// <summary>
/// 함께 쏜 발의 순번에 따라 머리 스프라이트와 꼬리 텍스처를 바꾼다.
/// 프리팹 하나로 색이 다른 투사체들을 낸다 (파이썬의 파랑·노랑 뱀).
/// </summary>
public class VolleySkin : MonoBehaviour
{
    [System.Serializable]
    public class Skin
    {
        public Sprite head;
        [Tooltip("꼬리 텍스처. 비우면 머티리얼 그대로.")]
        public Texture2D tail;
    }

    static readonly int MainTex = Shader.PropertyToID("_MainTex");

    public SpriteRenderer head;
    public TrailRenderer tail;

    [Tooltip("0번째 발은 첫 스킨, 1번째 발은 두 번째 스킨… 모자라면 처음부터 반복.")]
    public Skin[] skins;

    MaterialPropertyBlock block;

    public void Apply(int shotIndex)
    {
        if (skins == null || skins.Length == 0) return;

        Skin skin = skins[shotIndex % skins.Length];

        if (head != null && skin.head != null) head.sprite = skin.head;

        if (tail != null && skin.tail != null)
        {
            block ??= new MaterialPropertyBlock();
            tail.GetPropertyBlock(block);
            block.SetTexture(MainTex, skin.tail);
            tail.SetPropertyBlock(block);
        }
    }
}
