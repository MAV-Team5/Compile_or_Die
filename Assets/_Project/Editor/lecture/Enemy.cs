using System.Collections;
using UnityEngine;

/// <summary>
/// 적(언데드) AI 스크립트
/// 플레이어 추적 / 피격 / 넉백 / 사망 처리 담당
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("# 스탯")]
    public float speed;
    public float health;
    public float maxHealth;
    public bool isLive;

    [Header("# 참조")]
    public RuntimeAnimatorController[] animCon;

    public Rigidbody2D target;

    Rigidbody2D rigid;
    SpriteRenderer spriter;
    Collider2D coll;
    Animator anim;
    WaitForFixedUpdate wait;

    void Awake()
    {
        rigid   = GetComponent<Rigidbody2D>();
        spriter = GetComponent<SpriteRenderer>();
        coll    = GetComponent<Collider2D>();
        anim    = GetComponent<Animator>();
        wait    = new WaitForFixedUpdate();
    }

    void OnEnable()
    {
        target               = GameManager.instance.player.GetComponent<Rigidbody2D>();
        isLive               = true;
        health               = maxHealth;
        coll.enabled         = true;
        rigid.simulated      = true;
        spriter.sortingOrder = 2;
    }

    public void Init(SpawnData data)
    {
        if (animCon != null && data.spriteType < animCon.Length)
            anim.runtimeAnimatorController = animCon[data.spriteType];

        speed     = data.speed;
        maxHealth = data.health;
        health    = data.health;
    }

    void FixedUpdate()
    {
        if (!isLive) return;
        if (!GameManager.instance.isLive) return;
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Hit")) return;

        Vector2 dirVec  = target.position - rigid.position;
        Vector2 nextVec = dirVec.normalized * speed * Time.fixedDeltaTime;
        rigid.MovePosition(rigid.position + nextVec);
        rigid.linearVelocity = Vector2.zero;
    }

    void LateUpdate()
    {
        if (!isLive) return;
        if (!GameManager.instance.isLive) return;
        spriter.flipX = target.position.x < rigid.position.x;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Bullet")) return;
        if (!isLive) return;

        health -= collision.GetComponent<Bullet>().damage;

        if (health > 0)
        {
            anim.SetTrigger("Hit");
            StartCoroutine(KnockBack());
            // AudioManager가 씬에 없을 수도 있으므로 null 체크
            PlaySfxSafe(AudioManager.Sfx.Hit);
        }
        else
        {
            isLive               = false;
            coll.enabled         = false;
            rigid.simulated      = false;
            spriter.sortingOrder = 1;
            anim.SetTrigger("Dead");

            GameManager.instance.kill++;
            GameManager.instance.GetExp();

            // 게임 종료 중 EnemyCleaner 대량 처치 시 효과음 폭발 방지
            if (GameManager.instance.isLive)
                PlaySfxSafe(AudioManager.Sfx.Dead);
        }
    }

    IEnumerator KnockBack()
    {
        yield return wait;
        Vector3 dir = transform.position - GameManager.instance.player.transform.position;
        rigid.AddForce(dir.normalized * 3f, ForceMode2D.Impulse);
    }

    void Dead()
    {
        // Animation Event에서 호출 (Dead 애니메이션 마지막 프레임)
        gameObject.SetActive(false);
    }

    /// <summary>
    /// AudioManager null 안전 재생 헬퍼
    /// AudioManager 오브젝트가 씬에 없거나 sfxClips 미연결 시 조용히 넘어감
    /// → 오디오 파일 없어도 게임 동작에 영향 없음
    /// </summary>
    void PlaySfxSafe(AudioManager.Sfx sfx)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(sfx);
    }
}
