using UnityEngine;

/// <summary>
/// 데미지 표시 관리 싱글톤.
/// </summary>
public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance;
    private void Awake()
    {
        Instance = this;
    }
    /// <summary>
    /// 텍스트 프리팹 생성
    /// </summary>
    /// <param name="damage"></param>
    /// <param name="target"></param>
    public void ShowDamage(float damage, Transform target)
    {
        Vector3 spawnPos = GetSpawnPosition(target);

        GameObject textObject = GameManager.instance.pool.Get(PoolType.Effect, 0);

        textObject.transform.position = spawnPos;

        DamageText damageText = textObject.GetComponent<DamageText>();
        damageText.Initialize(damage);
    }
    /// <summary>
    /// 생성 위치 조정.
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    private Vector3 GetSpawnPosition(Transform target)
    {
        Vector3 pos = target.position;

        pos += Vector3.up * 1.0f;

        pos.x += Random.Range(-0.3f, 0.3f);
        pos.y += Random.Range(-0.2f, 0.2f);

        return pos;
    }
}