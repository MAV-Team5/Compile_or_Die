using UnityEngine;

public class ExpMove : MonoBehaviour
{

    public float exp = 1;
    public float baseSpeed = 1f;
    public float accel = 5f;

    private float currentSpeed = 0f;
    private bool isMagneted = false;

    private Player player;

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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log(exp);

        Destroy(gameObject);
    }
}

