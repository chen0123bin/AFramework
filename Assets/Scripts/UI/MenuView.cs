using LWUI;
using UnityEngine.UI;
using UnityEngine;
using LWCore;

[UIViewData("Assets/0Res/Prefabs/UI/MenuView.prefab", (int)FindType.Name, "LWFramework/Canvas/Normal")]
public class MenuView : BaseUIView
{

    public const string EVENT_CLOSE = "MenuView.Close";
    public const string EVENT_OPEN_ASSETS_DEMO = "MenuView.OpenAssetsDemo";
    public const string EVENT_OPEN_UI_DEMO = "MenuView.OpenUIDemo";
    public const string EVENT_OPEN_PROCEDURE_DEMO = "MenuView.OpenProcedureDemo";
    public const string EVENT_OPEN_AUDIO_DEMO = "MenuView.OpenAudioDemo";

    [UIElement("PnlTop/BtnClose")]
    private Button m_BtnClose;
    [UIElement("PnlCard/ScrMenu/Viewport/Content/BtnAssetsDemo")]
    private Button m_BtnAssetsDemo;
    [UIElement("PnlCard/ScrMenu/Viewport/Content/BtnUIDemo")]
    private Button m_BtnUIDemo;
    [UIElement("PnlCard/ScrMenu/Viewport/Content/BtnProcedureDemo")]
    private Button m_BtnProcedureDemo;
    [UIElement("PnlCard/ScrMenu/Viewport/Content/BtnAudioDemo")]
    private Button m_BtnAudioDemo;

    /// <summary>
    /// 创建并初始化菜单界面：将按钮点击转为事件派发。
    /// </summary>
    /// <param name="gameObject">界面实例对象</param>
    public override void CreateView(GameObject gameObject)
    {
        base.CreateView(gameObject);
        m_BtnClose.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_CLOSE);
        });

        m_BtnAssetsDemo.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_OPEN_ASSETS_DEMO);
        });

        m_BtnUIDemo.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_OPEN_UI_DEMO);
        });

        m_BtnProcedureDemo.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_OPEN_PROCEDURE_DEMO);
        });

        m_BtnAudioDemo.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_OPEN_AUDIO_DEMO);
        });

    }
}
