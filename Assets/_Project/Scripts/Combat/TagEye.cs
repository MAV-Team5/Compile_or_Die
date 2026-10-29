using UnityEngine;

/// <summary>
/// 적의 눈 오브젝트 표시. 탐색 강조 때 <see cref="MarkerHolder"/> 가 이 컴포넌트로 눈을 찾는다.
///
/// 이름으로 찾으면 눈 프리팹을 색마다 다르게 쓸 수 없다.
/// 컴포넌트로 찾으면 오브젝트 이름이 무엇이든, 몇 번째 자식이든 상관없다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class TagEye : MonoBehaviour { }
