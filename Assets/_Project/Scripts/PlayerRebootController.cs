using System.Collections;
using UnityEngine;

/// <summary>
/// 블루스크린 발동 시 플레이어 이동을 좌우반전시킨다("재부팅 중").
/// Player.invertX 플래그만 켰다 끄는 얇은 컴포넌트 — 이동 로직 자체는 Player.cs 가 그대로 맡는다.
/// </summary>
[RequireComponent(typeof(Player))]
public class PlayerRebootController : MonoBehaviour
{
    [Tooltip("재부팅(조작 반전) 지속시간(초).")]
    [SerializeField] float rebootDuration = 3f;

    /// <summary>재부팅 지속시간(초). BlueScreenOverlay 가 연출 길이를 맞춤 때 쓴다.</summary>
    public float RebootDuration => rebootDuration;

    Player player;
    Coroutine rebootRoutine;
    bool subscribed;

    public bool IsRebooting => rebootRoutine != null;

    void Awake() => player = GetComponent<Player>();

    void OnEnable() => TrySubscribe();

    void OnDisable()
    {
        if (subscribed && BlueScreenGauge.Instance != null)
            BlueScreenGauge.Instance.Triggered -= HandleTriggered;

        subscribed = false;
    }

    // OnEnable 시점에 BlueScreenGauge.Instance 가 아직 null일 수 있다
    // (초기화 순서가 뿐뿐하게 어긋난 때 첫 번째 바 에서 구독이 영원히 안 되던 버그입다).
    // Instance 가 생길 때까지 매 프레임 재시도해서 순서에 안 휴리게 한다
    void Update()
    {
        if (!subscribed) TrySubscribe();
    }

    void TrySubscribe()
    {
        if (subscribed || BlueScreenGauge.Instance == null) return;

        BlueScreenGauge.Instance.Triggered += HandleTriggered;
        subscribed = true;
    }

    void HandleTriggered()
    {
        // 재부팅 중 또 발동되면 남은 시간을 갱신하고 새로 시작한다
        if (rebootRoutine != null) StopCoroutine(rebootRoutine);
        rebootRoutine = StartCoroutine(RebootSequence());
    }

    IEnumerator RebootSequence()
    {
        player.invertX = true;

        if (LogManager.Instance != null)
            LogManager.Instance.Error("REBOOTING...");

        yield return new WaitForSeconds(rebootDuration);

        player.invertX = false;
        rebootRoutine = null;

        // 재부팅이 끝난 시점에만 게이지 잠금을 푸는다 — 재부팅 중에는 다시 차오를 수 없다
        if (BlueScreenGauge.Instance != null)
            BlueScreenGauge.Instance.Unlock();
    }
}
