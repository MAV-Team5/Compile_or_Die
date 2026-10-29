using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 할로윈 시연용 몬스터를 <b>원본을 복사해서</b> 따로 만든다. 메뉴 <c>CoD/Halloween/할로윈 몬스터 만들기</c>.
///
/// <list type="number">
/// <item>스킨 폴더가 있는 적마다 프리팹·EnemyData 를 "○○○ HW" 로 복사한다</item>
/// <item>복사본에서 원래 연출(애니메이터·눈·아이콘·글자·아우라)을 걷어내고 치이카와 그림을 박는다</item>
/// <item>Stage 01 을 복사해 HW 몬스터로 갈아끼운 "Stage 01 Halloween" 을 만든다</item>
/// <item>Run·MainB 씬이 가리키는 Stage 01 을 Stage 01 Halloween 으로 바꾼다</item>
/// </list>
///
/// <b>원본은 하나도 안 건드린다.</b> 되돌리려면 두 씬의 스테이지만 Stage 01 로 돌려놓으면 된다.
/// 여러 번 돌려도 같은 결과가 나온다(이미 있는 복사본은 GUID 를 유지한 채 내용만 다시 맞춘다).
///
/// <b>걷어내는 방식이 둘인 이유</b> — 스크립트가 이름으로 찾는 자식을 지우면 동작이 바뀐다.
/// 예: BlueScreenCaster 는 "Aura" 가 없으면 본체 렌더러를 대신 점멸시키는데, 시전이 끝날 때
/// 투명색으로 되돌리면서 보스가 통째로 안 보이게 된다. 그래서 지워도 안전한 것만 지우고,
/// 나머지는 오브젝트는 남긴 채 렌더러만 끈다.
/// </summary>
public static class HalloweenPrefabMaker
{
    public const string Suffix = " HW";

    /// <summary>보스를 뺀 할로윈 적의 체력 배율(원본 대비).</summary>
    const float HalfHealth = 0.5f;

    /// <summary>할로윈 보스의 체력 배율(원본 대비).</summary>
    const float BossHealth = 4f;

    const string EnemyDataRoot = "Assets/_Project/Data/Enemy_Data";
    const string DataDir = EnemyDataRoot + "/Halloween";
    const string PrefabDir = "Assets/_Project/Prefabs/Enemy_Prefabs/Halloween";
    const string SkinEnemies = HalloweenSkinBuilder.Root + "/Enemies";
    const string StageSrc = "Assets/_Project/Data/Stage_Data/Stage 01.asset";
    const string StageDst = "Assets/_Project/Data/Stage_Data/Stage 01 Halloween.asset";
    static readonly string[] Scenes =
    {
        "Assets/_Project/Scenes/MainA.unity", "Assets/_Project/Scenes/MainB.unity",
        "Assets/_Project/Scenes/Run.unity", "Assets/_Project/Scenes/StageResult.unity"
    };

    /// <summary>할로윈 배경음. 모든 씬이 같은 곡을 물면 BgmPlayer 가 씬을 넘어가도 끊지 않고 잇는다.</summary>
    const string BgmPath = "Assets/_Project/Art/Audio/bgm/9Halloween_uwawa_loop.wav";

    /// <summary>원래 그림이 없을 때 쓰는 크기. 기존 적은 256px · PPU 128 = 2유닛이다.</summary>
    const float FallbackUnits = 2f;

