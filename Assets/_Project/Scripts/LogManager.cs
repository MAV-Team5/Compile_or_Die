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

    [Header("Settings")]
    [SerializeField] private int maxLines = 10;

    private readonly Queue<LogEntry> logs = new();

    // 로그 맨 아래 흰색으로 고정되는 상태줄. 체력·레벨·킬 수 표시용
    private string statusLine;

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