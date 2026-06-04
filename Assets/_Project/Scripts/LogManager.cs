using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum LogType
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
    public LogType Type;
    public string Message;
    public int Count = 1;

    public LogEntry(LogType type, string message)
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

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AddLog(LogType type, string message)
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

        logText.text = sb.ToString();
    }

    private string FormatLog(LogEntry log, float alpha)
    {
        string color = log.Type switch
        {
            LogType.System  => "#00FF00",
            LogType.Combat  => "#FFAA00",
            LogType.Exp     => "#00FFFF",
            LogType.Skill   => "#AA66FF",
            LogType.Loot    => "#FFD700",
            LogType.Warning => "#FFFF00",
            LogType.Error   => "#FF4444",
            LogType.Debug   => "#888888",
            LogType.None    => "#888888",
            _               => "#FFFFFF"
        };

        Color c;
        
        ColorUtility.TryParseHtmlString(color, out c);

        c.a = alpha;

        string hex = ColorUtility.ToHtmlStringRGBA(c);

        string message = log.Message;

        if (log.Count > 1)
            message += $" x{log.Count}";

        if(log.Type == LogType.None)
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
        AddLog(LogType.System, msg);
    }

    public void Combat(string msg)
    {
        AddLog(LogType.Combat, msg);
    }

    public void Exp(string msg)
    {
        AddLog(LogType.Exp, msg);
    }

    public void Skill(string msg)
    {
        AddLog(LogType.Skill, msg);
    }

    public void Warning(string msg)
    {
        AddLog(LogType.Warning, msg);
    }

    public void Error(string msg)
    {
        AddLog(LogType.Error, msg);
    }

    public void DebugLog(string msg)
    {
        AddLog(LogType.Debug, msg);
    }

    public void NoneLog(string msg)
    {
        AddLog(LogType.None, msg);
    }

    #endregion
}