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
    Debug
}
public class LogEntry
{
    public LogType Type;
    public string Message;

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
    [SerializeField] private int maxLines = 6;

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

        foreach (var log in logs)
        {
            sb.AppendLine(FormatLog(log));
        }

        logText.text = sb.ToString();
    }

    private string FormatLog(LogEntry log)
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
            _               => "#FFFFFF"
        };

        return $"<color={color}>[{log.Type}]</color> {log.Message}";
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

    #endregion
}