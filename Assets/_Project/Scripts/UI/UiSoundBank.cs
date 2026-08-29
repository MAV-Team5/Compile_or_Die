using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 어떤 상황에 어떤 소리를 낼지 적어둔 표. <see cref="UiTheme"/> 와 같은 자리에 둔다 —
/// <c>Assets/Resources/UiSoundBank.asset</c>.
///
/// <b>Cue 하나에 클립을 여러 개 물린다.</b> 같은 클릭음이 100번 반복되면 귀가 피로해진다.
/// 매번 랜덤으로 하나 고르면 같은 소리로 들리면서도 질리지 않는다 —
/// 지금 가진 라이브러리가 <c>click_001~005</c> 처럼 변형을 주는 이유가 이것이다.
///
/// 에셋이 없으면 아무 소리도 안 난다. 게임은 그대로 돈다.
/// </summary>
[CreateAssetMenu(fileName = "UiSoundBank", menuName = "CoD/UI Sound Bank")]
public class UiSoundBank : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public UiCue cue;

        [Tooltip("이 중 하나를 매번 랜덤으로 고른다. 하나만 넣어도 된다.")]
        public AudioClip[] clips;

        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("이 소리를 다시 내기까지의 최소 간격(초).\n" +
                 "호버처럼 빠르게 연달아 발생하는 것에 쓴다 — 버튼 위를 훑으면 소리가 쏟아진다.")]
        [Min(0f)] public float minInterval = 0.04f;

        /// <summary>마지막으로 울린 시각. 멈춘 화면에서도 재야 하므로 실제 시간을 쓴다.</summary>
        [System.NonSerialized] public float LastPlayed = -999f;
    }

    [Tooltip("Cue 하나당 한 줄. 없는 Cue 는 그냥 소리가 안 난다.")]
    public List<Entry> entries = new();

    static UiSoundBank current;
    static bool searched;

    /// <summary>
    /// <c>Assets/Resources/UiSoundBank.asset</c> 을 찾는다. 없으면 null —
    /// 소리가 없다고 게임이 멈출 이유는 없으므로 임시 에셋을 만들지 않는다.
    /// </summary>
    public static UiSoundBank Current
    {
        get
        {
            if (current != null) return current;
            if (searched) return null;

            searched = true;
            current = Resources.Load<UiSoundBank>("UiSoundBank");

            if (current == null)
                Debug.LogWarning("[UiSoundBank] Resources/UiSoundBank.asset 이 없어 UI 가 무음이다. " +
                                 "Create → CoD → UI Sound Bank 로 만들어 Resources 폴더에 둘 것.");

            return current;
        }
        set { current = value; searched = true; }
    }

    /// <summary>이 Cue 로 지금 울릴 클립을 고른다. 간격이 안 됐거나 클립이 없으면 false.</summary>
    public bool TryPick(UiCue cue, out AudioClip clip, out float volume)
    {
        clip = null;
        volume = 1f;

        Entry entry = Find(cue);
        if (entry == null || entry.clips == null || entry.clips.Length == 0) return false;

        // 멈춘 화면(증강 선택은 timeScale 0)에서도 재야 하므로 실제 시간
        float now = Time.unscaledTime;
        if (now - entry.LastPlayed < entry.minInterval) return false;

        // 빈 칸이 섞여 있어도 소리가 사라지지 않게, 고른 것이 비면 한 번 더 훑는다
        clip = entry.clips[Random.Range(0, entry.clips.Length)];

        if (clip == null)
        {
            for (int i = 0; i < entry.clips.Length && clip == null; i++) clip = entry.clips[i];
            if (clip == null) return false;
        }

        entry.LastPlayed = now;
        volume = entry.volume;

        return true;
    }

    Entry Find(UiCue cue)
    {
        for (int i = 0; i < entries.Count; i++)
            if (entries[i] != null && entries[i].cue == cue) return entries[i];

        return null;
    }
}
