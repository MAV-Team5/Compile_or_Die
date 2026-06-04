using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reposition : MonoBehaviour
{
    Collider2D collider2d;
    
    private void Awake()
    {
        collider2d = GetComponent<Collider2D>();
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Area"))
            return;

        Vector3 playerPos = GameManager.instance.player.transform.position;
        Vector3 myPos = transform.position;
        Vector3 playerDir = GameManager.instance.player.transform.position;
        float dirX = playerPos.x - myPos.x;
        float dirY = playerPos.y - myPos.y;
        float diffX = Mathf.Abs(dirX);
        float diffY = Mathf.Abs(dirY);

        dirX = dirX > 0 ? 1 : -1;
        dirY = dirY > 0 ? 1 : -1;

        switch (transform.tag)
        {
            case "Ground":
                if (diffX > diffY)
                {
                    transform.Translate(Vector3.right * dirX * 80);
                }
                else if (diffX < diffY)
                {
                    transform.Translate(Vector3.up * dirY * 80);
                }
                else
                {
                    transform.Translate(Vector3.right * dirX * 80);
                    transform.Translate(Vector3.up * dirY * 80);
                }
                break;
            //일정거리 멀어지면 원래 방향에서 상대 위치 보정.
            case "Enemy":
                if (collider2d.enabled)
                {
                    float distance = Vector3.Distance(transform.position, playerPos);

                    if (distance > 50f)
                    {
                        Vector3 dir = (transform.position - playerPos).normalized;

                        transform.position =
                            playerPos + dir * 40f;
                    }
                }
                break;
        }
    }
}

