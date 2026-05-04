using UnityEngine;

public class Hand : MonoBehaviour
{
    public bool isLeft;
    public SpriteRenderer spriter;

    SpriteRenderer player;

    Vector3 rightPos  = new Vector3( 0.35f, -0.15f, 0);
    Vector3 rightPosR = new Vector3(-0.35f, -0.15f, 0);
    Quaternion leftRot  = Quaternion.Euler(0, 0, -35f);
    Quaternion leftRotR = Quaternion.Euler(0, 0, -145f);

    void Awake()
    {
        spriter = GetComponent<SpriteRenderer>();
        player  = GetComponentsInParent<SpriteRenderer>()[1];
    }

    void LateUpdate()
    {
        bool isReverse = player.flipX;

        if (isLeft)
        {
            transform.localRotation = isReverse ? leftRotR : leftRot;
            spriter.flipY           = isReverse;
            spriter.sortingOrder    = isReverse ? 4 : 6;
        }
        else
        {
            transform.localPosition = isReverse ? rightPosR : rightPos;
            spriter.flipX           = isReverse;
            spriter.sortingOrder    = isReverse ? 6 : 4;
        }
    }
}
