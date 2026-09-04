using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스택 증강이 쌓아두는 것. <b>프레임 하나 = 피해 한 번</b>이다.
///
/// 좌표와 그때 준 피해를 같이 들고 있어서, 되짚을 때 "어디서 얼마나" 가 살아난다.
/// 숫자 하나만 누적하면 그건 스택이 아니라 누산기고, 되짚을 자리가 없다.
/// </summary>
public class StackState
{
    public struct Frame
    {
        public Vector2 Position;
        public float Damage;
    }

    /// <summary>스택이 지금 무엇을 하고 있는가.</summary>
    public enum Phase
    {
        /// <summary>쌓는 중. 쿨타임마다 맨 위 하나씩 터뜨린다.</summary>
        Filling,

        /// <summary>
        /// 경계를 넘었지만 아직 안 터뜨리는 구간. <b>계속 push 를 받는다.</b>
        ///
        /// 진짜 스택 오버플로우도 경계를 넘는 순간 멈추지 않고, 넘어서 계속 써 내려가다 터진다.
        /// 게임에서는 이 1초가 "지금 최대한 때려라" 는 개입 구간이 된다.
        /// </summary>
        Grace,

        /// <summary>맨 위부터 역순으로 쏟아내는 중.</summary>
        Unwinding,

        /// <summary>다 쏟고 쉬는 중. 이 동안은 기록도 안 한다.</summary>
        Cooldown
    }

    public readonly List<Frame> Frames = new();

    public Phase Now = Phase.Filling;

    /// <summary>현재 단계에 남은 시간(초).</summary>
    public float Timer;

    /// <summary>언와인딩 중 다음 폭발까지 남은 시간.</summary>
    public float PopTimer;

    /// <summary>이번 오버플로우가 시작될 때의 프레임 수. 연출 길이를 나눌 때 쓴다.</summary>
    public int BurstTotal;

    /// <summary>이번 오버플로우로 들어간 누적 피해. HUD 가 실시간으로 세어 보여준다.</summary>
    public float BurstDamage;

    /// <summary>지금 쌓아도 되는 상태인가. 대기·언와인딩 중에는 안 받는다.</summary>
    public bool Accepting => Now == Phase.Filling || Now == Phase.Grace;

    public void Push(Vector2 position, float damage)
    {
        if (!Accepting || damage <= 0f) return;

        Frames.Add(new Frame { Position = position, Damage = damage });
    }

    /// <summary>맨 위 프레임을 꺼낸다. 비었으면 false.</summary>
    public bool Pop(out Frame frame)
    {
        int last = Frames.Count - 1;

        if (last < 0)
        {
            frame = default;
            return false;
        }

        frame = Frames[last];
        Frames.RemoveAt(last);

        return true;
    }

    public void Clear()
    {
        Frames.Clear();
        BurstTotal = 0;
        BurstDamage = 0f;
    }
}