    [MenuItem("CoD/Halloween/할로윈 몬스터 만들기")]
    public static void Make()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Halloween] 플레이 중에는 만들 수 없다. 플레이를 끄고 다시 실행할 것.");
            return;
        }

        EnsureFolder(DataDir);
        EnsureFolder(PrefabDir);

        var map = new Dictionary<EnemyData, EnemyData>();
        var units = new Dictionary<EnemyData, float>();
        var log = new List<string>();

        foreach (string guid in AssetDatabase.FindAssets("t:EnemyData", new[] { EnemyDataRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith(DataDir + "/")) continue;

            var src = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (src == null || src.prefab == null) continue;

            string name = Path.GetFileNameWithoutExtension(path);
            string skinDir = $"{SkinEnemies}/{name}";
            string skinDirHW = skinDir + Suffix;

            // 스킨 폴더가 있는 적만 만든다. 처음엔 원래 이름, 한 번 돌린 뒤엔 HW 이름으로 있다
            string useDir = AssetDatabase.IsValidFolder(skinDirHW) ? skinDirHW
                          : AssetDatabase.IsValidFolder(skinDir) ? skinDir : null;
            if (useDir == null) continue;

            Sprite idle = IdleSprite(useDir);

            // 원래 그림의 월드 크기. 복사본 몸통엔 치이카와가 박히므로 지금 재둬야 한다 — 콜라이더가 이 크기에 맞춰져 있다
            float refUnits = FallbackUnits;
            var srcRenderer = src.prefab.GetComponent<SpriteRenderer>();
            if (srcRenderer != null && srcRenderer.sprite != null)
            {
                Vector3 size = srcRenderer.sprite.bounds.size;
                refUnits = Mathf.Max(size.x, size.y);
            }

            string prefabPath = $"{PrefabDir}/{name}{Suffix}.prefab";
            GameObject prefab = MakePrefab(src.prefab, prefabPath, name + Suffix, idle);

            EnemyData dst = MakeData(src, path, $"{DataDir}/{name}{Suffix}.asset", prefab, idle);

            // 스킨 폴더 이름을 HW 쪽으로 옮긴다 — 원본 적은 원래 모습으로 돌아가고 HW 만 치이카와가 된다
            if (useDir == skinDir)
            {
                string err = AssetDatabase.MoveAsset(skinDir, skinDirHW);
                if (!string.IsNullOrEmpty(err)) Debug.LogWarning($"[Halloween] 스킨 폴더 이동 실패 {skinDir}: {err}");
            }

            map[src] = dst;
            units[dst] = refUnits;
            log.Add($"  {name} → {dst.name} ({dst.displayName}, 기준 크기 {refUnits:0.##})");
        }

        AssetDatabase.SaveAssets();

        // 스킨 목록을 HW 이름으로 다시 채우고 기준 크기를 박는다
        HalloweenSkinBuilder.Rebuild();
        ApplyReferenceUnits(units);

        StageData stage = MakeStage(map);
        AudioClip bgm = PrepareBgm();
        int sceneRefs = stage != null ? PointScenesTo(stage, bgm) : 0;

        AssetDatabase.SaveAssets();

        Debug.Log($"[Halloween] 할로윈 몬스터 {map.Count}종 생성, 스테이지 {(stage != null ? stage.name : "없음")}, 씬 참조 {sceneRefs}곳 교체\n" +
                  string.Join("\n", log));
    }

    // ───────────────────────── 프리팹 ─────────────────────────

    static GameObject MakePrefab(GameObject source, string prefabPath, string rootName, Sprite idle)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), prefabPath);

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            root.name = rootName;

            // 원래 애니메이터는 원래 그림을 키프레임으로 박는다. 남겨두면 치이카와와 번갈아 깜빡인다.
            // 쓰는 쪽(Enemy·DeadlockCycle·SemicolonCaster)은 모두 null 을 견딘다
            foreach (Animator a in root.GetComponentsInChildren<Animator>(true))
                Object.DestroyImmediate(a);

            // 블루스크린 단계 글자("코드 오류 / MEMORY / FAILED"). 런타임에 글자 오브젝트를 만들어 붙이는 컴포넌트다
            foreach (BlueScreenCasterLabel l in root.GetComponentsInChildren<BlueScreenCasterLabel>(true))
                Object.DestroyImmediate(l);

            // 세미콜론 조준 아이콘. SemicolonCaster 가 이름으로 찾고 없으면 건너뛴다.
            // 렌더러만 끄면 안 된다 — 조준할 때 스크립트가 다시 켠다
            Transform icon = root.transform.Find("ErrorIcon");
            if (icon != null) Object.DestroyImmediate(icon.gameObject);

            // 나머지 자식 그림(눈·아우라·이름 글자·장식)은 렌더러만 끈다. 오브젝트는 스크립트가 찾을 수 있게 남긴다
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
                if (r.gameObject != root) r.enabled = false;

            // 세미콜론 조준 글자(";" → "required"). 비우면 글자 오브젝트는 생겨도 아무것도 안 보인다
            if (root.TryGetComponent(out SemicolonCaster caster))
            {
                var so = new SerializedObject(caster);
                SetString(so, "stage2Text", string.Empty);
                SetString(so, "stage3Text", string.Empty);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // 몸통에 치이카와를 박는다. 런타임엔 HalloweenBody 가 상태별 그림으로 덮지만,
            // 씬 뷰·프리팹 미리보기에서도 무엇인지 보이게 한다
            if (idle != null && root.TryGetComponent(out SpriteRenderer body))
            {
                body.sprite = idle;
                body.color = Color.white;
            }

            return PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void SetString(SerializedObject so, string field, string value)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p != null) p.stringValue = value;
    }

    // ───────────────────────── EnemyData ─────────────────────────

    static EnemyData MakeData(EnemyData src, string srcPath, string dstPath, GameObject prefab, Sprite idle)
    {
        var dst = AssetDatabase.LoadAssetAtPath<EnemyData>(dstPath);
        string dstName = Path.GetFileNameWithoutExtension(dstPath);

        if (dst == null)
        {
            AssetDatabase.CopyAsset(srcPath, dstPath);
            dst = AssetDatabase.LoadAssetAtPath<EnemyData>(dstPath);
        }
        else
        {
            // 원본 밸런스가 바뀌었으면 따라간다. GUID 는 그대로라 스테이지 참조가 안 끊긴다.
            // 단 로그 문장은 캐릭터용으로 다시 쓴 것이라 지킨다 — 원본 문장은 세미콜론·경고 같은 버그 이야기다
            string[] kills = dst.killMessages;
            string spawn = dst.spawnMessage;

            EditorUtility.CopySerialized(src, dst);
            dst.name = dstName;

            dst.killMessages = kills;
            dst.spawnMessage = spawn;
        }

        dst.prefab = prefab;
        dst.animatorOverride = null;

        // 할로윈 시연 밸런스: 보스를 뺀 적은 체력 절반. 원본에서 다시 복사해 와도 매번 같은 값이 된다
        dst.health = src.health * (src.rank == EnemyData.Rank.Boss ? BossHealth : HalfHealth);

        // 처치 로그의 {name} 이 캐릭터 이름으로 나온다 — "Clear chiikawa"
        string character = CharacterName(idle);
        if (!string.IsNullOrEmpty(character)) dst.displayName = character;

        EditorUtility.SetDirty(dst);
        return dst;
    }

    /// <summary>"chiikawa_01_default_body" → "chiikawa". 상태 앞머리는 떼고 본다.</summary>
    static string CharacterName(Sprite sprite)
    {
        if (sprite == null) return null;

        string n = sprite.name;
        int cut = n.IndexOf('_');
        return cut > 0 ? n.Substring(0, cut) : n;
    }

    /// <summary>평소 그림 첫 장. 상태 앞머리(hit_·attack_ …)가 없는 그림 중 이름순 첫 번째.</summary>
    static Sprite IdleSprite(string dir)
    {
        string[] states = { "hit", "attack", "charge", "die", "death", "spawn", "p2_" };
        var paths = new SortedSet<string>(System.StringComparer.Ordinal);

        foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { dir }))
            paths.Add(AssetDatabase.GUIDToAssetPath(guid));

        Sprite any = null;

        foreach (string p in paths)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
            if (s == null) continue;
            if (any == null) any = s;

            bool isState = false;
            foreach (string st in states)
                if (s.name.StartsWith(st, System.StringComparison.OrdinalIgnoreCase)) { isState = true; break; }

            if (!isState) return s;
        }

        return any;
    }

    static void ApplyReferenceUnits(Dictionary<EnemyData, float> units)
    {
        var db = AssetDatabase.LoadAssetAtPath<HalloweenSkinDB>("Assets/_Project/Resources/HalloweenSkinDB.asset");
        if (db == null) return;

        foreach (var e in db.enemies)
            if (e != null && e.enemy != null && units.TryGetValue(e.enemy, out float u))
                e.referenceUnits = u;

        EditorUtility.SetDirty(db);
    }

    // ───────────────────────── 스테이지 ─────────────────────────

    static StageData MakeStage(Dictionary<EnemyData, EnemyData> map)
    {
        var src = AssetDatabase.LoadAssetAtPath<StageData>(StageSrc);
        if (src == null)
        {
            Debug.LogWarning($"[Halloween] {StageSrc} 가 없다. 스테이지는 만들지 않았다.");
            return null;
        }

        var dst = AssetDatabase.LoadAssetAtPath<StageData>(StageDst);

        if (dst == null)
        {
            AssetDatabase.CopyAsset(StageSrc, StageDst);
            dst = AssetDatabase.LoadAssetAtPath<StageData>(StageDst);
        }
        else
        {
            // 원본 웨이브 구성이 바뀌었으면 따라간다
            EditorUtility.CopySerialized(src, dst);
            dst.name = Path.GetFileNameWithoutExtension(StageDst);
        }

        // 웨이브·보스 어디에 있든 EnemyData 참조면 HW 로 바꾼다
        var so = new SerializedObject(dst);
        int swapped = SwapReferences(so, o => o is EnemyData ed && map.TryGetValue(ed, out EnemyData hw) ? hw : null);
        so.ApplyModifiedPropertiesWithoutUndo();

        // 원본을 다시 복사해 오면 배경이 원래 것으로 돌아가므로 매번 다시 물린다
        ApplyBackground(dst);

        EditorUtility.SetDirty(dst);
        Debug.Log($"[Halloween] {dst.name}: 적 참조 {swapped}곳을 HW 로 교체");

        return dst;
    }

    /// <summary>직렬화된 오브젝트 참조를 전부 훑어서 <paramref name="replace"/> 가 돌려준 것으로 바꾼다.</summary>
    static int SwapReferences(SerializedObject so, System.Func<Object, Object> replace)
    {
        int count = 0;
        SerializedProperty it = so.GetIterator();

        while (it.Next(true))
        {
            if (it.propertyType != SerializedPropertyType.ObjectReference) continue;

            Object current = it.objectReferenceValue;
            if (current == null) continue;

            Object next = replace(current);
            if (next == null || next == current) continue;

            it.objectReferenceValue = next;
            count++;
        }

        return count;
    }

    // ───────────────────────── 씬 ─────────────────────────

    /// <summary>
    /// 씬 안에서 Stage 01 을 가리키는 칸을 전부 Stage 01 Halloween 으로 바꾼다.
    /// Run 은 바로 재생할 때의 기본값(StageSetup), MainB 는 메뉴에서 고르는 스테이지(MenuManager)다.
    ///
    /// 열려 있는 씬에 저장 안 한 변경이 있으면 저장하지 않는다 — 남의 작업까지 같이 저장될 수 있다.
    /// </summary>
    static int PointScenesTo(StageData stage, AudioClip bgm)
    {
        var src = AssetDatabase.LoadAssetAtPath<StageData>(StageSrc);
        int total = 0;

        foreach (string path in Scenes)
        {
            if (!File.Exists(path)) continue;

            Scene scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            bool wasDirty = wasLoaded && scene.isDirty;

            if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            int n = 0;

            foreach (GameObject go in scene.GetRootGameObjects())
            {
                foreach (MonoBehaviour mb in go.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null) continue;

                    var so = new SerializedObject(mb);
                    int c = SwapReferences(so, o => o == src ? stage : null);

                    // 배경음 표지판이면 곡을 할로윈 곡으로 바꾼다. 메뉴(Main*)는 조용히 둔다 — 스테이지가 시작돼야 음악이 나온다
                    if (bgm != null && mb is BgmSource && !Path.GetFileName(path).StartsWith("Main"))
                    {
                        SerializedProperty clip = so.FindProperty("clip");
                        // takeOver 는 건드리지 않는다 — 씬마다 원래 설정을 따른다
                        if (clip != null && clip.objectReferenceValue != bgm) { clip.objectReferenceValue = bgm; c++; }
                    }

                    if (c == 0) continue;

                    so.ApplyModifiedPropertiesWithoutUndo();
                    n += c;
                }
            }

            if (n > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);

                if (wasDirty)
                    Debug.LogWarning($"[Halloween] {path} 에 저장 안 한 변경이 있어 저장하지 않았다. 확인하고 직접 저장할 것.");
                else
                    EditorSceneManager.SaveScene(scene);
            }

            if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);

            Debug.Log($"[Halloween] {path}: 스테이지·배경음 {n}곳 교체");
            total += n;
        }

        return total;
    }

    /// <summary>
    /// 배경음 임포트 설정. 루프 이음새에서 "틱" 소리가 나지 않게 무압축(PCM)으로 둔다 —
    /// 압축 포맷은 앞뒤에 여백이 붙어 루프가 미세하게 끊긴다. 14초짜리라 용량 부담이 없다.
    /// </summary>
    static AudioClip PrepareBgm()
    {
        if (AssetImporter.GetAtPath(BgmPath) is not AudioImporter ai)
        {
            Debug.LogWarning($"[Halloween] 배경음 {BgmPath} 가 없다. 곡은 바꾸지 않았다.");
            return null;
        }

        AudioImporterSampleSettings s = ai.defaultSampleSettings;
        bool changed = s.compressionFormat != AudioCompressionFormat.PCM || s.loadType != AudioClipLoadType.DecompressOnLoad || ai.forceToMono;

        if (changed)
        {
            s.compressionFormat = AudioCompressionFormat.PCM;
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            ai.defaultSampleSettings = s;
            ai.forceToMono = false;
            ai.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<AudioClip>(BgmPath);
    }

    /// <summary>
    /// 모든 씬의 배경음 크기를 하나로 맞춘다. 같은 곡을 씬마다 다른 크기로 물리면 넘어갈 때 볼륨이 튄다.
    /// 설정 화면의 배경음 볼륨이 여기에 한 번 더 곱해진다.
    /// </summary>
    public static void SetBgmVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);

        foreach (string path in Scenes)
        {
            if (!File.Exists(path)) continue;

            Scene scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            bool wasDirty = wasLoaded && scene.isDirty;

            if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            int n = 0;

            foreach (GameObject go in scene.GetRootGameObjects())
            {
                foreach (BgmSource bgm in go.GetComponentsInChildren<BgmSource>(true))
                {
                    var so = new SerializedObject(bgm);
                    SerializedProperty v = so.FindProperty("volume");
                    if (v == null || Mathf.Approximately(v.floatValue, volume)) continue;

                    v.floatValue = volume;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    n++;
                }
            }

            if (n > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);

                if (wasDirty)
                    Debug.LogWarning($"[Halloween] {path} 에 저장 안 한 변경이 있어 저장하지 않았다. 확인하고 직접 저장할 것.");
                else
                    EditorSceneManager.SaveScene(scene);
            }

            if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
        }

        Debug.Log($"[Halloween] 배경음 크기 {volume:0.##} 로 통일");
    }

    /// <summary>
    /// Run 씬의 어두운 시야 연출을 끈다. 원래는 전체 조명 0 + 플레이어 스포트라이트로 주변만 밝힌다.
    /// <list type="bullet">
    /// <item>전체 조명(Global Light 2D) → 1. 화면 전체가 고르게 밝아진다</item>
    /// <item>플레이어 조명 밝기 → 0. 남겨두면 전체 조명에 더해져 플레이어 주변만 하얗게 뜬다</item>
    /// <item>플레이어 조명 반경 → 크게. 발광 셰이더(SG_GlowFalloff)가 이 반경 밖을 어둡게 만들기 때문에,
    ///       화면 전부가 "반경 안" 이 되도록 넓힌다. PlayerLightController 는 이 값을 기준으로 삼는다</item>
    /// </list>
    /// 되돌리려면 git 으로 Run.unity 의 해당 두 조명만 원래 값으로 돌리면 된다(전체 0, 플레이어 1 · 반경 4~18).
    /// </summary>
    public static void BrightenRun()
    {
        const string path = "Assets/_Project/Scenes/Run.unity";
        const float WideOuter = 200f;

        Scene scene = SceneManager.GetSceneByPath(path);
        bool wasLoaded = scene.IsValid() && scene.isLoaded;
        bool wasDirty = wasLoaded && scene.isDirty;

        if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

        var log = new List<string>();

        foreach (GameObject go in scene.GetRootGameObjects())
        {
            foreach (UnityEngine.Rendering.Universal.Light2D light in go.GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>(true))
            {
                if (light.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global)
                {
                    light.intensity = 1f;
                    log.Add($"{light.name}: 전체 조명 1");
                }
                else if (light.GetComponent<PlayerLightController>() != null)
                {
                    float ratio = light.pointLightOuterRadius > 0f ? light.pointLightInnerRadius / light.pointLightOuterRadius : 0.2f;
                    light.intensity = 0f;
                    light.pointLightOuterRadius = WideOuter;
                    light.pointLightInnerRadius = WideOuter * ratio;
                    log.Add($"{light.name}: 밝기 0, 반경 {WideOuter * ratio:0}~{WideOuter:0}");
                }
                else continue;

                EditorUtility.SetDirty(light);
            }
        }

        if (log.Count > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);

            if (wasDirty)
                Debug.LogWarning($"[Halloween] {path} 에 저장 안 한 변경이 있어 저장하지 않았다. 확인하고 직접 저장할 것.");
            else
                EditorSceneManager.SaveScene(scene);
        }

        if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);

        Debug.Log("[Halloween] 시야 연출 끔 — " + string.Join(", ", log));
    }

    const string BgFolder = "Assets/_Project/Art/Background";
    const string BgTilePrefix = "HalloweenBG_tile_";
    const string BgPrefabPath = "Assets/_Project/Prefabs/UI_Prefabs/Background Halloween.prefab";

    /// <summary>그림 한 칸(원본 한 장)이 이 높이(유닛)로 보인다. 카메라가 24유닛을 보므로 화면 2배쯤 — 4K 원본이라 커도 안 뭉개진다.</summary>
    const float BgImageHeight = 54f;

    /// <summary>합친 그림에서 원본 한 장의 픽셀 높이. 4K(3840×2160) 그대로 붙였다.</summary>
    const float BgCellPixels = 2160f;

    /// <summary>
    /// 할로윈 배경 프리팹을 만들고 Stage 01 Halloween 에 물린다.
    /// <c>Art/Background/HalloweenBG_tile_NN.png</c> 를 전부 모아 고를 수 있게 넣는다(게임 중 B 키).
    /// 그림은 2×2 로 뒤집어 붙인 타일이라 Tiled 모드로 이어 깔아도 이음새가 없다(<see cref="HalloweenBackground"/>).
    /// </summary>
    public static void SetupBackground()
    {
        var sprites = new List<Sprite>();
        var paths = new SortedSet<string>(System.StringComparer.Ordinal);

        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D " + BgTilePrefix, new[] { BgFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileName(path).StartsWith(BgTilePrefix)) paths.Add(path);
        }

        foreach (string path in paths)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti) continue;

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = BgCellPixels / BgImageHeight;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.filterMode = FilterMode.Bilinear;
            // 4K 9장을 이어 붙인 큰 그림(3×3 = 11520×6480)이 줄지 않게 최대 한도로 둔다
            ti.maxTextureSize = 16384;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;

            // Tiled 모드는 FullRect 메시여야 반복된다. Tight 면 경고만 뜨고 늘어난다
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(settings);
            ti.SaveAndReimport();

            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) sprites.Add(s);
        }

        if (sprites.Count == 0)
        {
            Debug.LogWarning($"[Halloween] {BgFolder} 에 {BgTilePrefix}NN.png 가 없다.");
            return;
        }

        var go = new GameObject("Background Halloween");
        try
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprites[0];
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;

            // 3×3 칸. 카메라가 칸 경계 근처에 있어도 양쪽이 다 덮인다
            sr.size = (Vector2)sprites[0].bounds.size * 3f;

            sr.sortingLayerName = "Background";
            sr.sortingOrder = -10;

            // 세이렌 경고·보스 노래·복싱 링. 배경과 함께 할로윈 스테이지에서만 생긴다
            var show = go.AddComponent<HalloweenBossShow>();
            var showSo = new SerializedObject(show);
            showSo.FindProperty("ringMaterial").objectReferenceValue = RingMaterial();
            showSo.FindProperty("collectOnBoss").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AugmentData>("Assets/_Project/Data/Augment_Data/Item/GC.asset");
            showSo.FindProperty("originalBackground").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI_Prefabs/Background1.prefab");
            showSo.FindProperty("originalBgm").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Art/Audio/bgm/2Fresh Machine.wav");
            showSo.FindProperty("halloweenBgm").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(BgmPath);
            showSo.ApplyModifiedPropertiesWithoutUndo();

            var bg = go.AddComponent<HalloweenBackground>();
            var so = new SerializedObject(bg);
            SerializedProperty list = so.FindProperty("options");
            list.arraySize = sprites.Count;
            for (int i = 0; i < sprites.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(go, BgPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }

        var stage = AssetDatabase.LoadAssetAtPath<StageData>(StageDst);
        if (stage != null)
        {
            ApplyBackground(stage);
            EditorUtility.SetDirty(stage);
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"[Halloween] 배경 {sprites.Count}장 준비(게임 중 B 키로 넘김) — 그림 한 장 {BgImageHeight * 16f / 9f:0}×{BgImageHeight:0} 유닛, " +
                  $"{(stage != null ? stage.name : "스테이지 없음")}에 적용");
    }

    const string RingMaterialPath = "Assets/_Project/Art/Background/HalloweenRing.mat";

    /// <summary>
    /// 복싱 링용 머터리얼. 조명을 안 받아야 어두운 시야 밖에서도 빛난다.
    /// 에셋으로 만들어 두는 이유 — 런타임에 Shader.Find 로만 찾으면 빌드에서 셰이더가 빠질 수 있다.
    /// </summary>
    static Material RingMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(RingMaterialPath);
        if (mat != null) return mat;

        Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (s == null) s = Shader.Find("Sprites/Default");

        mat = new Material(s) { name = "HalloweenRing" };
        AssetDatabase.CreateAsset(mat, RingMaterialPath);
        return mat;
    }

    /// <summary>메뉴 씬(MainA·MainB)의 배경음을 끈다. 곡을 비우면 BgmSource 가 음악을 멈춘다.</summary>
    public static void SilenceMenus()
    {
        foreach (string path in new[] { "Assets/_Project/Scenes/MainA.unity", "Assets/_Project/Scenes/MainB.unity" })
        {
            EditScene(path, scene =>
            {
                int n = 0;
                foreach (GameObject go in scene.GetRootGameObjects())
                    foreach (BgmSource bgm in go.GetComponentsInChildren<BgmSource>(true))
                    {
                        var so = new SerializedObject(bgm);
                        SerializedProperty clip = so.FindProperty("clip");
                        SerializedProperty take = so.FindProperty("takeOver");
                        bool changed = false;
                        if (clip != null && clip.objectReferenceValue != null) { clip.objectReferenceValue = null; changed = true; }
                        if (take != null && !take.boolValue) { take.boolValue = true; changed = true; }
                        if (!changed) continue;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        n++;
                    }
                return n;
            });
        }

        Debug.Log("[Halloween] 메뉴 배경음 끔 — 스테이지가 시작되면 나온다");
    }

    /// <summary>결과 씬 배경음을 다시 평소 곡으로 — 엔딩곡을 없앴으니 결과 화면이 곡을 이어받을 필요가 없다.</summary>
    public static void RestoreResultBgm()
    {
        EditScene(ResultScene, scene =>
        {
            int n = 0;
            foreach (GameObject go in scene.GetRootGameObjects())
                foreach (BgmSource bgm in go.GetComponentsInChildren<BgmSource>(true))
                {
                    var so = new SerializedObject(bgm);
                    SerializedProperty take = so.FindProperty("takeOver");
                    if (take == null || take.boolValue) continue;
                    take.boolValue = true;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    n++;
                }
            return n;
        });

        Debug.Log("[Halloween] 결과 씬 배경음을 평소 곡으로 되돌림");
    }

    const string NeonIconDir = "Assets/_Project/Art/Augment/Neon";

    /// <summary>
    /// Tools/neon_icons.py 로 만든 네온 아이콘을 들여와 모든 증강에 물린다.
    /// 파일 이름은 Neon_&lt;증강 id&gt;.png (콜론은 밑줄). 맞는 파일이 없는 증강은 원래 아이콘을 둔다.
    /// </summary>
    public static void AssignNeonIcons()
    {
        if (!AssetDatabase.IsValidFolder(NeonIconDir))
        {
            Debug.LogWarning($"[Halloween] {NeonIconDir} 가 없다. 먼저 Tools/neon_icons.py 를 돌릴 것.");
            return;
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { NeonIconDir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti) continue;

            bool dirty = ti.textureType != TextureImporterType.Sprite || ti.mipmapEnabled || !ti.alphaIsTransparency;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.maxTextureSize = 512;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            if (dirty) ti.SaveAndReimport();
        }

        int set = 0;
        var missing = new List<string>();

        foreach (string guid in AssetDatabase.FindAssets("t:AugmentData", new[] { "Assets/_Project/Data/Augment_Data" }))
        {
            var data = AssetDatabase.LoadAssetAtPath<AugmentData>(AssetDatabase.GUIDToAssetPath(guid));
            string key = AugmentPalette.IconKey(data);
            if (key == null) continue;

            var icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{NeonIconDir}/Neon_{key}.png");
            if (icon == null) { missing.Add(data.name); continue; }
            if (data.icon == icon) continue;

            data.icon = icon;
            EditorUtility.SetDirty(data);
            set++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Halloween] 네온 아이콘 {set}개 교체. 아이콘 파일 없는 증강: {string.Join(", ", missing)}");
    }

    static void ApplyBackground(StageData stage)
    {
        var bg = AssetDatabase.LoadAssetAtPath<GameObject>(BgPrefabPath);
        if (bg != null) stage.backgroundPrefab = bg;
    }

    const string EndingPath = "Assets/_Project/Art/Audio/bgm/10Halloween_ending_shima_no_uta.wav";
    const string ResultScene = "Assets/_Project/Scenes/StageResult.unity";

    /// <summary>
    /// 엔딩곡을 준비한다.
    /// <list type="bullet">
    /// <item>임포트: 1분 넘는 곡이라 스트리밍으로 읽는다(메모리에 통째로 안 올린다). 스테레오 유지</item>
    /// <item>스킨 목록에 보스곡으로 물린다 — 세이렌 등장 순간 HalloweenSkin.PlayBossBgm 이 튼다</item>
    /// </list>
    /// </summary>
    public static void SetupEnding()
    {
        if (AssetImporter.GetAtPath(EndingPath) is not AudioImporter ai)
        {
            Debug.LogWarning($"[Halloween] 엔딩곡 {EndingPath} 가 없다.");
            return;
        }

        AudioImporterSampleSettings s = ai.defaultSampleSettings;
        if (s.loadType != AudioClipLoadType.Streaming || ai.forceToMono)
        {
            s.loadType = AudioClipLoadType.Streaming;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.8f;
            ai.defaultSampleSettings = s;
            ai.forceToMono = false;
            ai.SaveAndReimport();
        }

        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(EndingPath);
        var db = AssetDatabase.LoadAssetAtPath<HalloweenSkinDB>("Assets/_Project/Resources/HalloweenSkinDB.asset");
        if (db != null)
        {
            db.bossBgm = clip;
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"[Halloween] 보스곡 준비 — {(clip != null ? clip.name : "없음")} ({(clip != null ? clip.length : 0):0.#}초)");
    }

    /// <summary>Run 씬 조명을 원래 값으로 되돌린다 — 전체 0, 플레이어 1 · 반경 4~18. <see cref="BrightenRun"/> 의 반대.</summary>
    public static void RestoreRunLighting()
    {
        var log = new List<string>();

        EditScene("Assets/_Project/Scenes/Run.unity", scene =>
        {
            foreach (GameObject go in scene.GetRootGameObjects())
                foreach (var light in go.GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>(true))
                {
                    if (light.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global)
                    {
                        light.intensity = 0f;
                        log.Add($"{light.name}: 전체 조명 0");
                    }
                    else if (light.GetComponent<PlayerLightController>() != null)
                    {
                        light.intensity = 1f;
                        light.pointLightOuterRadius = 18f;
                        light.pointLightInnerRadius = 4f;
                        log.Add($"{light.name}: 밝기 1, 반경 4~18");
                    }
                    else continue;

                    EditorUtility.SetDirty(light);
                }
            return log.Count;
        });

        Debug.Log("[Halloween] 조명 원래대로 — " + string.Join(", ", log));
    }

    /// <summary>
    /// Run 씬 플레이어 조명(Spot Light 2D)의 반경을 정한다. PlayerLightController 가 이 값을 기준으로
    /// 레벨 성장·호흡을 곱하므로, 여기서 바꾸면 그 연출도 새 반경을 기준으로 돈다.
    /// </summary>
    public static void SetSpotRadius(float inner, float outer)
    {
        outer = Mathf.Max(outer, inner);
        var log = new List<string>();

        EditScene("Assets/_Project/Scenes/Run.unity", scene =>
        {
            foreach (GameObject go in scene.GetRootGameObjects())
                foreach (var light in go.GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>(true))
                {
                    if (light.GetComponent<PlayerLightController>() == null) continue;

                    light.pointLightOuterRadius = outer;
                    light.pointLightInnerRadius = inner;
                    EditorUtility.SetDirty(light);
                    log.Add($"{light.name}: 반경 {inner:0.##}~{outer:0.##}");
                }
            return log.Count;
        });

        Debug.Log("[Halloween] 플레이어 조명 — " + (log.Count > 0 ? string.Join(", ", log) : "못 찾음"));
    }

    /// <summary>씬을 (안 열려 있으면 잠깐 열어) 고치고 저장한다. 저장 안 한 변경이 있던 씬은 저장하지 않는다.</summary>
    static void EditScene(string path, System.Func<Scene, int> edit)
    {
        Scene scene = SceneManager.GetSceneByPath(path);
        bool wasLoaded = scene.IsValid() && scene.isLoaded;
        bool wasDirty = wasLoaded && scene.isDirty;

        if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

        if (edit(scene) > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);

            if (wasDirty)
                Debug.LogWarning($"[Halloween] {path} 에 저장 안 한 변경이 있어 저장하지 않았다. 확인하고 직접 저장할 것.");
            else
                EditorSceneManager.SaveScene(scene);
        }

        if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}

