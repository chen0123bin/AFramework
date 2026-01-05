using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;

[DefaultExecutionOrder(10000)]
public class LWDebugMono : MonoBehaviour
{
    private static LWDebugMono instance = null;
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

    private static GameObject m_LogObject;
    private static bool m_CanFindInstance = true;
    public static LWDebugMono GetInstance()
    {
        if (instance == null && m_CanFindInstance)
        {

            instance = FindObjectOfType<LWDebugMono>();
            if (instance == null)
            {
                m_LogObject = new GameObject();
                m_LogObject.name = typeof(LWDebugMono).Name;
                instance = m_LogObject.AddComponent<LWDebugMono>();
                DontDestroyOnLoad(m_LogObject);
            }
            ;
        }

        return instance;
    }
    void OnEnable()
    {
        m_CanFindInstance = true;
        SetLogSaveDir();
        skin = Resources.Load<GUISkin>("LogGUISkin");
        m_CurrLog = m_logAll;
        Application.logMessageReceivedThreaded += OnLogMessageReceivedThreaded;

    }
    void OnDisable()
    {
        m_CanFindInstance = false;
        Application.logMessageReceivedThreaded -= OnLogMessageReceivedThreaded;
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
        //LogInfo logInfo = new LogInfo(type, condition, stackTrace, DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss"));
        LogInfo logInfo = new LogInfo(type, condition, stackTrace, DateTime.Now.ToString("HH:mm:ss"));
        bool enableStack = true;
        switch (type)
        {
            case LogType.Warning:
                m_logWarning.Add(logInfo);
                break;
            case LogType.Log:
                m_logLog.Add(logInfo);
                enableStack = false;
                break;
            case LogType.Error:
            case LogType.Exception:
                m_logError.Add(logInfo);
                break;
        }
        m_logAll.Add(logInfo);
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
    void WriteToFile(string fileFullName, string content)
    {
        string fileFullPath = Path.Combine(m_LogSaveDirPath, fileFullName);
        FileInfo file = new FileInfo(fileFullPath);

        if (file != null)
        {
            //StreamWriter sw;
            //sw = file.AppendText();
            ////写入信息
            //sw.WriteLine(content);
            //sw.Flush();
            //sw.Close();
            //file.Refresh();

            using (StreamWriter streamWriter = new StreamWriter(new FileStream(fileFullPath, FileMode.Append), Encoding.UTF8))
            {
                streamWriter.WriteLine(content);
                streamWriter.Flush();
                streamWriter.Close();
            }
            file.Refresh();
        }


    }
    /////////////////////////界面Log日志///////////////////////////////////////

    void OnGUI()
    {
        if (!lwGuiLog || logLevel == -1)
        {
            return;
        }
        if (GUI.Button(new Rect(Screen.width - 80, 30, 50, 50), "", skin.customStyles[4]))
        {
            m_IsVisible = !m_IsVisible;
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
