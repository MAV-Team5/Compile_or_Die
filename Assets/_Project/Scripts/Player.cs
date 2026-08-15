using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour, IFacingProvider
{
    public GameObject characterPrefab;
    Transform visualRoot;

    public Vector2 inputVec;

    /// <summary>마지막으로 향했던 방향. 손을 떼도 유지된다. 증강이 이 방향으로 발동한다.</summary>
    Vector2 facing = Vector2.right;

    public Vector2 Facing => facing;
    public float speed;
    public Scanner scanner;


    public float pickupRange;
    Rigidbody2D rigid;
    
    public float exp;

    
    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        visualRoot = GetComponent<Transform>();
        
        scanner = GetComponent<Scanner>();

        SelectCharacter();
    }

    private void FixedUpdate()
    {
        Vector2 nextvec = inputVec * speed * Time.fixedDeltaTime;
        rigid.MovePosition(rigid.position + nextvec);
    }

    void OnMove(InputValue value)
    {
        inputVec = value.Get<Vector2>();

        // 입력이 0이 되는 순간(손 뗌)에는 갱신하지 않아야 마지막 방향이 남는다
        if (inputVec.sqrMagnitude > 0.0001f)
            facing = inputVec.normalized;
    }

    public void SelectCharacter()
    {
        GameObject visual = Instantiate(characterPrefab);
        visual.transform.SetParent(visualRoot,false);

    }

    public void GetExp(int amount)
    {
        exp += amount;

        GameManager.instance.expManager.AddExpLog(amount);
    }
}
