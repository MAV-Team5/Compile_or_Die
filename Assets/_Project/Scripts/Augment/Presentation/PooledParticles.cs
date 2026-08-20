using UnityEngine;

/// <summary>
/// 풀에서 다시 꺼낸 파티클을 되감아 튼다. <see cref="PooledSpawner"/> 가 자동으로 붙인다.
///
/// 풀은 오브젝트를 끄고 켜는 것뿐이라, 이미 끝난 파티클은 켜져도 아무것도 안 나온다.
/// 프리팹마다 이걸 챙기게 하면 언젠가 빠뜨리므로 코드가 대신 한다.
///
/// 프리팹이 신경 쓸 것은 <b>Stop Action 을 Destroy 로 두지 않는 것</b> 하나뿐이다.
/// </summary>
public class PooledParticles : MonoBehaviour
{
    ParticleSystem[] systems;

    void Awake() => systems = GetComponentsInChildren<ParticleSystem>(true);

    void OnEnable()
    {
        if (systems == null) return;

        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null) continue;

            systems[i].Clear(true);
            systems[i].Play(true);
        }
    }
}
