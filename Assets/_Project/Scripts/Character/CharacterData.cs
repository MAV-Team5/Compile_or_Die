using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터 한 명의 원본. 터미널·편집기 테마의 플레이어블 하나가 이 에셋 하나다.
///
/// <b>씬에는 수치를 두지 않는다.</b> 체력·이동속도·시작 증강은 전부 여기 있고,
/// 런이 시작될 때 <see cref="PlayerSetup"/> 이 씬의 플레이어에 깔아준다.
/// 그래서 캐릭터가 늘어도 Run 씬은 하나다 — StageData 와 같은 규칙.
///
/// <b>비주얼은 프리팹 그대로 쓴다.</b> 선택 화면에서도 이 프리팹을 실제로 세워서 보여주므로,
/// 고를 때 본 모습이 곧 게임에서 움직이는 모습이다.
/// </summary>
[CreateAssetMenu(fileName = "Character", menuName = "CoD/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("정체")]
    [Tooltip("선택 버튼이 넘길 번호. 겹치지 않게 둘 것.")]
    public int characterId = 1;

    public string displayName = "BASH";

    [Tooltip("선택 화면에 뜨는 한 줄 소개.")]
    [TextArea(2, 3)] public string tagline = "";

    [Header("해금")]
    [Tooltip("끄면 선택 화면에서 물음표로 나오고 고를 수 없다.\n" +
             "해금 조건이 생기면 이 칸 대신 그 조건을 보게 바꾼다.")]
    public bool unlocked = true;

    [Header("비주얼")]
    [Tooltip("＊ 필수 — 플레이어 본체 밑에 붙는 스프라이트+애니메이터 프리팹.\n" +
             "선택 화면에서도 이것을 그대로 세워 보여준다.")]
    public GameObject visualPrefab;

    [Header("기본 능력치")]
    [Tooltip("하드웨어(SSD) 보정은 이 값 위에 곱해진다.")]
    [Min(1f)] public float maxHealth = 100f;

    [Tooltip("하드웨어(키보드) 보정은 이 값 위에 곱해진다.")]
    [Min(0.1f)] public float moveSpeed = 3f;

    [Tooltip("경험치를 끌어당기는 반경.")]
    [Min(0f)] public float pickupRange = 1.5f;

    [Header("시작 증강")]
    [Tooltip("확정으로 얻고 시작하는 증강. 런이 시작될 때 카드 1장짜리 화면으로 하나씩 보여준다 —\n" +
             "몰래 주면 무엇을 들고 시작하는지 모른 채 게임이 돈다.")]
    public List<AugmentData> startingAugments = new();

    [Tooltip("풀에서 직접 고르는 시작 선택 횟수. 레벨업과 똑같은 3택 화면이 이만큼 더 뜬다.\n" +
             "메인보드 업그레이드분과 더해진다.")]
    [Min(0)] public int extraStartRounds;

    /// <summary>선택 화면에 띄울 이름. 잠겨 있으면 정체를 감춘다.</summary>
    public string NameOrLocked => unlocked ? displayName : "?";
}
