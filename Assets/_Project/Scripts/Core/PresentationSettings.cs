using UnityEngine;

/// <summary>
/// 발표·시연용 연출 스위치. <c>Resources/PresentationSettings</c> 에 하나만 둔다.
/// 메뉴 <c>CoD/네온 연출</c> 로 켜고 끈다. 에셋이 없으면 전부 꺼진 것으로 본다.
/// </summary>
[CreateAssetMenu(menuName = "CoD/Presentation Settings", fileName = "PresentationSettings")]
public class PresentationSettings : ScriptableObject
{
    const string ResourcePath = "PresentationSettings";

    [Tooltip("증강 네온 연출 — 고유색 칠하기·네온 꼬리·연쇄선·간선 빛·네온 아이콘·네온 투사체.\n" +
             "끄면 원래 모습으로 돌아간다. 투사체 모양은 다음 플레이부터 적용된다.")]
    public bool neon = true;

    static PresentationSettings asset;

    public static PresentationSettings Asset
    {
        get
        {
            if (asset == null) asset = Resources.Load<PresentationSettings>(ResourcePath);
            return asset;
        }
    }

    public static bool Neon => Asset != null && Asset.neon;
}
