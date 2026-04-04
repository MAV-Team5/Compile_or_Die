using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public GameObject test_Bullet; // 위에서 만든 프리팹을 여기에 넣으세요
    public Transform firePoint;     // 총알이 나갈 위치 (캐릭터 중심 등)

    void Update()
    {
        // 마우스 왼쪽 클릭이나 스페이스바를 누르면 발사!
        if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Space))
        {
            Attack();
        }
    }

    void Attack()
    {
        // 1. 총알 생성
        Instantiate(test_Bullet, transform.position, transform.rotation);
        
        // 2. (보너스) 아까 만든 Animator에 "Attack" 트리거가 있다면 실행!
        GetComponent<Animator>().SetTrigger("OnAttack");
    }
}