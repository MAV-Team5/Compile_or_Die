using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 대상이 지닌 상태이상 목록. 필요할 때 자동으로 붙는다.
/// 만료 정리 · 주기 실행 · 시각 오브젝트 수명을 맡는다.
///
/// 탐색 표식은 MarkerHolder 가 따로 관리한다. 둘은 시각 자리도 겹치지 않는다 —
/// 표식은 머리 위 앵커, 상태이상은 대상 몸에 붙는다.
/// </summary>
public class StatusHolder : MonoBehaviour
{
    /// <summary>붙어 있는 상태 1개분. 개체별 값은 전부 여기 있다.</summary>
    public class Active
    {
        /// <summary>설계도. 증강 에셋에 사는 공유 객체라 상태를 담지 않는다.</summary>
        public Status Source;

        /// <summary>누가 걸었나. 해제할 때 쓴다.</summary>
        public AugmentInstance Owner;

        /// <summary>이 시각이 지나면 사라진다. 0이면 무기한.</summary>
        public float ExpireAt;

        /// <summary>이 상태의 세기. 지속 피해량 등. 시트 수치에서 뽑아 온다.</summary>
        public float Magnitude;

        public GameObject Visual;

        /// <summary>주기형 상태가 쓰는 개별 타이머.</summary>
        public float TickTimer;

        public bool IsExpired => ExpireAt > 0f && Time.time >= ExpireAt;
    }

    readonly List<Active> statuses = new();

    /// <summary>이동속도 배율. 여러 둔화가 겹치면 곱해진다.</summary>
    public float SpeedMultiplier { get; private set; } = 1f;

    public int Count => statuses.Count;

    /// <summary>이 대상의 피해 진입점. 지속 피해가 여기로 들어간다.</summary>
    public IDamageReceiver Receiver { get; private set; }

    void Awake() => CacheReceiver();

    void CacheReceiver()
    {
        if (!TryGetComponent(out IDamageReceiver receiver))
            receiver = GetComponentInParent<IDamageReceiver>();

        Receiver = receiver;
    }

    public static StatusHolder GetOrAdd(Transform target)
    {
        if (target == null) return null;

        if (target.TryGetComponent(out StatusHolder holder)) return holder;

        holder = target.gameObject.AddComponent<StatusHolder>();
        return holder;
    }

    /// <summary>상태를 건다. 같은 증강·같은 종류면 설정에 따라 갱신하거나 쌓는다.</summary>
    public void Apply(Status source, AugmentInstance owner, float duration, float magnitude)
    {
        if (source == null) return;
        if (Receiver == null) CacheReceiver();

        if (source.refreshInsteadOfStack)
        {
            Active existing = Find(source, owner);

            if (existing != null)
            {
                existing.ExpireAt = duration > 0f ? Time.time + duration : 0f;
                existing.Magnitude = magnitude;
                Rebuild();
                return;
            }
        }

        var active = new Active
        {
            Source = source,
            Owner = owner,
            ExpireAt = duration > 0f ? Time.time + duration : 0f,
            Magnitude = magnitude,
            Visual = CreateVisual(source)
        };

        statuses.Add(active);
        source.OnApplied(this, active);

        Rebuild();
    }

    /// <summary>이 증강이 건 상태를 전부 뗀다.</summary>
    public void RemoveByOwner(AugmentInstance owner)
    {
        bool changed = false;

        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            if (statuses[i].Owner != owner) continue;

            Remove(i);
            changed = true;
        }

        if (changed) Rebuild();
    }

    Active Find(Status source, AugmentInstance owner)
    {
        for (int i = 0; i < statuses.Count; i++)
            if (statuses[i].Source == source && statuses[i].Owner == owner) return statuses[i];

        return null;
    }

    void Update()
    {
        if (statuses.Count == 0) return;

        float dt = Time.deltaTime;
        bool changed = false;

        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            Active active = statuses[i];

            if (active.IsExpired)
            {
                Remove(i);
                changed = true;
                continue;
            }

            // 주기형 상태가 스스로 일하는 자리
            active.Source.Tick(this, active, dt);

            // Tick 이 대상을 죽이면 OnDisable 이 목록을 통째로 비운다.
            // 그대로 두면 다음 회차가 사라진 칸을 읽는다
            if (!isActiveAndEnabled || i > statuses.Count) return;
        }

        if (changed) Rebuild();
    }

    // 대상이 풀로 반납될 때 상태가 남아있으면 다음 개체가 물려받는다
    void OnDisable()
    {
        for (int i = statuses.Count - 1; i >= 0; i--) Remove(i);

        SpeedMultiplier = 1f;
    }

    void Remove(int index)
    {
        Active active = statuses[index];

        active.Source.OnRemoved(this, active);

        if (active.Visual != null) Destroy(active.Visual);

        statuses.RemoveAt(index);
    }

    /// <summary>속도 배율처럼 합쳐 쓰는 값을 다시 계산한다.</summary>
    void Rebuild()
    {
        float speed = 1f;

        for (int i = 0; i < statuses.Count; i++)
            speed *= statuses[i].Source.SpeedMultiplier(statuses[i]);

        // 0이 되면 영영 못 움직인다. 완전 정지는 별도 스턴 상태로 다룰 것
        SpeedMultiplier = Mathf.Max(0.05f, speed);
    }

    GameObject CreateVisual(Status source)
    {
        if (source.statusVfx == null) return null;

        // 적 몸에 붙는 연출이라 위치는 대상 원점 그대로 둔다
        GameObject visual = Instantiate(source.statusVfx, transform);
        visual.name = $"Status_{source.GetType().Name}";

        return visual;
    }
}
