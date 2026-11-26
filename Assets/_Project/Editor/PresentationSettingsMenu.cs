using UnityEditor;
using UnityEngine;

/// <summary>메뉴 <c>CoD/네온 연출</c> — 체크 표시가 현재 상태다.</summary>
static class PresentationSettingsMenu
{
    const string NeonMenu = "CoD/네온 연출";

    [MenuItem(NeonMenu)]
    static void ToggleNeon()
    {
        PresentationSettings settings = PresentationSettings.Asset;
        if (settings == null)
        {
            Debug.LogWarning("[Presentation] Resources/PresentationSettings 에셋이 없습니다.");
            return;
        }

        Undo.RecordObject(settings, "네온 연출 전환");
        settings.neon = !settings.neon;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Presentation] 네온 연출 {(settings.neon ? "켜짐" : "꺼짐")}.");
    }

    [MenuItem(NeonMenu, true)]
    static bool ToggleNeonValidate()
    {
        Menu.SetChecked(NeonMenu, PresentationSettings.Neon);
        return true;
    }
}
