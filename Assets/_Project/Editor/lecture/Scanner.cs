using UnityEngine;

public class Scanner : MonoBehaviour
{
    public float scanRange;
    public LayerMask targetLayer;
    public RaycastHit2D[] targets;
    public Transform nearestTarget;

    void FixedUpdate()
    {
        targets       = Physics2D.CircleCastAll(transform.position, scanRange, Vector2.zero, 0, targetLayer);
        nearestTarget = GetNearest();
    }

    Transform GetNearest()
    {
        Transform result = null;
        float diff = 100f;

        foreach (RaycastHit2D target in targets)
        {
            float currDiff = Vector3.Distance(transform.position, target.transform.position);
            if (currDiff < diff)
            {
                diff   = currDiff;
                result = target.transform;
            }
        }

        return result;
    }
}
