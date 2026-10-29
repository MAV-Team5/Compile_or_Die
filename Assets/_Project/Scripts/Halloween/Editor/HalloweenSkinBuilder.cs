using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// <c>Art/Halloween</c> 폴더를 보고 <see cref="HalloweenSkinDB"/> 를 채운다.
///
/// <b>규칙은 폴더 이름 하나뿐이다.</b>
/// <list type="bullet">
/// <item><c>Enemies/&lt;EnemyData 에셋 이름&gt;/</c> — 그림·소리 모두 파일 이름 앞머리로 상태를 나눈다.
///       hit… = 피격, attack… = 공격, charge… = 기모으기, die…/death… = 사망, spawn… = 등장(소리만).
///       그림은 앞머리가 없으면 평소, 소리는 앞머리가 없으면 사망.</item>
/// <item><c>Projectiles/&lt;투사체 프리팹 이름&gt;/</c> — 그림은 프레임, 소리는 전부 발사음</item>
/// </list>
/// 파일을 넣거나 지우면 임포트가 끝난 뒤 자동으로 다시 채운다. 메뉴 <c>CoD/Halloween</c> 에서 손으로도 돌릴 수 있다.
/// </summary>
public class HalloweenSkinBuilder : AssetPostprocessor
{
    public const string Root = "Assets/_Project/Art/Halloween";
    const string EnemiesRoot = Root + "/Enemies";
    const string ProjectilesRoot = Root + "/Projectiles";
    const string DbAssetPath = "Assets/_Project/Resources/HalloweenSkinDB.asset";
    const string ToggleMenu = "CoD/Halloween/할로윈 스킨 켜기 끄기";

    static bool scheduled;

    static string Norm(string path) => path.Replace('\\', '/');

    static bool Under(string path) => Norm(path).StartsWith(Root + "/");

    // ───────────────────────── 임포트 설정 ─────────────────────────

    /// <summary>그림은 전부 같은 설정으로 들어온다. 크기는 런타임에 원래 그림에 맞추므로 PPU 는 아무 값이어도 된다.</summary>
    void OnPreprocessTexture()
    {
        if (!Under(assetPath)) return;

        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 256f;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.filterMode = FilterMode.Bilinear;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.maxTextureSize = 1024;

        // 캐릭터 외곽선이 뭉개지지 않게 압축하지 않는다. 512px 몇십 장이라 용량 부담이 없다
        ti.textureCompression = TextureImporterCompression.Uncompressed;
    }

