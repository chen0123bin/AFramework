using LWFMS;
using LWCore;
using LWUI;
using UnityEngine;

[FSMTypeAttribute("Procedure", false)]
public class LoginProcedure : BaseFSMState
{
    private const string EVENT_LOGIN_SUBMIT = "Auth.Login.Submit";
    private const string EVENT_LOGIN_CANCEL = "Auth.Login.Cancel";

    /// <summary>
    /// 初始化登录流程。
    /// </summary>
    public override void OnInit()
    {
    }

    /// <summary>
    /// 进入登录流程：打开登录界面并注册事件。
    /// </summary>
    /// <param name="lastState">上一个状态</param>
    public override void OnEnter(BaseFSMState lastState)
    {
        ManagerUtility.EventMgr.AddListener<string, string, bool>(EVENT_LOGIN_SUBMIT, OnLoginSubmit);
        ManagerUtility.EventMgr.AddListener(EVENT_LOGIN_CANCEL, OnLoginCancel);
        ManagerUtility.UIMgr.OpenView<LoginView>();
    }

    /// <summary>
    /// 离开登录流程：关闭界面并移除事件。
    /// </summary>
    /// <param name="nextState">下一个状态</param>
    public override void OnLeave(BaseFSMState nextState)
    {
        ManagerUtility.EventMgr.RemoveListener<string, string, bool>(EVENT_LOGIN_SUBMIT, OnLoginSubmit);
        ManagerUtility.EventMgr.RemoveListener(EVENT_LOGIN_CANCEL, OnLoginCancel);
        ManagerUtility.UIMgr.CloseView<LoginView>();
    }

    /// <summary>
    /// 流程终止时的清理。
    /// </summary>
    public override void OnTermination()
    {

    }

    /// <summary>
    /// 流程更新（当前流程无轮询逻辑）。
    /// </summary>
    public override void OnUpdate()
    {
    }

    /// <summary>
    /// 处理登录提交：示例中做基础校验并进入下一流程。
    /// </summary>
    /// <param name="userName">账号</param>
    /// <param name="password">密码</param>
    /// <param name="isRemember">是否记住账号</param>
    private void OnLoginSubmit(string userName, string password, bool isRemember)
    {
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            LWDebug.LogWarning("账号或密码为空，无法登录");
            return;
        }

        ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<TestProcedure>();
    }

    /// <summary>
    /// 处理取消登录：示例中直接退出应用。
    /// </summary>
    private void OnLoginCancel()
    {
        Application.Quit();
    }
}
