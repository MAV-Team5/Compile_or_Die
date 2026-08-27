using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 구글 시트의 <c>1_설계</c> 탭을 읽어 증강의 레벨 표를 채운다.
/// 메뉴: <c>CoD → 증강 시트 가져오기</c>
///
/// <b>시트의 2_Augment_Levels 탭은 안 본다.</b> 곡선 계산을 여기서 직접 하므로
/// 그 탭은 눈으로 확인하는 용도로만 남는다 — 수식이 깨져도 게임에는 영향이 없다.
///
/// <b>성장은 곱셈이 아니라 덧셈이다.</b>
/// <code>
/// 실수 스탯   Lv(n) = 1레벨값 + 증가량 × (n-1)
/// 정수 스탯   Lv(n) = 1레벨값 + 내림(증가량 × (n-1))   ← 0.5 면 2레벨마다 +1
/// </code>
/// 곱셈은 후반에 폭주해서 8레벨을 손으로 못 잡는다. 덧셈이면 "몇 번 더해졌나"가 암산된다.
/// </summary>
public static class AugmentSheetImporter
{
    /// <summary>채울 레벨 수. 시트와 맞춰둘 것.</summary>
    const int MaxLevel = 8;

    /// <summary>시트 열 이름 → AugmentLevelData 필드. 정수 스탯은 계단으로 오른다.</summary>
    static readonly (string Sheet, string Field, bool Whole)[] Stats =
    {
        ("피해량",   "damage",       false),
        ("효과피해", "effectDamage", false),
        ("쿨타임",   "cooldown",     false),
        ("사거리",   "range",        false),
        ("효과범위", "effectRange",  false),
        ("속도",     "speed",        false),
        ("지속시간", "duration",     false),
        ("수량",     "count",        true),
        ("관통력",   "pierce",       true),
        ("깊이",     "depth",        true)
    };

    [MenuItem("CoD/증강 시트 가져오기", priority = 100)]
    static void Import()
    {
        string path = EditorUtility.OpenFilePanel("1_설계 탭을 CSV 로 내려받아 고르세요",
                                                  "", "csv");
        if (string.IsNullOrEmpty(path)) return;

        string[] lines = File.ReadAllLines(path, Encoding.UTF8);

        var table = new List<string[]>();
        for (int i = 0; i < lines.Length; i++) table.Add(SplitCsv(lines[i]));

        int header = FindHeader(table);

        if (header < 0)
        {
            EditorUtility.DisplayDialog("가져오기 실패",
                "'id' 열을 못 찾았습니다.\n1_설계 탭을 CSV 로 내려받았는지 확인하세요.", "확인");
            return;
        }

        // ★ 스탯 열이 하나도 없으면 여기서 멈춘다.
        //   예전에는 그대로 진행해 모든 수치를 0으로 덮어썼다 —
        //   엉뚱한 탭(2_Augment_Levels 등)에도 'id' 열은 있기 때문에
        //   경고 한 줄 없이 증강이 통째로 망가졌다
        List<string> found = FoundStats(table[header]);

        if (found.Count == 0)
        {
            EditorUtility.DisplayDialog("가져오기 실패",
                "스탯 열을 하나도 못 찾았습니다. 아무것도 바꾸지 않았습니다.\n\n" +
                "'피해량 1레벨' 같은 열이 있는 1_설계 탭인지 확인하세요.\n" +
                "2_Augment_Levels 탭에는 이 열들이 없습니다.\n\n" +
                "읽은 헤더: " + string.Join(", ", table[header]), "확인");
            return;
        }

        if (!EditorUtility.DisplayDialog("증강 시트 가져오기",
                $"찾은 스탯 열 {found.Count}개\n  {string.Join(", ", found)}\n\n" +
                "이 열들로 덮어씁니다. 진행할까요?", "가져오기", "취소"))
            return;

        Apply(table, header);
    }

    /// <summary>헤더에 실제로 있는 스탯 이름. 하나도 없으면 시트를 잘못 고른 것이다.</summary>
    static List<string> FoundStats(string[] header)
    {
        var found = new List<string>();

        for (int i = 0; i < Stats.Length; i++)
            if (IndexOf(header, $"{Stats[i].Sheet} 1레벨") >= 0) found.Add(Stats[i].Sheet);

        return found;
    }

