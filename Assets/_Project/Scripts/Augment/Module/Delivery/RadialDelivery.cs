using UnityEngine;

/// <summary>
/// 기준 방향을 중심으로 여러 발 방사한다. 타겟 위치가 아니라 각도로 쏘는 것이 Projectile 과 다르다.
/// 퍼짐 각도가 360이면 사방으로 균등, 좁히면 부채꼴(산탄)이 된다.
/// </summary>
[System.Serializable]
[ModuleInfo("각도로 방사 — 타겟 위치 무시", "360이면 사방, 좁히면 산탄")]
public class RadialDelivery : ProjectileDeliveryBase
{
    public enum AimBasis
    {
        /// <summary>매번 무작위 방향. 사방 방사에 어울린다.</summary>
        Random,

        /// <summary>첫 타겟 쪽. 부채꼴을 적에게 겨눌 때.</summary>
        FirstTarget,

        /// <summary>아래 고정 각도.</summary>
        Fixed,

        /// <summary>여기까지 오게 한 진행 방향. 하위 파이프라인에서 "가던 쪽으로 계속" 퍼질 때.</summary>
        Incoming
    }

    [Header("방사")]
    [Sheet("수량")]
    [Tooltip("발사 수.\n" +
             "0 × 1 이면 시트 그대로, 0 × 2 면 시트의 두 배 — 레벨업을 따라간다.")]
    public Scalable projectileCount = Scalable.Ratio(1f);

    [Tooltip("퍼지는 각도(도). 360이면 사방으로 균등, 60이면 좁은 부채꼴(산탄).")]
    [Range(0f, 360f)] public float spreadAngle = 360f;

    [Tooltip("부채꼴의 중심을 어느 방향으로 잡을지.")]
    public AimBasis aimBasis = AimBasis.Random;

    [Detail]
    [Tooltip("aimBasis 가 Fixed 일 때 쓰는 각도(도). 0이 오른쪽, 90이 위.")]
    public float fixedAngle = 90f;

    [Detail]
    [Tooltip("각 발에 더해지는 무작위 흔들림(도). 0이면 정확히 균등하게 나간다.")]
    public float angleJitter = 0f;

    public override void Execute(AugmentContext ctx, System.Action<HitInfo> onHit)
    {
        if (!HasPrefab(ctx)) return;

        int count = projectileCount.IntOf(ctx.Stat.count);

        Vector2 origin = ctx.Owner.position;
        PlayLaunch(ctx, origin);

        // 이번 방사분 전체가 공유할 적중 기록. 한 놈에게 몰리는 것을 막는다
        var volley = NewVolley();

        // 부채꼴 중심을 통째로 돌린다. 같은 방사를 각도만 바꿔 여러 개 넣을 수 있다
        float center = CenterAngle(ctx, origin) + directionOffset;
        bool full = spreadAngle >= 359.9f;

        // 360도는 첫 발과 끝 발이 겹치므로 count 로, 부채꼴은 양 끝을 채우도록 count-1 로 나눈다
        float step = count <= 1 ? 0f
                   : full ? spreadAngle / count
                          : spreadAngle / (count - 1);

        float start = (full || count <= 1) ? center : center - spreadAngle * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float deg = start + step * i;

            if (angleJitter > 0f)
                deg += Random.Range(-angleJitter, angleJitter);

            float rad = deg * Mathf.Deg2Rad;

            Fire(ctx, origin, new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)), onHit, null, volley);
        }
    }

    /// <summary>부채꼴 중심 각도(도).</summary>
    float CenterAngle(AugmentContext ctx, Vector2 origin)
    {
        if (aimBasis == AimBasis.Fixed) return fixedAngle;

        if (aimBasis == AimBasis.Incoming)
        {
            // 최초 발동은 온 방향이 없다. 그때는 고정 각도로 물러난다
            if (!ctx.HasDirection) return fixedAngle;

            Vector2 d = ctx.Heading;
            return Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        }

        if (aimBasis == AimBasis.FirstTarget)
        {
            for (int i = 0; i < ctx.Targets.Count; i++)
            {
                Vector2 delta = ctx.Targets.Items[i].Position - origin;

                if (delta.sqrMagnitude > 0.0001f)
                    return Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            }

            // 쓸 만한 타겟이 없으면 고정 각도로 물러난다
            return fixedAngle;
        }

        return Random.Range(0f, 360f);
    }
}
