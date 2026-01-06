using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class LWDebug
{
    private static bool m_WriteLog = false;
    private static bool m_LwGuiLog = false;
    private static int m_LogLevel;
    public static void Log(object info, LogColor color = LogColor._default)
    {
        if (IsSelectLogType(LogType.Log))
        {
            MethodBase member = m_LwGuiLog && m_WriteLog ? new StackFrame(1).GetMethod() : null;
            Debug.Log(GetLogInfo(info, color, member));
        }
    }

    public static void LogError(object info, LogColor color = LogColor._default)
    {
        if (IsSelectLogType(LogType.Error))
        {
            MethodBase member = m_LwGuiLog && m_WriteLog ? new StackFrame(1).GetMethod() : null;
            Debug.LogError(GetLogInfo(info, color, member));
        }

    }
    public static void LogWarning(object info, LogColor color = LogColor._default)
    {
        if (IsSelectLogType(LogType.Warning))
        {
            MethodBase member = m_LwGuiLog && m_WriteLog ? new StackFrame(1).GetMethod() : null;
            Debug.LogWarning(GetLogInfo(info, color, member));
        }
    }
    static string GetLogInfo(object info, LogColor color, MethodBase member = null)
    {
        string retInfo = "";
        if (member != null)
        {

            if (LWDebugMono.Instance.writeLog)
            {
                retInfo = $"[{member.ReflectedType}]:::{info}";
            }
            else
            {
                retInfo = $"[{member.ReflectedType}]:::{string.Format(GetHexByColor(color), info)}";
            }
        }
        else
        {
            if (LWDebugMono.Instance.writeLog)
            {
                retInfo = info.ToString();
            }
            else
            {
                retInfo = string.Format(GetHexByColor(color), info);
            }
        }
        return retInfo;

    }
    public static void SetLogConfig(bool lwGuiLog, int logLevel, bool writeLog)
    {
        m_WriteLog = writeLog;
        m_LwGuiLog = lwGuiLog;
        m_LogLevel = logLevel;
        LWDebugMono.Instance.lwGuiLog = lwGuiLog;
        LWDebugMono.Instance.logLevel = logLevel;
        LWDebugMono.Instance.writeLog = writeLog;
    }
    //判断是否选择了该枚举值
    static bool IsSelectLogType(LogType _logType)
    {

        int logTypeValue = _logType == LogType.Exception ? (int)LogType.Error : (int)_logType;
        if (logTypeValue <= m_LogLevel)
        {
            return true;
        }
        else
        {
            return false;
        }


    }

    static string GetHexByColor(LogColor color)
    {
        string defaultHex = "<color=#000000ff>{0}</color>";

        switch (color)
        {
            case LogColor.red:
                defaultHex = "<color=#ff0000>{0}</color>";
                break;
            case LogColor.yellow:
                defaultHex = "<color=#ffff00ff>{0}</color>";
                break;
            case LogColor.blue:
                defaultHex = "<color=#0000ffff>{0}</color>";
                break;
            case LogColor.green:
                defaultHex = "<color=#008000ff>{0}</color>";
                break;
            case LogColor.cyan:
                defaultHex = "<color=#00ffffff>{0}</color>";
                break;
            case LogColor.grey:
                defaultHex = "<color=#808080ff>{0}</color>";
                break;
            case LogColor.white:
                defaultHex = "<color=#ffffffff>{0}</color>";
                break;
            case LogColor._default:

                defaultHex = "{0}";
                break;
            default:
                break;
        }
        return defaultHex;
    }
}
public enum LogColor
{
    red, yellow, blue, green, cyan, grey, white, _default
}
public enum LWLogLevel
{
    None = -1, Error = 0, Assert = 1, Warning = 2, All = 3
}