    /// <summary>목소리는 짧고 자주 나므로 메모리에 풀어둔다. 끊김 없이 바로 나와야 한다.</summary>
    void OnPreprocessAudio()
    {
        if (!Under(assetPath)) return;

        var ai = (AudioImporter)assetImporter;
        ai.forceToMono = true;
        ai.loadInBackground = false;

        AudioImporterSampleSettings s = ai.defaultSampleSettings;
        s.loadType = AudioClipLoadType.DecompressOnLoad;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = 0.7f;
        ai.defaultSampleSettings = s;
    }

    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (imported.Any(Under) || deleted.Any(Under) || moved.Any(Under) || movedFrom.Any(Under))
            Schedule();
    }

    /// <summary>파일을 여러 개 한꺼번에 넣으면 임포트 콜백이 여러 번 온다. 다 끝난 뒤 한 번만 돈다.</summary>
    static void Schedule()
    {
        if (scheduled) return;

        scheduled = true;
        EditorApplication.delayCall += () =>
        {
            scheduled = false;
            Rebuild();
        };
    }

    // ───────────────────────── 메뉴 ─────────────────────────

    [MenuItem("CoD/Halloween/스킨 다시 읽기")]
    public static void Rebuild()
    {
        HalloweenSkinDB db = LoadOrCreateDb();

        // 사람이 조정한 값(크기 배율·fps)을 지키려고 기존 항목을 들고 있다가 재사용한다
        var oldEnemies = new Dictionary<EnemyData, HalloweenSkinDB.EnemySkin>();
        foreach (var e in db.enemies)
            if (e != null && e.enemy != null) oldEnemies[e.enemy] = e;

        var oldProjectiles = new Dictionary<GameObject, HalloweenSkinDB.ProjectileSkin>();
        foreach (var p in db.projectiles)
            if (p != null && p.prefab != null) oldProjectiles[p.prefab] = p;

        Dictionary<string, EnemyData> enemyByName = EnemyDataByName();

        var enemies = new List<HalloweenSkinDB.EnemySkin>();
        var projectiles = new List<HalloweenSkinDB.ProjectileSkin>();
        var log = new List<string>();

        if (AssetDatabase.IsValidFolder(EnemiesRoot))
        {
            foreach (string dir in AssetDatabase.GetSubFolders(EnemiesRoot))
            {
                string name = Path.GetFileName(dir);

                Sprite[] sprites = SpritesIn(dir);
                List<AudioClip> clips = ClipsIn(dir);
                if (sprites.Length == 0 && clips.Count == 0) continue;

                if (!enemyByName.TryGetValue(name, out EnemyData data))
                {
                    Debug.LogWarning($"[Halloween] '{name}' 이름의 EnemyData 가 없다. 폴더 이름을 EnemyData 에셋 이름과 똑같이 맞출 것.");
                    continue;
                }

                if (!oldEnemies.TryGetValue(data, out var skin))
                    skin = new HalloweenSkinDB.EnemySkin { enemy = data };

                // p2_ 로 시작하는 그림은 2페이즈용. 앞머리를 떼고 같은 규칙으로 나눈다
                Sprite[] p1 = sprites.Where(s => !IsPhase2(s)).ToArray();
                Sprite[] p2 = sprites.Where(IsPhase2).ToArray();

                skin.hitFrames = p1.Where(s => Starts(s, "hit")).ToArray();
                skin.attackFrames = p1.Where(s => Starts(s, "attack")).ToArray();
                skin.chargeFrames = p1.Where(s => Starts(s, "charge")).ToArray();
                skin.deathFrames = p1.Where(IsDeath).ToArray();
                skin.idleFrames = p1.Where(s => !IsState(s)).ToArray();

                skin.phase2Hit = p2.Where(s => Starts(s, "hit")).ToArray();
                skin.phase2Attack = p2.Where(s => Starts(s, "attack")).ToArray();
                skin.phase2Charge = p2.Where(s => Starts(s, "charge")).ToArray();
                skin.phase2Idle = p2.Where(s => !IsState(s)).ToArray();

                // 히트박스를 맞출 실제 캐릭터 영역 — 평소 그림(없으면 아무 그림) 첫 장의 불투명한 부분
                Sprite basis = skin.idleFrames.Length > 0 ? skin.idleFrames[0] : sprites.FirstOrDefault();
                skin.idleContent = basis != null ? ContentRect(basis) : new Rect(0f, 0f, 1f, 1f);

                skin.hitClips = clips.Where(c => Starts(c, "hit")).ToArray();
                skin.attackClips = clips.Where(c => Starts(c, "attack")).ToArray();
                skin.chargeClips = clips.Where(c => Starts(c, "charge")).ToArray();
                skin.spawnClips = clips.Where(c => Starts(c, "spawn")).ToArray();
                skin.deathClips = clips.Where(c => !IsState(c) || IsDeath(c)).ToArray();

                enemies.Add(skin);
                log.Add($"  적 {name}: 2페이즈 그림 {p2.Length} / 그림 평소 {skin.idleFrames.Length} · 피격 {skin.hitFrames.Length} · 공격 {skin.attackFrames.Length} · 기모으기 {skin.chargeFrames.Length} · 사망 {skin.deathFrames.Length}" +
                        $" / 소리 사망 {skin.deathClips.Length} · 피격 {skin.hitClips.Length} · 공격 {skin.attackClips.Length} · 기모으기 {skin.chargeClips.Length} · 등장 {skin.spawnClips.Length}");
            }
        }

        if (AssetDatabase.IsValidFolder(ProjectilesRoot))
        {
            foreach (string dir in AssetDatabase.GetSubFolders(ProjectilesRoot))
            {
                string name = Path.GetFileName(dir);

                Sprite[] sprites = SpritesIn(dir);
                List<AudioClip> clips = ClipsIn(dir);
                if (sprites.Length == 0 && clips.Count == 0) continue;

                GameObject prefab = FindPrefab(name);
                if (prefab == null)
                {
                    Debug.LogWarning($"[Halloween] '{name}' 이름의 프리팹이 없다. 폴더 이름을 투사체 프리팹 이름과 똑같이 맞출 것.");
                    continue;
                }

                if (!oldProjectiles.TryGetValue(prefab, out var skin))
                    skin = new HalloweenSkinDB.ProjectileSkin { prefab = prefab };

                skin.frames = sprites;
                skin.fireClips = clips.ToArray();

                projectiles.Add(skin);
                log.Add($"  투사체 {name}: 그림 {sprites.Length} / 발사음 {skin.fireClips.Length}");
            }
        }

        db.enemies = enemies;
        db.projectiles = projectiles;

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Halloween] 스킨 목록 갱신 — 적 {enemies.Count}종, 투사체 {projectiles.Count}종 ({(db.active ? "켜짐" : "꺼짐")})\n" +
                  string.Join("\n", log), db);
    }

    [MenuItem("CoD/Halloween/폴더 만들기")]
    public static void MakeFolders()
    {
        EnsureFolder(EnemiesRoot);
        EnsureFolder(ProjectilesRoot);

        foreach (string name in EnemyDataByName().Keys)
            EnsureFolder($"{EnemiesRoot}/{name}");

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab Proj_"))
        {
            string name = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
            if (name.StartsWith("Proj_")) EnsureFolder($"{ProjectilesRoot}/{name}");
        }

        // 적이 쏘는 탄
        if (FindPrefab("003 Bullet") != null) EnsureFolder($"{ProjectilesRoot}/003 Bullet");

        AssetDatabase.Refresh();
    }

    [MenuItem(ToggleMenu)]
    static void Toggle()
    {
        HalloweenSkinDB db = LoadOrCreateDb();
        db.active = !db.active;

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Halloween] 스킨 {(db.active ? "켜짐" : "꺼짐")}. 다음 플레이부터 적용된다.");
    }

    [MenuItem(ToggleMenu, true)]
    static bool ToggleValidate()
    {
        var db = AssetDatabase.LoadAssetAtPath<HalloweenSkinDB>(DbAssetPath);
        Menu.SetChecked(ToggleMenu, db != null && db.active);
        return true;
    }

    [MenuItem("CoD/Halloween/스킨 목록 선택")]
    static void SelectDb()
    {
        Selection.activeObject = LoadOrCreateDb();
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    // ───────────────────────── 도우미 ─────────────────────────

    static HalloweenSkinDB LoadOrCreateDb()
    {
        var db = AssetDatabase.LoadAssetAtPath<HalloweenSkinDB>(DbAssetPath);
        if (db != null) return db;

        EnsureFolder(Path.GetDirectoryName(DbAssetPath));

        db = ScriptableObject.CreateInstance<HalloweenSkinDB>();
        AssetDatabase.CreateAsset(db, DbAssetPath);
        AssetDatabase.SaveAssets();

        return db;
    }

    static Dictionary<string, EnemyData> EnemyDataByName()
    {
        var map = new Dictionary<string, EnemyData>();

        foreach (string guid in AssetDatabase.FindAssets("t:EnemyData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (data != null) map[Path.GetFileNameWithoutExtension(path)] = data;
        }

        return map;
    }

    static GameObject FindPrefab(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets($"t:Prefab {name}"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == name)
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        return null;
    }

    /// <summary>폴더 바로 아래의 그림만. 파일 이름순 — 프레임은 01, 02 처럼 자릿수를 맞출 것.</summary>
    static Sprite[] SpritesIn(string dir)
    {
        return AssetsIn<Sprite>(dir, "t:Sprite");
    }

    static List<AudioClip> ClipsIn(string dir)
    {
        return AssetsIn<AudioClip>(dir, "t:AudioClip").ToList();
    }

    static T[] AssetsIn<T>(string dir, string filter) where T : Object
    {
        var paths = new SortedSet<string>(System.StringComparer.Ordinal);

        foreach (string guid in AssetDatabase.FindAssets(filter, new[] { dir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Norm(Path.GetDirectoryName(path)) == dir) paths.Add(path);
        }

        var result = new List<T>();
        foreach (string path in paths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) result.Add(asset);
        }

        return result.ToArray();
    }

    const string Phase2Prefix = "p2_";

    static bool IsPhase2(Object asset) => asset.name.StartsWith(Phase2Prefix, System.StringComparison.OrdinalIgnoreCase);

    /// <summary>상태 앞머리로 시작하는가. 2페이즈 앞머리(p2_)는 떼고 본다.</summary>
    static bool Starts(Object asset, string prefix)
    {
        string n = IsPhase2(asset) ? asset.name.Substring(Phase2Prefix.Length) : asset.name;
        return n.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 그림에서 불투명한 부분이 차지하는 영역(0~1). 임포트된 텍스처는 읽기 금지라 원본 파일을 따로 읽는다.
    /// 2픽셀 간격으로 훑는다 — 히트박스 용도라 그 정도 오차는 상관없다.
    /// </summary>
    static Rect ContentRect(Sprite sprite)
    {
        string path = AssetDatabase.GetAssetPath(sprite);
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return new Rect(0f, 0f, 1f, 1f);

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!tex.LoadImage(File.ReadAllBytes(path))) return new Rect(0f, 0f, 1f, 1f);

            int w = tex.width, h = tex.height;
            Color32[] px = tex.GetPixels32();
            int minX = w, minY = h, maxX = -1, maxY = -1;

            for (int y = 0; y < h; y += 2)
            for (int x = 0; x < w; x += 2)
            {
                if (px[y * w + x].a < 25) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            if (maxX < 0) return new Rect(0f, 0f, 1f, 1f);
            return Rect.MinMaxRect(minX / (float)w, minY / (float)h, (maxX + 2) / (float)w, (maxY + 2) / (float)h);
        }
        finally
        {
            Object.DestroyImmediate(tex);
        }
    }

    static bool IsDeath(Object asset) => Starts(asset, "die") || Starts(asset, "death");

    /// <summary>상태 앞머리가 붙은 파일인가. 안 붙은 그림은 평소, 안 붙은 소리는 사망으로 간다.</summary>
    static bool IsState(Object asset)
        => Starts(asset, "hit") || Starts(asset, "attack") || Starts(asset, "charge")
           || Starts(asset, "spawn") || IsDeath(asset);

    static void EnsureFolder(string path)
    {
        path = Norm(path);
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Norm(Path.GetDirectoryName(path));
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
