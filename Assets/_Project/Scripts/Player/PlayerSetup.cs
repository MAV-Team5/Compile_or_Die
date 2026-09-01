using UnityEngine;

/// <summary>
/// 런이 시작될 때 고른 캐릭터를 씬의 플레이어에 깔아준다. 플레이어 오브젝트에 붙인다.
///
/// <b>왜 한자리에 모으나</b> — 예전에는 이동속도는 Player 인스펙터에, 체력은 PlayerHealth 에,
/// 비주얼 프리팹은 또 Player 에 흩어져 있었다. 캐릭터를 하나 늘릴 때마다
/// 씬을 열어 세 군데를 고쳐야 했고, 무엇이 그 캐릭터의 진짜 값인지 알 수 없었다.
/// 이제 원본은 <see cref="CharacterData"/> 하나뿐이고 여기서 한 번에 깐다.
///
/// <b>Awake 여야 하는 이유</b> — 하드웨어 보정(<see cref="HardwareBonus"/>)이 Start 에서
/// 이 값들 위에 배율을 곱한다. 매 런 Awake 가 원본을 다시 깔아주므로
/// <c>speed *= 1+보너스</c> 같은 곱셈이 누적되지 않는다. 배율용 여분 칸이 필요 없는 이유다.
/// </summary>
[DisallowMultipleComponent]
public class PlayerSetup : MonoBehaviour
{
    [Tooltip("캐릭터 선택을 거치지 않고 이 씬을 바로 재생했을 때 쓸 캐릭터.\n" +
             "선택 화면에서 넘어온 것이 있으면 그쪽이 이긴다.")]
    [SerializeField] CharacterData defaultCharacter;

    [Tooltip("캐릭터 비주얼을 붙일 자리. 비우면 이 오브젝트 밑에 바로 붙인다.")]
    [SerializeField] Transform visualRoot;

    /// <summary>이번 런의 캐릭터. Awake 에서 확정된다.</summary>
    public CharacterData Character { get; private set; }

    /// <summary>깔아둔 비주얼 인스턴스. 방향 표시 같은 것이 찾아 쓸 수 있게 열어둔다.</summary>
    public GameObject Visual { get; private set; }

    void Awake()
    {
        Character = CharacterContext.Begin(defaultCharacter);

        if (Character == null)
        {
            Debug.LogError("[PlayerSetup] 캐릭터가 없다. Default Character 에 CharacterData 를 물릴 것.", this);
            return;
        }

        ApplyStats();
        SpawnVisual();
    }

    void ApplyStats()
    {
        if (TryGetComponent(out Player player))
        {
            player.speed = Character.moveSpeed;
            player.pickupRange = Character.pickupRange;
        }

        // 필드에 직접 쓰면 안 된다. PlayerHealth.Awake 가 Current 를 채우는데
        // Awake 끼리는 순서가 없어서, 먼저 돌았다면 현재 체력이 옛 최대치로 남는다.
        // 최대치와 현재치를 같이 세팅하는 메서드를 부르면 순서와 무관해진다
        if (TryGetComponent(out PlayerHealth health))
            health.SetMaxHealth(Character.maxHealth);
    }

    /// <summary>
    /// 캐릭터 비주얼을 본체 밑에 붙인다. 예전 <c>Player.SelectCharacter()</c> 가 하던 일.
    ///
    /// PlayerHealth 는 Start 에서 자식 SpriteRenderer 를 걷어 피격 연출에 쓴다.
    /// 모든 Awake 가 끝난 뒤에 Start 가 오므로 여기서 붙이면 반드시 잡힌다.
    /// </summary>
    void SpawnVisual()
    {
        if (Character.visualPrefab == null)
        {
            Debug.LogWarning($"[PlayerSetup] {Character.name} 에 Visual Prefab 이 없다. " +
                             "캐릭터가 안 보인다.", this);
            return;
        }

        Transform root = visualRoot != null ? visualRoot : transform;

        // 세 번째 인자 false 가 SetParent(root, false) 와 같다 — 부모 기준 원점에 놓인다
        Visual = Instantiate(Character.visualPrefab, root, false);
    }
}
