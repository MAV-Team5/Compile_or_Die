using UnityEngine;

public class Reposition : MonoBehaviour
{
    Collider2D coll;

    void Awake()
    {
        coll = GetComponent<Collider2D>();
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Area")) return;

        Vector3 playerPos = GameManager.instance.player.transform.position;
        Vector3 myPos     = transform.position;

        float diffX = Mathf.Abs(playerPos.x - myPos.x);
        float diffY = Mathf.Abs(playerPos.y - myPos.y);

        float dirX = playerPos.x - myPos.x;
        float dirY = playerPos.y - myPos.y;

        switch (transform.tag)
        {
            case "Ground":
                if (diffX > diffY)
                    transform.Translate(dirX > 0 ? Vector3.right * 40 : Vector3.left * 40);
                else
                    transform.Translate(dirY > 0 ? Vector3.up * 40 : Vector3.down * 40);
                break;

            case "Enemy":
                if (coll.enabled)
                {
                    Vector3 dist   = playerPos - myPos;
                    Vector3 random = new Vector3(
                        Random.Range(-3f, 3f),
                        Random.Range(-3f, 3f),
                        0);
                    transform.position = playerPos + dist * 2 + random;
                }
                break;
        }
    }
}
