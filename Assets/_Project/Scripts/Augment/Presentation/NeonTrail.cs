using UnityEngine;

/// <summary>
/// 투사체 뒤에 끌리는 네온 꼬리. 증강 고유색(<see cref="AugmentPalette"/>)이라
/// 화면에 투사체가 뒤섞여도 "저건 Bash, 저건 C" 가 색으로 읽힌다.
///
/// 투사체 그림 자체에 색을 곱하지 않는 이유 — 원래 네온 아트가 탁해진다. 꼬리만 덧붙인다.
/// 풀에서 다시 나온 투사체는 꼬리를 지우고 색만 바꿔 다시 쓴다.
/// </summary>
public static class NeonTrail
{
    const float TrailTime = 0.22f;
    const float StartWidth = 0.32f;

    static Material material;

    public static void Attach(GameObject projectile, Color color)
    {
        if (projectile == null) return;

        if (!projectile.TryGetComponent(out TrailRenderer trail))
        {
            trail = projectile.AddComponent<TrailRenderer>();
            trail.time = TrailTime;
            trail.minVertexDistance = 0.05f;
            trail.numCapVertices = 2;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.widthMultiplier = StartWidth;

            if (material == null) material = Resources.Load<Material>("NeonLine");
            trail.sharedMaterial = material;

            // 투사체 그림 바로 아래에 그린다
            var sr = projectile.GetComponentInChildren<SpriteRenderer>();
            trail.sortingLayerName = sr != null ? SortingLayer.IDToName(sr.sortingLayerID) : "Effect";
            trail.sortingOrder = sr != null ? sr.sortingOrder - 1 : 0;
        }

        // 지난 비행의 꼬리가 새 자리까지 길게 늘어나지 않게
        trail.Clear();

        var g = new Gradient();
        Color hot = Color.Lerp(color, Color.white, 0.35f);
        g.SetKeys(new[] { new GradientColorKey(hot, 0f), new GradientColorKey(color, 1f) },
                  new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = g;
        trail.emitting = true;
    }
}
