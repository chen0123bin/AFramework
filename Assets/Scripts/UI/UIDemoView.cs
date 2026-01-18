using LWCore;
using LWUI;
using UnityEngine;
using UnityEngine.UI;

[UIViewData("Assets/0Res/Prefabs/UI/UIDemoView.prefab", (int)FindType.Name, "LWFramework/Canvas/Normal")]
public class UIDemoView : BaseUIView
{
    public const string EVENT_CLOSE = "CloseUIDemoView";
    public const string EVENT_OPEN_LOGIN_VIEW = "UIDemoView.OpenLoginView";
    public const string EVENT_OPEN_LOADING_VIEW = "UIDemoView.OpenLoadingView";
    public const string EVENT_OPEN_FUNCTION_SHOWCASE_VIEW = "UIDemoView.OpenFunctionShowcaseView";
    public const string EVENT_CLOSE_TOP_VIEW = "UIDemoView.CloseTopView";
    public const string EVENT_CLOSE_ALL_VIEWS = "UIDemoView.CloseAllViews";
    public const string EVENT_BACK_VIEW = "UIDemoView.BackView";
    public const string EVENT_OPEN_DIALOG = "UIDemoView.OpenDialog";

    [UIElement("PnlTop/BtnClose")]
    private Button m_BtnClose;

    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnOpenLoginView")]
    private Button m_BtnOpenLoginView;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnOpenLoadingView")]
    private Button m_BtnOpenLoadingView;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnOpenFunctionShowcaseView")]
    private Button m_BtnOpenFunctionShowcaseView;

    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnCloseTopView")]
    private Button m_BtnCloseTopView;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnCloseAllViews")]
    private Button m_BtnCloseAllViews;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnBackView")]
    private Button m_BtnBackView;

    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnOpenDialog")]
    private Button m_BtnOpenDialog;

    /// <summary>
    /// 创建并初始化 UI 演示界面：将按钮点击转为事件派发。
    /// </summary>
    /// <param name="gameObject">界面实例对象</param>
    public override void CreateView(GameObject gameObject)
    {
        base.CreateView(gameObject);

        m_BtnClose.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_CLOSE);
        });

        m_BtnOpenLoginView.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_OPEN_LOGIN_VIEW);
        });

        m_BtnOpenLoadingView.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_OPEN_LOADING_VIEW);
        });

        m_BtnOpenFunctionShowcaseView.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_OPEN_FUNCTION_SHOWCASE_VIEW);
        });

        m_BtnCloseTopView.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_CLOSE_TOP_VIEW);
        });

        m_BtnCloseAllViews.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_CLOSE_ALL_VIEWS);
        });

        m_BtnBackView.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_BACK_VIEW);
        });

        m_BtnOpenDialog.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_OPEN_DIALOG);
        });
    }
}

