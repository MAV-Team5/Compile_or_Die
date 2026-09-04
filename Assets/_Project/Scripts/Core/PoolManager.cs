using System.Collections.Generic;
using UnityEngine;
public enum PoolType
{
    Enemy,
    Bullet,
    Effect,
    Exp,
    Item
}
//오브젝트 풀링을 위한 풀 매니저, 각각의 풀 리스트를 사용.
public class PoolManager : MonoBehaviour
{
    [Header("Pool Parents")]
    [SerializeField] 
    private Transform enemyParent;
    [SerializeField] 
    private Transform bulletParent;
    [SerializeField] 
    private Transform effectParent;
    [SerializeField] 
    private Transform expParent;
    [SerializeField] 
    private Transform itemParent;

    [Header("Enemy")]
    public GameObject[] enemyPrefabs;
    [Header("Bullet")]
    public GameObject[] bulletPrefabs;
    [Header("Effect")]
    public GameObject[] effectPrefabs;
    [Header("EXP")]
    public GameObject[] expPrefabs;
    [Header("Item")]
    public GameObject[] itemPrefabs;


    // 프리팹 참조를 키로 쓰는 풀. 인덱스 등록 없이 증강 투사체를 담는다
    readonly Dictionary<GameObject, List<GameObject>> prefabPools = new();

    List<GameObject>[] enemyPools;
    List<GameObject>[] bulletPools;
    List<GameObject>[] effectPools;
    List<GameObject>[] expPools;
    List<GameObject>[] itemPools;

    void Awake()
    {
        enemyPools = CreatePools(enemyPrefabs.Length);
        bulletPools = CreatePools(bulletPrefabs.Length);
        effectPools = CreatePools(effectPrefabs.Length);
        expPools = CreatePools(expPrefabs.Length);
        itemPools = CreatePools(itemPrefabs.Length);
    }

    /// <summary>
    /// 각 풀 초기화 함수.
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    List<GameObject>[] CreatePools(int size)
    {
        List<GameObject>[] pools = new List<GameObject>[size];

        for (int i = 0; i < size; i++)
        {
            pools[i] = new List<GameObject>();
        }

        return pools;
    }
    /// <summary>
    /// 각 풀링 타입에 맞춰 오브젝트 찾아오기. 호출용.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public GameObject Get(PoolType type, int index)
    {
        switch (type)
        {
            case PoolType.Enemy:
                return GetObject(enemyPools, enemyPrefabs, index, enemyParent);
            case PoolType.Bullet:
                return GetObject(bulletPools, bulletPrefabs, index, bulletParent);
            case PoolType.Effect:
                return GetObject(effectPools, effectPrefabs, index, effectParent);
            case PoolType.Exp:
                return GetObject(expPools, expPrefabs, index, expParent);
            case PoolType.Item:
                return GetObject(itemPools, itemPrefabs, index, itemParent);
            default:
                return null;
        }
    }

    /// <summary>
    /// 프리팹을 직접 넘겨 받아온다. 인덱스 등록이 필요 없어 증강처럼 프리팹이 많은 쪽에 쓴다.
    /// category 는 하이어라키에서 어느 묶음 밑으로 들어갈지만 정한다.
    /// </summary>
    public GameObject Get(GameObject prefab, PoolType category = PoolType.Bullet)
    {
        if (prefab == null) return null;

        if (!prefabPools.TryGetValue(prefab, out List<GameObject> pool))
        {
            pool = new List<GameObject>();
            prefabPools[prefab] = pool;
        }

        GameObject found = TakeFree(pool);

        if (found != null)
        {
            // 지난번에 적에게 붙었을 수 있다. 안 떼면 그 적이 되살아날 때 같이 떠오른다
            Transform parent = ParentOf(category);
            if (found.transform.parent != parent) found.transform.SetParent(parent, false);

            return found;
        }

        GameObject created = Instantiate(prefab, ParentOf(category));
        pool.Add(created);
        return created;
    }

    Transform ParentOf(PoolType category) => category switch
    {
        PoolType.Enemy  => enemyParent,
        PoolType.Effect => effectParent,
        PoolType.Exp    => expParent,
        PoolType.Item   => itemParent,
        _               => bulletParent
    };

    /// <summary>
    /// 해당하는 풀에서 인덱스로 프리팹 목록에서 찾아 활성화.
    /// </summary>
    /// <param name="pools"></param>
    /// <param name="prefabs"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <summary>
    /// 풀별로 "지난번에 꺼낸 자리". 여기서부터 이어서 찾는다.
    ///
    /// <b>매번 0번부터 훑으면 풀이 커질수록 느려진다.</b> 풀은 줄어들지 않으므로
    /// 런이 길어질수록 한 번 꺼내는 비용이 계속 오르고, 광역기로 수십 마리가
    /// 동시에 맞아 피해 숫자를 한꺼번에 꺼낼 때 그게 프레임으로 나타난다.
    /// 쓰고 반납하는 순서가 대체로 비슷하므로, 이어서 찾으면 보통 한두 번에 걸린다.
    /// </summary>
    readonly Dictionary<List<GameObject>, int> cursors = new();

    public GameObject GetObject(List<GameObject>[] pools, GameObject[] prefabs, int index, Transform parent)
    {
        List<GameObject> pool = pools[index];

        GameObject select = TakeFree(pool);

        if (select == null)
        {
            select = Instantiate(prefabs[index], parent);
            pool.Add(select);
        }

        return select;
    }

    /// <summary>커서 자리에서 시작해 한 바퀴만 돌며 쉬고 있는 것을 찾는다.</summary>
    GameObject TakeFree(List<GameObject> pool)
    {
        if (pool.Count == 0) return null;

        cursors.TryGetValue(pool, out int cursor);

        for (int step = 0; step < pool.Count; step++)
        {
            int i = (cursor + step) % pool.Count;
            GameObject item = pool[i];

            // 누군가 Destroy 로 없앤 항목이 섞이면 activeSelf 를 읽는 순간 터진다
            if (item == null) continue;
            if (item.activeSelf) continue;

            cursors[pool] = (i + 1) % pool.Count;

            item.SetActive(true);
            return item;
        }

        // 한 바퀴 다 돌았는데 없다. 다음엔 새로 만든 것(맨 뒤)부터 보게 둔다
        cursors[pool] = pool.Count;

        return null;
    }
}
