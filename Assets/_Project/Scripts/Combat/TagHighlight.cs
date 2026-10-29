using UnityEngine;

/// <summary>
/// 탐색 표식이 붙은 적을 강조할 때 갈아끼울 머터리얼 창구. 씬 오브젝트가 필요 없다.
///
/// 적 프리팹마다 머터리얼을 들고 있게 하면 프리팹 12개를 전부 손대야 한다.
/// 강조 모습은 한 곳에서 정하는 편이 맞아서 <c>Resources</c> 에서 한 번만 찾아 쓴다.
/// <see cref="DamageTextSpawner"/> 의 팔레트와 같은 방식이다.
///
/// 둘 다 SG_GlowFalloff 로 만들고 <b>Falloff Strength 를 0</b> 으로 둔다 — 플레이어 광원 밖에서도 보여야 하기 때문이다.
/// 없으면 강조만 안 되고 나머지는 그대로 동작한다.
/// </summary>
public static class TagHighlight
{
    const string BodyPath = "TagBodyMaterial";
    const string EyePath = "TagEyeMaterial";

    static Material body;
    static Material eye;
    static bool searched;

    /// <summary>몸통용. 광원과 무관하게 밝게 보인다.</summary>
    public static Material Body
    {
        get { Load(); return body; }
    }

    /// <summary>눈용. 플레이어 광원 밖에서도 눈이 보인다.</summary>
    public static Material Eye
    {
        get { Load(); return eye; }
    }

    static void Load()
    {
        if (searched) return;
        searched = true;

        body = Resources.Load<Material>(BodyPath);
        eye = Resources.Load<Material>(EyePath);

        if (body == null)
            Debug.LogWarning($"[TagHighlight] Resources/{BodyPath}.mat 이 없다. 탐색된 적의 몸통이 강조되지 않는다.");
    }
}
