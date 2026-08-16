using UnityEngine;
using UnityEngine.VFX;

public class Effect : MonoBehaviour
{
    public float duration;
    bool playing = true;

    float elapsed = 0.0f;
    
    void Update()
    {
        if (!playing) return;

        elapsed += Time.deltaTime;

        if (elapsed >= duration)
        {
            Destroy(gameObject);
            return;
        }
    }
}
