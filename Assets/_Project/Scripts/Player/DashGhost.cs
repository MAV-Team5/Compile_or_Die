using UnityEngine;

/// <summary>
/// 대시 잔상. 그 순간 캐릭터의 그림(자식 SpriteRenderer 전부)을 그대로 찍어 두고 서서히 지운다.
/// 캐릭터마다 몸통·배경 같은 그림 구성이 달라서, 특정 렌더러를 고르지 않고 보이는 것을 전부 복사한다.
/// </summary>
public static class DashGhost
{
    public static void Spawn(Transform root, Color tint, float life)
    {
        if (root == null) return;

        var ghost = new GameObject("DashGhost");
        int count = 0;

        foreach (SpriteRenderer src in root.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!src.enabled || src.sprite == null) continue;

            var part = new GameObject("part");
            part.transform.SetParent(ghost.transform, false);
            part.transform.SetPositionAndRotation(src.transform.position, src.transform.rotation);
            part.transform.localScale = src.transform.lossyScale;

            var sr = part.AddComponent<SpriteRenderer>();
            sr.sprite = src.sprite;
            sr.flipX = src.flipX;
            sr.flipY = src.flipY;
            sr.sharedMaterial = src.sharedMaterial;
            sr.sortingLayerID = src.sortingLayerID;

            // 본체보다 뒤에 그려야 잔상이 본체를 덮지 않는다
            sr.sortingOrder = src.sortingOrder - 5;
            sr.color = new Color(src.color.r * tint.r, src.color.g * tint.g, src.color.b * tint.b, src.color.a * tint.a);

            count++;
        }

        if (count == 0)
        {
            Object.Destroy(ghost);
            return;
        }

        ghost.AddComponent<DashGhostFade>().Begin(life);
    }
}
