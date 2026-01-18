using LWCore;
using LWUI;
using UnityEngine;
using UnityEngine.UI;

[UIViewData("Assets/0Res/Prefabs/UI/ProcedureDemoView.prefab", (int)FindType.Name, "LWFramework/Canvas/Normal")]
public class ProcedureDemoView : BaseUIView
{
    public const string EVENT_CLOSE = "CloseProcedureDemoView";
    public const string EVENT_START_PROCEDURE = "ProcedureDemoView.StartProcedure";
    public const string EVENT_GET_PROCEDURE_STATE = "ProcedureDemoView.GetProcedureState";
    public const string EVENT_SWITCH_PROCEDURE_LOGIN = "ProcedureDemoView.SwitchProcedureLogin";
    public const string EVENT_SWITCH_PROCEDURE_MAIN = "ProcedureDemoView.SwitchProcedureMain";
    public const string EVENT_CLEAR_PROCEDURE = "ProcedureDemoView.ClearProcedure";

    [UIElement("PnlTop/BtnClose")]
    private Button m_BtnClose;

    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnStartProcedure")]
    private Button m_BtnStartProcedure;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnGetProcedureState")]
    private Button m_BtnGetProcedureState;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnSwitchProcedureLogin")]
    private Button m_BtnSwitchProcedureLogin;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnSwitchProcedureMain")]
    private Button m_BtnSwitchProcedureMain;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnClearProcedure")]
    private Button m_BtnClearProcedure;

    /// <summary>
    /// 创建并初始化流程演示界面：将按钮点击转为事件派发。
    /// </summary>
    /// <param name="gameObject">界面实例对象</param>
    public override void CreateView(GameObject gameObject)
    {
        base.CreateView(gameObject);

        m_BtnClose.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_CLOSE);
        });

        m_BtnStartProcedure.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_START_PROCEDURE);
        });

        m_BtnGetProcedureState.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_GET_PROCEDURE_STATE);
        });

        m_BtnSwitchProcedureLogin.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_SWITCH_PROCEDURE_LOGIN);
        });

        m_BtnSwitchProcedureMain.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_SWITCH_PROCEDURE_MAIN);
        });

        m_BtnClearProcedure.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_CLEAR_PROCEDURE);
        });
    }
}

