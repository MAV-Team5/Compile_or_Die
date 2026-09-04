using UnityEngine;

/// <summary>
/// 바닥에 떨어져 있다가 주우면 즉시 효과가 걸리는 아이템. 상자(.zip)가 떨군다.
///
/// 경험치 오브(<see cref="ExpMove"/>)와 같은 자석 구조지만 <b>자석 범위가 따로다</b> —
/// 경험치는 SSD 업그레이드를 따라 넓어져야 하고, 아이템은 "부수고 주우러 간다"는
/// 판단이 남아야 해서 발밑에서만 붙는다. 둘을 같은 값으로 묶으면 상자를 부수는 순간
/// 자동으로 먹혀서 상자가 그냥 "때리면 버프" 가 된다.
///
/// 프리팹에 <c>CircleCollider2D (Is Trigger ✔)</c> 가 있어야 한다.
/// <b>레이어는 적이 아니어야 한다</b> — TargetQuery.Mask 에 걸리면 증강이 아이템을 공격한다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    [Header("무엇")]
    [Tooltip("＊ 필수 — 주웠을 때 걸릴 즉시 효과. 0xCAFE · 0xBEEF 같은 아이템 에셋.")]
    [SerializeField] AugmentData item;

    [Header("자석")]
    [Tooltip("이 거리 안에 들어오면 끌려온다. 경험치와 달리 좁게 — 발밑에서 안 미끄러질 정도.\n" +
             "0이면 자석 없이 직접 밟아야 한다.")]
    [SerializeField] float magnetRange = 1.4f;

    [SerializeField] float accel = 6f;

    [Header("연출")]
    public FxGroup pickupFx = new();

    [Tooltip("여러 개 넣으면 매번 하나를 랜덤으로 고른다. 비우면 소리가 안 난다.")]
    [SerializeField] AudioClip[] pickupClips;

    [Range(0f, 1f)] [SerializeField] float pickupVolume = 0.5f;

    Player player;
    float speed;
    bool taken;

    /// <summary>상자가 코드로 떨굴 때 무엇인지 정해준다.</summary>
    public void Setup(AugmentData data)
    {
        item = data;
        taken = false;
        speed = 0f;
    }

    // 풀에서 다시 꺼내 쓸 수 있게 켜질 때마다 초기화한다
    void OnEnable()
    {
        taken = false;
        speed = 0f;
    }

    void Update()
    {
        if (taken || magnetRange <= 0f) return;

        if (player == null)
        {
            player = GameManager.instance != null ? GameManager.instance.player : null;
            if (player == null) return;
        }

        Vector2 toPlayer = player.transform.position - transform.position;

        if (toPlayer.sqrMagnitude > magnetRange * magnetRange)
        {
            speed = 0f;
            return;
        }

        speed += accel * Time.deltaTime;

        transform.position += (Vector3)(toPlayer.normalized * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 같은 프레임에 두 콜라이더가 겹치면 두 번 먹힌다
        if (taken || !other.CompareTag("Player")) return;

        taken = true;

        InstantItem.Apply(item);

        pickupFx.PlayAt(transform.position);
        SfxPlayer.PlayAny(pickupClips, pickupVolume);

        gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (magnetRange <= 0f) return;

        Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, magnetRange);
    }
#endif
}
