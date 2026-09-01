using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 증강 레벨 표를 시트에서 통째로 가져온다.
/// 메뉴: <c>CoD → 증강 시트 가져오기</c>
///
/// <b>계산을 하지 않는다.</b> 시트에 적힌 값이 그대로 들어간다 —
/// 예전 임포터는 1레벨값과 증가량으로 곡선을 만들어서 선형 성장밖에 못 했다.
/// 3레벨마다 비약을 주는 곡선은 수식으로 표현할 수 없고, 표현하려 들면
/// 기획자가 원하는 모양과 코드가 만드는 모양이 어긋난다.
///
/// <b>시트 모양 — 한 행이 증강 하나의 한 레벨이다.</b>
/// <code>
/// id     레벨  피해량  효과피해  쿨타임  사거리  효과범위  속도  지속시간  수량  관통력  깊이
/// BASH    1     3      0.15     4.0     6      0        9     0        0     0      1
/// BASH    2     4      0.17     3.8     6      0        9     0        0     0      1
/// BASH    3    12      0.30     3.0     8      0        9     0        0     0      2
/// DFS     1    ...
/// </code>
///
/// 빈 칸은 0이다. 밸런싱 곡선은 시트 안에서 따로 만들고 결과 값만 이 표에 둘 것.
/// </summary>
public static class AugmentSheetImporter
{
    const string IdColumn = "id";
    const string LevelColumn = "레벨";

    /// <summary>시트 열 이름 → AugmentLevelData 필드. 정수 칸은 반올림해서 넣는다.</summary>
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
        string path = EditorUtility.OpenFilePanel("증강 레벨 표를 CSV 로 내려받아 고르세요", "", "csv");
        if (string.IsNullOrEmpty(path)) return;

        List<string[]> table = ReadCsv(path);

        int header = FindHeader(table);

        if (header < 0)
        {
            Fail($"'{IdColumn}' 과 '{LevelColumn}' 이 같이 있는 줄을 못 찾았습니다.\n\n"
               + "한 행이 증강 하나의 한 레벨인 표여야 합니다.\n"
               + $"예) {IdColumn} · {LevelColumn} · 피해량 · 쿨타임 ...");
            return;
        }

        List<string> found = FoundStats(table[header]);

        // 스탯 열이 하나도 없으면 여기서 멈춘다.
        // 예전에 이 검사가 없어서, 엉뚱한 탭을 골랐는데 경고 한 줄 없이 모든 증강이 0이 됐다
        if (found.Count == 0)
        {
            Fail("스탯 열을 하나도 못 찾았습니다. 아무것도 바꾸지 않았습니다.\n\n"
               + "읽은 헤더: " + string.Join(", ", table[header]));
            return;
        }

        Dictionary<string, List<Row>> rows = Collect(table, header, out int skipped);

        if (rows.Count == 0)
        {
            Fail("헤더는 찾았는데 값이 있는 행이 하나도 없습니다.");
            return;
        }

        if (!EditorUtility.DisplayDialog("증강 시트 가져오기",
                $"증강 {rows.Count}개 · 레벨 행 {Total(rows)}줄\n"
              + $"스탯 열 {found.Count}개 — {string.Join(", ", found)}\n\n"
              + "이 값으로 덮어씁니다. 진행할까요?", "가져오기", "취소"))
            return;

