using UnityEngine;

public class Weapon : MonoBehaviour
{
    public int id;
    public int prefabId;
    public float damage;
    public int count;
    public float speed;

    float timer;
    Player player;

    // Start 제거 — Init(ItemData)로만 초기화, 자동 호출 없음

    void Update()
    {
        if (!GameManager.instance.isLive) return;

        switch (id)
        {
            case 0:
                transform.Rotate(Vector3.forward * speed * Time.deltaTime);
                break;
            default:
                timer += Time.deltaTime;
                if (timer > speed)
                {
                    timer = 0;
                    Fire();
                }
                break;
        }
    }

    // Item.OnClick()에서 호출. 부모(Player) 설정 후 초기화
    public void Init(ItemData data)
    {
        // 부모를 Player로 설정 (Awake보다 먼저 player 참조를 직접 받아야 함)
        player = GameManager.instance.player;
        transform.parent = player.transform;
        transform.localPosition = Vector3.zero;

        name     = "Weapon " + data.itemId;
        id       = data.itemId;
        prefabId = data.prefabId;
        damage   = data.damages[0] * Character.WeaponDamage;
        count    = data.counts[0]  + Character.WeaponCount;

        switch (id)
        {
            case 0:
                speed = -150f * Character.WeaponSpeed;
                Batch();
                break;
            default:
                speed = 0.3f * Character.WeaponSpeed;
                break;
        }

        // 손 스프라이트 적용
        Hand hand = GameManager.instance.player.hands[(int)data.itemType];
        hand.spriter.sprite = data.hand;
        hand.gameObject.SetActive(true);
    }

    public void LevelUp(float damage, int count)
    {
        this.damage += damage * Character.WeaponDamage;
        this.count  += count;
        if (id == 0) Batch();
    }

    void Batch()
    {
        for (int i = 0; i < count; i++)
        {
            Transform bullet;

            if (i < transform.childCount)
                bullet = transform.GetChild(i);
            else
            {
                bullet = GameManager.instance.pool.Get(prefabId).transform;
                bullet.parent = transform;
            }

            bullet.localPosition = Vector3.zero;
            bullet.localRotation = Quaternion.identity;

            Vector3 rotVec = Vector3.forward * 360f * i / count;
            bullet.Rotate(rotVec);
            bullet.Translate(bullet.up * 1.5f, Space.World);
            bullet.GetComponent<Bullet>().Init(damage, -100, Vector3.zero);
        }
    }

    void Fire()
    {
        if (!player.scanner.nearestTarget) return;

        Vector3 targetPos = player.scanner.nearestTarget.position;
        Vector3 dir = (targetPos - transform.position).normalized;

        Transform bullet = GameManager.instance.pool.Get(prefabId).transform;
        bullet.position = transform.position;
        bullet.rotation = Quaternion.FromToRotation(Vector3.up, dir);
        bullet.GetComponent<Bullet>().Init(damage, count, dir);

        AudioManager.instance.PlaySfx(AudioManager.Sfx.Range);
    }
}
