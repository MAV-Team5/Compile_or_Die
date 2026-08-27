using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 기획서의 부품 9종이 채워진 <see cref="HardwareTable"/> 을 한 번에 만든다.
/// 메뉴: <c>CoD → 하드웨어 표 만들기</c>
///
/// <b>여기 적힌 값은 초안이다.</b> 만든 뒤에는 에셋에서 고치면 되고, 이 파일은
/// 표를 처음 만들 때 빈 칸 아홉 줄을 손으로 채우지 않으려는 용도다.
/// </summary>
public static class HardwareTableCreator
{
    const string Folder = "Assets/_Project/Data";
    const string Path = Folder + "/HardwareTable.asset";

    [MenuItem("CoD/하드웨어 표 만들기", priority = 110)]
    static void Create()
    {
        var existing = AssetDatabase.LoadAssetAtPath<HardwareTable>(Path);

        if (existing != null &&
            !EditorUtility.DisplayDialog("하드웨어 표 만들기",
                $"{Path} 가 이미 있습니다.\n기본값으로 덮어쓸까요?\n\n"
              + "지금 표에 손으로 넣은 값은 사라집니다.", "덮어쓰기", "취소"))
            return;

        HardwareTable table = existing != null ? existing : ScriptableObject.CreateInstance<HardwareTable>();

        table.EditorFill(Build());

        if (existing == null)
        {
            EnsureFolder();
            AssetDatabase.CreateAsset(table, Path);
        }

        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();

        Selection.activeObject = table;
        EditorGUIUtility.PingObject(table);

        Debug.Log($"[하드웨어 표] {Path} 준비 완료. "
                + "HardwareLoader 와 UpgradeShop 에 물릴 것.", table);
    }

    // ── 초안 값 ───────────────────────────────────────────

    static List<HardwareTable.Entry> Build() => new()
    {
        Part(HardwareKind.Cpu, "CPU", "연산이 빨라져 증강이 더 자주 터진다.",
             cost: 50, growth: 1.5f,
             Stat(StatKind.Cooldown, 0.04f)),

        Part(HardwareKind.Ram, "RAM", "한 번에 더 많이 담아 경험치를 더 얻는다.",
             cost: 60, growth: 1.5f,
             Ratio(HardwareTarget.Exp, 0.05f)),

        // ⚠ CPU 와 같은 수치를 올린다. 이 게임에는 '공격속도'와 '쿨타임'이 나뉘어 있지 않다.
        //   SSD 를 다른 역할로 바꿀지는 기획에서 정할 것
        Part(HardwareKind.Ssd, "SSD", "읽기가 빨라져 재사용 대기가 짧아진다.",
             cost: 55, growth: 1.5f,
             Stat(StatKind.Cooldown, 0.03f)),

        Part(HardwareKind.Gpu, "GPU", "더 넓게 그린다. 사거리와 효과 범위가 함께 늘어난다.",
             cost: 70, growth: 1.55f,
             Stat(StatKind.Range, 0.04f),
             Stat(StatKind.EffectRange, 0.04f)),

        Part(HardwareKind.Power, "파워", "공급이 늘어 모든 증강의 피해가 오른다.",
             cost: 80, growth: 1.6f,
             Stat(StatKind.Damage, 0.05f)),

        Part(HardwareKind.Monitor, "모니터", "더 멀리 본다. 적을 먼저 찾는다.",
             cost: 45, growth: 1.45f,
             Ratio(HardwareTarget.Vision, 0.06f)),

        // 잠금 — 크리티컬 판정기가 아직 없다. 만들면 Max Level 을 올릴 것
        Locked(HardwareKind.Mouse, "마우스", "정확도가 올라 치명타가 자주 난다. (준비 중)",
               Ratio(HardwareTarget.Critical, 0.02f)),

        Part(HardwareKind.Keyboard, "키보드", "입력이 빨라져 더 빨리 움직인다.",
             cost: 50, growth: 1.5f,
             Ratio(HardwareTarget.MoveSpeed, 0.03f)),

        // 잠금 — 스타트 증강은 캐릭터 시스템과 함께 온다
        Locked(HardwareKind.Mainboard, "메인보드", "슬롯이 늘어 시작 증강을 더 받는다. (준비 중)",
               Add(HardwareTarget.StartingAugments, 1f))
    };

    // ── 조립 도우미 ───────────────────────────────────────

    static HardwareTable.Entry Part(HardwareKind kind, string name, string description,
                                    int cost, float growth, params HardwareEffect[] effects)
        => new()
        {
            kind = kind,
            displayName = name,
            description = description,
            maxLevel = 10,
            baseCost = cost,
            costGrowth = growth,
            effects = new List<HardwareEffect>(effects)
        };

    /// <summary>최대 레벨 0 — 상점에 LOCKED 로 뜨고 살 수 없다.</summary>
    static HardwareTable.Entry Locked(HardwareKind kind, string name, string description,
                                      params HardwareEffect[] effects)
    {
        HardwareTable.Entry entry = Part(kind, name, description, 0, 1f, effects);

        entry.maxLevel = 0;

        return entry;
    }

    static HardwareEffect Stat(StatKind kind, float perLevel) => new()
    {
        target = HardwareTarget.Stat,
        statKind = kind,
        mode = HardwareMode.Percent,
        perLevel = perLevel
    };

    static HardwareEffect Ratio(HardwareTarget target, float perLevel) => new()
    {
        target = target,
        mode = HardwareMode.Percent,
        perLevel = perLevel
    };

    static HardwareEffect Add(HardwareTarget target, float perLevel) => new()
    {
        target = target,
        mode = HardwareMode.Add,
        perLevel = perLevel
    };

    static void EnsureFolder()
    {
        if (AssetDatabase.IsValidFolder(Folder)) return;

        AssetDatabase.CreateFolder("Assets/_Project", "Data");
    }
}
