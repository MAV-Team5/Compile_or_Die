using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 월드에 놓인 소환물 하나. 수명과 개수 상한을 맡는다.
///
/// 실제 공격은 같은 오브젝트에 붙은 <see cref="AugmentRunner"/> 가 한다 —
/// 러너는 자기 transform 을 원점으로 삼으므로, 떼어놓으면 그 자리가 곧 시전자가 된다.
/// 그래서 소환물의 행동을 증강 에셋으로 그대로 조립할 수 있다.
/// </summary>
public class Summon : MonoBehaviour
{
    /// <summary>증강별 생존 목록. 상한을 넘으면 가장 오래된 것부터 걷어낸다.</summary>
    static readonly Dictionary<AugmentInstance, List<Summon>> alive = new();

    AugmentInstance owner;
    float expireAt;

    /// <summary>사라지기 직전 순서대로 한 번씩 실행할 증강. 예) JIT 레이저 → 폭발.</summary>
    List<AugmentData> finale;
    float finaleInterval;
    bool expiring;

    public static Summon Place(GameObject prefab, Vector3 position,
                               AugmentInstance owner, AugmentData behaviour,
                               float duration, int maxAlive,
                               List<AugmentData> finale = null, float finaleInterval = 0f)
    {
        if (prefab == null || behaviour == null) return null;

        GameObject go = Instantiate(prefab, position, Quaternion.identity);

        Summon summon = go.GetComponent<Summon>();
        if (summon == null) summon = go.AddComponent<Summon>();

        summon.owner = owner;
        summon.expireAt = duration > 0f ? Time.time + duration : 0f;
        summon.finale = finale;
        summon.finaleInterval = finaleInterval;

        summon.Drive(behaviour, owner);
        Register(summon, maxAlive);

        return summon;
    }

    /// <summary>
    /// 러너를 붙여 자기 파이프라인을 돌리게 한다.
    /// 레벨은 소환한 증강을 따라가므로 소환물도 같이 성장한다.
    /// </summary>
    void Drive(AugmentData behaviour, AugmentInstance summoner)
    {
        AugmentRunner runner = GetComponent<AugmentRunner>();
        if (runner == null) runner = gameObject.AddComponent<AugmentRunner>();

        // 소환물이 준 피해는 소환한 증강의 성적이다
        runner.Setup(new AugmentInstance(behaviour, summoner.Level) { StatsOwner = summoner });
        runner.DriveSelf();
    }

    static void Register(Summon summon, int maxAlive)
    {
        if (!alive.TryGetValue(summon.owner, out List<Summon> list))
        {
            list = new List<Summon>();
            alive[summon.owner] = list;
        }

        list.Add(summon);

        if (maxAlive <= 0) return;

        // 새로 부른 것이 살아남는 편이 플레이어가 읽기 쉽다
        while (list.Count > maxAlive)
        {
            Summon oldest = list[0];
            list.RemoveAt(0);

            // 밀려난 것도 수명이 다한 것과 똑같이 마지막 공격을 하고 사라진다
            if (oldest != null) oldest.Expire();
        }
    }

    void Update()
    {
        if (expireAt > 0f && Time.time >= expireAt) Expire();
    }

    /// <summary>
    /// 수명이 다했다. 평소 공격을 멈추고, 마지막 증강들을 순서대로 한 번씩 실행한 뒤 사라진다.
    /// 마지막이 없으면 바로 사라진다.
    /// </summary>
    public void Expire()
    {
        if (expiring) return;
        expiring = true;

        if (owner != null && alive.TryGetValue(owner, out List<Summon> list)) list.Remove(this);

        if (TryGetComponent(out AugmentRunner runner)) runner.enabled = false;

        if (finale == null || finale.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        StartCoroutine(PlayFinale());
    }

    System.Collections.IEnumerator PlayFinale()
    {
        for (int i = 0; i < finale.Count; i++)
        {
            if (finale[i] != null) FireOnce(finale[i]);

            // 마지막 단계 뒤에는 기다리지 않는다. 폭발과 함께 바로 사라져야 한다
            if (i < finale.Count - 1 && finaleInterval > 0f) yield return new WaitForSeconds(finaleInterval);
        }

        Destroy(gameObject);
    }

    /// <summary>증강 하나를 이 자리에서 한 번만 실행한다. 레벨·통계는 소환한 증강을 따른다.</summary>
    void FireOnce(AugmentData step)
    {
        var instance = new AugmentInstance(step, owner != null ? owner.Level : 1) { StatsOwner = owner };

        var ctx = new AugmentContext();
        ctx.Begin(transform, instance);

        AugmentPipeline.Run(ctx, instance.Build.Targeting, instance.Build.Deliveries, instance.Build.Effects);
    }

    void OnDestroy()
    {
        if (owner == null) return;

        if (alive.TryGetValue(owner, out List<Summon> list)) list.Remove(this);
    }

    /// <summary>런을 다시 시작할 때 부른다. 안 부르면 죽은 참조가 남는다.</summary>
    public static void Clear() => alive.Clear();
}
