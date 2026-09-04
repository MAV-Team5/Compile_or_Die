using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적이 지닌 탐색 표식 목록. 필요할 때 자동으로 붙으므로 프리팹을 손댈 필요가 없다.
/// 표식 오브젝트의 생성·배치·정리와 전역 탐색풀 등록을 맡는다.
///
/// 자리와 크기는 적 프리팹의 <see cref="MarkMount"/> 가, 몇 번 자리에 붙을지는
/// 표식 프리팹의 <see cref="MarkSlot"/> 이 정한다. 코드는 몸집을 재지 않는다.
/// </summary>
public class MarkerHolder : MonoBehaviour
{
    /// <summary>한 자리를 여럿이 나눠 쓸 때의 겹 간격 배율.</summary>
    const float FallbackRing = 1.15f;

    readonly List<SearchMark> marks = new();

    MarkMount[] mounts;
    bool mountsSearched;

    /// <summary>
    /// 자리 목록을 한 번만 찾아 캐시한다. 표식이 붙을 때마다 계층을 훑으면 그게 진짜 비용이다.
    /// 이 컴포넌트는 한 번 붙으면 안 떨어지므로 풀에서 다시 꺼내도 캐시가 살아 있다.
    /// </summary>
    void EnsureMounts()
    {
        if (mountsSearched) return;

        mountsSearched = true;
        mounts = GetComponentsInChildren<MarkMount>(true);
    }

    /// <summary>이 표식의 자리. 번호가 맞는 MarkMount 가 없으면 null — 그때는 본체에 붙는다.</summary>
    MarkMount MountOf(SearchMark mark)
    {
        EnsureMounts();

        if (mounts == null) return null;

        for (int i = 0; i < mounts.Length; i++)
            if (mounts[i] != null && mounts[i].slot == mark.Slot) return mounts[i];

        return null;
    }

    Transform MountTransformOf(SearchMark mark)
    {
        MarkMount mount = MountOf(mark);

        return mount != null ? mount.transform : transform;
    }

    /// <summary>
    /// 이 표식에 줄 스케일. 자리의 <c>size</c> 를 그대로 쓴다.
    ///
    /// <b>월드 크기가 아니라 로컬 값이다.</b> 표식은 자리의 자식이라 적의 몸집(lossyScale)을
    /// 이미 물려받는다 — 여기서 또 곱하면 두 번 커진다.
    /// </summary>
    float MountSizeOf(SearchMark mark)
    {
        MarkMount mount = MountOf(mark);

        if (mount != null) return mount.size;

        // 자리를 못 찾으면 조용히 원본 크기로 나온다. 표식 프리팹은 보통 화면을 덮을 만큼 크다
        if (!warnedNoMount)
        {
            warnedNoMount = true;
            Debug.LogWarning($"[MarkerHolder] {name} 에 slot {mark.Slot} 자리(MarkMount)가 없다. " +
                             "표식이 원본 크기로 나온다.", this);
        }

        return 0f;
    }

    bool warnedNoMount;

    public int Count => marks.Count;

    /// <summary>없으면 붙여서 돌려준다.</summary>
    public static MarkerHolder GetOrAdd(Transform target)
    {
        if (target == null) return null;

        return target.TryGetComponent(out MarkerHolder holder)
            ? holder
            : target.gameObject.AddComponent<MarkerHolder>();
    }

    /// <summary>이 증강의 표식을 지니고 있는지. 탐색풀 조회에 쓴다.</summary>
    public bool HasOwner(AugmentInstance owner)
    {
        for (int i = 0; i < marks.Count; i++)
            if (marks[i].Owner == owner) return true;

        return false;
    }

    /// <summary>같은 증강의 표식이 이미 있으면 덮어쓴다.</summary>
    public void Apply(SearchMark mark)
    {
        RemoveByOwner(mark.Owner);

        marks.Add(mark);

        CreateVisual(mark);
        Reposition();
        SyncRegistry();
    }

