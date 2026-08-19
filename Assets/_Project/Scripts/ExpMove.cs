using UnityEngine;

public class ExpMove : MonoBehaviour
{

    public int exp = 1;
    public float baseSpeed = 1f;
    public float accel = 5f;

    private float currentSpeed = 0f;
    private bool isMagneted = false;

    private Player player;

    public FxGroup fxG = new();

    void Start()
    {
        player = GameManager.instance.player;
    }

    void Update()
    {
        if (player == null)
        {
            Debug.Log("PLAYER NULL");
            return;
        }

        float distance = Vector2.Distance(transform.position, player.transform.position);
        if (distance < player.pickupRange)
        {
            isMagneted = true;
        }
        else
        {
            isMagneted = false;
            currentSpeed = 0f;
        }

        if (isMagneted)
        {
            currentSpeed += accel * Time.deltaTime;

            Vector2 dir = (player.transform.position - transform.position).normalized;

            transform.position += (Vector3)(dir * currentSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 겸험치 획득
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        GameManager.instance.player.GetExp(exp);
        fxG.PlayAt(Vector2.zero);
        gameObject.SetActive(false);
    }
}

