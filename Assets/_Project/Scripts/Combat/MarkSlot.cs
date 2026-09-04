using UnityEngine;

/// <summary>
/// 이 표식이 적의 몇 번 자리에 붙을지. <b>표식 프리팹에</b> 붙인다.
///
/// 적 쪽의 <see cref="MarkMount"/> 와 같은 번호끼리 만난다.
///
/// <code>
///   BFS 표식 프리팹  [MarkSlot 0]  ──▶  적 프리팹의  Mount_0
///   DFS 표식 프리팹  [MarkSlot 1]  ──▶  적 프리팹의  Mount_1
/// </code>
///
/// <b>왜 증강 에셋이 아니라 표식 프리팹인가</b> — 자리는 표식의 생김새와 함께 정해진다.
/// 머리 위에 뜰 작은 배지를 만들었으면 그 프리팹이 곧 "머리 위 것" 이다.
/// 증강 에셋에 두면 프리팹을 갈아 끼울 때마다 양쪽을 같이 고쳐야 한다.
///
/// 안 붙이면 0번이다.
/// </summary>
public class MarkSlot : MonoBehaviour
{
    [Tooltip("적 프리팹의 MarkMount 번호와 맞춘다.\n\n" +
             "＊ 표식마다 다른 번호를 줄 것 — 같은 번호끼리는 한 자리에 겹쳐서 겹겹이 커진다.\n" +
             "  그 번호의 자리가 적에 없으면 적 본체에 붙는다.")]
    public int slot = 0;
}
