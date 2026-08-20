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

    public static Summon Place(GameObject prefab, Vector3 position,
                               AugmentInstance owner, AugmentData behaviour,
                               float duration, int maxAlive)
    {
        if (prefab == null || behaviour == null) return null;

        GameObject go = Instantiate(prefab, position, Quaternion.identity);

        Summon summon = go.GetComponent<Summon>();
        if (summon == null) summon = go.AddComponent<Summon>();

        summon.owner = owner;
        summon.expireAt = duration > 0f ? Time.time + duration : 0f;

        summon.Drive(behaviour, owner.Level);
        Register(summon, maxAlive);

        return summon;
    }

    /// <summary>
    /// 러너를 붙여 자기 파이프라인을 돌리게 한다.
    /// 레벨은 소환한 증강을 따라가므로 소환물도 같이 성장한다.
    /// </summary>
    void Drive(AugmentData behaviour, int level)
    {
        AugmentRunner runner = GetComponent<AugmentRunner>();
        if (runner == null) runner = gameObject.AddComponent<AugmentRunner>();

        runner.Setup(new AugmentInstance(behaviour, level));
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

            if (oldest != null) Destroy(oldest.gameObject);
        }
    }

    void Update()
    {
        if (expireAt > 0f && Time.time >= expireAt) Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (owner == null) return;

        if (alive.TryGetValue(owner, out List<Summon> list)) list.Remove(this);
    }

    /// <summary>런을 다시 시작할 때 부른다. 안 부르면 죽은 참조가 남는다.</summary>
    public static void Clear() => alive.Clear();
}
