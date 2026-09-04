using UnityEngine;

/// <summary>
/// 세미콜론 몬스터가 쏘는 투사체. 유도 없이 발사 순간 플레이어 위치를 한 번만 겨눠
/// 그대로 직선으로 날아간다("세미콜론을 뒤늦게 던지는" 느낌이라 정교하게 유도할 필요가 없다).
///
/// <b>부모까지 훑지 않는다.</b> 플레이어의 무기 판정용 "Area" 콜라이더는 PlayerHealth 가 없는
/// 자식(또는 형제) 오브젝트인데, 부모까지 올라가서 찾으면 그 넓은 판정 범위에 스치기만 해도
/// 플레이어 루트의 PlayerHealth 를 찾아내 데미지가 들어가버린다 — 몸에 닿지 않았는데도 맞는
/// 버그의 정체. PlayerHealth 는 자기 몸통 Collider2D 와 같은 오브젝트에 있으므로,
/// 그 콜라이더 자체에서만 직접 찾는다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SemicolonProjectile : MonoBehaviour
{
    [Tooltip("이동 속도(유닛/초).")]
    [SerializeField] float speed = 8f;

    [Tooltip("맞았을 때 주는 피해.")]
    [SerializeField] float damage = 5f;

    [Tooltip("이 시간(초)이 지나면 화면 안이어도 스스로 사라진다.")]
    [SerializeField] float lifetime = 4f;

    Vector2 direction;
    float elapsed;
    bool becameVisibleOnce;

    void Awake()
    {
        if (!TryGetComponent<Rigidbody2D>(out _))
        {
            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        // \"Enemy\" 레이어에 남으면, 플레이어 스킬(TargetQuery)이 적을 찾을 때 이 투사체도
        // \"적\"로 오인해서 표식/연결 같은 스킬 이펙트가 붙어버린다.
        // 투사체는 전투 대상이 아니므로 기본(\"Default\") 레이어로 강제 고정한다.
        // 충돌 매트리스가 전체 허용(all-true)이라 Default 로 옮겨도 플레이어와의 트리거 판정은 그대로 살아있다
        gameObject.layer = LayerMask.NameToLayer("Default");

        // 기본 레이어(\"Default\")에 남으면 몬스터·배경 스프라이트에 가려 안 보일 수 있다.
        var sr = GetComponent<SpriteRenderer>();
        sr.sortingLayerName = "Effect";
        sr.sortingOrder = 40;
    }

    void OnEnable()
    {
        elapsed = 0f;
        becameVisibleOnce = false;
    }

    /// <summary>발사 시 한 번 방향을 정한다. 스폰 직후 반드시 호출할 것.</summary>
    public void Launch(Vector2 targetPosition)
    {
        Vector2 toTarget = targetPosition - (Vector2)transform.position;
        direction = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.right;

        // 회전은 일부러 안 한다 — 세미콜론(;)은 좌우 대칭이 아니라서 방향을 따라 돌리면
        // 왼쪽으로 날아갈 때 뒤집혀 보인다. 항상 정자세 그대로 유지한다
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        elapsed += Time.deltaTime;
        if (elapsed >= lifetime) Despawn();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 부모까지 안 올라간다 — 무기 판정용 "Area" 처럼 넓은 자식 콜라이더에 스치기만 해도
        // 부모(플레이어 루트)의 PlayerHealth 를 잘못 찾아내는 것을 막기 위함.
        // PlayerHealth 도, Enemy 도 전부 자기 콜라이더와 같은 오브젝트에 있으므로 이걸로 충분하다
        if (!other.TryGetComponent(out IDamageReceiver receiver)) return;

        // 몬스터끼리는 서로 안 맞아야 한다
        if (receiver is Enemy) return;

        // 무적 상태면 통과시킨다 — 여기서 안 막으면 총알만 조용히 사라진다
        if (!receiver.AcceptsDamage) return;

        receiver.TakeDamage(damage);
        Despawn();
    }

    void Despawn() => PooledSpawner.Despawn(gameObject);

    // 카메라에 한 번이라도 보인 적이 있는데 다시 안 보이게 되면 화면을 벗어난 것으로 본다.
    // 스폰 직후(아직 한 번도 안 보인 시점)에 잘못 걸리지 않도록 becameVisibleOnce 로 막는다.
    // 지난번에 이 콜백을 의심했던 건 원인이 아니었다 — 진짜 원인은 OnTriggerEnter2D 의
    // GetComponentInParent 폴백이었고 그건 이미 고쳤다
    void OnBecameVisible() => becameVisibleOnce = true;

    void OnBecameInvisible()
    {
        if (becameVisibleOnce) Despawn();
    }
}
