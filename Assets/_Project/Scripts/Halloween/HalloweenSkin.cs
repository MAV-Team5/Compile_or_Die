using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 할로윈 시연용 겉모습·소리 교체 창구. 게임 코드는 여기만 부른다.
///
/// <b>프리팹을 안 건드리는 이유</b> — 시연이 끝나면 원래대로 돌아가야 하고,
/// 프리팹·씬은 충돌이 잦은 파일이다(시행착오 노트 참고). 그래서 스프라이트만
/// 런타임에 갈아끼우고, 끄면 아무 일도 없었던 것처럼 된다.
///
/// <b>상태 신호</b> — 적 스크립트들은 "맞았다 / 공격했다 / 기를 모은다" 만 알린다.
/// 그 순간 무슨 그림·소리를 낼지는 <see cref="HalloweenBody"/> 가 스킨을 보고 정한다.
/// 스킨이 없는 적에게 알려도 아무 일도 없다.
/// </summary>
public static class HalloweenSkin
{
    const string DbPath = "HalloweenSkinDB";

    /// <summary>이 크기 이상으로 나온 잡몹은 등장 목소리를 낸다.</summary>
    public const float LoudSize = 2f;

    static HalloweenSkinDB db;
    static bool searched;

    static readonly Dictionary<EnemyData, HalloweenSkinDB.EnemySkin> enemyMap = new();
    static readonly Dictionary<string, HalloweenSkinDB.ProjectileSkin> projectileMap = new();

    /// <summary>크기를 맞춘 스프라이트. 종류·상태마다 한 번만 만든다 — 개체마다 만들면 수백 개가 생긴다.</summary>
    static readonly Dictionary<(Object, int), Sprite[]> resizedCache = new();

