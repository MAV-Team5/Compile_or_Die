using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum GameLogType
{
    System,
    Combat,
    Exp,
    Skill,
    Loot,
    Warning,
    Error,
    Debug,
    None
}
public class LogEntry
{
    public GameLogType Type;
    public string Message;
    public int Count = 1;

    public LogEntry(GameLogType type, string message)
    {
        Type = type;
        Message = message;
    }
}

public class LogManager : MonoBehaviour
{
    public static LogManager Instance;

    [Header("UI")]
    [SerializeField] private TMP_Text logText;

    /// <summary>로그 텍스트의 RectTransform. 다른 HUD가 겹치지 않게 위치를 미룰 때 쓴다.</summary>
    public RectTransform TextRect => logText != null ? (RectTransform)logText.transform : null;

    [Header("Settings")]
    [SerializeField] private int maxLines = 10;

    private readonly Queue<LogEntry> logs = new();

    // 로그 맨 아래 흰색으로 고정되는 상태줄. 체력·레벨·킬 수 표시용
    private string statusLine;

    // ── 경험치 로그 묶기 ──────────────────────────────────

    /// <summary>첫 획득 뒤 이만큼 기다렸다가 낸다.</summary>
    const float ExpFlushDelay = 0.5f;

    /// <summary>이어서 먹을 때마다 기다림을 이만큼 늘린다.</summary>
    const float ExpExtendStep = 0.15f;

    /// <summary>아무리 이어 먹어도 이보다 오래 참지는 않는다.</summary>
    const float ExpMaxDelay = 1.5f;

    int pendingExp;
    float expTimer;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AddLog(GameLogType type, string message)
    {
        if (logs.Count > 0)
        {
            LogEntry[] temp = logs.ToArray();

            LogEntry lastLog = temp[temp.Length - 1];

            if ( lastLog.Type == type && lastLog.Message == message)
            {
                lastLog.Count++;

                RefreshUI();
                return;
            }
        }
        logs.Enqueue(new LogEntry(type, message));

        while (logs.Count > maxLines)
        {
            logs.Dequeue();
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        System.Text.StringBuilder sb = new();

        LogEntry[] logArray = logs.ToArray();

        for(int i = 0; i < logArray.Length; i++)
        {
            float t = logArray.Length <= 1 ? 1f : (float)i /(logArray.Length - 1);
            float alpha = Mathf.Lerp(0.1f, 1f, t);
            sb.AppendLine(FormatLog(logArray[i], alpha));
        }

        if (!string.IsNullOrEmpty(statusLine))
            sb.AppendLine($"<color=#FFFFFF>> {statusLine}</color>");

        logText.text = sb.ToString();
    }

    /// <summary>로그 맨 아래 흰색 고정 줄을 갱신한다. 값이 같으면 다시 그리지 않는다.</summary>
    public void SetStatusLine(string line)
    {
        if (statusLine == line) return;

        statusLine = line;
        RefreshUI();
    }

    private string FormatLog(LogEntry log, float alpha)
    {
        string color = log.Type switch
        {
            GameLogType.System  => "#00FF00",
            GameLogType.Combat  => "#FFAA00",
            GameLogType.Exp     => "#00FFFF",
            GameLogType.Skill   => "#AA66FF",
            GameLogType.Loot    => "#FFD700",
            GameLogType.Warning => "#FFFF00",
            GameLogType.Error   => "#FF4444",
            GameLogType.Debug   => "#888888",
            GameLogType.None    => "#888888",
            _               => "#FFFFFF"
        };

        Color c;
        
        ColorUtility.TryParseHtmlString(color, out c);

        c.a = alpha;

        string hex = ColorUtility.ToHtmlStringRGBA(c);

        string message = log.Message;

        if (log.Count > 1)
            message += $" x{log.Count}";

        if(log.Type == GameLogType.None)
        {
            return $"<color=#{hex}> {message}</color>";
        }
        return $"<color=#{hex}>[{log.Type}] {message}</color>";
        
    }

    /// <summary>
    /// 외부에서 빠른 사용 메서드들
    /// LogManger.instance.System("msg");
    /// </summary>
    /// <param name="msg"></param>
    #region Shortcut Methods

    public void System(string msg)
    {
        AddLog(GameLogType.System, msg);
    }

    public void Combat(string msg)
    {
        AddLog(GameLogType.Combat, msg);
    }

    /// <summary>
    /// 경험치를 먹었다. 짧은 시간에 여러 번 먹으면 한 줄로 묶어서 낸다 —
    /// 낱개로 흘리면 로그 창이 경험치로만 가득 찬다.
    ///
    /// 예전에는 ExpManager 라는 별도 오브젝트가 하던 일이다. 로그를 묶는 것은
    /// 로그 창의 일이라 여기로 들여왔다.
    /// </summary>
    public void ExpGained(int amount)
    {
        if (amount <= 0) return;

        pendingExp += amount;

        // 처음이면 기본 뜸, 이어서 먹으면 조금씩 늘려 한 줄로 더 모은다
        if (expTimer <= 0f) expTimer = ExpFlushDelay;
        else expTimer = Mathf.Min(expTimer + ExpExtendStep, ExpMaxDelay);
    }

    /// <summary>모아둔 경험치를 뜸이 다하면 한 줄로 흘린다.</summary>
    void Update()
    {
        if (pendingExp <= 0) return;

        expTimer -= Time.deltaTime;
        if (expTimer > 0f) return;

        Exp($"EXP GAINED (+{pendingExp})");
        pendingExp = 0;
    }

    public void Exp(string msg)
    {
        AddLog(GameLogType.Exp, msg);
    }

    public void Skill(string msg)
    {
        AddLog(GameLogType.Skill, msg);
    }

    public void Warning(string msg)
    {
        AddLog(GameLogType.Warning, msg);
    }

    public void Error(string msg)
    {
        AddLog(GameLogType.Error, msg);
    }

    public void DebugLog(string msg)
    {
        AddLog(GameLogType.Debug, msg);
    }

    public void NoneLog(string msg)
    {
        AddLog(GameLogType.None, msg);
    }

    #endregion
}