using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 증강을 뽑는 규칙. 어떤 증강이 후보가 되고 어떤 게 걸러지는지를 여기서만 정한다.
///
/// <b>MonoBehaviour 가 아니다.</b> 화면과 상관없는 판단이라 UI 밖에 두었다 —
/// 레벨업 선택지든 상자(.zip) 드랍이든 캐릭터 스타트 증강이든 같은 규칙을 써야 하고,
/// UI 를 띄우지 않고도 결과를 확인할 수 있어야 한다.
///
/// 뽑기만 한다. 화면에 그리는 것도, 실제로 지급하는 것도 부르는 쪽 몫이다.
/// </summary>
public class AugmentDraft
{
    readonly AugmentPool pool;
    readonly AugmentManager owned;

    /// <summary>매번 새 리스트를 만들지 않으려고 재사용한다. 결과는 바로 쓰고 버릴 것.</summary>
    readonly List<AugmentData> buffer = new();

    public AugmentDraft(AugmentPool pool, AugmentManager owned)
    {
        this.pool = pool;
        this.owned = owned;
    }

    public bool IsReady => pool != null && owned != null && pool.Count > 0;

    // ── 후보 고르기 ───────────────────────────────────────

    /// <summary>
    /// 지금 뽑을 수 있는 증강 전부. 결과 리스트는 다음 호출에 덮어쓰이므로
    /// 오래 들고 있어야 하면 복사할 것.
    /// </summary>
    public List<AugmentData> Candidates()
    {
        buffer.Clear();

        if (!IsReady) return buffer;

        IReadOnlyList<AugmentData> all = pool.All;

        for (int i = 0; i < all.Count; i++)
        {
            AugmentData data = all[i];

            if (data == null) continue;
            if (!CanAppear(data)) continue;

            buffer.Add(data);
        }

        return buffer;
    }

    /// <summary>이 증강이 지금 선택지에 나올 수 있는가.</summary>
    public bool CanAppear(AugmentData data)
    {
        if (data == null || owned == null) return false;

        // 즉시 효과 아이템은 카드로 안 나온다. 상자에서 떨어진 것을 주워서만 얻는다 —
        // 그래야 "부수고 주우러 간다" 는 판단이 생기고, 레벨업 3택이 증강 선택에만 쓰인다.
        // 풀에 남아 있어도 여기서 걸러지므로 에셋을 안 빼도 안전하다
        if (data.instantEffect != InstantItemEffect.None) return false;

        if (data.levelStats == null || data.levelStats.Length == 0) return false;

        // 이미 만렙이면 더 올릴 수 없다
        AugmentRunner mine = owned.Find(data);
        if (mine != null && mine.Instance.Level >= mine.Instance.MaxLevel) return false;

        // 내부 증강은 뿌리 증강이 조건 레벨에 닿아야 풀린다
        if (data.rootAugment != null)
        {
            AugmentRunner root = owned.Find(data.rootAugment);
            if (root == null || root.Instance.Level < data.requiredRootLevel) return false;
        }

        return true;
    }

    // ── 뽑기 ──────────────────────────────────────────────

    /// <summary>
    /// 서로 겹치지 않게 count 개를 뽑아 results 에 담는다. 후보가 모자라면 있는 만큼만.
    /// exclude 에 든 것은 빼고 뽑는다 — 리롤이 같은 것을 다시 주지 않게 할 때 쓴다.
    /// </summary>
    public int Pick(int count, List<AugmentData> results, IList<AugmentData> exclude = null)
    {
        results.Clear();

        List<AugmentData> candidates = Candidates();

        // 원본을 섞으면 다음 호출의 순서까지 바뀐다. 복사본을 섞는다
        var shuffled = new List<AugmentData>(candidates);

        if (exclude != null)
            for (int i = 0; i < exclude.Count; i++) shuffled.Remove(exclude[i]);

        // TODO 기획: 보유/시너지 증강 확률 보정. 지금은 균등 랜덤
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        int take = Mathf.Min(count, shuffled.Count);

        for (int i = 0; i < take; i++) results.Add(shuffled[i]);

        return take;
    }

    /// <summary>exclude 를 뺀 후보를 하나만. 없으면 null.</summary>
    public AugmentData PickOne(IList<AugmentData> exclude)
    {
        List<AugmentData> candidates = Candidates();

        var left = new List<AugmentData>(candidates);

        if (exclude != null)
            for (int i = 0; i < exclude.Count; i++) left.Remove(exclude[i]);

        return left.Count == 0 ? null : left[Random.Range(0, left.Count)];
    }

    /// <summary>exclude 를 뺀 후보가 하나라도 남아 있는가. 리롤 버튼을 잠글지 판단한다.</summary>
    public bool HasAlternative(IList<AugmentData> exclude)
    {
        List<AugmentData> candidates = Candidates();

        for (int i = 0; i < candidates.Count; i++)
            if (exclude == null || !exclude.Contains(candidates[i])) return true;

        return false;
    }

}
