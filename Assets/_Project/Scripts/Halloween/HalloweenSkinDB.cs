using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 할로윈 시연용 스킨 목록. <c>Resources/HalloweenSkinDB</c> 에 하나만 둔다.
///
/// <b>손으로 채우지 않는다.</b> <c>Art/Halloween</c> 폴더에 파일을 넣으면
/// 에디터 빌더(<c>HalloweenSkinBuilder</c>)가 알아서 다시 채운다.
/// 크기 배율·프레임 속도처럼 사람이 조정한 값은 다시 채울 때도 유지된다.
///
/// <see cref="active"/> 를 끄면 원래 게임 그대로 돌아간다 — 프리팹은 하나도 안 건드렸다.
/// </summary>
[CreateAssetMenu(fileName = "HalloweenSkinDB", menuName = "CoD/Halloween Skin DB")]
public class HalloweenSkinDB : ScriptableObject
{
    [Tooltip("끄면 원래 그림·소리로 돌아간다.")]
    public bool active = true;

    [Tooltip("캐릭터 목소리 전체 크기. 효과음 설정 볼륨이 한 번 더 곱해진다.")]
    [Range(0f, 1f)] public float voiceVolume = 0.6f;

    [Header("보스")]
    [Tooltip("세이렌(판을 끝내는 보스)이 나오는 순간 바뀌는 배경음. 비우면 원래 곡이 이어진다.")]
    [FormerlySerializedAs("endingBgm")] public AudioClip bossBgm;

    [FormerlySerializedAs("endingVolume")]
    [Range(0f, 1f)] public float bossVolume = 0.5f;

    public List<EnemySkin> enemies = new();
    public List<ProjectileSkin> projectiles = new();

    static bool Has(AudioClip[] clips) => clips != null && clips.Length > 0;
    static bool Has(Sprite[] sprites) => sprites != null && sprites.Length > 0;

    /// <summary>
    /// 적 한 종류의 겉모습과 목소리. 상태마다 그림·소리를 따로 둘 수 있다.
    /// 우선순위는 사망 &gt; 피격 &gt; 공격 &gt; 기모으기 &gt; 평소. 그림이 없는 상태는 평소 그림으로 보인다.
    /// </summary>
    [System.Serializable]
    public class EnemySkin
    {
        public EnemyData enemy;

        [Header("그림 — 파일 이름 앞머리로 나뉜다")]
        [Tooltip("평소(반복). 앞머리 없는 그림. 한 장이면 말랑하게 찌그러지는 모션이 붙는다.")]
        public Sprite[] idleFrames;

        [Tooltip("맞았을 때(한 번). hit_")]
        public Sprite[] hitFrames;

        [Tooltip("공격할 때(한 번). attack_ — 근접은 닿아서 때릴 때, 원거리는 쏘는 순간.")]
        public Sprite[] attackFrames;

        [Tooltip("기모으기(반복). charge_ — 세미콜론 조준, 블루스크린 시전, 데드락 잠김 동안.")]
        public Sprite[] chargeFrames;

        [Tooltip("죽을 때(한 번, 마지막 장에서 멈춤). die_ — 있으면 하얗게 사라지는 연출 대신 이 그림이 나온다.")]
        public Sprite[] deathFrames;

        [Min(0.1f)] public float fps = 8f;

        [Tooltip("원래 그림 크기에 맞춘 뒤 곱하는 배율. 캡처 여백 때문에 작아 보이면 올릴 것.")]
        [Min(0.1f)] public float sizeMultiplier = 1f;

        [Tooltip("살아 있는 동안 말랑하게 찌그러지는 모션.")]
        public bool bounce = true;

        [Tooltip("3초에 한 번 지지직거리는 글리치. 좌우로 튀고 빨강·청록으로 갈라진다.")]
        public bool glitch = true;

        [Tooltip("원래 몸통 그림의 월드 크기(긴 변). 0이면 프리팹 몸통 그림으로 잰다.\n" +
                 "HW 프리팹은 몸통에 이미 치이카와가 박혀 있어서, 콜라이더가 맞춰진 원래 크기를 여기에 따로 적어둔다.")]
        [Min(0f)] public float referenceUnits = 0f;

        [Header("소리 — 파일 이름 앞머리로 나뉜다")]
        [Tooltip("죽을 때. die_ 또는 앞머리 없는 소리.")]
        public AudioClip[] deathClips;

        [Tooltip("맞을 때. hit_")]
        public AudioClip[] hitClips;

        [Tooltip("공격할 때. attack_")]
        public AudioClip[] attackClips;

        [Tooltip("기모으기 시작할 때. charge_")]
        public AudioClip[] chargeClips;

        [Tooltip("엘리트·보스가 나올 때. spawn_ — 잡몹은 안 낸다.")]
        public AudioClip[] spawnClips;

        public bool HasSprites => Has(idleFrames) || Has(hitFrames) || Has(attackFrames)
                                  || Has(chargeFrames) || Has(deathFrames);

        public bool HasAny => HasSprites || Has(deathClips) || Has(hitClips) || Has(attackClips)
                              || Has(chargeClips) || Has(spawnClips);
    }

    /// <summary>투사체 한 종류의 겉모습과 발사음.</summary>
    [System.Serializable]
    public class ProjectileSkin
    {
        public GameObject prefab;

        public Sprite[] frames;

        [Min(0.1f)] public float fps = 10f;

        [Min(0.1f)] public float sizeMultiplier = 1f;

        [Tooltip("꺼낼 때(발사) 내는 소리.")]
        public AudioClip[] fireClips;

        public bool HasAny => Has(frames) || Has(fireClips);
    }
}
