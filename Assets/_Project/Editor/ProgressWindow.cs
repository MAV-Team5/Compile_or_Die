using UnityEditor;
using UnityEngine;

/// <summary>
/// 저장된 진행상황을 손으로 만지는 테스트 도구. <c>Tools → CoD → 진행상황</c>
///
/// 재화와 하드웨어 레벨은 PlayerPrefs 에 들어 있어서 씬이나 에셋을 봐도 안 보인다.
/// 밸런싱하려면 "비트 10만으로 놓고 전부 찍어보기" 같은 일을 자주 하는데,
/// 그때마다 게임을 여러 판 도는 것은 시간 낭비다.
///
/// <b>에디터 전용이다.</b> 빌드에는 들어가지 않는다.
/// </summary>
public class ProgressWindow : EditorWindow
{
    HardwareTable table;
    int bitsField;

    [MenuItem("Tools/CoD/진행상황")]
    static void Open()
    {
        GetWindow<ProgressWindow>("진행상황").minSize = new Vector2(340f, 420f);
    }

    void OnEnable()
    {
        // 표를 매번 끌어다 놓지 않도록 프로젝트에서 찾아둔다
        if (table == null) table = FindTable();

        bitsField = PlayerProgress.Bits;
    }

    static HardwareTable FindTable()
    {
        string[] found = AssetDatabase.FindAssets("t:HardwareTable");

        if (found.Length == 0) return null;

        return AssetDatabase.LoadAssetAtPath<HardwareTable>(
            AssetDatabase.GUIDToAssetPath(found[0]));
    }

    void OnGUI()
    {
        EditorGUILayout.Space(6f);

        table = (HardwareTable)EditorGUILayout.ObjectField("표", table, typeof(HardwareTable), false);

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("재화", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("지금 보유", $"{PlayerProgress.Bits:N0} bit");

        bitsField = EditorGUILayout.IntField("정할 값", bitsField);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("적용")) Apply(bitsField);
            if (GUILayout.Button("+10,000")) Apply(PlayerProgress.Bits + 10000);
            if (GUILayout.Button("+100,000")) Apply(PlayerProgress.Bits + 100000);
        }

        EditorGUILayout.Space(14f);
        EditorGUILayout.LabelField("하드웨어", EditorStyles.boldLabel);

        if (table == null)
        {
            EditorGUILayout.HelpBox("HardwareTable 을 못 찾았다. 위에 끌어다 놓을 것.", MessageType.Warning);
        }
        else
        {
            foreach (HardwareKind kind in System.Enum.GetValues(typeof(HardwareKind)))
            {
                HardwareTable.Entry entry = table.Find(kind);

                string name = entry != null && !string.IsNullOrEmpty(entry.displayName)
                    ? entry.displayName
                    : kind.ToString();

                int purchased = PlayerProgress.PurchasedLevel(kind);
                int active = PlayerProgress.ActiveLevel(kind);
                int max = table.MaxLevelOf(kind);

                string value = max == 0 ? "잠김" : $"적용 {active} / 구매 {purchased} / 최대 {max}";

                EditorGUILayout.LabelField(name, value);
            }

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("전부 물리고 환불"))
            {
                PlayerProgress.RefundAll(table);
                bitsField = PlayerProgress.Bits;
                Repaint();
            }
        }

        EditorGUILayout.Space(14f);

        if (GUILayout.Button("세이브 통째로 삭제"))
        {
            // 환불과 달리 비트까지 사라지므로 한 번 되묻는다
            if (EditorUtility.DisplayDialog("세이브 삭제",
                    "비트와 하드웨어 레벨을 전부 지운다. 되돌릴 수 없다.", "삭제", "취소"))
            {
                PlayerProgress.Wipe();
                bitsField = 0;
                Repaint();
            }
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.HelpBox("플레이 중에 바꾸면 이미 시작된 런에는 반영되지 않는다. "
                              + "하드웨어는 런이 시작될 때 한 번만 읽는다.", MessageType.Info);
    }

    void Apply(int amount)
    {
        PlayerProgress.SetBits(amount);

        bitsField = PlayerProgress.Bits;

        Repaint();
    }
}
