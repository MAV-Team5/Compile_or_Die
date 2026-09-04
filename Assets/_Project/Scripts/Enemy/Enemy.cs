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
    private float contactDamage;


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

        // 잠긴 동안은 제자리다. 넉백과 달리 시간이 아니라 상태가 풀려야 끝난다
        if (movementLocked)
        {
            rigid.linearVelocity = Vector2.zero;
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

        // ★ 풀에서 재사용되므로 반드시 푼다. 안 그러면 지난번 잠금 상태로 살아나
        //   영영 안 죽고 안 움직이는 개체가 생긴다
        invulnerable = false;
        movementLocked = false;
    }

    /// <summary>
    /// 스폰 직후 이 적이 무엇인지 새겨 넣는다.
    /// 기본 스탯은 <paramref name="data"/>, 이번 웨이브의 배율은 <paramref name="wave"/> 가 준다.
    ///
    /// OnEnable 이 먼저 돌아 이전 개체의 값으로 살아나므로, 여기서 반드시 덮어써야 한다.
    /// </summary>
    /// <summary>안 움직이는 설치물인가. 스포너가 스폰 위치와 회수 규칙을 가를 때 본다.</summary>
    public bool IsStationary => source != null && source.stationary;

    /// <summary>
    /// 지금 무적인가. <see cref="DeadlockCycle"/> 처럼 적 자신이 한시적으로 켠다.
    ///
    /// 콜라이더는 켜둔 채라 <b>몸으로 막고 닿으면 아프다</b> — 안 죽을 뿐이지 벽은 벽이다.
    /// </summary>
    [System.NonSerialized] public bool invulnerable;

    /// <summary>
    /// 지금 스스로 못 움직이는가. 넉백(moveSuppressRemain)과 달리 <b>지속</b>이다.
    /// 잠긴 데드락처럼 상태가 풀릴 때까지 붙박이인 경우에 쓴다.
    /// </summary>
    [System.NonSerialized] public bool movementLocked;

    /// <summary>
    /// <b>무적만 본다. 죽음은 여기서 안 막는다.</b>
    ///
    /// 죽은 적은 <see cref="TakeDamage"/> 가 이미 걸러내므로 체력은 안 깎인다.
    /// 그런데 여기서까지 막으면 <see cref="DamagePipeline"/> 이 숫자 표시도 건너뛰어,
    /// <b>마지막 일격에 얹히는 효과가 통째로 안 보이게 된다</b> —
    /// Bash:kill 처럼 "끝낸 대상에 한 번 더" 인 증강이 죽은 것처럼 보인다.
    /// </summary>
    public bool AcceptsDamage => !invulnerable;

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

        // 풀에서 꺼낸 개체는 지난번 죽음 상태의 마지막 프레임에 멈춰 있다.
        // 되감지 않으면 새로 나온 적이 죽은 모습으로 살아난다
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

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

        // 판정을 먼저 끊는다. 죽음 연출이 도는 동안 증강이 시체를 계속 겨누거나,
        // 시체에 닿아서 피가 깎이면 안 된다
        if (coll != null) coll.enabled = false;

        // 상자 같은 설치물은 처치 수에 안 센다. 재화가 처치 수를 기준으로 계산되기 때문
        if (RunDirector.Current != null && (source == null || source.countsAsKill))
            RunDirector.Current.AddKill();

        DropLoot();

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

        Despawn();
    }

    /// <summary>
    /// 죽음 연출이 있으면 그동안 남았다가 사라진다. 없으면 지금 바로.
    ///
    /// <b>보상은 이미 다 나갔다.</b> 경험치·드롭·비트는 연출을 기다리지 않는다 —
    /// 때린 순간과 보상 사이가 벌어지면 뭘 맞혀서 얻은 건지 연결이 끊긴다.
    /// </summary>
    void Despawn()
    {
        float wait = source != null ? source.deathDuration : 0f;

        if (wait <= 0f)
        {
            gameObject.SetActive(false);
            return;
        }

        if (anim != null && !string.IsNullOrEmpty(source.deathState))
            anim.Play(source.deathState, 0, 0f);

        StartCoroutine(DespawnAfter(wait));
    }

    System.Collections.IEnumerator DespawnAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 죽은 자리에 픽업을 떨군다. 무엇을 떨굴지는 <see cref="EnemyData.dropTable"/> 이 정한다.
    ///
    /// 경험치(<see cref="Spawner.ReportKill"/>)와 따로 두는 이유 — 경험치는 웨이브가 정하고
    /// 드롭은 적이 정한다. 상자는 어느 웨이브에서 나왔든 같은 것을 떨궈야 한다.
    /// </summary>
    void DropLoot()
    {
        if (source == null || source.dropCount <= 0) return;
        if (Random.value > source.dropChance) return;

        for (int i = 0; i < source.dropCount; i++)
        {
            GameObject prefab = source.PickDrop();
            if (prefab == null) continue;

            // 여러 개면 겹쳐서 몇 개인지 안 읽힌다. 조금씩 흩어 놓는다
            Vector2 at = (Vector2)transform.position
                       + (source.dropCount > 1 ? Random.insideUnitCircle * 0.5f : Vector2.zero);

            PooledSpawner.Spawn(prefab, at, PoolType.Item);
        }
    }
    public float GetContectDamage()
    {
        return  contactDamage;
    }
}