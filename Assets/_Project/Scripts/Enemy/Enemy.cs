using UnityEngine;

/// <summary>
/// 몬스터 스탯.
/// </summary>
public class Enemy : MonoBehaviour, IDamageReceiver
{
    public float speed;
    public float health;
    public float maxHealth;
    public RuntimeAnimatorController[] animCon;
    public Rigidbody2D target;
    public GameObject expPrefab;
    bool isLive;

    Rigidbody2D rigid;
    Animator anim;
    SpriteRenderer spriter;
    Collider2D coll;

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

        Vector2 dirVec = target.position - rigid.position;
        Vector2 nextVec = dirVec.normalized * speed * Time.deltaTime;
        rigid.MovePosition(rigid.position + nextVec);
        rigid.linearVelocity = Vector2.zero;
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
    }

    public void Init(SpawnData data)
    {
        anim.runtimeAnimatorController = animCon[data.spriteType];
        speed = data.speed;
        maxHealth = data.health;
        health = data.health;
    }
    /// <summary>피해 진입점. 기존 무기와 증강이 모두 여기로 들어온다.</summary>
    public void TakeDamage(float amount)
    {
        // 같은 프레임에 여러 발이 맞아도 Dead가 두 번 돌지 않게 막는다
        if (!isLive || amount <= 0f)
            return;

        health -= amount;

        DamageTextManager.Instance.ShowDamage(amount, transform);

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

        TakeDamage(bullet.damage);
    }

    void Dead()
    {
        isLive = false;

        GameObject exp = GameManager.instance.poolManager.Get(PoolType.Exp, 0);
        exp.transform.position = transform.position;

        LogManager.Instance.Combat($"Clear {name}");
        gameObject.SetActive(false);
    }
}