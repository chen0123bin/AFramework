using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using System.Collections.Concurrent;
using LWCore;

[DefaultExecutionOrder(10000)]
public class LWDebugMono : MonoSingleton<LWDebugMono>
{

    //错误详情
    public List<LogInfo> m_logAll = new List<LogInfo>();
    public List<LogInfo> m_logLog = new List<LogInfo>();
    public List<LogInfo> m_logWarning = new List<LogInfo>();
    public List<LogInfo> m_logError = new List<LogInfo>();
    public List<LogInfo> m_CurrLog = new List<LogInfo>();
    //是否显示错误窗口
    private bool m_IsVisible = false;
    //窗口显示区域
    private Rect m_WindowRect = new Rect(0, 40, Screen.width, Screen.height - 40);
    //窗口滚动区域
    private Vector2 m_scrollPositionText = Vector2.zero;
    //字体大小
    private int fontSize = 30;
    private GUISkin skin;

    //文件的路径
    private string m_LogSaveDirPath;
    //[InfoBox("设置项3210-1,3:All,2:Warning,1:Assert,0:Error,-1:None不显示")]
    public int logLevel = 3;
    public bool writeLog = false;
    public bool lwGuiLog = true;
    public int maxLogCount = 2000;

    private float m_LastToggleClickTime = 0f;

    private struct PendingLog
    {
        public string condition;
        public string stackTrace;
        public LogType type;
        public string nowDate;
    }

    private readonly ConcurrentQueue<PendingLog> m_PendingLogs = new ConcurrentQueue<PendingLog>();
    private StreamWriter m_StreamWriter;
    private string m_CurrentFileFullPath;

    void OnEnable()
    {
        SetLogSaveDir();
        skin = Resources.Load<GUISkin>("LogGUISkin");
        m_CurrLog = m_logAll;
        Application.logMessageReceivedThreaded += OnLogMessageReceivedThreaded;

    }
    void OnDisable()
    {
        Application.logMessageReceivedThreaded -= OnLogMessageReceivedThreaded;
        CloseLogFileWriter();
    }

    private void OnApplicationQuit()
    {
        CloseLogFileWriter();
    }
    string FileFullName
    {
        get
        {
            return "LWLogInfo " + DateTime.Now.ToString("yyyyMMdd-HH") + ".txt";
        }
    }

