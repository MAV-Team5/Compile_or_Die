using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed;
    public float health;
    public float maxHealth;
    public bool isLive;
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
        target  = GameManager.instance.player.GetComponent<Rigidbody2D>();
        isLive  = true;
        health  = maxHealth;
        coll.enabled = true;
        rigid.simulated = true;
        spriter.sortingOrder = 2;
    }

    public void Init(SpawnData data)
    {
        anim.runtimeAnimatorController = animCon[data.spriteType];
        speed     = data.speed;
        maxHealth = data.health;
        health    = data.health;
    }

    void FixedUpdate()
    {
        if (!isLive) return;
        if (!GameManager.instance.isLive) return;

        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Hit"))
            return;

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
        }
        else
        {
            isLive = false;
            coll.enabled = false;
            rigid.simulated = false;
            spriter.sortingOrder = 1;
            anim.SetTrigger("Dead");

            GameManager.instance.kill++;
            GameManager.instance.GetExp();

            if (GameManager.instance.isLive)
                AudioManager.instance.PlaySfx(AudioManager.Sfx.Dead);
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
        // Animation Event에서 호출 (Dead 애니메이션 끝에 연결)
        gameObject.SetActive(false);
    }
}
