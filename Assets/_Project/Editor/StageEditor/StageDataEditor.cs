using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 스테이지 에셋 인스펙터. 웨이브 표 위에 <b>시간축 타임라인</b>과 <b>시간대별 그래프</b>를 그린다.
/// 막대를 누르면 바로 아래 편집 칸에 그 웨이브의 모든 값이 뜬다.
/// 막대 몸통을 끌면 시작 시각, 오른쪽 끝을 끌면 기간이 바뀐다(1초 단위, Alt 를 누르면 0.1초).
/// 보스 선도 같은 방식으로 고르고 끈다. 모든 수정은 Ctrl+Z 로 되돌릴 수 있다.
///
/// 그래프 종류를 늘리려면 <see cref="Metrics"/> 에 한 줄 추가하면 된다.
/// </summary>
[CustomEditor(typeof(StageData))]
public class StageDataEditor : Editor
{
    // ── 그래프 지표 ───────────────────────────────────────

    /// <summary>웨이브 하나가 초당 얼마를 내보내는지 재는 지표. 그래프는 이것을 시간대별로 더한다.</summary>
    struct Metric
    {
        public string Name;
        public string Unit;
        public Color Color;
        public System.Func<StageWave, float> PerSpawn;   // 한 마리당 값
    }

    static readonly Metric[] Metrics =
    {
        new() { Name = "스폰", Unit = "마리/s", Color = new Color(0.35f, 0.75f, 1f),
                PerSpawn = _ => 1f },

        new() { Name = "체력 유입", Unit = "HP/s (필요 DPS)", Color = new Color(1f, 0.45f, 0.4f),
                PerSpawn = w => w.enemy.health * w.healthScale },

        new() { Name = "경험치", Unit = "EXP/s", Color = new Color(0.55f, 1f, 0.5f),
                PerSpawn = w => w.expPerKill * OrbExp(w) },
    };

    // ── 배치 ──────────────────────────────────────────────

    const float LabelWidth = 120f;
    const float RulerHeight = 18f;
    const float RowHeight = 18f;
    const float RowGap = 3f;
    const float GraphHeight = 70f;
    const float MinViewSeconds = 240f;   // 4분 런 기준. 더 긴 웨이브가 있으면 늘어난다

    static readonly Color MinionColor = new(0.35f, 0.65f, 1f);
    static readonly Color EliteColor = new(1f, 0.7f, 0.25f);
    static readonly Color StationaryColor = new(0.6f, 0.9f, 0.4f);
    static readonly Color InvalidColor = new(0.45f, 0.45f, 0.45f);
    static readonly Color BossColor = new(1f, 0.3f, 0.35f);
    static readonly Color SelectedOutline = Color.white;

    const float EdgeGrab = 6f;   // 막대 오른쪽 끝을 잡는 폭(px)

    static int metricIndex;
    static bool showTimeline = true;

    enum Kind { None, Wave, Boss }
    enum Drag { None, Move, Resize }

    Kind selectedKind;
    int selectedIndex = -1;

    Drag drag;
    float dragOffset;   // 잡은 자리와 시작 시각의 차(초)
    int controlId;      // 마우스를 붙잡는 표. 창 밖에서 놓아도 MouseUp 을 받는다
    int undoGroup;      // 끌기 한 번을 Ctrl+Z 한 번으로 묶는다

    SerializedProperty Waves => serializedObject.FindProperty("waves");
    SerializedProperty Bosses => serializedObject.FindProperty("bosses");

    public override void OnInspectorGUI()
    {
        var stage = (StageData)target;

        serializedObject.Update();

        showTimeline = EditorGUILayout.BeginFoldoutHeaderGroup(showTimeline, "타임라인");
        if (showTimeline)
        {
            DrawTimeline(stage);
            DrawListButtons();
            DrawSelectedPanel();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(8f);
        DrawDefaultInspector();
    }

    void DrawTimeline(StageData stage)
    {
        controlId = GUIUtility.GetControlID(FocusType.Passive);

        float length = ViewLength(stage);
        int rows = stage.waves.Count;

        float height = RulerHeight + rows * (RowHeight + RowGap) + 6f;
        Rect area = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));
        Rect track = new(area.x + LabelWidth, area.y, area.width - LabelWidth - 4f, area.height);

