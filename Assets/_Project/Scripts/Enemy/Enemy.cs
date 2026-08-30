using UnityEngine;

/// <summary>
/// 화면에 떠 있는 적 하나. 움직이고, 맞고, 죽는다.
///
/// <b>수치는 여기서 정하지 않는다</b> — <see cref="EnemyData"/> 가 설계도이고
/// 여기 있는 값은 지금 이 개체의 상태다. 증강의 AugmentData / AugmentInstance 와 같은 관계.
/// </summary>
public class Enemy : MonoBehaviour, IDamageReceiver, IDisplaceable
{
    // 넉백 직후 스스로 못 움직이는 시간
    float moveSuppressRemain;

    private float speed;
    private float health;
    private float maxHealth;

    /// <summary>플레이어와 닿아 있는 동안 초당 주는 피해.</summary>
    public float contactDamage = 1f;
    public Rigidbody2D target;
    bool isLive;

    Rigidbody2D rigid;
    Animator anim;
    SpriteRenderer spriter;
    Collider2D coll;

    /// <summary>상태이상 목록. 증강이 걸 때 자동으로 붙으므로 처음엔 없을 수 있다.</summary>
    StatusHolder status;

    /// <summary>어떤 적인가. 경험치·비트 보상이 여기서 나온다.</summary>
    EnemyData source;

    /// <summary>
    /// 나를 낸 웨이브의 번호. 죽을 때 스포너에게 이 번호로 알린다.
    ///
    /// <b>웨이브 객체가 아니라 번호만 든다.</b> 적이 StageWave 를 직접 참조하면
    /// 적 하나가 스테이지 구조 전체에 묶인다. 보스처럼 웨이브 소속이 아니면 -1.
    /// </summary>
    int waveIndex = -1;

    /// <summary>플레이어 쪽으로 뒤집을지. 글자 모양 적은 끈다.</summary>
    bool flipToFace = true;

    /// <summary>프리팹에 그려진 원래 크기. 배율은 여기에 곱한다.</summary>
    Vector3 baseScale = Vector3.one;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriter = GetComponent<SpriteRenderer>();
        coll = GetComponent<Collider2D>();

        // 풀에서 꺼내 쓰기 전 한 번. 배율을 곱할 기준이 필요하다
        baseScale = transform.localScale;
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
        if (!isLive) return;

        // 글자 모양 적은 뒤집으면 읽을 수 없게 된다
        if (!flipToFace) return;

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

    /// <summary>
    /// 스폰 직후 이 적이 무엇인지 새겨 넣는다.
    /// 기본 스탯은 <paramref name="data"/>, 이번 웨이브의 배율은 <paramref name="wave"/> 가 준다.
    ///
    /// OnEnable 이 먼저 돌아 이전 개체의 값으로 살아나므로, 여기서 반드시 덮어써야 한다.
    /// </summary>
    public void Init(EnemyData data, EnemyScale scale, int wave = -1)
    {
        // 풀에서 재사용되므로 항상 다시 정해야 한다. 안 그러면 지난 개체의 웨이브로 집계된다
        waveIndex = wave;

        if (data == null)
        {
            Debug.LogWarning($"[{name}] EnemyData 없이 스폰됐다. 이전 개체의 수치로 돌아다닌다.", this);
            return;
        }

        source = data;
        flipToFace = data.flipToFace;

        // 적마다 프리팹이 다르면 컨트롤러도 프리팹에 있다. 지정된 것이 있을 때만 갈아끼운다
        if (data.animatorOverride != null && anim != null)
            anim.runtimeAnimatorController = data.animatorOverride;

        speed = data.speed * scale.Speed;
        maxHealth = data.health * scale.Health;
        health = maxHealth;
        contactDamage = data.contactDamage * scale.Damage;

        // 풀에서 재사용되므로 항상 다시 정해야 한다. 안 그러면 직전 개체의 크기로 나온다
        transform.localScale = baseScale * Mathf.Max(0.01f, data.scale * scale.Size);

        // 뒤집기를 끈 적이 이전 개체의 반전 상태를 물려받지 않게
        if (spriter != null && !flipToFace) spriter.flipX = false;
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

    void Dead()
    {
        isLive = false;

        if (RunDirector.Current != null) RunDirector.Current.AddKill();

        // 경험치는 웨이브가 정한다. 적은 자기가 뭘 떨구는지 모른다 —
        // 이월(carry)은 죽으면 사라지는 적이 아니라 웨이브가 들고 있어야 하기 때문
        if (Spawner.Current != null) Spawner.Current.ReportKill(waveIndex, transform.position);

        if (source != null && source.bits > 0 && RunDirector.Current != null)
            RunDirector.Current.AddBits(source.bits);

        // 소리도 적이 정한다. 잡몹과 보스가 같은 소리로 죽으면 무게가 안 실린다
        if (source != null)
            SfxPlayer.PlayAny(source.deathClips, source.deathVolume, source.deathInterval);

        if (LogManager.Instance != null)
        {
            // 문구도 종류도 적이 정한다. 잡몹과 보스가 같은 말투로 죽으면 무게가 안 실린다
            if (source != null) LogManager.Instance.AddLog(source.killLog, source.PickKillMessage());
            else LogManager.Instance.Combat($"Clear {name}");
        }
        gameObject.SetActive(false);
    }
}