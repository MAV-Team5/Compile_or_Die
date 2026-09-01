using UnityEngine;

/// <summary>
/// 증강이 넘긴 파라미터를 Animator 에 꽂는 기본 구현. 플레이어 · 적 · 소환물에 붙인다.
///
/// 모션을 고르고 되돌리는 것은 전부 Animator 몫이다 —
/// 여기서는 값만 넣고, 어떤 상태로 가서 언제 돌아올지는 전이 조건이 정한다.
/// 그래서 애니메이션 담당은 Animator 창만 보면 되고 증강 에셋을 안 봐도 된다.
/// </summary>
public class AnimatorDriver : MonoBehaviour, IAnimationDriver
{
    [System.Serializable]
    public class Mapping
    {
        [Tooltip("증강이 부르는 이름.")]
        public string from;

        [Tooltip("이 오브젝트의 Animator 파라미터 이름. 비우면 위 이름을 그대로 쓴다.")]
        public string to;
    }

    [Tooltip("값을 넣을 Animator 들. 비우면 자기 자신과 자식에서 전부 모은다.\n" +
             "캐릭터 몸통처럼 나중에 붙는 경우도 알아서 다시 찾으므로 보통 비워둔다.\n" +
             "특정 애니메이터에만 보내고 싶을 때 직접 채운다.")]
    public Animator[] animators;

    /// <summary>자동으로 모아둔 것. 수동 지정이 있으면 그쪽이 우선한다.</summary>
    Animator[] found;

    Animator[] Targets => animators != null && animators.Length > 0 ? animators : found;

    [Tooltip("이름이 다를 때만 채우면 된다.\n" +
             "증강 에셋 하나로 오브젝트마다 다른 파라미터를 건드릴 때 쓴다.")]
    public Mapping[] mappings;

    void Awake() => Rebind();

    /// <summary>
    /// Animator 를 다시 찾는다.
    /// 캐릭터 몸통처럼 <b>나중에 자식으로 붙는</b> 경우가 있어서 한 번만 찾으면 놓친다.
    /// 캐릭터를 갈아끼운 뒤 직접 불러도 되고, 안 불러도 첫 모션 때 알아서 찾는다.
    /// </summary>
    public void Rebind() => found = GetComponentsInChildren<Animator>(true);

    /// <summary>
    /// 파라미터를 가진 애니메이터에게만 값을 보낸다.
    ///
    /// <b>파라미터 이름이 곧 대상 구분이다.</b> 몸통에만 Attack 이 있으면 몸통만 반응하고,
    /// 무기에도 있으면 둘 다 반응한다 — 채널을 따로 두지 않아도 이름으로 갈린다.
    /// </summary>
    public void SetMotion(string parameter, float value)
    {
        if (string.IsNullOrEmpty(parameter)) return;

        // 씬 시작 뒤에 몸통이 붙었을 수 있다
        if (Targets == null || Targets.Length == 0) Rebind();

        Animator[] targets = Targets;
        if (targets == null) return;

        string name = Resolve(parameter);

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) continue;

            // 없는 파라미터에 값을 넣으면 Animator 가 경고를 쏟아낸다. 먼저 있는지 본다
            if (TryGetType(targets[i], name, out AnimatorControllerParameterType type))
                Apply(targets[i], name, value, type);
        }
    }

    static void Apply(Animator target, string name, float value,
                      AnimatorControllerParameterType type)
    {
        switch (type)
        {
            case AnimatorControllerParameterType.Trigger:
                // 0을 넘기면 걸어둔 트리거를 도로 내린다
                if (Mathf.Approximately(value, 0f)) target.ResetTrigger(name);
                else target.SetTrigger(name);
                break;

            case AnimatorControllerParameterType.Bool:
                target.SetBool(name, !Mathf.Approximately(value, 0f));
                break;

            case AnimatorControllerParameterType.Int:
                target.SetInteger(name, Mathf.RoundToInt(value));
                break;

            case AnimatorControllerParameterType.Float:
                target.SetFloat(name, value);
                break;
        }
    }

    string Resolve(string parameter)
    {
        if (mappings == null) return parameter;

        for (int i = 0; i < mappings.Length; i++)
        {
            if (mappings[i].from != parameter) continue;

            return string.IsNullOrEmpty(mappings[i].to) ? parameter : mappings[i].to;
        }

        return parameter;
    }

    static bool TryGetType(Animator target, string name,
                           out AnimatorControllerParameterType type)
    {
        for (int i = 0; i < target.parameterCount; i++)
        {
            AnimatorControllerParameter p = target.GetParameter(i);

            if (p.name != name) continue;

            type = p.type;
            return true;
        }

        type = default;
        return false;
    }
}
