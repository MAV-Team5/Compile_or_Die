using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// "화면 밖에 있으면 가장자리에 화살표로 알려달라" 고 등록하는 표식.
/// 상자처럼 <b>안 움직이는 보상</b>에 붙인다.
///
/// <b>왜 필요한가</b> — 안 움직이는 것을 화면 안에 스폰하면 몇 발짝 거리라 그냥 지나가다 먹힌다.
/// 화면 밖에 두어야 "갈까 말까" 라는 판단이 생기는데, 그러려면 어디 있는지는 알려줘야 한다.
/// 위치를 모르면 판단이 아니라 운이다.
///
/// 그리는 일은 <see cref="OffscreenMarkerHud"/> 가 한다 — 화살표가 여럿이어도
/// 캔버스 하나에서 한 번에 돌려야 한다.
/// </summary>
public class OffscreenTarget : MonoBehaviour
{
    [Tooltip("화살표에 쓸 그림. 비우면 HUD 의 기본 화살표를 쓴다.")]
    public Sprite icon;

    public Color tint = Color.white;

    [Tooltip("이 거리 안에 있을 때만 화살표가 뜬다.\n\n" +
             "＊ 무한대로 두면 안 된다 — 지나쳐 온 상자가 계속 화면 가장자리에 쌓여서\n" +
             "  어느 것이 가까운지 못 읽게 된다. 스폰 최대 거리보다 조금 크게 잡을 것.")]
    public float showWithin = 22f;

    [Tooltip("옆에 거리를 숫자로 띄울지. 화살표 프리팹에 TMP 글자가 있어야 보인다.")]
    public bool showDistance = true;

    /// <summary>지금 표시를 원하는 것들. 켜질 때 등록하고 꺼질 때 빠진다.</summary>
    public static readonly List<OffscreenTarget> Active = new();

    void OnEnable()
    {
        if (!Active.Contains(this)) Active.Add(this);
    }

    void OnDisable() => Active.Remove(this);
}