    // ── 본작업 ────────────────────────────────────────────

    static void Apply(List<string[]> table, int header)
    {
        Dictionary<string, AugmentData> byId = CollectAssets(out List<string> duplicates);

        var filled = new List<string>();
        var locked = new List<string>();
        var missing = new List<string>();
        var empty = new List<string>();
        var thin = new List<string>();

        int idColumn = IndexOf(table[header], "id");

        for (int r = header + 1; r < table.Count; r++)
        {
            string[] row = table[r];
            if (idColumn >= row.Length) continue;

            string id = row[idColumn].Trim();
            if (id.Length == 0) continue;

            if (!byId.TryGetValue(id, out AugmentData data))
            {
                // 시트에만 있고 아직 에셋을 안 만든 증강. 흔한 일이라 조용히 모아만 둔다
                missing.Add(id);
                continue;
            }

            // 인스펙터에서 잠근 증강은 손대지 않는다. 손으로 맞춰둔 값을 지키려는 자물쇠다
            if (data.lockStats)
            {
                locked.Add(id);
                continue;
            }

            AugmentLevelData[] levels = BuildLevels(table[header], row);

            // 이 줄이 통째로 비어 있으면 아직 안 채운 증강이다.
            // 0으로 덮어쓰면 손으로 넣어둔 값까지 날아간다
            if (IsAllZero(levels[0]))
            {
                empty.Add(id);
                continue;
            }

            Undo.RecordObject(data, "증강 시트 가져오기");

            data.levelStats = levels;

            EditorUtility.SetDirty(data);
            filled.Add(id);

            if (LooksUnusable(data.levelStats[0])) thin.Add(id);
        }

        AssetDatabase.SaveAssets();

        Report(filled, locked, missing, empty, thin, duplicates);
    }

    static AugmentLevelData[] BuildLevels(string[] header, string[] row)
    {
        var levels = new AugmentLevelData[MaxLevel];

        for (int i = 0; i < Stats.Length; i++)
        {
            (string sheet, string field, bool whole) = Stats[i];

            float baseValue = Read(header, row, $"{sheet} 1레벨");
            float step = ReadStep(header, row, sheet);

            for (int lv = 0; lv < MaxLevel; lv++)
            {
                float v = baseValue + step * lv;

                // 정수 스탯은 내림한다. 0.5 면 2레벨마다 한 칸 오른다
                if (whole) v = Mathf.Floor(baseValue + step * lv);

                Assign(ref levels[lv], field, Mathf.Max(0f, v), whole);
            }
        }

        return levels;
    }

    /// <summary>
    /// 구조체라 리플렉션 없이 직접 꽂는다.
    /// 필드가 열 개뿐이고, 여기가 시트와 코드를 잇는 유일한 자리라 눈에 보이는 편이 낫다.
    /// </summary>
    static void Assign(ref AugmentLevelData level, string field, float value, bool whole)
    {
        int n = Mathf.RoundToInt(value);

        switch (field)
        {
            case "damage": level.damage = value; break;
            case "effectDamage": level.effectDamage = value; break;
            case "cooldown": level.cooldown = value; break;
            case "range": level.range = value; break;
            case "effectRange": level.effectRange = value; break;
            case "speed": level.speed = value; break;
            case "duration": level.duration = value; break;

            case "count": level.count = n; break;
            case "pierce": level.pierce = n; break;
            case "depth": level.depth = n; break;
        }
    }

    // ── 시트 읽기 ─────────────────────────────────────────

    /// <summary>
    /// 증가량 열. 이름이 여러 번 바뀌었으므로 후보를 다 본다 —
    /// 시트 열 이름 하나 때문에 가져오기가 통째로 실패하면 원인을 찾기 어렵다.
    /// </summary>
    static float ReadStep(string[] header, string[] row, string sheet)
    {
        string[] names = { $"{sheet} 레벨당 증가", $"{sheet} 증가", $"{sheet} 성장", $"{sheet} 계단" };

        for (int i = 0; i < names.Length; i++)
        {
            int c = IndexOf(header, names[i]);
            if (c >= 0) return Number(row, c);
        }

        return 0f;
    }

    static float Read(string[] header, string[] row, string name)
    {
        int c = IndexOf(header, name);
        return c < 0 ? 0f : Number(row, c);
    }

