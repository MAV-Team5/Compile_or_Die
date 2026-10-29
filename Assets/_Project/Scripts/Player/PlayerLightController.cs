using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 플레이어 주변을 밝히는 광원. 레벨이 오르면 조금 커지고, 숨 쉬듯 미세하게 흔들리고,
/// 맞으면 붉게 어두워졌다 돌아온다.
///
/// 기준 반경·밝기·색은 Light2D 컴포넌트에 둔 값을 그대로 쓴다 (임시 — 능력치로 만들면 CharacterData 로 옮길 것).
/// 반경은 노리고 키우는 능력치가 아니라 연출이라 성장 폭을 작게, 상한을 낮게 둔다.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class PlayerLightController : MonoBehaviour
{
    [Header("레벨 성장")]
    [Tooltip("레벨이 1 오를 때마다 기준 반경에 더해지는 비율. 0.02 = +2%.")]
    [SerializeField] float growthPerLevel = 0.02f;

    [Tooltip("성장 상한. 0.5 = 기준 반경의 1.5배까지.")]
    [SerializeField] float maxGrowth = 0.5f;

    [Tooltip("레벨업 뒤 새 반경까지 커지는 데 걸리는 시간(초).")]
    [Min(0.01f)] [SerializeField] float growSmoothTime = 0.5f;

    [Header("호흡")]
    [Tooltip("반경이 흔들리는 폭. 0.04 = ±4%. 5%를 넘으면 시야 판단이 흔들린다.")]
    [Range(0f, 0.1f)] [SerializeField] float breathAmplitude = 0.04f;

    [Tooltip("초당 호흡 횟수. 0.5 = 2초에 한 번.")]
    [Min(0.05f)] [SerializeField] float breathsPerSecond = 0.5f;

    [Header("레벨업 펄스")]
    [Tooltip("레벨업 순간 반경이 잠깐 더 부푸는 비율.")]
    [Range(0f, 0.5f)] [SerializeField] float levelUpPulse = 0.15f;

    [Min(0.05f)] [SerializeField] float levelUpPulseTime = 0.4f;

    [Header("피격")]
    [Tooltip("맞는 동안 밝기가 줄어드는 최대 비율. 반경(정보)은 건드리지 않는다.\n" +
             "적에게 둘러싸이면 계속 맞으므로 크게 잡으면 위험할수록 시야가 나빠진다.")]
    [Range(0f, 0.4f)] [SerializeField] float hitDim = 0.2f;

    [SerializeField] Color hitTint = new(1f, 0.35f, 0.3f);
    [Range(0f, 1f)] [SerializeField] float hitTintAmount = 0.5f;

    // 광원 밖 오브젝트가 거리로 밝기를 조절하도록 셰이더에 넘기는 전역값 (SG_GlowFalloff 가 읽는다)
    static readonly int LightPosId = Shader.PropertyToID("_PlayerLightPos");
    static readonly int LightOuterId = Shader.PropertyToID("_PlayerLightOuter");
    static readonly int LightInnerId = Shader.PropertyToID("_PlayerLightInner");

    Light2D playerLight;
    PlayerHitFeedback hitFeedback;
    LevelSystem levelSystem;

    float baseOuterRadius;
    float baseInnerRatio;
    float baseIntensity;
    Color baseColor;

    float growth;
    float targetGrowth;
    float growthVelocity;
    float pulseRemain;

    void Awake()
    {
        playerLight = GetComponent<Light2D>();
        hitFeedback = GetComponentInParent<PlayerHitFeedback>();

        baseOuterRadius = playerLight.pointLightOuterRadius;
        baseInnerRatio = baseOuterRadius > 0f ? playerLight.pointLightInnerRadius / baseOuterRadius : 0f;
        baseIntensity = playerLight.intensity;
        baseColor = playerLight.color;
    }

    void Start()
    {
        levelSystem = GameManager.instance != null ? GameManager.instance.levelSystem : null;
        if (levelSystem == null) return;

        levelSystem.LeveledUp += OnLeveledUp;
        targetGrowth = GrowthAt(levelSystem.Level);
        growth = targetGrowth;
    }

    void OnDestroy()
    {
        if (levelSystem != null) levelSystem.LeveledUp -= OnLeveledUp;
    }

    void Update()
    {
        growth = Mathf.SmoothDamp(growth, targetGrowth, ref growthVelocity, growSmoothTime);
        if (pulseRemain > 0f) pulseRemain -= Time.deltaTime;

        float breath = 1f + breathAmplitude * Mathf.Sin(Time.time * breathsPerSecond * Mathf.PI * 2f);
        float pulse = 1f + levelUpPulse * PulseShape();
        float outer = baseOuterRadius * (1f + growth) * breath * pulse;

        playerLight.pointLightOuterRadius = outer;
        playerLight.pointLightInnerRadius = outer * baseInnerRatio;

        Shader.SetGlobalVector(LightPosId, transform.position);
        Shader.SetGlobalFloat(LightOuterId, outer);
        Shader.SetGlobalFloat(LightInnerId, outer * baseInnerRatio);

        float hit = hitFeedback != null ? hitFeedback.Level : 0f;
        playerLight.intensity = baseIntensity * (1f - hitDim * hit);
        playerLight.color = Color.Lerp(baseColor, hitTint, hitTintAmount * hit);
    }

    void OnLeveledUp(int newLevel)
    {
        targetGrowth = GrowthAt(newLevel);
        pulseRemain = levelUpPulseTime;
    }

    float GrowthAt(int level) => Mathf.Min(maxGrowth, growthPerLevel * (level - 1));

    /// <summary>0에서 시작해 부풀었다 0으로 돌아오는 모양. 시작이 튀지 않는다.</summary>
    float PulseShape()
    {
        if (pulseRemain <= 0f) return 0f;
        return Mathf.Sin(pulseRemain / levelUpPulseTime * Mathf.PI);
    }
}