    /// <summary>특정 증강이 남긴 표식만 걷어낸다. 쿨타임 갱신 시 지난 탐색 해제용.</summary>
    public void RemoveByOwner(AugmentInstance owner)
    {
        bool changed = false;

        for (int i = marks.Count - 1; i >= 0; i--)
        {
            if (marks[i].Owner != owner) continue;

            Remove(i);
            changed = true;
        }

        if (!changed) return;

        Reposition();
        SyncRegistry();
    }

    /// <summary>
    /// 표식들이 더해주는 추가 피해 합계. 실제로 들어간 표식은 연출도 낸다.
    /// 표식은 사라지지 않는다 — 지속되며 매 공격마다 다시 얹힌다.
    /// </summary>
    public float Consume(float baseAmount)
    {
        float sum = 0f;

        for (int i = 0; i < marks.Count; i++)
        {
            float bonus = marks[i].Evaluate(baseAmount);
            if (bonus <= 0f) continue;

            sum += bonus;

            // 표식이 붙어 있는 자리에서 터뜨린다. 오브젝트가 없으면 적 위치
            Vector2 at = marks[i].Visual != null
                ? (Vector2)marks[i].Visual.transform.position
                : (Vector2)transform.position;

            marks[i].PlayBurst(at, transform);
        }

        return sum;
    }

    void Update()
    {
        // 이 컴포넌트는 한 번 붙으면 안 떨어진다. 표식이 없는 동안 헛돌지 않게 막는다
        if (marks.Count == 0) return;

        bool changed = false;

        for (int i = marks.Count - 1; i >= 0; i--)
        {
            if (!marks[i].IsExpired) continue;

            Remove(i);
            changed = true;
        }

        if (!changed) return;

        Reposition();
        SyncRegistry();
    }

    // 적이 풀로 반납될 때 표식이 남아있으면 다음 적이 물려받는다
    void OnDisable()
    {
        for (int i = marks.Count - 1; i >= 0; i--) Remove(i);

        SearchRegistry.Unregister(this);
    }

    void Remove(int index)
    {
        if (marks[index].Visual != null) Destroy(marks[index].Visual);

        marks.RemoveAt(index);
    }

    /// <summary>표식이 하나라도 있으면 탐색풀에, 없으면 빠진다.</summary>
    void SyncRegistry()
    {
        if (marks.Count > 0) SearchRegistry.Register(this);
        else SearchRegistry.Unregister(this);
    }

    /// <summary>같은 자리를 쓰는 표식이 여럿일 때만 쓰는 겹 배율.</summary>
    static float RingScale(int index)
        => index <= 0 ? 1f : Mathf.Pow(FallbackRing, index);

    void CreateVisual(SearchMark mark)
    {
        if (mark.VisualPrefab == null) return;

        // 자리 번호는 표식 프리팹이 들고 있다. 안 붙어 있으면 0번
        mark.Slot = mark.VisualPrefab.TryGetComponent(out MarkSlot tag) ? tag.slot : 0;

        // worldPositionStays 를 끈다. 어차피 FitTo 가 스케일을 덮어쓰므로
        // 부모 스케일을 상쇄하는 계산이 낭비이고, 자리가 없을 때의 결과도 예측하기 쉬워진다
        mark.Visual = Instantiate(mark.VisualPrefab, MountTransformOf(mark), false);
        mark.Visual.name = $"Mark_{mark.Owner.Data.name}";

        // 자리는 부모가 정한다. 표식은 그 칸 한가운데에 놓인다
        mark.Visual.transform.localPosition = Vector3.zero;

        FitTo(mark.Visual, MountSizeOf(mark));

        // 칸에 꽉 찬 상태를 기억해둔다. 겹이 늘 때마다 여기에 배율만 곱한다 —
        // 현재 스케일에 곱하면 값이 겹겹이 불어난다
        mark.BaseScale = mark.Visual.transform.localScale;
    }

