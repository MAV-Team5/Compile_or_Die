using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 끝난 런 하나의 성적표. 결과 씬으로 넘어가는 짐이다.
///
/// 씬을 넘길 때 오브젝트를 들고 갈 수 없으므로 <see cref="Last"/> 에 담아 건넨다.
/// 순수 C# 객체라 씬이 바뀌어도 살아남는다.
/// </summary>
public class RunResult
{
    /// <summary>가장 최근에 끝난 런. 결과 씬이 이걸 읽는다.</summary>
    public static RunResult Last;

    public bool Cleared;

    /// <summary>버틴 시간(초).</summary>
    public float Elapsed;

    public int Kills;

    /// <summary>도달 레벨.</summary>
    public int Level;

    /// <summary>런 중에 주워 모은 비트. 드랍물이 생기기 전까지는 0.</summary>
    public int BitsCollected;

    /// <summary>실제로 지급된 재화. 아래 세 항목의 합이다.</summary>
    public int Reward;

    /// <summary>보상 내역 — 결과 패널이 "왜 이만큼인가"를 보여줄 수 있게 쪼개 둔다.</summary>
    public int RewardFromKills;
    public int RewardFromTime;
    public int RewardFromClear;

    /// <summary>잡은 보스 이름들. 순서대로.</summary>
    public List<string> BossesDefeated = new();

    /// <summary>증강별 피해 성적표. 피해 내림차순.</summary>
    public List<RunStats.Entry> Damage = new();

    /// <summary>이번 런의 총 피해. 비중 계산의 분모.</summary>
    public float TotalDamage;

    /// <summary>분(:)초 표기. 결과 패널과 HUD 가 같은 형식을 쓰게 여기 둔다.</summary>
    public string ElapsedText => Format(Elapsed);

    public static string Format(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return $"{total / 60:00}:{total % 60:00}";
    }
}
