using System.Collections.Generic;
using LWCore;
using LWUI;
using UnityEngine;
using UnityEngine.UI;

[UIViewData("Assets/0Res/Prefabs/UI/ShowcaseView.prefab", (int)FindType.Name, "LWFramework/Canvas/Normal")]
public class ShowcaseView : BaseUIView
{
    public const string EVENT_CLOSE = "CloseShowcaseView";
    public const string EVENT_LOAD_SCENE = "LoadSceneShowcaseView";
    public const string EVENT_INSTANTIATE_CUBE = "InstantiateCubeShowcaseView";
    public const string EVENT_LOAD_RAW_FILE = "LoadRawFileShowcaseView";
    public const string EVENT_DISPATCH_TEST_EVENT = "DispatchTestEventShowcaseView";
    public const string EVENT_OPEN_LOADING = "OpenLoadingShowcaseView";
    public const string EVENT_CLOSE_OTHER_VIEWS = "CloseOtherViewsShowcaseView";
    public const string EVENT_BACK_VIEW = "BackViewShowcaseView";

    private const int MAX_LOG_LINES = 12;

    [UIElement("PnlTop/BtnClose")]
    private Button m_BtnClose;

    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnLoadScene")]
    private Button m_BtnLoadScene;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnInstantiateCube")]
    private Button m_BtnInstantiateCube;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnLoadRawFile")]
    private Button m_BtnLoadRawFile;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnDispatchEvent")]
    private Button m_BtnDispatchEvent;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnOpenLoading")]
    private Button m_BtnOpenLoading;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnCloseOtherViews")]
    private Button m_BtnCloseOtherViews;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnBackView")]
    private Button m_BtnBackView;

    [UIElement("PnlCard/TxtLog")]
    private Text m_TxtLog;

    private readonly List<string> m_LogLines = new List<string>(MAX_LOG_LINES);

    /// 创建并初始化功能展示页（绑定按钮事件并初始化日志）。
    /// </summary>
    /// <param name="gameObject">界面实例对象</param>
    public override void CreateView(GameObject gameObject)
    {
        base.CreateView(gameObject);
        m_BtnClose.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_CLOSE);
        });
        m_BtnLoadScene.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_LOAD_SCENE);
        });
        m_BtnInstantiateCube.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_INSTANTIATE_CUBE);
        });
        m_BtnLoadRawFile.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_LOAD_RAW_FILE);
        });
        m_BtnDispatchEvent.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_DISPATCH_TEST_EVENT);
        });
        m_BtnOpenLoading.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_OPEN_LOADING);
        });
        m_BtnCloseOtherViews.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_CLOSE_OTHER_VIEWS);
        });
        m_BtnBackView.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_BACK_VIEW);
        });
    }

    /// <summary>
    /// 清理界面（移除事件绑定并销毁界面实例）。
    /// </summary>
    public override void ClearView()
    {
        base.ClearView();
    }

    public void SetActionsInteractable(bool isInteractable)
    {
        if (m_BtnLoadScene != null)
        {
            m_BtnLoadScene.interactable = isInteractable;
        }
        if (m_BtnInstantiateCube != null)
        {
            m_BtnInstantiateCube.interactable = isInteractable;
        }
        if (m_BtnLoadRawFile != null)
        {
            m_BtnLoadRawFile.interactable = isInteractable;
        }
        if (m_BtnDispatchEvent != null)
        {
            m_BtnDispatchEvent.interactable = isInteractable;
        }
        if (m_BtnOpenLoading != null)
        {
            m_BtnOpenLoading.interactable = isInteractable;
        }
        if (m_BtnCloseOtherViews != null)
        {
            m_BtnCloseOtherViews.interactable = isInteractable;
        }
        if (m_BtnBackView != null)
        {
            m_BtnBackView.interactable = isInteractable;
        }
    }

    public void ClearLog()
    {
        m_LogLines.Clear();
        RefreshLogText();
    }
    /// <summary>
    /// 追加一行日志并刷新日志文本显示。
    /// </summary>
    /// <param name="message">要追加的日志内容</param>
    public void AppendLog(string message)
    {
        if (m_TxtLog == null)
        {
            return;
        }

        string time = System.DateTime.Now.ToString("HH:mm:ss");
        string line = "[" + time + "] " + message;
        m_LogLines.Add(line);

        while (m_LogLines.Count > MAX_LOG_LINES)
        {
            m_LogLines.RemoveAt(0);
        }

        RefreshLogText();
    }

    private void RefreshLogText()
    {
        if (m_TxtLog == null)
        {
            return;
        }

        string logText = "日志：\n" + string.Join("\n", m_LogLines.ToArray());
        m_TxtLog.text = logText;
    }
}
