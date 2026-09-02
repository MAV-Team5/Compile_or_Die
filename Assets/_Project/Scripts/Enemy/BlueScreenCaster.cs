using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 블루스크린 몬스터의 캐스트 상태. 3단계 텍스트(코드 오류 → MEMORY → FAILED)를 순환하며
/// 아우라를 파랗게 점멸시킨다. 완주하면 UI 번쩍임 신호만 보내고(게이지는 시간 기반으로 따로 찬다 —
/// BlueScreenGauge 참고), 다음 캐스트를 곧장 재시작한다.
///
/// <b>발동 조건</b> — 화면에 처음 잡히면 캐스팅을 시작한다 (거리는 시작 조건이 아니다 —
/// 거리까지 시작 조건에 넣으면 "보이지만 사거리 밖"인 경우 캐스트 시작 직후 즉시 취소가
/// 한 프레임 안에서 무한 반복될 위험이 있다). 시작한 뒤로는 화면 밖으로 나가도 계속 진행하되,
/// 사거리(<see cref="maxCastDistance"/>)를 벗어나는 순간 중단하고 다시 보일 때 재개한다.
///
/// 몬스터가 죽으면(SetActive(false)) OnDisable 이 자동으로 캐스트를 취소한다 —
/// Enemy.cs 를 건드리지 않아도 "죽이면 캔슬"이 성립하는 이유가 이것이다.
///
/// <b>활성 목록</b> — 씬에 존재하는(풀에서 꺼내져 켜진) 인스턴스를 정적 목록에 등록해둔다.
/// BlueScreenGauge 가 "지금 사거리 안에 몇 마리 있는가"를 물을 때 쓴다 — 마릿수가 곧 게이지 속도다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BlueScreenCaster : MonoBehaviour
{
    /// <summary>씬에 켜져 있는 모든 인스턴스. OnEnable/OnDisable 이 스스로 등록·해제한다.</summary>
    static readonly List<BlueScreenCaster> active = new();

    [Header("발동 조건")]
    [Tooltip("캐스팅 시작 후 이 거리(유닛)를 넘으면 캐스팅을 멈추고 다시 화면에 보일 때까지 기다린다.\n" +
             "게이지도 이 거리 안에 있는 마릿수만 센다.\n\n" +
             "★ 카메라가 보는 범위보다 훨씬 커야 한다. 작으면 화면 절반만 이동해도 끊긴다.\n" +
             "  카메라 Orthographic Size 기준으로 최소 3~4배는 넉넉히 줄 것.")]
    [SerializeField] float maxCastDistance = 45f;

    [Header("캐스트 타이밍")]
    [Tooltip("각 단계가 유지되는 시간(초). 3단계 전체 캐스트 시간 = 이 값 × 3.")]
    [SerializeField] float stageDuration = 1f;

    [Header("텍스트 (팀장님 LogManager 나 별도 월드 텍스트에 연결)")]
    [SerializeField] string[] stageTexts = { "코드 오류", "MEMORY", "FAILED" };

    [Header("아우라 — 캐스팅 중 파란 점멸 (경고등)")]
    [Tooltip("평상시 색(투명). 캐스팅이 아니면 이 색으로 돌아간다.")]
    [SerializeField] Color idleAuraColor = new(1f, 1f, 1f, 0f);
    [Tooltip("점멸의 파란 쪽 끝 색.")]
    [SerializeField] Color pulseColor = new(0.25f, 0.55f, 1f, 0.9f);
    [SerializeField] float pulseSpeed = 4f;

    SpriteRenderer bodyRenderer;
    SpriteRenderer auraRenderer;
    Coroutine mainRoutine;

    /// <summary>지금 어느 단계인지. UI가 텍스트를 그릴 때 참조. -1이면 캐스팅 중이 아니다.</summary>
    public int CurrentStage { get; private set; } = -1;

    public event System.Action<int, string> StageChanged;

    void Awake()
    {
        bodyRenderer = GetComponent<SpriteRenderer>();

        auraRenderer = transform.Find("Aura")?.GetComponent<SpriteRenderer>()
                       ?? bodyRenderer;

        // 에디터에서 Aura 를 만들 때 Sorting Layer 를 깜빡하기 쉽다.
        // "Default" 에 남아 있으면 몬스터 본체(커스텀 레이어)에 가려 색이 바뀌어도 안 보인다.
        // 코드에서 강제로 위에 뜨는 레이어로 고정해서, 에디터 설정과 무관하게 항상 보이게 한다
        if (auraRenderer != null && auraRenderer != bodyRenderer)
        {
            auraRenderer.sortingLayerName = "Effect";
            auraRenderer.sortingOrder = 50;
        }
    }

    void OnEnable()
    {
        CurrentStage = -1;
        active.Add(this);
        mainRoutine = StartCoroutine(MainLoop());
    }

    void OnDisable()
    {
        active.Remove(this);
        ResetCast();
    }

    // 캐스팅 중일 때만 제자리에서 파란 점멸(색만 오가가)을 매 프레임 갱신한다.
    // 크기나 회전은 건드리지 않는다 — 단순히 제자리에서 밝았다 어두워졌다 하는 경보등 느낌만 낸다
    void Update()
    {
        if (auraRenderer == null || CurrentStage < 0) return;

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);

        auraRenderer.color = Color.Lerp(idleAuraColor, pulseColor, pulse);
    }

    /// <summary>처음 보일 때까지 기다리다가 캐스트를 돌리고, 사거리를 벗어나면 다시 보일 때까지 기다린다.</summary>
    IEnumerator MainLoop()
    {
        while (true)
        {
            while (!bodyRenderer.isVisible)
                yield return null;

            yield return RunCastCycle();
        }
    }

    /// <summary>거리가 유효한 동안 3단계 캐스트를 계속 반복한다.</summary>
    IEnumerator RunCastCycle()
    {
        // 시작하자마자 거리 초과일 수 있다(화면엔 보이지만 사거리 밖). 그 경우에도
        // 최소 한 프레임은 반드시 넘기고 나가야 MainLoop 와 맞물려 무한 루프에 빠지지 않는다
        yield return null;

        while (true)
        {
            for (int stage = 0; stage < stageTexts.Length; stage++)
            {
                CurrentStage = stage;
                StageChanged?.Invoke(stage, stageTexts[stage]);

                float elapsed = 0f;
                while (elapsed < stageDuration)
                {
                    if (IsTooFar())
                    {
                        ResetCast();
                        yield break;
                    }

                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            // FAILED 까지 도달 = UI 번쩍임 신호만 보낸다. 게이지 자체는 시간 기반으로 따로 찬다
            BlueScreenGauge.Instance.NotifyCastCompleted();

            ResetVisualOnly();
            CurrentStage = -1;

            if (IsTooFar())
                yield break;

            yield return null;
        }
    }

    bool IsTooFar()
    {
        if (maxCastDistance <= 0f) return false;

        Transform player = GameManager.instance != null && GameManager.instance.player != null
            ? GameManager.instance.player.transform
            : null;

        if (player == null) return false;

        float sqrDist = ((Vector2)transform.position - (Vector2)player.position).sqrMagnitude;
        return sqrDist > maxCastDistance * maxCastDistance;
    }

    void ResetVisualOnly()
    {
        if (auraRenderer == null) return;

        auraRenderer.color = idleAuraColor;
        auraRenderer.transform.localScale = Vector3.one;
        auraRenderer.transform.localRotation = Quaternion.identity;
    }

    void ResetCast()
    {
        ResetVisualOnly();
        CurrentStage = -1;
    }

    /// <summary>지금 사거리 안에 있는 블루스크린 몬스터 수. 게이지가 이 수에 비례해 빨리 찬다.</summary>
    public static int CountInRange()
    {
        Transform player = GameManager.instance != null && GameManager.instance.player != null
            ? GameManager.instance.player.transform
            : null;

        if (player == null) return 0;

        Vector2 playerPos = player.position;
        int count = 0;

        for (int i = 0; i < active.Count; i++)
        {
            BlueScreenCaster caster = active[i];
            if (caster == null) continue;

            float limit = caster.maxCastDistance;
            if (limit <= 0f) { count++; continue; }

            float sqrDist = ((Vector2)caster.transform.position - playerPos).sqrMagnitude;
            if (sqrDist <= limit * limit) count++;
        }

        return count;
    }
}
