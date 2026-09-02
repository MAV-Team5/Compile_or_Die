using UnityEngine;

/// <summary>
/// 블루스크린 게이지. <b>시간 기반으로 계속 찬다</b> — 사거리 안에 있는 블루스크린 몬스터 수에
/// 비례해서 초당 얼마씩 차오르고, 다 차면 발동한다. 몬스터가 많이 몰릴수록 위협이 빨리 닥친다.
///
/// 몬스터의 개별 3단계 캐스트(코드 오류→MEMORY→FAILED)는 이제 게이지 진행과 직접 연결되지 않는다 —
/// 순수 연출이자 UI 번쩍임 신호(<see cref="NotifyCastCompleted"/>)로만 쓰인다.
///
/// <b>재부팅 중 잠금</b> — 발동되면 즉시 잠긴다. 재부팅이 끝나면 <see cref="Unlock"/>으로 푼다.
/// <b>진행도 리셋</b> — 사거리 안에 아무도 없어지면(전부 죽거나 멀어지면) 자동으로 0이 된다.
///
/// GameManager 처럼 씬당 하나. [Game] 오브젝트에 붙인다.
/// </summary>
public class BlueScreenGauge : MonoBehaviour
{
    public static BlueScreenGauge Instance { get; private set; }

    [Tooltip("사거리 안 몬스터 1마리당 1초에 채우는 비율(0~1). 몬스터가 2마리면 2배 속도로 찬다.")]
    [SerializeField] float fillRatePerMonster = 0.08f;

    /// <summary>지금 진행도(0~1). UI가 바 채우는 데 직접 쓴다.</summary>
    public float Progress { get; private set; }

    /// <summary>지금 사거리 안에 블루스크린 몬스터가 하나라도 있는가. UI가 게이지를 보이거나 숨길 때 쓴다.</summary>
    public bool AnyoneInRange { get; private set; }

    /// <summary>재부팅(리부팅) 중이라 게이지가 잠겨 있는가. 잠긴 동안은 진행도가 안 늘어난다.</summary>
    public bool IsLocked { get; private set; }

    /// <summary>몬스터 하나가 3단계 캐스트를 완주할 때마다. UI 번쩍임 트리거용 — 진행도엔 영향 없다.</summary>
    public event System.Action CastRegistered;

    /// <summary>블루스크린 발동.</summary>
    public event System.Action Triggered;

    void Awake() => Instance = this;

    void Update()
    {
        int count = BlueScreenCaster.CountInRange();
        bool inRange = count > 0;

        // 방금까지 있었는데 지금 아무도 없다 — 위협이 해소됐으니 진행도도 같이 비운다
        if (AnyoneInRange && !inRange) Progress = 0f;

        AnyoneInRange = inRange;

        if (IsLocked || !inRange) return;

        Progress += fillRatePerMonster * count * Time.deltaTime;

        if (Progress >= 1f)
        {
            Progress = 0f;
            IsLocked = true;
            Triggered?.Invoke();
        }
    }

    /// <summary>몬스터가 3단계 캐스트를 완주했을 때 부른다. 순수 연출 신호.</summary>
    public void NotifyCastCompleted() => CastRegistered?.Invoke();

    /// <summary>재부팅이 끝났을 때 부른다. PlayerRebootController 가 담당한다.</summary>
    public void Unlock() => IsLocked = false;
}