/// <summary>
/// 에디터 밖에서 <see cref="HalloweenPrefabMaker.Make"/> 를 돌리는 창구.
/// 프로젝트의 <c>Temp/hw_make.request</c> 파일이 있으면 컴파일·리로드 직후 한 번 돌리고 지운다.
/// </summary>
[InitializeOnLoad]
static class HalloweenPrefabMakerRequest
{
    const string RequestPath = "Temp/hw_make.request";

    /// <summary>내용에 숫자 하나(0~1). 모든 씬의 배경음 크기를 그 값으로 맞춘다.</summary>
    const string BgmVolumePath = "Temp/hw_bgmvol.request";

    /// <summary>있으면 Run 씬의 어두운 시야 연출을 끈다.</summary>
    const string BrightPath = "Temp/hw_bright.request";

    /// <summary>있으면 조명을 원래대로 되돌리고 엔딩곡을 준비한다.</summary>
    const string EndingRequestPath = "Temp/hw_ending.request";

    /// <summary>있으면 할로윈 배경을 만들어 스테이지에 물린다.</summary>
    const string BgRequestPath = "Temp/hw_bg.request";

    /// <summary>있으면 결과 씬 배경음을 되돌리고 보스 연출이 붙은 배경을 다시 만든다.</summary>
    const string BossRequestPath = "Temp/hw_boss.request";

