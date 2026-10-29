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
            // 원본 밸런스가 바뀌었으면 따라간다. GUID 는 그대로라 스테이지 참조가 안 끊긴다
            EditorUtility.CopySerialized(src, dst);
            dst.name = dstName;
        }

        dst.prefab = prefab;
        dst.animatorOverride = null;

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
        string[] states = { "hit", "attack", "charge", "die", "death", "spawn" };
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

                    // 배경음 표지판이면 곡을 할로윈 곡으로 바꾼다
                    if (bgm != null && mb is BgmSource)
                    {
                        SerializedProperty clip = so.FindProperty("clip");
                        SerializedProperty take = so.FindProperty("takeOver");
                        if (clip != null && clip.objectReferenceValue != bgm) { clip.objectReferenceValue = bgm; c++; }
                        if (take != null && !take.boolValue) { take.boolValue = true; c++; }
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

    static HalloweenPrefabMakerRequest()
    {
        EditorApplication.delayCall += Check;
    }

    static void Check()
    {
        if (!File.Exists(RequestPath) && !File.Exists(BgmVolumePath)) return;

        // 플레이 중이면 끝날 때까지 미룬다
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Check;
            return;
        }

        if (File.Exists(RequestPath))
        {
            File.Delete(RequestPath);
            HalloweenPrefabMaker.Make();
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
