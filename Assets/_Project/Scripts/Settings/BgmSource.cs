using UnityEngine;

/// <summary>
/// "이 씬은 이 곡" 을 정하는 표지판. 씬의 아무 오브젝트에나 하나 붙인다.
///
/// 실제 재생은 <see cref="BgmPlayer"/> 가 맡는다 — 그쪽은 씬을 넘어 살아남으므로,
/// 여러 씬에 같은 클립을 물려두면 넘어가도 음악이 안 끊긴다.
/// </summary>
public class BgmSource : MonoBehaviour
{
    [Tooltip("이 씬에서 흐를 곡. 비우면 음악을 끈다.")]
    [SerializeField] AudioClip clip;

    [Tooltip("이 곡 자체의 크기. 곡마다 녹음 크기가 달라서 여기서 맞춘다.\n" +
             "설정의 배경음 볼륨은 이 값에 곱해진다.")]
    [Range(0f, 1f)] [SerializeField] float volume = 1f;

    [Tooltip("끄면 이 씬에서는 음악을 건드리지 않는다 — 앞 씬의 곡이 그대로 이어진다.")]
    [SerializeField] bool takeOver = true;

    void Start()
    {
        if (!takeOver) return;

        BgmPlayer.Play(clip, volume);
    }
}
