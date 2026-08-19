using UnityEngine;

/// <summary>
/// 몬스터 스탯.
/// </summary>
public class Enemy : MonoBehaviour, IDamageReceiver, IDisplaceable
{
    // 넉백 직후 스스로 못 움직이는 시간
    float moveSuppressRemain;

    public float speed;
    public float health;
    public float maxHealth;

    /// <summary>플레이어와 닿아 있는 동안 초당 주는 피해.</summary>
    public float contactDamage = 10f;
    public RuntimeAnimatorController[] animCon;
    public Rigidbody2D target;
    public GameObject expPrefab;
    bool isLive;

    Rigidbody2D rigid;
    Animator anim;
    SpriteRenderer spriter;
    Collider2D coll;

    /// <summary>상태이상 목록. 증강이 걸 때 자동으로 붙으므로 처음엔 없을 수 있다.</summary>
    StatusHolder status;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriter = GetComponent<SpriteRenderer>();
        coll = GetComponent<Collider2D>();
    }

    void FixedUpdate()
    {
        if(isLive == false)
        {
            return;
        }

        // 넉백 중에는 추적을 멈춘다. 안 그러면 밀어낸 만큼 즉시 되돌아온다
        if (moveSuppressRemain > 0f)
        {
            moveSuppressRemain -= Time.fixedDeltaTime;
            rigid.linearVelocity = Vector2.zero;
            return;
        }

        // StatusHolder 는 증강이 상태를 걸 때 비로소 붙는다. 한 번 붙으면 계속 남으므로
        // 이 조회는 상태가 걸린 적 없는 개체에서만 돈다
        if (status == null) TryGetComponent(out status);

        // 둔화 같은 상태이상이 걸려 있으면 그만큼 느려진다. 없으면 배율이 1이라 그대로다
        float moveSpeed = speed * (status != null ? status.SpeedMultiplier : 1f);

        Vector2 dirVec = target.position - rigid.position;
        Vector2 nextVec = dirVec.normalized * moveSpeed * Time.deltaTime;
        rigid.MovePosition(rigid.position + nextVec);
        rigid.linearVelocity = Vector2.zero;
    }

    public void Displace(Vector2 delta, float suppressDuration)
    {
        rigid.MovePosition(rigid.position + delta);
        moveSuppressRemain = Mathf.Max(moveSuppressRemain, suppressDuration);
    }
    void LateUpdate()
    {
        if (!isLive)
            return;
        spriter.flipX = target.position.x < rigid.position.x;
    }

    void OnEnable()
    {
        target = GameManager.instance.player.GetComponent<Rigidbody2D>();
        isLive = true;
        health = maxHealth;
        coll.enabled = true;
        moveSuppressRemain = 0f;
    }

    public void Init(SpawnData data)
    {
        anim.runtimeAnimatorController = animCon[data.spriteType];
        speed = data.speed;
        maxHealth = data.health;
        health = data.health;
        contactDamage = data.contactDamage;
    }
    /// <summary>
    /// 피해 진입점. 피해량만 깎는다 —
    /// 숫자 표시는 맥락(크리티컬·어느 증강)을 아는 DamagePipeline 이 맡는다.
    /// </summary>
    public void TakeDamage(float amount)
    {
        // 같은 프레임에 여러 발이 맞아도 Dead가 두 번 돌지 않게 막는다
        if (!isLive || amount <= 0f)
            return;

        health -= amount;

        if (health <= 0f)
            Dead();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Bullet"))
            return;

        // 태그만 Bullet 이고 컴포넌트가 없는 오브젝트가 섞여도 죽지 않게
        if (!collision.TryGetComponent(out Bullet bullet))
            return;

        // ⚠️ 옛 무기 경로. DamagePipeline 을 안 거쳐서 표식 보정도 숫자 표시도 없다.
        // 증강으로 완전히 대체되면 이 블록째로 지울 것
        TakeDamage(bullet.damage);
        DamageTextManager.Instance.ShowDamage(bullet.damage, transform);
    }

    void Dead()
    {
        isLive = false;

        GameManager.instance.AddKill();

        GameObject exp = GameManager.instance.poolManager.Get(PoolType.Exp, 0);
        exp.transform.position = transform.position;

        LogManager.Instance.Combat($"Clear {name}");
        gameObject.SetActive(false);
    }
}