        EditorGUI.DrawRect(track, new Color(0f, 0f, 0f, 0.18f));

        DrawRuler(track, length);

        for (int i = 0; i < rows; i++)
        {
            float y = area.y + RulerHeight + i * (RowHeight + RowGap);
            DrawWaveRow(stage, i, new Rect(area.x, y, LabelWidth - 6f, RowHeight), track, y, length);
        }

        DrawBossLines(stage, track, length);

        HandleDrag(track, length);

        EditorGUILayout.Space(6f);
        DrawGraph(stage, length);
    }

    // ── 눈금 ──────────────────────────────────────────────

    static void DrawRuler(Rect track, float length)
    {
        var style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperLeft };

        for (int s = 0; s <= length; s += 30)
        {
            float x = TimeToX(track, s, length);
            bool major = s % 60 == 0;

            EditorGUI.DrawRect(new Rect(x, track.y + (major ? 0f : 8f), 1f, track.height),
                               new Color(1f, 1f, 1f, major ? 0.18f : 0.07f));

            if (major) GUI.Label(new Rect(x + 2f, track.y, 40f, RulerHeight), Clock(s), style);
        }
    }

    // ── 웨이브 막대 ───────────────────────────────────────

    void DrawWaveRow(StageData stage, int index, Rect labelRect, Rect track, float y, float length)
    {
        StageWave wave = stage.waves[index];
        if (wave == null) return;

        GUI.Label(labelRect, string.IsNullOrEmpty(wave.label) ? $"Wave {index}" : wave.label,
                  EditorStyles.miniBoldLabel);

        float end = WaveEnd(wave, length);
        float x0 = TimeToX(track, wave.startAt, length);
        float x1 = TimeToX(track, end, length);

        Rect bar = new(x0, y + 1f, Mathf.Max(3f, x1 - x0), RowHeight - 2f);

        EditorGUI.DrawRect(bar, BarColor(wave));
        if (selectedKind == Kind.Wave && index == selectedIndex) DrawOutline(bar, SelectedOutline);

        string text = wave.IsValid ? $"{wave.enemy.displayName}  {wave.interval:0.##}s ×{wave.burst}" : "적 없음";
        if (wave.maxSpawns > 0) text += $"  (최대 {wave.maxSpawns})";

        GUI.Label(bar, new GUIContent(text, Tooltip(wave, end)), EditorStyles.whiteMiniLabel);

        Rect edge = new(bar.xMax - EdgeGrab, bar.y, EdgeGrab, bar.height);
        EditorGUIUtility.AddCursorRect(bar, MouseCursor.Pan);
        EditorGUIUtility.AddCursorRect(edge, MouseCursor.ResizeHorizontal);

        Event e = Event.current;
        if (e.type != EventType.MouseDown || e.button != 0 || !bar.Contains(e.mousePosition)) return;

        Select(Kind.Wave, index);

        drag = edge.Contains(e.mousePosition) ? Drag.Resize : Drag.Move;
        dragOffset = XToTime(track, e.mousePosition.x, length) - wave.startAt;
        GUIUtility.hotControl = controlId;
        undoGroup = Undo.GetCurrentGroup();

        e.Use();
    }

    void Select(Kind kind, int index)
    {
        selectedKind = kind;
        selectedIndex = index;
        GUI.FocusControl(null);   // 고르던 입력칸 값이 새 항목으로 옮겨 붙지 않게
        Repaint();
    }

    /// <summary>잡은 막대·선을 마우스를 따라 옮긴다. 값은 직렬화 속성으로 써서 되돌리기가 된다.</summary>
    void HandleDrag(Rect track, float length)
    {
        Event e = Event.current;

        if (drag == Drag.None) return;

        // 다른 곳이 마우스를 가져갔으면 끌기를 놓는다. 안 그러면 엉뚱한 조작에 막대가 따라온다
        if (GUIUtility.hotControl != controlId)
        {
            drag = Drag.None;
            return;
        }

        EventType type = e.GetTypeForControl(controlId);

        if (type == EventType.MouseUp)
        {
            GUIUtility.hotControl = 0;
            drag = Drag.None;
            Undo.CollapseUndoOperations(undoGroup);
            e.Use();
            return;
        }

        if (type != EventType.MouseDrag) return;

        float step = e.alt ? 0.1f : 1f;
        float t = Snap(XToTime(track, e.mousePosition.x, length), step);

        SerializedProperty item = SelectedProperty();
        if (item == null) { drag = Drag.None; return; }

        if (selectedKind == Kind.Boss)
        {
            item.FindPropertyRelative("atSeconds").floatValue = Mathf.Max(0f, t);
        }
        else if (drag == Drag.Move)
        {
            item.FindPropertyRelative("startAt").floatValue = Mathf.Max(0f, Snap(t - dragOffset, step));
        }
        else
        {
            float start = item.FindPropertyRelative("startAt").floatValue;
            item.FindPropertyRelative("duration").floatValue = Mathf.Max(step, t - start);
        }

        e.Use();
        Repaint();
    }

    static string Tooltip(StageWave wave, float end)
    {
        if (!wave.IsValid) return "EnemyData 또는 프리팹이 비어 있다.";

        return $"{Clock(wave.startAt)} ~ {Clock(end)}\n" +
               $"초당 {wave.burst / Mathf.Max(0.05f, wave.interval):0.#}마리\n" +
               $"체력 ×{wave.healthScale}  속도 ×{wave.speedScale}  피해 ×{wave.damageScale}\n" +
               $"경험치 {wave.expPerKill:0.##}개/처치 × {OrbExp(wave)}";
    }

    // ── 추가 · 복제 · 삭제 ────────────────────────────────

    void DrawListButtons()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("+ 웨이브", EditorStyles.miniButtonLeft)) Add(Waves, Kind.Wave);
        if (GUILayout.Button("+ 보스", EditorStyles.miniButtonMid)) Add(Bosses, Kind.Boss);

        using (new EditorGUI.DisabledScope(SelectedProperty() == null))
        {
            if (GUILayout.Button("복제", EditorStyles.miniButtonMid)) Duplicate();
            if (GUILayout.Button("삭제", EditorStyles.miniButtonRight)) Remove();
        }

        EditorGUILayout.EndHorizontal();
    }

    void Add(SerializedProperty list, Kind kind)
    {
        // 끝에 넣으면 유니티가 마지막 항목 값을 복사해 온다. 새로 고르기 편하게 시작 시각만 되돌린다
        int index = list.arraySize;
        list.InsertArrayElementAtIndex(index);

        SerializedProperty item = list.GetArrayElementAtIndex(index);
        if (kind == Kind.Wave) item.FindPropertyRelative("label").stringValue = $"Wave {index}";

        Select(kind, index);
    }

    void Duplicate()
    {
        SerializedProperty list = selectedKind == Kind.Boss ? Bosses : Waves;

        list.InsertArrayElementAtIndex(selectedIndex);   // 같은 값으로 바로 뒤에 하나 더 생긴다
        Select(selectedKind, selectedIndex + 1);
    }

    void Remove()
    {
        SerializedProperty list = selectedKind == Kind.Boss ? Bosses : Waves;

        list.DeleteArrayElementAtIndex(selectedIndex);
        Select(Kind.None, -1);
    }

    SerializedProperty SelectedProperty()
    {
        SerializedProperty list = selectedKind switch
        {
            Kind.Wave => Waves,
            Kind.Boss => Bosses,
            _ => null
        };

        if (list == null || selectedIndex < 0 || selectedIndex >= list.arraySize) return null;

        return list.GetArrayElementAtIndex(selectedIndex);
    }

    // ── 고른 항목 편집 ────────────────────────────────────

    /// <summary>고른 웨이브·보스의 모든 필드를 그린다. 필드가 늘어도 자동으로 따라온다.</summary>
    void DrawSelectedPanel()
    {
        SerializedProperty item = SelectedProperty();
        if (item == null)
        {
            EditorGUILayout.HelpBox("막대나 보스 선을 누르면 여기서 바로 고칠 수 있다.", MessageType.None);
            return;
        }

        string title = selectedKind == Kind.Boss ? $"보스 {selectedIndex}" : $"웨이브 {selectedIndex}";

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        SerializedProperty it = item.Copy();
        SerializedProperty end = item.GetEndProperty();

        for (bool next = it.NextVisible(true); next && !SerializedProperty.EqualContents(it, end); next = it.NextVisible(false))
            EditorGUILayout.PropertyField(it, true);

        EditorGUILayout.EndVertical();
    }

    // ── 보스 ──────────────────────────────────────────────

    void DrawBossLines(StageData stage, Rect track, float length)
    {
        for (int i = 0; i < stage.bosses.Count; i++)
        {
            BossSpawn boss = stage.bosses[i];
            if (boss == null) continue;

            float x = TimeToX(track, boss.atSeconds, length);

            bool selected = selectedKind == Kind.Boss && selectedIndex == i;
            EditorGUI.DrawRect(new Rect(x - 1f, track.y, selected ? 4f : 2f, track.height),
                               selected ? SelectedOutline : BossColor);

            Rect grab = new(x - 5f, track.y, 10f, track.height);
            EditorGUIUtility.AddCursorRect(grab, MouseCursor.ResizeHorizontal);

            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && grab.Contains(e.mousePosition))
            {
                Select(Kind.Boss, i);
                drag = Drag.Move;
                GUIUtility.hotControl = controlId;
                undoGroup = Undo.GetCurrentGroup();
                e.Use();
            }

            string name = boss.IsValid ? boss.DisplayName : "보스 없음";
            string tag = boss.endsRun ? "BOSS" : "중간보스";

            GUI.Label(new Rect(x + 3f, track.yMax - 14f, 160f, 14f),
                      new GUIContent($"▼ {tag} {name}", Clock(boss.atSeconds)), EditorStyles.miniLabel);
        }
    }

    // ── 그래프 ────────────────────────────────────────────

    static void DrawGraph(StageData stage, float length)
    {
        string[] names = new string[Metrics.Length];
        for (int i = 0; i < Metrics.Length; i++) names[i] = Metrics[i].Name;

        metricIndex = GUILayout.Toolbar(metricIndex, names, EditorStyles.miniButton);
        Metric metric = Metrics[Mathf.Clamp(metricIndex, 0, Metrics.Length - 1)];

        Rect area = GUILayoutUtility.GetRect(0f, GraphHeight, GUILayout.ExpandWidth(true));
        Rect plot = new(area.x + LabelWidth, area.y, area.width - LabelWidth - 4f, area.height);

        EditorGUI.DrawRect(plot, new Color(0f, 0f, 0f, 0.18f));

        int seconds = Mathf.CeilToInt(length);
        float[] values = Sample(stage, metric, seconds);

        float peak = 0f;
        float total = 0f;
        for (int s = 0; s < values.Length; s++)
        {
            peak = Mathf.Max(peak, values[s]);
            total += values[s];
        }

        float column = plot.width / seconds;

        for (int s = 0; s < values.Length && peak > 0f; s++)
        {
            float h = plot.height * (values[s] / peak);
            EditorGUI.DrawRect(new Rect(plot.x + s * column, plot.yMax - h, Mathf.Max(1f, column), h), metric.Color);
        }

        // 보스 시각을 그래프에도 표시한다
        for (int i = 0; i < stage.bosses.Count; i++)
            if (stage.bosses[i] != null)
                EditorGUI.DrawRect(new Rect(TimeToX(plot, stage.bosses[i].atSeconds, length) - 1f, plot.y, 2f, plot.height), BossColor);

        GUI.Label(new Rect(area.x, area.y, LabelWidth - 6f, area.height),
                  $"{metric.Unit}\n최대 {peak:0.#}\n총 {total:N0}", EditorStyles.miniLabel);
    }

    /// <summary>1초 단위로 활성 웨이브의 지표를 더한다. maxSpawns 로 일찍 끝나는 웨이브도 반영한다.</summary>
    static float[] Sample(StageData stage, Metric metric, int seconds)
    {
        var values = new float[seconds];

        foreach (StageWave wave in stage.waves)
        {
            if (wave == null || !wave.IsValid) continue;

            float rate = Mathf.Max(1, wave.burst) / Mathf.Max(0.05f, wave.interval);
            float perSpawn = metric.PerSpawn(wave);
            float end = WaveEnd(wave, seconds);

            for (int s = 0; s < seconds; s++)
            {
                float overlap = Mathf.Min(s + 1, end) - Mathf.Max(s, wave.startAt);
                if (overlap > 0f) values[s] += rate * perSpawn * overlap;
            }
        }

        return values;
    }

    // ── 계산 ──────────────────────────────────────────────

    /// <summary>웨이브가 실제로 끝나는 시각. 기간·누적 상한 중 먼저 오는 쪽, 둘 다 없으면 화면 끝.</summary>
    static float WaveEnd(StageWave wave, float viewEnd)
    {
        float end = wave.duration > 0f ? wave.startAt + wave.duration : viewEnd;

        if (wave.maxSpawns > 0)
        {
            int volleys = Mathf.CeilToInt(wave.maxSpawns / (float)Mathf.Max(1, wave.burst));
            end = Mathf.Min(end, wave.startAt + volleys * Mathf.Max(0.05f, wave.interval));
        }

        return Mathf.Min(end, viewEnd);
    }

    static float ViewLength(StageData stage)
    {
        float length = MinViewSeconds;

        foreach (StageWave wave in stage.waves)
            if (wave != null && wave.duration > 0f) length = Mathf.Max(length, wave.startAt + wave.duration);

        foreach (BossSpawn boss in stage.bosses)
            if (boss != null) length = Mathf.Max(length, boss.atSeconds + 30f);

        return Mathf.Ceil(length / 30f) * 30f;
    }

    static float OrbExp(StageWave wave)
        => wave.expOrb != null && wave.expOrb.TryGetComponent(out ExpMove orb) ? orb.exp : 0f;

    static Color BarColor(StageWave wave)
    {
        if (!wave.IsValid) return InvalidColor;
        if (wave.enemy.stationary) return StationaryColor;

        return wave.enemy.rank == EnemyData.Rank.Elite ? EliteColor : MinionColor;
    }

    static float XToTime(Rect track, float x, float length)
        => Mathf.Clamp01((x - track.x) / track.width) * length;

    static float Snap(float value, float step) => Mathf.Round(value / step) * step;

    static float TimeToX(Rect track, float seconds, float length)
        => track.x + track.width * Mathf.Clamp01(seconds / length);

    static string Clock(float seconds)
        => $"{Mathf.FloorToInt(seconds / 60f)}:{Mathf.FloorToInt(seconds % 60f):00}";

    static void DrawOutline(Rect r, Color c)
    {
        EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1f), c);
        EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1f, r.width, 1f), c);
        EditorGUI.DrawRect(new Rect(r.x, r.y, 1f, r.height), c);
        EditorGUI.DrawRect(new Rect(r.xMax - 1f, r.y, 1f, r.height), c);
    }
}