    void SetLogSaveDir()
    {

        if (m_LogSaveDirPath == null)
        {
            // m_LogFilePath = Application.persistentDataPath + "/";
            if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                string rootPath = Application.dataPath;
                rootPath = rootPath.Substring(0, rootPath.LastIndexOf("/"));
                m_LogSaveDirPath = rootPath + "/LWLogs/";
            }
            else
            {
                m_LogSaveDirPath = Application.persistentDataPath + "/LWLogs/";
            }
            if (!Directory.Exists(m_LogSaveDirPath))
            {
                Directory.CreateDirectory(m_LogSaveDirPath);
            }
        }
    }

    private void OnLogMessageReceivedThreaded(string condition, string stackTrace, LogType type)
    {
        string nowDate = DateTime.Now.ToString("HH:mm:ss");
        m_PendingLogs.Enqueue(new PendingLog
        {
            condition = condition,
            stackTrace = stackTrace,
            type = type,
            nowDate = nowDate
        });
    }

    private void Update()
    {
        DrainPendingLogs(200);
    }

    private void DrainPendingLogs(int maxCount)
    {
        if (maxCount <= 0)
        {
            return;
        }

        int processedCount = 0;
        while (processedCount < maxCount && m_PendingLogs.TryDequeue(out PendingLog pendingLog))
        {
            processedCount++;

            if (!IsSelectLogType(pendingLog.type))
            {
                continue;
            }

            bool enableStack = true;
            if (pendingLog.type == LogType.Log)
            {
                enableStack = false;
            }

            string stackTrace = enableStack ? pendingLog.stackTrace : string.Empty;
            LogInfo logInfo = new LogInfo(pendingLog.type, pendingLog.condition, stackTrace, pendingLog.nowDate);

            switch (pendingLog.type)
            {
                case LogType.Warning:
                    m_logWarning.Add(logInfo);
                    TrimLogs(m_logWarning);
                    break;
                case LogType.Log:
                    m_logLog.Add(logInfo);
                    TrimLogs(m_logLog);
                    break;
                case LogType.Error:
                case LogType.Exception:
                    m_logError.Add(logInfo);
                    TrimLogs(m_logError);
                    break;
            }

            m_logAll.Add(logInfo);
            TrimLogs(m_logAll);

            if (writeLog)
            {
                if (enableStack)
                {
                    WriteToFile(FileFullName, logInfo.ToString());
                }
                else
                {
                    WriteToFile(FileFullName, logInfo.GetCondition());
                }
            }
        }
    }

    private bool IsSelectLogType(LogType logType)
    {
        if (logLevel < 0)
        {
            return false;
        }

        int logTypeValue = logType == LogType.Exception ? (int)LogType.Error : (int)logType;
        return logTypeValue <= logLevel;
    }

    private void TrimLogs(List<LogInfo> logs)
    {
        if (maxLogCount <= 0)
        {
            return;
        }

        int removeCount = logs.Count - maxLogCount;
        if (removeCount <= 0)
        {
            return;
        }

        logs.RemoveRange(0, removeCount);
    }

    void WriteToFile(string fileFullName, string content)
    {
        string fileFullPath = Path.Combine(m_LogSaveDirPath, fileFullName);
        EnsureLogFileWriter(fileFullPath);
        if (m_StreamWriter == null)
        {
            return;
        }

        m_StreamWriter.WriteLine(content);
        m_StreamWriter.Flush();
    }

    private void EnsureLogFileWriter(string fileFullPath)
    {
        if (string.Equals(m_CurrentFileFullPath, fileFullPath, StringComparison.Ordinal) && m_StreamWriter != null)
        {
            return;
        }

        CloseLogFileWriter();

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fileFullPath));
            FileStream fileStream = new FileStream(fileFullPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            m_StreamWriter = new StreamWriter(fileStream, Encoding.UTF8);
            m_CurrentFileFullPath = fileFullPath;
        }
        catch
        {
            m_StreamWriter = null;
            m_CurrentFileFullPath = null;
        }
    }

    private void CloseLogFileWriter()
    {
        try
        {
            if (m_StreamWriter != null)
            {
                m_StreamWriter.Flush();
                m_StreamWriter.Dispose();
                m_StreamWriter = null;
            }
        }
        catch
        {
        }
        finally
        {
            m_CurrentFileFullPath = null;
        }
    }
    /////////////////////////界面Log日志///////////////////////////////////////

    void OnGUI()
    {
        if (!lwGuiLog || logLevel == -1)
        {
            return;
        }
        if (GUI.Button(new Rect(Screen.width - 70, 0, 70, 70), "", skin.customStyles[4]))
        {
            float now = Time.realtimeSinceStartup;
            if (now - m_LastToggleClickTime <= 0.45f)
            {
                m_IsVisible = !m_IsVisible;
                m_LastToggleClickTime = 0f;
            }
            else
            {
                m_LastToggleClickTime = now;
            }
        }
        if (!m_IsVisible)
        {
            return;
        }
        m_WindowRect = GUILayout.Window(0, m_WindowRect, ConsoleWindow, "Console");
    }

    //日志窗口
    void ConsoleWindow(int windowID)
    {
        GUILayout.BeginHorizontal();
        skin.button.fontSize = fontSize;
        skin.textArea.fontSize = fontSize;
        if (GUILayout.Button("Clear", skin.button, GUILayout.MaxWidth(120), GUILayout.MaxHeight(35)))
        {
            m_logAll.Clear();
            m_logLog.Clear();
            m_logWarning.Clear();
            m_logError.Clear();
            m_CurrLog = m_logAll;
        }
        if (GUILayout.Button("Log", skin.button, GUILayout.MaxWidth(120), GUILayout.MaxHeight(35)))
        {
            if (m_CurrLog == m_logLog)
                m_CurrLog = m_logAll;
            else
                m_CurrLog = m_logLog;
        }
        if (GUILayout.Button("Warning", skin.button, GUILayout.MaxWidth(120), GUILayout.MaxHeight(35)))
        {
            if (m_CurrLog == m_logWarning)
                m_CurrLog = m_logAll;
            else
                m_CurrLog = m_logWarning;
        }
        if (GUILayout.Button("Error", skin.button, GUILayout.MaxWidth(120), GUILayout.MaxHeight(35)))
        {
            if (m_CurrLog == m_logError)
                m_CurrLog = m_logAll;
            else
                m_CurrLog = m_logError;
        }
        if (GUILayout.Button("Quit", skin.button, GUILayout.MaxWidth(120), GUILayout.MaxHeight(35)))
        {
            Application.Quit();
        }
        if (GUILayout.Button("Device", skin.button, GUILayout.MaxWidth(120), GUILayout.MaxHeight(35)))
        {
            Debug.Log(SystemInfo.graphicsDeviceID + "-" + SystemInfo.graphicsDeviceVendorID + Application.productName);
        }
        GUILayout.EndHorizontal();
        m_scrollPositionText = GUILayout.BeginScrollView(m_scrollPositionText, skin.horizontalScrollbar, skin.verticalScrollbar);
        for (int i = 0; i < m_CurrLog.Count; i++)
        {

            Color currentColor = GUI.contentColor;
            switch (m_CurrLog[i].type)
            {
                case LogType.Warning:
                    GUI.contentColor = Color.white;
                    break;
                case LogType.Assert:
                    GUI.contentColor = Color.black;
                    break;
                case LogType.Log:
                    GUI.contentColor = Color.green;
                    break;
                case LogType.Error:
                case LogType.Exception:
                    GUI.contentColor = Color.red;
                    break;
            }
            if (GUILayout.Button(m_CurrLog[i].condition, skin.textArea))
            {
                m_CurrLog[i].isOpen = !m_CurrLog[i].isOpen; ;
            }
            if (m_CurrLog[i].isOpen)
            {
                GUILayout.Label(m_CurrLog[i].stackTrace, skin.box);
            }

            GUI.contentColor = currentColor;
        }


        GUILayout.EndScrollView();
    }


}