    static float Number(string[] row, int column)
    {
        if (column >= row.Length) return 0f;

        string cell = row[column].Trim();

        // 빈칸은 "이 증강은 이 항목을 안 씀"이다. 0 과 같게 다뤄도 결과는 같다
        return float.TryParse(cell, NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
            ? v : 0f;
    }

    static int IndexOf(string[] header, string name)
    {
        for (int i = 0; i < header.Length; i++)
            if (header[i].Trim() == name) return i;

        return -1;
    }

    /// <summary>'id' 가 있는 줄이 헤더다. 위에 안내문이 몇 줄이든 상관없게 한다.</summary>
    static int FindHeader(List<string[]> table)
    {
        for (int r = 0; r < table.Count; r++)
            if (IndexOf(table[r], "id") >= 0) return r;

        return -1;
    }

    /// <summary>따옴표 안의 쉼표를 지킨다. 설명 칸에 쉼표가 들어가는 일이 흔하다.</summary>
    static string[] SplitCsv(string line)
    {
        var cells = new List<string>();
        var cell = new StringBuilder();
        bool quoted = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                // 따옴표 두 개는 따옴표 한 글자를 뜻한다
                if (quoted && i + 1 < line.Length && line[i + 1] == '"') { cell.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (c == ',' && !quoted) { cells.Add(cell.ToString()); cell.Clear(); }
            else cell.Append(c);
        }

        cells.Add(cell.ToString());

        return cells.ToArray();
    }

    // ── 에셋 찾기 ─────────────────────────────────────────

    static Dictionary<string, AugmentData> CollectAssets(out List<string> duplicates)
    {
        var byId = new Dictionary<string, AugmentData>();
        duplicates = new List<string>();

        string[] guids = AssetDatabase.FindAssets("t:AugmentData");

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var data = AssetDatabase.LoadAssetAtPath<AugmentData>(path);

            if (data == null || string.IsNullOrEmpty(data.id)) continue;

            // 같은 id 가 둘이면 어느 쪽에 넣을지 코드가 정할 수 없다. 둘 다 건드리지 않는다
            if (byId.ContainsKey(data.id)) duplicates.Add(data.id);
            else byId[data.id] = data;
        }

        return byId;
    }

    /// <summary>시트에 아무 값도 안 적힌 줄인가. 이런 줄로 에셋을 덮으면 안 된다.</summary>
    static bool IsAllZero(AugmentLevelData s)
        => s.damage == 0f && s.effectDamage == 0f && s.cooldown == 0f && s.range == 0f
        && s.effectRange == 0f && s.speed == 0f && s.duration == 0f
        && s.count == 0 && s.pierce == 0 && s.depth == 0;

    /// <summary>흔한 빈칸 사고. 이 값이 0이면 조립에 따라 아예 발동이 안 된다.</summary>
    static bool LooksUnusable(AugmentLevelData first)
        => Mathf.Approximately(first.speed, 0f) || Mathf.Approximately(first.effectRange, 0f);

    // ── 결과 ──────────────────────────────────────────────

    static void Report(List<string> filled, List<string> locked, List<string> missing,
                       List<string> empty, List<string> thin, List<string> duplicates)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"채움 {filled.Count}개");
        if (filled.Count > 0) sb.AppendLine("  " + string.Join(", ", filled));

        if (locked.Count > 0)
            sb.AppendLine($"\n잠겨서 건너뜀 {locked.Count}개\n  " + string.Join(", ", locked));

        if (empty.Count > 0)
            sb.AppendLine($"\n시트가 비어 건너뜀 {empty.Count}개\n  " + string.Join(", ", empty));

        if (missing.Count > 0)
            sb.AppendLine($"\n에셋이 없는 id {missing.Count}개\n  " + string.Join(", ", missing));

        if (duplicates.Count > 0)
            sb.AppendLine($"\n⚠ id 가 겹쳐 건드리지 않음\n  " + string.Join(", ", duplicates));

        if (thin.Count > 0)
            sb.AppendLine($"\n⚠ 속도나 효과범위가 0 — 조립에 따라 발동이 안 됩니다\n  "
                        + string.Join(", ", thin));

        Debug.Log("[증강 시트] " + sb);
        EditorUtility.DisplayDialog("증강 시트 가져오기", sb.ToString(), "확인");
    }
}
