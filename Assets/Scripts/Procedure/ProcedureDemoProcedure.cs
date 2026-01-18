using LWCore;
using LWFMS;
using LWUI;

[FSMTypeAttribute("Procedure", false)]
public class ProcedureDemoProcedure : BaseFSMState
{
    public override void OnInit()
    {
    }

    /// <summary>
    /// 进入流程演示：打开界面并注册事件监听。
    /// </summary>
    /// <param name="lastState">上一个状态</param>
    public override void OnEnter(BaseFSMState lastState)
    {
        ManagerUtility.EventMgr.AddListener(ProcedureDemoView.EVENT_CLOSE, OnClose);
        ManagerUtility.EventMgr.AddListener(ProcedureDemoView.EVENT_START_PROCEDURE, OnStartProcedure);
        ManagerUtility.EventMgr.AddListener(ProcedureDemoView.EVENT_GET_PROCEDURE_STATE, OnGetProcedureState);
        ManagerUtility.EventMgr.AddListener(ProcedureDemoView.EVENT_SWITCH_PROCEDURE_LOGIN, OnSwitchProcedureLogin);
        ManagerUtility.EventMgr.AddListener(ProcedureDemoView.EVENT_SWITCH_PROCEDURE_MAIN, OnSwitchProcedureMain);
        ManagerUtility.EventMgr.AddListener(ProcedureDemoView.EVENT_CLEAR_PROCEDURE, OnClearProcedure);

        ManagerUtility.UIMgr.OpenView<ProcedureDemoView>();
    }

    /// <summary>
    /// 离开流程演示：关闭界面并移除事件监听。
    /// </summary>
    /// <param name="nextState">下一个状态</param>
    public override void OnLeave(BaseFSMState nextState)
    {
        ManagerUtility.EventMgr.RemoveListener(ProcedureDemoView.EVENT_CLOSE, OnClose);
        ManagerUtility.EventMgr.RemoveListener(ProcedureDemoView.EVENT_START_PROCEDURE, OnStartProcedure);
        ManagerUtility.EventMgr.RemoveListener(ProcedureDemoView.EVENT_GET_PROCEDURE_STATE, OnGetProcedureState);
        ManagerUtility.EventMgr.RemoveListener(ProcedureDemoView.EVENT_SWITCH_PROCEDURE_LOGIN, OnSwitchProcedureLogin);
        ManagerUtility.EventMgr.RemoveListener(ProcedureDemoView.EVENT_SWITCH_PROCEDURE_MAIN, OnSwitchProcedureMain);
        ManagerUtility.EventMgr.RemoveListener(ProcedureDemoView.EVENT_CLEAR_PROCEDURE, OnClearProcedure);

        ManagerUtility.UIMgr.CloseView<ProcedureDemoView>();
    }

    public override void OnTermination()
    {
    }

    public override void OnUpdate()
    {
    }

    /// <summary>
    /// 关闭流程演示界面并返回菜单流程。
    /// </summary>
    private void OnClose()
    {
        ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<MenuProcedure>();
    }

    /// <summary>
    /// 切换到 StartProcedure（用于演示流程状态切换）。
    /// </summary>
    private void OnStartProcedure()
    {
        ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<StartProcedure>();
    }

    /// <summary>
    /// 输出当前流程状态信息。
    /// </summary>
    private void OnGetProcedureState()
    {
        BaseFSMState currentState = ManagerUtility.FSMMgr.GetFSMProcedure().CurrentState;
        string stateName = currentState != null ? currentState.GetType().Name : "null";
        LWDebug.Log("当前 Procedure 状态: " + stateName);
    }

    /// <summary>
    /// 切换到登录流程。
    /// </summary>
    private void OnSwitchProcedureLogin()
    {
        ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<LoginProcedure>();
    }

    /// <summary>
    /// 切换到主菜单流程。
    /// </summary>
    private void OnSwitchProcedureMain()
    {
        ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<MenuProcedure>();
    }

    /// <summary>
    /// 清空当前流程的参数字典（安全演示，不破坏状态机结构）。
    /// </summary>
    private void OnClearProcedure()
    {
        BaseFSMState currentState = ManagerUtility.FSMMgr.GetFSMProcedure().CurrentState;
        if (currentState == null)
        {
            LWDebug.LogWarning("当前没有流程状态，无法清空参数");
            return;
        }

        currentState.Param.Clear();
        LWDebug.Log("已清空当前流程的 Param 参数: " + currentState.GetType().Name);
    }
}