    // 도메인 리로드를 꺼둔 에디터에서도 플레이할 때마다 새로 읽게 한다
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        db = null;
        searched = false;
        enemyMap.Clear();
        projectileMap.Clear();
        resizedCache.Clear();
    }

    static HalloweenSkinDB DB
    {
        get
        {
            if (searched) return db;

            searched = true;
            db = Resources.Load<HalloweenSkinDB>(DbPath);
            if (db == null) return null;

            foreach (var e in db.enemies)
                if (e != null && e.enemy != null && e.HasAny) enemyMap[e.enemy] = e;

            foreach (var p in db.projectiles)
                if (p != null && p.prefab != null && p.HasAny) projectileMap[p.prefab.name] = p;

            Debug.Log($"[Halloween] 스킨 {(db.active ? "켜짐" : "꺼짐")} — 적 {enemyMap.Count}종, 투사체 {projectileMap.Count}종");

            return db;
        }
    }

    public static bool Active
    {
        get
        {
            HalloweenSkinDB d = DB;
            return d != null && d.active;
        }
    }

    /// <summary>목소리 전체 크기. 스킨이 없으면 0.</summary>
    internal static float VoiceVolume => db != null ? db.voiceVolume : 0f;
    internal static float GlitchGap => db != null ? db.glitchGap : 0f;
    internal static int GlitchMaxAtOnce => db != null ? db.glitchMaxAtOnce : int.MaxValue;

    static HalloweenSkinDB.EnemySkin SkinOf(EnemyData data)
    {
        if (data == null || !Active) return null;

        return enemyMap.TryGetValue(data, out var skin) ? skin : null;
    }

    internal static bool Has(AudioClip[] clips) => clips != null && clips.Length > 0;
    internal static bool Has(Sprite[] sprites) => sprites != null && sprites.Length > 0;

    static HalloweenBody BodyOf(Component who)
    {
        if (who == null) return null;

        // 원거리 몬스터 스크립트는 적 루트에 같이 붙어 있다. 자식에서 불려도 찾게 위로 올라간다
        return who.TryGetComponent(out HalloweenBody body) ? body : who.GetComponentInParent<HalloweenBody>();
    }

    // ───────────────────────── 적 ─────────────────────────

    /// <summary>
    /// <see cref="Enemy.Init"/> 끝에서 부른다. 스킨이 있으면 겉모습을 갈아끼우고,
    /// 엘리트·보스, 또는 크게 나온 개체(<see cref="LoudSize"/> 이상)면 등장 목소리를 낸다.
    /// 스킨이 없으면 지난번에 갈아끼운 것을 되돌린다.
    /// </summary>
    /// <param name="size">이번 개체의 크기 배율(EnemyData.scale × 웨이브 sizeScale).</param>
    public static void ApplyEnemy(Enemy enemy, EnemyData data, float size = 1f)
    {
        if (enemy == null) return;

        HalloweenSkinDB.EnemySkin skin = SkinOf(data);

        if (!enemy.TryGetComponent(out HalloweenBody body))
        {
            // 스킨도 없고 붙은 적도 없으면 아무것도 안 붙인다
            if (skin == null) return;

            body = enemy.gameObject.AddComponent<HalloweenBody>();
        }

        body.Setup(skin, data);

        // 잡몹은 초당 수십 마리가 나오므로 등장 목소리를 안 낸다.
        // 단 크게 나온 잡몹(자란 Warning, 큰 Prontf, 데드락 떼)은 등장 자체가 사건이라 낸다
        bool loud = data.rank != EnemyData.Rank.Minion || size >= LoudSize;
        if (skin != null && loud && Has(skin.spawnClips))
            SfxPlayer.PlayAny(skin.spawnClips, VoiceVolume, HalloweenBody.VoiceInterval);
    }

    /// <summary>맞았을 때. <see cref="Enemy.TakeDamage"/> 가 죽지 않았을 때만 부른다.</summary>
    public static void PlayHit(Enemy enemy)
    {
        HalloweenBody body = BodyOf(enemy);
        if (body != null) body.OnHit();
    }

    /// <summary>
    /// 공격한 순간. 근접은 플레이어에 닿아 피해를 줄 때(<c>PlayerHealth</c>),
    /// 원거리는 쏘는 순간(<c>SemicolonCaster</c>), 블루스크린은 시전이 끝난 순간이다.
    /// </summary>
    public static void PlayAttack(Component who)
    {
        HalloweenBody body = BodyOf(who);
        if (body != null) body.OnAttack();
    }

    /// <summary>페이즈가 바뀌었다. 스킨에 그 페이즈 그림(p2_)이 있으면 그림이 바뀐다.</summary>
    public static void SetPhase(Component who, int phase)
    {
        HalloweenBody body = BodyOf(who);
        if (body != null) body.SetPhase(phase);
    }

    /// <summary>기를 모으는 동안 켜둔다. 조준·시전·잠김처럼 "곧 뭔가 한다" 를 보여주는 자세.</summary>
    public static void SetCharging(Component who, bool on)
    {
        HalloweenBody body = BodyOf(who);
        if (body != null) body.SetCharging(on);
    }

    /// <summary>죽는 소리. 스킨 목소리가 있으면 그걸, 없으면 원래 소리를 낸다.</summary>
    public static void PlayDeath(EnemyData data)
    {
        if (data == null) return;

        HalloweenSkinDB.EnemySkin skin = SkinOf(data);

        if (skin != null && Has(skin.deathClips))
        {
            SfxPlayer.PlayAny(skin.deathClips, VoiceVolume, Mathf.Max(HalloweenBody.VoiceInterval, data.deathInterval));
            return;
        }

        SfxPlayer.PlayAny(data.deathClips, data.deathVolume, data.deathInterval);
    }

    /// <summary>
    /// 죽음 모션.
    /// <list type="bullet">
    /// <item>스킨에 사망 그림이 있으면 None — 하얗게 덮으면 그 그림이 안 보인다.</item>
    /// <item>사망 그림은 없는데 원래 사망 애니메이션 때문에 None 이던 적은, 그 애니메이션 그림이
    ///       스킨에 덮이므로 대신 하얗게 줄어드는 모션을 쓴다.</item>
    /// </list>
    /// </summary>
    public static EnemyData.DeathStyle DeathStyleOf(EnemyData data)
    {
        if (data == null) return EnemyData.DeathStyle.None;

        HalloweenSkinDB.EnemySkin skin = SkinOf(data);
        if (skin == null) return data.deathStyle;

        if (Has(skin.deathFrames)) return EnemyData.DeathStyle.None;

        if (data.deathStyle == EnemyData.DeathStyle.None && skin.HasSprites)
            return EnemyData.DeathStyle.ShrinkFade;

        return data.deathStyle;
    }

    /// <summary>죽고 사라지기까지 기다릴 시간. 사망 그림이 있으면 끝까지 보여줄 만큼 기다린다.</summary>
    public static float DeathWaitOf(EnemyData data)
    {
        if (data == null) return 0f;

        HalloweenSkinDB.EnemySkin skin = SkinOf(data);
        if (skin == null) return data.DeathWait;

        if (Has(skin.deathFrames))
            return Mathf.Max(data.deathDuration, HalloweenBody.OneShotLength(skin.deathFrames.Length, skin.fps) + HalloweenBody.DeathHold);

        if (data.deathStyle == EnemyData.DeathStyle.None && data.deathDuration <= 0f && skin.HasSprites)
            return EnemyData.DefaultDeathMotion;

        return data.DeathWait;
    }

    // ───────────────────────── 보스 ─────────────────────────

    /// <summary>
    /// 세이렌이 나온 순간 배경음을 세이렌 노래로 바꾼다. <see cref="HalloweenBossShow"/> 가 부른다.
    /// 판이 끝나 결과 씬으로 가면 그쪽 BgmSource 가 평소 곡으로 되돌린다.
    /// </summary>
    public static void PlayBossBgm()
    {
        if (!Active || db.bossBgm == null) return;

        BgmPlayer.Play(db.bossBgm, db.bossVolume);
    }

    // ───────────────────────── 투사체 ─────────────────────────

    /// <summary>
    /// <see cref="PoolManager"/> 가 꺼내줄 때 부른다. 프리팹 이름으로 스킨을 찾는다 —
    /// 풀에서 나온 것은 이름이 "(Clone)" 이라 원본 프리팹 쪽 이름을 써야 한다.
    /// </summary>
    public static void ApplyProjectile(GameObject prefab, GameObject instance)
    {
        if (prefab == null || instance == null) return;

        HalloweenSkinDB.ProjectileSkin skin = null;
        if (Active) projectileMap.TryGetValue(prefab.name, out skin);

        if (!instance.TryGetComponent(out HalloweenProjectile look))
        {
            if (skin == null) return;

            look = instance.AddComponent<HalloweenProjectile>();
        }

        look.Setup(skin, prefab);

        if (skin != null && Has(skin.fireClips))
            SfxPlayer.PlayAny(skin.fireClips, VoiceVolume * 0.5f, 0.08f);
    }

    // ───────────────────────── 공용 ─────────────────────────

    /// <summary>
    /// <paramref name="reference"/> 의 긴 변이 <paramref name="targetUnits"/> 월드 유닛으로 보이게 하는 PPU.
    /// 한 캐릭터의 모든 상태 그림에 같은 값을 써야 상태가 바뀔 때 크기가 튀지 않는다.
    /// </summary>
    internal static float PpuFor(Sprite reference, float targetUnits)
    {
        if (reference == null) return 100f;

        Rect r = reference.rect;
        return Mathf.Max(r.width, r.height) / Mathf.Max(0.01f, targetUnits);
    }

    /// <summary>
    /// <paramref name="ppu"/> 로 다시 잡은 스프라이트를 만든다. 텍스처는 그대로 쓰므로 메모리가 거의 안 든다.
    /// (<paramref name="key"/>, <paramref name="slot"/>) 마다 한 번만 만든다.
    /// </summary>
    /// <param name="pivotShiftX">피벗을 가로로 이만큼(그림 너비 비율) 민다. 트랜스폼을 안 건드리고 그림만 옆으로 튀게 할 때 쓴다(글리치).</param>
    internal static Sprite[] Resized(Object key, int slot, Sprite[] frames, float ppu, float pivotShiftX = 0f)
    {
        if (key != null && resizedCache.TryGetValue((key, slot), out Sprite[] cached)) return cached;

        var list = new List<Sprite>();

        if (frames != null)
        {
            foreach (Sprite f in frames)
            {
                if (f == null) continue;

                Rect rect = f.rect;
                Vector2 pivot = new(f.pivot.x / rect.width + pivotShiftX, f.pivot.y / rect.height);

                Sprite s = Sprite.Create(f.texture, rect, pivot, ppu, 0, SpriteMeshType.FullRect);
                s.name = f.name + "_hw";
                list.Add(s);
            }
        }

        Sprite[] result = list.ToArray();
        if (key != null) resizedCache[(key, slot)] = result;

        return result;
    }
}