        Apply(rows, skipped);
    }

    // ── 읽기 ──────────────────────────────────────────────

    /// <summary>시트 한 줄. 레벨 번호와 그 레벨의 수치.</summary>
    struct Row
    {
        public int Level;
        public AugmentLevelData Stats;
    }

    /// <summary>
    /// id 별로 행을 모은다. 레벨 순서는 시트에서 뒤죽박죽이어도 뒤에서 정렬한다.
    /// </summary>
    static Dictionary<string, List<Row>> Collect(List<string[]> table, int header, out int skipped)
    {
        var byId = new Dictionary<string, List<Row>>();

        string[] head = table[header];
        int idAt = IndexOf(head, IdColumn);
        int levelAt = IndexOf(head, LevelColumn);

        skipped = 0;

        for (int r = header + 1; r < table.Count; r++)
        {
            string[] row = table[r];

            if (idAt >= row.Length || levelAt >= row.Length) continue;

            string id = row[idAt].Trim();
            if (id.Length == 0) continue;

            int level = Mathf.RoundToInt(Number(row, levelAt));

            // 레벨이 없거나 0 이하인 줄은 표가 아니라 안내문일 가능성이 높다
            if (level <= 0) { skipped++; continue; }

            if (!byId.TryGetValue(id, out List<Row> list))
            {
                list = new List<Row>();
                byId[id] = list;
            }

            list.Add(new Row { Level = level, Stats = ReadStats(head, row) });
        }

        return byId;
    }

    static AugmentLevelData ReadStats(string[] head, string[] row)
    {
        var s = new AugmentLevelData();

        for (int i = 0; i < Stats.Length; i++)
        {
            (string sheet, string field, bool whole) = Stats[i];

            int c = IndexOf(head, sheet);
            if (c < 0) continue;

            Assign(ref s, field, Mathf.Max(0f, Number(row, c)), whole);
        }

        return s;
    }

    /// <summary>
    /// 구조체라 리플렉션 없이 직접 꽂는다.
    /// 칸이 열 개뿐이고, 여기가 시트와 코드를 잇는 유일한 자리라 눈에 보이는 편이 낫다.
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

    // ── 쓰기 ──────────────────────────────────────────────

    static void Apply(Dictionary<string, List<Row>> rows, int skippedRows)
    {
        Dictionary<string, AugmentData> byId = CollectAssets(out List<string> duplicates);

        var filled = new List<string>();
        var locked = new List<string>();
        var missing = new List<string>();
        var gaps = new List<string>();

        foreach (KeyValuePair<string, List<Row>> pair in rows)
        {
            if (!byId.TryGetValue(pair.Key, out AugmentData data)) { missing.Add(pair.Key); continue; }

            // 인스펙터에서 잠근 증강은 손대지 않는다. 손으로 맞춰둔 값을 지키려는 자물쇠다
            if (data.lockStats) { locked.Add(pair.Key); continue; }

            List<Row> list = pair.Value;
            list.Sort((a, b) => a.Level.CompareTo(b.Level));

            // 레벨이 1부터 연속이 아니면 배열 인덱스와 레벨이 어긋난다.
            // 3레벨이 빠진 채로 넣으면 4레벨 값이 3레벨 자리에 앉는다
            string gap = FindGap(list);
            if (gap != null) { gaps.Add($"{pair.Key} ({gap})"); continue; }

            var levels = new AugmentLevelData[list.Count];
            for (int i = 0; i < list.Count; i++) levels[i] = list[i].Stats;

            Undo.RecordObject(data, "증강 시트 가져오기");

            data.levelStats = levels;

            EditorUtility.SetDirty(data);
            filled.Add($"{pair.Key} ({levels.Length}렙)");
        }

        AssetDatabase.SaveAssets();

        Report(filled, locked, missing, gaps, duplicates, skippedRows);
    }

    /// <summary>레벨이 1,2,3... 으로 이어지는가. 어긋나면 그 자리를 알려준다.</summary>
    static string FindGap(List<Row> sorted)
    {
        for (int i = 0; i < sorted.Count; i++)
        {
            if (sorted[i].Level == i + 1) continue;

            return sorted[i].Level == (i > 0 ? sorted[i - 1].Level : 0)
                ? $"{sorted[i].Level}레벨이 두 줄"
                : $"{i + 1}레벨이 없음";
        }

        return null;
    }

    // ── 잔가지 ────────────────────────────────────────────

    static List<string> FoundStats(string[] header)
    {
        var found = new List<string>();

        for (int i = 0; i < Stats.Length; i++)
            if (IndexOf(header, Stats[i].Sheet) >= 0) found.Add(Stats[i].Sheet);

        return found;
    }

    static int Total(Dictionary<string, List<Row>> rows)
    {
        int n = 0;
        foreach (List<Row> v in rows.Values) n += v.Count;
        return n;
    }

    /// <summary>id 와 레벨이 함께 있는 줄이 헤더다. 위에 안내문이 몇 줄이든 상관없게 한다.</summary>
    static int FindHeader(List<string[]> table)
    {
        for (int r = 0; r < table.Count; r++)
            if (IndexOf(table[r], IdColumn) >= 0 && IndexOf(table[r], LevelColumn) >= 0) return r;

        return -1;
    }

    static List<string[]> ReadCsv(string path)
    {
        string[] lines = File.ReadAllLines(path, Encoding.UTF8);

        var table = new List<string[]>(lines.Length);
        for (int i = 0; i < lines.Length; i++) table.Add(SplitCsv(lines[i]));

        return table;
    }

    /// <summary>빈 칸은 0이다. 안 쓰는 스탯을 매 줄 채우지 않아도 되게.</summary>
    static float Number(string[] row, int column)
    {
        if (column < 0 || column >= row.Length) return 0f;

        string cell = row[column].Trim();

        return float.TryParse(cell, NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
            ? v : 0f;
    }

    static int IndexOf(string[] header, string name)
    {
        for (int i = 0; i < header.Length; i++)
            if (header[i].Trim() == name) return i;

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

    static Dictionary<string, AugmentData> CollectAssets(out List<string> duplicates)
    {
        var byId = new Dictionary<string, AugmentData>();
        duplicates = new List<string>();

        string[] guids = AssetDatabase.FindAssets("t:AugmentData");

        for (int i = 0; i < guids.Length; i++)
        {
            var data = AssetDatabase.LoadAssetAtPath<AugmentData>(AssetDatabase.GUIDToAssetPath(guids[i]));

            if (data == null || string.IsNullOrEmpty(data.id)) continue;

            // 같은 id 가 둘이면 어느 쪽에 넣을지 코드가 정할 수 없다. 둘 다 건드리지 않는다
            if (byId.ContainsKey(data.id)) duplicates.Add(data.id);
            else byId[data.id] = data;
        }

        return byId;
    }

    static void Fail(string message)
    {
        Debug.LogWarning("[증강 시트] " + message);
        EditorUtility.DisplayDialog("가져오기 실패", message, "확인");
    }

    static void Report(List<string> filled, List<string> locked, List<string> missing,
                       List<string> gaps, List<string> duplicates, int skippedRows)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"채움 {filled.Count}개");
        if (filled.Count > 0) sb.AppendLine("  " + string.Join(", ", filled));

        if (locked.Count > 0)
            sb.AppendLine($"\n잠겨서 건너뜀 {locked.Count}개\n  " + string.Join(", ", locked));

        if (missing.Count > 0)
            sb.AppendLine($"\n에셋이 없는 id {missing.Count}개\n  " + string.Join(", ", missing));

        if (gaps.Count > 0)
            sb.AppendLine($"\n⚠ 레벨이 이어지지 않아 건드리지 않음\n  " + string.Join(", ", gaps));

        if (duplicates.Count > 0)
            sb.AppendLine($"\n⚠ id 가 겹쳐 건드리지 않음\n  " + string.Join(", ", duplicates));

        if (skippedRows > 0)
            sb.AppendLine($"\n레벨 칸이 비어 건너뛴 줄 {skippedRows}개");

        Debug.Log("[증강 시트] " + sb);
        EditorUtility.DisplayDialog("증강 시트 가져오기", sb.ToString(), "확인");
    }
}
