using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 이 스테이지에서 등장할 수 있는 증강 목록. <c>Create → CoD → Augment Pool</c>
///
/// <b>왜 에셋인가</b> — 증강을 얻는 길은 셋이다.
/// 레벨업 3택 · 상자(.zip) 드랍 · 캐릭터 스타트 증강.
/// 목록이 UI 안에 있으면 나머지 둘이 UI 를 찾아가야 한다.
/// 에셋으로 두면 누구든 같은 목록을 보고, 스테이지마다 다른 풀을 물릴 수도 있다.
/// </summary>
[CreateAssetMenu(fileName = "AugmentPool", menuName = "CoD/Augment Pool")]
public class AugmentPool : ScriptableObject
{
    [Tooltip("등장 후보 전체. 내부 증강도 여기 넣는다 — 해금 조건은 증강 자신이 들고 있다.")]
    [SerializeField] List<AugmentData> augments = new();

    public IReadOnlyList<AugmentData> All => augments;

    public int Count => augments.Count;

#if UNITY_EDITOR
    // 같은 증강을 두 번 넣으면 그 증강만 두 배로 잘 나온다. 의도한 게 아니면 사고다
    void OnValidate()
    {
        var seen = new HashSet<AugmentData>();

        for (int i = 0; i < augments.Count; i++)
        {
            if (augments[i] == null) continue;

            if (!seen.Add(augments[i]))
                Debug.LogWarning($"[AugmentPool] '{augments[i].name}' 가 두 번 들어 있다. " +
                                 "등장 확률이 두 배가 된다.", this);
        }
    }
#endif
}
