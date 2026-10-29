using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 증강을 뽑는 규칙. 어떤 증강이 후보가 되고 어떤 게 걸러지는지를 여기서만 정한다.
///
/// <b>MonoBehaviour 가 아니다.</b> 화면과 상관없는 판단이라 UI 밖에 두었다 —
/// 레벨업 선택지든 상자(.zip) 드랍이든 캐릭터 스타트 증강이든 같은 규칙을 써야 하고,
/// UI 를 띄우지 않고도 결과를 확인할 수 있어야 한다.
///
/// 뽑기만 한다. 화면에 그리는 것도, 실제로 지급하는 것도 부르는 쪽 몫이다.
///
/// <b>뽑기는 주머니 방식이다.</b> 증강마다 칸을 차지하고, 칸이 많을수록 잘 뽑힌다
/// (<see cref="Tickets"/>). 보유하고 키울수록 칸이 늘어 빌드가 자연스럽게 굳는다.
/// 초반 구간(<c>earlyUntilLevel</c> 이하)에는 초반 풀에서만 뽑는다.
/// </summary>
public class AugmentDraft
{
    readonly AugmentPool pool;
    readonly AugmentManager owned;

    /// <summary>초반에만 쓰는 풀. 없으면 처음부터 pool 을 쓴다.</summary>
    readonly AugmentPool earlyPool;

    /// <summary>이 레벨의 선택지까지 earlyPool 에서 뽑는다.</summary>
    readonly int earlyUntilLevel;

    /// <summary>지금 뽑는 선택지가 몇 레벨 몫인가. 레벨을 아는 쪽(UI)이 알려준다.</summary>
    readonly System.Func<int> choiceLevel;

    /// <summary>매번 새 리스트를 만들지 않으려고 재사용한다. 결과는 바로 쓰고 버릴 것.</summary>
    readonly List<AugmentData> buffer = new();

    /// <summary>해금된 뒤 한 번이라도 선택지에 나온 내부 증강. 해금 보장은 한 번만 한다.</summary>
    readonly HashSet<AugmentData> announced = new();

    public AugmentDraft(AugmentPool pool, AugmentManager owned,
                        AugmentPool earlyPool = null, int earlyUntilLevel = 0,
                        System.Func<int> choiceLevel = null)
    {
        this.pool = pool;
        this.owned = owned;
        this.earlyPool = earlyPool;
        this.earlyUntilLevel = earlyUntilLevel;
        this.choiceLevel = choiceLevel;
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

        IReadOnlyList<AugmentData> all = ActivePool().All;

        for (int i = 0; i < all.Count; i++)
        {
            AugmentData data = all[i];

            if (data == null) continue;
            if (!CanAppear(data)) continue;

            buffer.Add(data);
        }

        return buffer;
    }

    /// <summary>
    /// 이번 선택지를 뽑을 풀. 초반 구간이면 초반 풀이지만,
    /// 거기서 더 뽑을 것이 없으면(전부 만렙 등) 전체 풀로 넘어간다 — 선택지가 비면 안 된다.
    /// </summary>
    AugmentPool ActivePool()
    {
        if (earlyPool == null || choiceLevel == null) return pool;
        if (choiceLevel() > earlyUntilLevel) return pool;

        return HasAny(earlyPool) ? earlyPool : pool;
    }

    bool HasAny(AugmentPool from)
    {
        IReadOnlyList<AugmentData> all = from.All;

        for (int i = 0; i < all.Count; i++)
            if (all[i] != null && CanAppear(all[i])) return true;

        return false;
    }

    // ── 칸 수 ─────────────────────────────────────────────

    /// <summary>
    /// 이 증강이 주머니에서 차지하는 칸 수.
    /// 미보유 1칸 — 내부 증강도 해금되는 순간부터 새 증강과 똑같이 1칸이다.
    /// 보유 중이면 레벨마다 perLevel 칸씩 더한다.
    ///
    /// 내부 증강을 요구 레벨만큼 주던 시절에는 해금되자마자 3칸으로 들어와 본체보다 자주 떴다.
    /// 해금 직후 한 번은 <see cref="EnsureUnlockedInner"/> 가 보장하므로 칸을 더 줄 이유가 없다.
    /// </summary>
    float Tickets(AugmentData data, float perLevel)
    {
        const float baseTickets = 1f;

        AugmentRunner mine = owned.Find(data);
        if (mine == null) return baseTickets;

        return baseTickets + mine.Instance.Level * perLevel;
    }

    /// <summary>칸 수에 비례해 하나를 고른다. 리스트의 위치를 돌려준다.</summary>
    int DrawIndex(List<AugmentData> bag, float perLevel)
    {
        float total = 0f;
        for (int i = 0; i < bag.Count; i++) total += Tickets(bag[i], perLevel);

        float roll = Random.value * total;

        for (int i = 0; i < bag.Count; i++)
        {
            roll -= Tickets(bag[i], perLevel);
            if (roll < 0f) return i;
        }

        // Random.value 가 1.0 이 나오거나 부동소수 오차가 남으면 끝까지 온다
        return bag.Count - 1;
    }

    static void RemoveExcluded(List<AugmentData> bag, IList<AugmentData> exclude)
    {
        if (exclude == null) return;

        for (int i = 0; i < exclude.Count; i++) bag.Remove(exclude[i]);
    }

