using UnityEngine;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance;

    [SerializeField]
    private DamageText damageTextPrefab;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowDamage(float damage, Transform target)
    {
        Vector3 spawnPos = GetSpawnPosition(target);

        DamageText text =
            Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);

        text.Initialize(damage);
    }

    private Vector3 GetSpawnPosition(Transform target)
    {
        Vector3 pos = target.position;

        pos += Vector3.up * 1.5f;

        pos.x += Random.Range(-0.3f, 0.3f);
        pos.y += Random.Range(-0.2f, 0.2f);

        return pos;
    }
}