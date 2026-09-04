using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 세미콜론 몬스터. 3단계로 경고하고 발사한다:
///
/// <code>
/// 1단계  컴파일러의 그 빨간 오류 아이콘이 몸통에 뜬다
/// 2단계  ";"
/// 3단계  "required"
/// 떨림   몸이 부르르 떨린다
/// 발사   세미콜론 투사체가 플레이어를 향해 날아간다
/// </code>
///
/// <b>사거리 안에 있는 동안은 매 프레임 계속 멈춰있는다</b>(<see cref="Update"/>) — 발사 직전에만
/// 잠깐 멈추면, 발사 사이 텀(fireInterval)에 Enemy.cs 의 무제한 추적 이동 때문에 결국
/// 플레이어와 완전히 겹쳐서 접촉 피해(PlayerHealth.contactDamage 경로)가 나가버린다.
/// 매 프레임 Enemy.Displace(0, 0.2초) 를 계속 불러 그 착각을 원천 차단한다 —
/// Enemy.cs 는 전혀 안 건드리고 이미 있는 "넉백 중 정지" 메커니즘을 재활용한 것.
///
/// <b>떨림</b>은 Animator 에 진짜로 <see cref="shakeTrigger"/> 라는 이름의 Trigger 파라미터가
/// 있을 때만 그쪽에 맡긴다 — 파라미터가 없는데도 SetTrigger 를 부르면 유니티가 조용히
/// 무시하고 넘어가서 "아무 일도 안 일어나는" 버그가 생기기 쉽다. 없으면 코드로 직접
/// 몸통을 흔드는 폴백으로 자동 대체한다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SemicolonCaster : MonoBehaviour
{
    [Header("발사 조건")]
    [Tooltip("이 거리(유닛) 안에 플레이어가 있을 때만 멈추고 공격한다.")]
    [SerializeField] float fireRange = 10f;

    [Tooltip("한 사이클(대기 + 3단계 텔레그래프 + 떨림 + 발사) 사이 대기 시간(초).")]
    [SerializeField] float fireInterval = 2.5f;

    [Header("3단계 텔레그래프")]
    [Tooltip("각 단계가 유지되는 시간(초).")]
    [SerializeField] float stageDuration = 0.4f;

    [SerializeField] string stage2Text = ";";
    [SerializeField] string stage3Text = "required";

    [Header("떨림")]
    [Tooltip("Animator 에 이 이름의 Trigger 파라미터가 실제로 있을 때만 애니메이션을 쓴다. " +
             "없으면 자동으로 코드 폴백을 쓴다.")]
    [SerializeField] string shakeTrigger = "Shake";

    [Tooltip("떨림이 지속되는 시간(초). Animator 를 쓸 때는 이 시간이 클립 길이와 비슷해야 " +
             "발사 타이밍이 안 깨진다.")]
    [SerializeField] float shakeDuration = 0.25f;

    [Tooltip("Animator 트리거가 없을 때의 폴백 지터 폭.")]
    [SerializeField] float shakeMagnitude = 0.08f;

    [Header("텍스트 스타일")]
    [SerializeField] Vector3 textOffset = Vector3.zero;
    [SerializeField] float fontSize = 3f;
    [SerializeField] Color textColor = new(1f, 0.25f, 0.25f, 1f);

    [Header("투사체")]
    [Tooltip("SemicolonProjectile 이 붙은 세미콜론 모양 프리팹.")]
    [SerializeField] GameObject projectilePrefab;

    [Tooltip("발사 지점 오프셋(로컬).")]
    [SerializeField] Vector3 muzzleOffset = Vector3.zero;

    TextMeshPro label;
    SpriteRenderer errorIcon; // 자식 "ErrorIcon". 없으면 1단계에서 그냥 아무 그림도 안 뜬다
    Animator anim;
    Enemy enemyRef; // 사거리 안에서 멈추게 할 때 Displace(0, duration)를 빌려 쓴다
    Coroutine routine;
    bool hasShakeTrigger;

    void Awake()
    {
        enemyRef = GetComponent<Enemy>();

        errorIcon = transform.Find("ErrorIcon")?.GetComponent<SpriteRenderer>();
        if (errorIcon != null)
        {
            errorIcon.enabled = false;

            // 기본 레이어("Default")에 남으면 몬스터 본체(커스텀 정렬 레이어)에 가려 안 보인다.
            // CastLabel/Aura 에서 반복됐던 문제라 여기서도 미리 고정해둔다
            errorIcon.sortingLayerName = "Effect";
            errorIcon.sortingOrder = 49;
        }

        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();

        hasShakeTrigger = HasTrigger(anim, shakeTrigger);

        var go = new GameObject("ErrorLabel");
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = textOffset;

        label = go.AddComponent<TextMeshPro>();
        label.fontSize = fontSize;
        label.color = textColor;
        label.alignment = TextAlignmentOptions.Center;
        label.text = string.Empty;

        Renderer labelRenderer = label.GetComponent<Renderer>();
        labelRenderer.sortingLayerName = "Effect";
        labelRenderer.sortingOrder = 50;
    }

    /// <summary>Animator 에 그 이름의 Trigger 파라미터가 진짜로 있는지 확인한다.
    /// 없는데 SetTrigger 를 부르면 유니티가 경고만 찍고 조용히 무시한다 — "떨지 않는" 버그의 원인.</summary>
    static bool HasTrigger(Animator animator, string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName)) return false;

        foreach (var p in animator.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName)
                return true;

        return false;
    }

    void OnEnable() => routine = StartCoroutine(FireLoop());

    void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        SetIcon(false);
        SetText(string.Empty);
    }

    // 사거리 안이면 매 프레임 계속 멈춰둔다. FireLoop 쪽은 "발사 타이밍"만 맡고,
    // "이동 정지"는 여기서 따로 책임진다 — 안 그러면 fireInterval 사이에 이동이 재개되어
    // 사거리를 가뿐히 무시하고 플레이어를 향해 계속 걸어가 결국 맞붙어버린다
    // (Enemy.cs 의 추적 이동이 거리 제한 없이 항상 플레이어를 향하기 때문이다).
    void Update()
    {
        if (enemyRef == null) return;

        Transform player = GameManager.instance != null && GameManager.instance.player != null
            ? GameManager.instance.player.transform
            : null;

        if (player == null) return;

        float sqrDist = ((Vector2)transform.position - (Vector2)player.position).sqrMagnitude;
        if (sqrDist > fireRange * fireRange) return;

        // 매 프레임 짧게(0.2초) 정지 시간을 다시 채우는 식 — Displace 가 Mathf.Max 로 누적되므로
        // 계속 불러주기만 해도 "사거리 안에 있는 동안은 계속 멈춰있기"가 자연스럽게 성립한다
        enemyRef.Displace(Vector2.zero, 0.2f);
    }

    // 부모(몬스터) 스케일이 커지거나 뒤집혀도 텍스트는 항상 일정한 크기·정자세로 보이게 보정
    void LateUpdate()
    {
        if (label == null || label.transform.parent == null) return;

        Vector3 parentScale = label.transform.parent.lossyScale;
        float px = Mathf.Abs(parentScale.x) > 0.0001f ? parentScale.x : 1f;
        float py = Mathf.Abs(parentScale.y) > 0.0001f ? parentScale.y : 1f;

        label.transform.localScale = new Vector3(1f / px, 1f / Mathf.Abs(py), 1f);
    }

    IEnumerator FireLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(fireInterval);

            Transform player = GameManager.instance != null && GameManager.instance.player != null
                ? GameManager.instance.player.transform
                : null;

            if (player == null) continue;

            float sqrDist = ((Vector2)transform.position - (Vector2)player.position).sqrMagnitude;
            if (sqrDist > fireRange * fireRange) continue;

            // Update() 가 이미 사거리 안에서 계속 멈춰두고 있으므로, 여기서는 텔레그래프만 진행한다
            yield return RunTelegraph();

            // 텔레그래프 도중 플레이어가 움직였을 수 있으니 발사 직전 위치를 다시 잰다
            Fire(player.position);
        }
    }

    IEnumerator RunTelegraph()
    {
        SetIcon(true);
        SetText(string.Empty);
        yield return new WaitForSeconds(stageDuration);

        SetIcon(false);
        SetText(stage2Text);
        yield return new WaitForSeconds(stageDuration);

        SetText(stage3Text);
        yield return new WaitForSeconds(stageDuration);

        yield return Shake();

        SetText(string.Empty);
    }

    IEnumerator Shake()
    {
        if (hasShakeTrigger)
        {
            anim.SetTrigger(shakeTrigger);
            yield return new WaitForSeconds(shakeDuration);
            yield break;
        }

        Vector3 basePos = transform.position;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float ox = Random.Range(-shakeMagnitude, shakeMagnitude);
            float oy = Random.Range(-shakeMagnitude, shakeMagnitude);
            transform.position = basePos + new Vector3(ox, oy, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = basePos;
    }

    void SetIcon(bool on)
    {
        if (errorIcon != null) errorIcon.enabled = on;
    }

    void SetText(string text)
    {
        if (label != null) label.text = text;
    }

    void Fire(Vector2 targetPosition)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"[{name}] projectilePrefab 이 비어 있어 발사할 수 없다.", this);
            return;
        }

        Vector2 spawnAt = (Vector2)transform.position + (Vector2)muzzleOffset;

        GameObject go = PooledSpawner.Spawn(projectilePrefab, spawnAt, PoolType.Bullet);
        if (go.TryGetComponent(out SemicolonProjectile projectile))
            projectile.Launch(targetPosition);
    }
}
