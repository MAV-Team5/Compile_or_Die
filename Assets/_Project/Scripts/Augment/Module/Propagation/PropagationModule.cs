using System.Collections.Generic;
using UnityEngine;

/// <summary>적중 지점에서 다음 대상을 찾는다. 전파·연쇄 전용.</summary>
[System.Serializable]
public abstract class PropagationModule : AugmentModule
{
    /// <summary>from 에서 다음 대상들을 찾아 results 에 담는다. ctx.Excluded 는 제외.</summary>
    public abstract void Next(AugmentContext ctx, Vector2 from, List<Transform> results);
}