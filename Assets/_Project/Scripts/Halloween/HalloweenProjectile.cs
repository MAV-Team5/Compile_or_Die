using UnityEngine;

/// <summary>
/// 투사체 스프라이트를 스킨으로 덮는다. <see cref="HalloweenSkin.ApplyProjectile"/> 가 붙인다.
/// 크기는 원래 그림과 면적이 같게 맞춘다 — 화살처럼 길쭉한 그림의 긴 변에 맞추면 캐릭터가 너무 커진다.
/// </summary>
[DisallowMultipleComponent]
public class HalloweenProjectile : MonoBehaviour
{
    SpriteRenderer sr;
    bool cached;

    bool measured;
    float originalUnits = 1f;
    Sprite originalSprite;

    Sprite[] frames;
    float fps;
    float startTime;

    public void Setup(HalloweenSkinDB.ProjectileSkin skin, GameObject prefab)
    {
        if (!cached)
        {
            cached = true;
            sr = GetComponentInChildren<SpriteRenderer>(true);
        }

        if (skin == null || sr == null)
        {
            Restore();
            return;
        }

        if (!measured)
        {
            measured = true;
            originalSprite = sr.sprite;

            if (originalSprite != null)
            {
                Vector3 size = originalSprite.bounds.size;
                originalUnits = Mathf.Sqrt(Mathf.Max(0.0001f, size.x * size.y));
            }
        }

        Sprite reference = skin.frames != null && skin.frames.Length > 0 ? skin.frames[0] : null;
        frames = HalloweenSkin.Resized(prefab, 0, skin.frames,
                                       HalloweenSkin.PpuFor(reference, originalUnits * skin.sizeMultiplier));

        if (frames.Length == 0)
        {
            Restore();
            return;
        }

        fps = skin.fps;
        startTime = Time.time;
        sr.sprite = frames[0];
        enabled = true;
    }

    void Restore()
    {
        frames = null;
        enabled = false;

        if (sr != null && originalSprite != null) sr.sprite = originalSprite;
    }

    void LateUpdate()
    {
        if (frames == null || sr == null) return;

        sr.sprite = frames.Length > 1
            ? frames[(int)((Time.time - startTime) * fps) % frames.Length]
            : frames[0];
    }
}