    void LogTickets(AugmentPool from, List<AugmentData> bag)
    {
        var sb = new StringBuilder();
        float total = 0f;

        for (int i = 0; i < bag.Count; i++)
        {
            float t = Tickets(bag[i], from.ticketsPerLevel);
            total += t;
            sb.Append($"{bag[i].displayName} {t:0.#}  ");
        }

        string level = choiceLevel != null ? $"Lv {choiceLevel()}" : "Lv ?";
        Debug.Log($"[AugmentDraft] {from.name} ({level}) 총 {total:0.#}칸 | {sb}");
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

        // 캐릭터 전용 증강(Bash 등)은 그 캐릭터일 때만. 그 뿌리에 붙는 내부 증강은 뿌리 조건에서 같이 걸러진다.
        // 캐릭터를 모르면(씬 바로 재생) 막지 않는다 — 테스트가 안 되면 더 곤란하다
        CharacterData character = CharacterContext.Active;
        if (data.exclusiveTo != null && character != null && character != data.exclusiveTo) return false;

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

        if (!IsReady) return 0;

        AugmentPool from = ActivePool();

        // 원본은 다음 호출에 덮어쓰이는 버퍼다. 복사본에서 뽑는다
        var bag = new List<AugmentData>(Candidates());
        RemoveExcluded(bag, exclude);

        if (from.logTickets) LogTickets(from, bag);

        for (int i = 0; i < count && bag.Count > 0; i++)
        {
            int index = DrawIndex(bag, from.ticketsPerLevel);

            results.Add(bag[index]);

            // 뽑힌 증강의 칸은 통째로 빠진다. 같은 증강이 두 장 나오지 않는다
            bag.RemoveAt(index);
        }

        EnsureNewCard(results, bag, from.ticketsPerLevel);
        EnsureUnlockedInner(results, bag);

        return results.Count;
    }

    /// <summary>
    /// 막 해금된 내부 증강은 바로 다음 선택지에 한 번 보장한다. 뿌리를 키운 보상이 눈에 보여야 한다 —
    /// 운이 나쁘면 끝까지 안 나와서 해금됐는지조차 모르고 끝난다.
    /// 자리는 보유 증강 칸을 우선 뺏는다. 새 증강 보장 칸까지 먹으면 다른 길이 사라진다.
    /// </summary>
    void EnsureUnlockedInner(List<AugmentData> results, List<AugmentData> remaining)
    {
        foreach (AugmentData shown in results)
            if (shown.rootAugment != null) announced.Add(shown);

        if (results.Count == 0) return;

        AugmentData waiting = remaining.Find(d => d.rootAugment != null && IsNew(d) && !announced.Contains(d));
        if (waiting == null) return;

        int slot = results.FindIndex(d => !IsNew(d));                    // 보유 증강 칸
        if (slot < 0 && CountNew(results) > 1)
            slot = results.FindIndex(d => d.rootAugment == null);         // 새 증강이 여럿이면 그중 하나
        if (slot < 0) return;                                              // 바꿀 자리가 없다. 다음 선택지에서 다시 본다

        results[slot] = waiting;
        announced.Add(waiting);
    }

    int CountNew(List<AugmentData> list)
    {
        int count = 0;
        foreach (AugmentData d in list) if (IsNew(d)) count++;
        return count;
    }

    /// <summary>아직 안 가진 증강인가. 선택지에 하나는 꼭 섞는다.</summary>
    public bool IsNew(AugmentData data) => data != null && owned != null && owned.Find(data) == null;

    /// <summary>
    /// 선택지가 전부 보유 증강이면 무작위 한 칸을 미보유 증강으로 바꾼다.
    /// 보유 증강은 칸이 계속 늘어 후반엔 업그레이드만 뜨기 쉽다 — 매번 새 길이 하나는 보여야 빌드가 갈린다.
    /// 뽑을 미보유 후보가 없으면 그대로 둔다.
    /// </summary>
    void EnsureNewCard(List<AugmentData> results, List<AugmentData> remaining, float perLevel)
    {
        if (results.Count == 0) return;

        for (int i = 0; i < results.Count; i++)
            if (IsNew(results[i])) return;

        var fresh = remaining.FindAll(IsNew);
        if (fresh.Count == 0) return;

        // 늘 같은 칸이면 "세 번째는 새것" 으로 읽힌다. 자리도 무작위로
        results[Random.Range(0, results.Count)] = fresh[DrawIndex(fresh, perLevel)];
    }

    /// <summary>
    /// exclude 를 뺀 후보를 하나만. 없으면 null.
    /// preferNew 면 미보유 증강 중에서 뽑는다(없으면 아무거나) — 리롤로 새 카드가 사라지지 않게 할 때 쓴다.
    /// </summary>
    public AugmentData PickOne(IList<AugmentData> exclude, bool preferNew = false)
    {
        if (!IsReady) return null;

        AugmentPool from = ActivePool();

        var bag = new List<AugmentData>(Candidates());
        RemoveExcluded(bag, exclude);

        if (preferNew)
        {
            var fresh = bag.FindAll(IsNew);
            if (fresh.Count > 0) bag = fresh;
        }

        if (bag.Count == 0) return null;

        AugmentData picked = bag[DrawIndex(bag, from.ticketsPerLevel)];
        if (picked.rootAugment != null) announced.Add(picked);   // 리롤로 보여줬어도 해금 보장은 끝난 것이다

        return picked;
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
