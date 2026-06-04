using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public GameObject characterPrefab;
    Transform visualRoot;

    public Vector2 inputVec;
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