    /// <summary>내용에 "안쪽 바깥" 두 숫자. 플레이어 조명 반경을 그 값으로 바꾼다.</summary>
    const string SpotRequestPath = "Temp/hw_spot.request";

    /// <summary>있으면 메뉴 배경음을 끈다.</summary>
    const string MenuRequestPath = "Temp/hw_menu.request";

    /// <summary>있으면 네온 아이콘을 들여와 증강에 물린다.</summary>
    const string IconRequestPath = "Temp/hw_icons.request";

    /// <summary>요청 파일을 확인하는 간격(초). 컴파일 직후뿐 아니라 평소에도 본다 — 코드 변경 없이 요청만 와도 처리되게.</summary>
    const double PollEvery = 2.0;
    static double nextPoll;

    static HalloweenPrefabMakerRequest()
    {
        EditorApplication.delayCall += Check;
        EditorApplication.update += Poll;
    }

    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + PollEvery;
        Check();
    }

    static void Check()
    {
        if (!File.Exists(RequestPath) && !File.Exists(BgmVolumePath) && !File.Exists(BrightPath) && !File.Exists(EndingRequestPath) && !File.Exists(BgRequestPath) && !File.Exists(BossRequestPath) && !File.Exists(SpotRequestPath) && !File.Exists(MenuRequestPath) && !File.Exists(IconRequestPath)) return;

        // 플레이 중이면 끝날 때까지 미룬다
        // 플레이·컴파일 중이면 미룬다. Poll 이 계속 다시 본다
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        if (File.Exists(RequestPath))
        {
            File.Delete(RequestPath);
            HalloweenPrefabMaker.Make();
        }

        if (File.Exists(IconRequestPath))
        {
            File.Delete(IconRequestPath);
            HalloweenPrefabMaker.AssignNeonIcons();
        }

        if (File.Exists(MenuRequestPath))
        {
            File.Delete(MenuRequestPath);
            HalloweenPrefabMaker.SilenceMenus();

            // 2페이즈 그림(p2_) 같은 새 규칙이 들어왔을 수 있어 스킨 목록도 다시 읽는다
            HalloweenSkinBuilder.Rebuild();
        }

        if (File.Exists(SpotRequestPath))
        {
            string[] parts = File.ReadAllText(SpotRequestPath).Split(new[] { ' ', '\n', '\r', ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            File.Delete(SpotRequestPath);

            var inv = System.Globalization.CultureInfo.InvariantCulture;
            if (parts.Length >= 2 && float.TryParse(parts[0], System.Globalization.NumberStyles.Float, inv, out float inner)
                                  && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, inv, out float outer))
                HalloweenPrefabMaker.SetSpotRadius(inner, outer);
        }

        if (File.Exists(BossRequestPath))
        {
            File.Delete(BossRequestPath);
            HalloweenPrefabMaker.RestoreResultBgm();
            HalloweenPrefabMaker.SetupBackground();
        }

        if (File.Exists(BgRequestPath))
        {
            File.Delete(BgRequestPath);
            HalloweenPrefabMaker.SetupBackground();
        }

        if (File.Exists(EndingRequestPath))
        {
            File.Delete(EndingRequestPath);
            HalloweenPrefabMaker.RestoreRunLighting();
            HalloweenPrefabMaker.SetupEnding();
        }

        if (File.Exists(BrightPath))
        {
            File.Delete(BrightPath);
            HalloweenPrefabMaker.BrightenRun();
        }

        if (File.Exists(BgmVolumePath))
        {
            string text = File.ReadAllText(BgmVolumePath).Trim();
            File.Delete(BgmVolumePath);

            if (float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v))
                HalloweenPrefabMaker.SetBgmVolume(v);
        }
    }
}
