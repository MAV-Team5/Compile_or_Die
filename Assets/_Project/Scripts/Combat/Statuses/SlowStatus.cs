using UnityEngine;

/// <summary>
/// 이동속도를 줄인다. 정렬 계열의 군중 제어.
/// 여러 둔화가 겹치면 배율이 곱해지되 완전 정지(0)에는 닿지 않는다.
/// </summary>
[System.Serializable]
[ModuleInfo("이동속도 감소", "밀치거나 당기려면 Knockback")]
public class SlowStatus : Status
{
    [Tooltip("줄일 비율. 0.3이면 30% 느려진다(속도 70%).\n" +
             "0이면 상태를 걸 때 정한 세기를 비율로 쓴다.")]
    [Range(0f, 0.95f)] public float slowRatio = 0.3f;

    public override float SpeedMultiplier(StatusHolder.Active active)
    {
        float ratio = slowRatio > 0f ? slowRatio : active.Magnitude;

        return 1f - Mathf.Clamp(ratio, 0f, 0.95f);
    }
}
