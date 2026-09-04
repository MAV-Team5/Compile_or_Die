using UnityEngine;

public class ExpMove : MonoBehaviour
{

    public int exp = 1;
    public float baseSpeed = 1f;
    public float accel = 5f;

    private float currentSpeed = 0f;
    private bool isMagneted = false;

    /// <summary>
    /// 거리와 무관하게 끌려오는 상태. GC 아이템이 켠다.
    ///
    /// <b>풀에서 재사용되므로 켜질 때마다 반드시 꺼야 한다.</b>
    /// 안 그러면 다음에 나온 오브가 스폰되자마자 플레이어에게 날아온다.
    /// </summary>
    [System.NonSerialized] public bool forcedMagnet;

    private Player player;

    void OnEnable()
    {
        forcedMagnet = false;
        isMagneted = false;
        currentSpeed = 0f;
    }

    public FxGroup fxG = new();

    [Header("획득 소리")]
    [Tooltip("여러 개 넣으면 매번 하나를 랜덤으로 고른다. 비우면 소리가 안 난다.")]
    [SerializeField] AudioClip[] pickupClips;

    [Range(0f, 1f)] [SerializeField] float pickupVolume = 0.35f;

    [Tooltip("최소 간격(초). ＊ 이걸 짧게 두면 안 된다 —\n" +
             "자석 범위에 들어오면 오브 수십 개가 한꺼번에 빨려와서 소리가 폭발한다.\n" +
             "0.08 쯤이면 여러 개를 먹어도 \"드르륵\" 한 번으로 들린다.")]
    [Min(0f)] [SerializeField] float pickupInterval = 0.08f;

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

        // 한 번 강제로 끌리기 시작하면 거리와 무관하게 끝까지 온다.
        // 도중에 풀리면 화면 밖에서 멈춰 서 있는 오브가 생긴다
        if (forcedMagnet || distance < player.pickupRange)
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

        SfxPlayer.PlayAny(pickupClips, pickupVolume, pickupInterval);

        gameObject.SetActive(false);
    }
}
