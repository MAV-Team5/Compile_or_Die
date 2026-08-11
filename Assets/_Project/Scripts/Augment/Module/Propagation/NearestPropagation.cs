using System.Collections.Generic;
using UnityEngine;

/// <summary>적중 지점에서 가장 가까운 1체. Bash · C 계열.</summary>
[System.Serializable]
public class NearestPropagation : PropagationModule
{
    public float radius = 4f;

    public override void Next(AugmentContext ctx, Vector2 from, List<Transform> results)
    {
        Transform best = null;
        float bestSqr = float.MaxValue;

        List<Collider2D> hits = TargetQuery.Overlap(from, radius);

        for (int i = 0; i < hits.Count; i++)
        {
            Transform t = hits[i].transform;
            if (ctx.Excluded.Contains(t)) continue;

            float sqr = ((Vector2)t.position - from).sqrMagnitude;

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = t;
            }
        }

        if (best != null) results.Add(best);
    }
}