    /// <summary>
    /// 표식 크기를 자리가 정한 값으로 <b>덮어쓴다</b>.
    ///
    /// 예전에는 렌더러 bounds 를 재서 칸에 맞췄는데, 그 값이 스프라이트 PPU · 드로우 모드 ·
    /// TMP 메시 생성 시점에 따라 흔들려서 결과를 예측할 수 없었다. 지금은 재지 않는다 —
    /// <b>표식 프리팹은 1유닛 규격</b>이고, 칸 값이 곧 배율이다.
    /// AreaDelivery 의 본체 프리팹이 쓰는 규칙과 같다.
    ///
    /// 곱이 아니라 대입이라 Instantiate 가 부모 스케일을 상쇄해둔 값도 같이 지워진다 —
    /// 그래야 적을 키우든 줄이든 표식이 몸집을 그대로 따라간다.
    /// </summary>
    static void FitTo(GameObject visual, float localSize)
    {
        if (localSize <= 0.0001f) return;

        visual.transform.localScale = Vector3.one * localSize;
    }

    /// <summary>
    /// 표식을 자기 칸 한가운데에 놓는다. 자리가 다르면 크기는 서로 영향을 주지 않는다.
    ///
    /// 같은 자리를 쓰는 표식이 둘 이상일 때만 <b>한 겹씩 바깥으로</b> 키운다 —
    /// 겹치면 맨 위엣것만 보이고 나머지는 없는 것과 같기 때문이다.
    /// 표식마다 다른 MarkSlot 번호를 주면 이 경로는 아예 안 탄다.
    /// </summary>
    void Reposition()
    {
        EnsureMounts();

        for (int i = 0; i < marks.Count; i++)
        {
            if (marks[i].Visual == null) continue;

            Transform t = marks[i].Visual.transform;

            t.localPosition = Vector3.zero;
            t.localScale = marks[i].BaseScale * RingScale(RingIndexOf(i));
        }
    }

    /// <summary>
    /// 같은 자리를 쓰는 표식 중 몇 번째 겹인가.
    ///
    /// <b>붙은 순서를 쓰면 안 된다.</b> 옆 표식이 만료될 때마다 남은 표식의 겹 번호가 밀려서
    /// 크기가 커졌다 작아졌다 하고, 화면 전체가 웅웅거린다. 게다가 같은 증강인데
    /// 적마다 크기가 달라 보여서 "저 적이 더 중요한가" 로 잘못 읽힌다.
    /// 증강 이름 순으로 매기면 누가 붙었다 사라지든 내 겹 번호는 그대로다.
    /// </summary>
    int RingIndexOf(int index)
    {
        int rank = 0;

        for (int i = 0; i < marks.Count; i++)
        {
            if (i == index) continue;
            if (marks[i].Slot != marks[index].Slot) continue;

            if (string.CompareOrdinal(OwnerNameOf(marks[i]), OwnerNameOf(marks[index])) < 0)
                rank++;
        }

        return rank;
    }

    static string OwnerNameOf(SearchMark mark)
        => mark.Owner != null && mark.Owner.Data != null ? mark.Owner.Data.name : string.Empty;

    /// <summary>
    /// 적이 회전 애니메이션을 가지면 표식도 같이 돈다. 표식은 읽는 것이라 돌면 안 된다.
    ///
    /// <b>Animator 가 돈 뒤여야 하므로 Update 가 아니라 LateUpdate 다.</b>
    /// Update 에서 되돌리면 같은 프레임에 Animator 가 다시 덮어써서 아무 일도 안 일어난다.
    /// 위치는 일부러 안 건드린다 — 몸이 흔들리면 표식도 같이 흔들리는 편이 붙어 보인다.
    /// </summary>
    void LateUpdate()
    {
        if (marks.Count == 0) return;

        for (int i = 0; i < marks.Count; i++)
            if (marks[i].Visual != null)
                marks[i].Visual.transform.rotation = Quaternion.identity;
    }

#if UNITY_EDITOR
    // 표식 오브젝트가 없어도 씬에서 탐색 대상을 눈으로 찾을 수 있게 한다
    void OnDrawGizmos()
    {
        if (marks.Count == 0) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.35f);

        int mountCount = mounts != null ? mounts.Length : 0;

        string where = mountCount > 0 ? $"자리 {mountCount}개" : "자리 없음 — 본체에 원본 크기";

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.4f, $"탐색 {marks.Count} · {where}");
    }
#endif
}
