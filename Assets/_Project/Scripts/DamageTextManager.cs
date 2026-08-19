using UnityEngine;

/// <summary>
/// 데미지 표시 관리 싱글톤.
/// 어떤 색·크기로 띄울지는 DamagePipeline 이 정해서 넘겨준다.
/// </summary>
public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance;

    [Tooltip("피해 숫자 색·크기 표. 비우면 전부 흰색 기본 스타일로 나온다.")]
    [SerializeField] DamageTextPalette palette;

    void Awake()
    {
        Instance = this;

        // 파이프라인은 씬 오브젝트를 모르므로 여기서 넘겨준다
        DamagePipeline.Palette = palette;
    }

    /// <summary>텍스트 프리팹 생성.</summary>
    public void ShowDamage(float damage, Transform target, DamageTextStyle style)
    {
        Vector3 spawnPos = GetSpawnPosition(target);

        GameObject textObject = GameManager.instance.poolManager.Get(PoolType.Effect, 0);

        textObject.transform.position = spawnPos;

        DamageText damageText = textObject.GetComponent<DamageText>();
        damageText.Initialize(damage, style);
    }

    /// <summary>스타일 없이 부르면 기본값. 옛 경로 호환용.</summary>
    public void ShowDamage(float damage, Transform target)
        => ShowDamage(damage, target, DamageTextStyle.Default);

    /// <summary>생성 위치 조정. 겹쳐 뜨지 않게 살짝 흩는다.</summary>
    Vector3 GetSpawnPosition(Transform target)
    {
        Vector3 pos = target.position;

        pos += Vector3.up * 1.0f;

        pos.x += Random.Range(-0.3f, 0.3f);
        pos.y += Random.Range(-0.2f, 0.2f);

        return pos;
    }
}
