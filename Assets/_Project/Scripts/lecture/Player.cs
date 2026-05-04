using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public Vector2 inputVec;
    public float speed;
    public Scanner scanner;
    public Hand[] hands;
    public RuntimeAnimatorController[] animCon;

    Rigidbody2D rigid;
    SpriteRenderer spriter;
    Animator anim;
    Vector2 nextVec;

    void Awake()
    {
        rigid   = GetComponent<Rigidbody2D>();
        spriter = GetComponent<SpriteRenderer>();
        anim    = GetComponent<Animator>();
        scanner = GetComponent<Scanner>();
        hands   = GetComponentsInChildren<Hand>(true);
    }

    void OnEnable()
    {
        speed = 3f;
        speed *= Character.Speed;
        anim.runtimeAnimatorController = animCon[GameManager.instance.playerId];
    }

    void OnMove(InputValue value)
    {
        inputVec = value.Get<Vector2>();
    }

    void Update()
    {
        if (!GameManager.instance.isLive) return;
        nextVec = inputVec * speed * Time.fixedDeltaTime;
    }

    void FixedUpdate()
    {
        if (!GameManager.instance.isLive) return;
        rigid.MovePosition(rigid.position + nextVec);
    }

    void LateUpdate()
    {
        if (!GameManager.instance.isLive) return;
        anim.SetFloat("Speed", inputVec.magnitude);
        if (inputVec.x != 0)
            spriter.flipX = inputVec.x < 0;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (!GameManager.instance.isLive) return;

        GameManager.instance.health -= Time.deltaTime * 10f;

        if (GameManager.instance.health < 0)
        {
            for (int i = 2; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(false);

            anim.SetTrigger("Dead");
            GameManager.instance.GameOver();
        }
    }
}
