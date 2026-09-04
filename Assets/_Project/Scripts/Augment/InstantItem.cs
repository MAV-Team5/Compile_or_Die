using UnityEngine;

/// <summary>
/// 보유 개념 없이 <b>얻는 순간 한 번</b> 적용되고 사라지는 아이템 효과.
///
/// <b>왜 UI 밖에 있나</b> — 예전에는 AugmentSelectUI 안에 있었다. 그때는 얻는 길이
/// 레벨업 카드뿐이었기 때문이다. 지금은 상자에서 떨어진 것을 주워서도 얻으므로,
/// 픽업이 UI 를 참조하는 거꾸로 된 그림이 된다. 얻는 길이 또 늘어도 여기만 부르면 된다.
///
/// <see cref="AugmentManager"/> 에 등록하지 않는다 — 레벨도 HUD 슬롯도 없다.
/// </summary>
public static class InstantItem
{
    /// <summary>효과를 지금 적용한다. 적용할 것이 없었으면 false.</summary>
    public static bool Apply(AugmentData data)
    {
        if (data == null || data.instantEffect == InstantItemEffect.None) return false;

        Player player = GameManager.instance != null ? GameManager.instance.player : null;
        if (player == null) return false;

        switch (data.instantEffect)
        {
            case InstantItemEffect.Heal:
                return Heal(player, data);

            case InstantItemEffect.SpeedBoost:
                return SpeedBoost(player, data);

            case InstantItemEffect.CollectExp:
                return CollectExp(player, data);
        }

        return false;
    }

    /// <summary>
    /// 흩어진 경험치를 전부 끌어온다. 오브를 순간이동시키지 않고 <b>끌리게만</b> 한다 —
    /// 사방에서 빨려오는 그림이 곧 이 아이템의 보상이고, 즉시 사라지면 뭘 먹었는지 모른다.
    ///
    /// <paramref name="data"/>.instantValue 가 0보다 크면 그 반경 안만 부른다.
    /// </summary>
    static bool CollectExp(Player player, AugmentData data)
    {
        ExpMove[] orbs = Object.FindObjectsByType<ExpMove>(FindObjectsSortMode.None);

        if (orbs.Length == 0) return false;

        float limit = data.instantValue;
        Vector2 from = player.transform.position;

        int pulled = 0;

        for (int i = 0; i < orbs.Length; i++)
        {
            if (orbs[i] == null) continue;

            if (limit > 0f &&
                Vector2.Distance(from, orbs[i].transform.position) > limit) continue;

            orbs[i].forcedMagnet = true;
            pulled++;
        }

        Log($"{data.displayName}: collected {pulled} object(s)");

        return pulled > 0;
    }

    static bool Heal(Player player, AugmentData data)
    {
        if (!player.TryGetComponent(out PlayerHealth health)) return false;

        float amount = health.Max * data.instantValue;
        health.Heal(amount);

        Log($"{data.displayName}: HP +{amount:0} ({data.instantValue:P0})");

        return true;
    }

    static bool SpeedBoost(Player player, AugmentData data)
    {
        player.ApplySpeedBoost(1f + data.instantValue, data.instantDuration);

        Log($"{data.displayName}: SPEED +{data.instantValue:P0} ({data.instantDuration:0}s)");

        return true;
    }

    static void Log(string line)
    {
        if (LogManager.Instance != null) LogManager.Instance.Skill(line);
    }
}
