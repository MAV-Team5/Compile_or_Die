using UnityEngine;

/// <summary>
/// 포물선 이동을 대신 돌려주는 공용 도구. 전달·효과 모듈은 코루틴을 못 돌리므로 여기에 맡긴다.
///
/// 2D 탑다운이라 진짜 높이가 없다 — 직선 위치에 위쪽(+y) 오프셋을 sin 모양으로 더해
/// "던져서 솟았다 떨어지는" 그림을 만든다. 게임 시간으로 재므로 레벨업·일시정지 동안 같이 멈춘다.
///
/// 쓰는 곳: <see cref="LobDelivery"/>(거품·수류탄 던지기), <see cref="TossEffect"/>(적 날려 보내기).
/// </summary>
public class ArcMover : MonoBehaviour
{
    static ArcMover host;

    /// <summary>
    /// body 를 from 에서 목적지까지 포물선으로 옮긴다. 목적지는 매 프레임 다시 묻는다(움직이는 적을 따라 떨어지게).
    /// isValid 가 false 를 돌려주면 그 자리에서 멈추고 onArrive 를 부르지 않는다.
    /// </summary>
    public static void Move(Transform body, Vector2 from, System.Func<Vector2> destination,
                            float duration, float height,
                            System.Action<Vector2> onArrive, System.Func<bool> isValid = null)
    {
        Host().StartCoroutine(host.Run(body, from, destination, duration, height, onArrive, isValid));
    }

    /// <summary>게임 시간으로 seconds 뒤에 action 을 부른다. 단계를 나눠 보여줄 때 쓴다.</summary>
    public static void After(float seconds, System.Action action)
    {
        Host().StartCoroutine(host.Wait(seconds, action));
    }

    static ArcMover Host()
    {
        // 씬마다 하나. 씬이 바뀌면 같이 사라져서 날던 것들도 정리된다
        if (host == null) host = new GameObject("ArcMover").AddComponent<ArcMover>();
        return host;
    }

    System.Collections.IEnumerator Wait(float seconds, System.Action action)
    {
        if (seconds > 0f) yield return new WaitForSeconds(seconds);
        action?.Invoke();
    }

    System.Collections.IEnumerator Run(Transform body, Vector2 from, System.Func<Vector2> destination,
                                       float duration, float height,
                                       System.Action<Vector2> onArrive, System.Func<bool> isValid)
    {
        duration = Mathf.Max(0.01f, duration);
        Vector2 to = destination();

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (isValid != null && !isValid()) yield break;

            to = destination();

            float k = t / duration;
            Vector2 pos = Vector2.Lerp(from, to, k) + Vector2.up * (height * 4f * k * (1f - k));

            if (body != null) body.position = pos;

            yield return null;
        }

        if (isValid != null && !isValid()) yield break;

        if (body != null) body.position = to;

        // 물리 좌표를 바로 맞춘다. 착지 직후 범위 판정이 옛 자리를 보면 안 된다
        Physics2D.SyncTransforms();

        onArrive?.Invoke(to);
    }
}
