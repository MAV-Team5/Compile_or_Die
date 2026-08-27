using UnityEngine;

/// <summary>
/// 피해 숫자를 띄우는 창구. 씬 오브젝트가 필요 없다.
///
/// 하는 일이 "팔레트를 들고 풀에서 꺼내 자리 잡기"뿐이라 매니저를 둘 이유가 없었다.
/// 팔레트는 <see cref="UiTheme"/> 와 같은 방식으로 <c>Resources</c> 에서 찾는다.
///
/// 어떤 색·크기로 띄울지는 <see cref="DamagePipeline"/> 이 정해서 넘겨준다.
/// </summary>
public static class DamageTextSpawner
{
    /// <summary>없어도 동작한다 — 전부 흰색 기본 스타일로 나온다.</summary>
    const string PalettePath = "DamageTextPalette";

    static DamageTextPalette palette;
    static bool searched;

    /// <summary>피해 숫자 색·크기 표.</summary>
    public static DamageTextPalette Palette
    {
        get
        {
            if (searched) return palette;

            searched = true;
            palette = Resources.Load<DamageTextPalette>(PalettePath);

            if (palette == null)
                Debug.LogWarning($"[DamageTextSpawner] Resources/{PalettePath}.asset 이 없다. " +
                                 "피해 숫자가 전부 흰색으로 나온다.");

            return palette;
        }
        set { palette = value; searched = true; }
    }

    public static void Show(float damage, Transform target, DamageTextStyle style)
    {
        if (target == null) return;

        PoolManager pool = GameManager.instance != null ? GameManager.instance.poolManager : null;
        if (pool == null) return;

        GameObject go = pool.Get(PoolType.Effect, 0);
        go.transform.position = SpawnPosition(target);

        if (go.TryGetComponent(out DamageText text)) text.Initialize(damage, style);
    }

    /// <summary>스타일 없이 부르면 기본값. 옛 무기 경로 호환용.</summary>
    public static void Show(float damage, Transform target)
        => Show(damage, target, DamageTextStyle.Default);

    /// <summary>같은 자리에 겹쳐 떠서 숫자가 안 읽히는 것을 막는다.</summary>
    static Vector3 SpawnPosition(Transform target)
    {
        Vector3 pos = target.position + Vector3.up;

        pos.x += Random.Range(-0.3f, 0.3f);
        pos.y += Random.Range(-0.2f, 0.2f);

        return pos;
    }
}
