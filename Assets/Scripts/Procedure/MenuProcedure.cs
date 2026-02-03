using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LWFMS;
using LWCore;
using Cysharp.Threading.Tasks;

[FSMTypeAttribute("Procedure", false)]
public class MenuProcedure : BaseFSMState
{
    /// <summary>
    /// 初始化菜单流程。
    /// </summary>
    public override void OnInit()
    {

    }

    /// <summary>
    /// 进入菜单流程：打开菜单界面并注册按钮事件。
    /// </summary>
    /// <param name="lastState">上一个状态</param>
    public override void OnEnter(BaseFSMState lastState)
    {
        ManagerUtility.EventMgr.AddListener(MenuView.EVENT_CLOSE, OnClose);
        ManagerUtility.EventMgr.AddListener(MenuView.EVENT_OPEN_ASSETS_DEMO, OnOpenAssetsDemo);
        ManagerUtility.EventMgr.AddListener(MenuView.EVENT_OPEN_UI_DEMO, OnOpenUIDemo);
        ManagerUtility.EventMgr.AddListener(MenuView.EVENT_OPEN_PROCEDURE_DEMO, OnOpenProcedureDemo);
        ManagerUtility.EventMgr.AddListener(MenuView.EVENT_OPEN_AUDIO_DEMO, OnOpenAudioDemo);
        ManagerUtility.EventMgr.AddListener(MenuView.EVENT_OPEN_STEP_DEMO, OnOpenStepDemo);

        ManagerUtility.UIMgr.OpenView<MenuView>();

    }

    /// <summary>
    /// 离开菜单流程：注销按钮事件并关闭菜单界面。
    /// </summary>
    /// <param name="nextState">下一个状态</param>
    public override void OnLeave(BaseFSMState nextState)
    {
        ManagerUtility.EventMgr.RemoveListener(MenuView.EVENT_CLOSE, OnClose);
        ManagerUtility.EventMgr.RemoveListener(MenuView.EVENT_OPEN_ASSETS_DEMO, OnOpenAssetsDemo);
        ManagerUtility.EventMgr.RemoveListener(MenuView.EVENT_OPEN_UI_DEMO, OnOpenUIDemo);
        ManagerUtility.EventMgr.RemoveListener(MenuView.EVENT_OPEN_PROCEDURE_DEMO, OnOpenProcedureDemo);
        ManagerUtility.EventMgr.RemoveListener(MenuView.EVENT_OPEN_AUDIO_DEMO, OnOpenAudioDemo);
        ManagerUtility.EventMgr.RemoveListener(MenuView.EVENT_OPEN_STEP_DEMO, OnOpenStepDemo);

        ManagerUtility.UIMgr.CloseView<MenuView>();
    }

    /// <summary>
    /// 流程终止：当前流程无额外清理。
    /// </summary>
    public override void OnTermination()
    {

    }

    /// <summary>
    /// 流程更新：当前流程无轮询逻辑。
    /// </summary>
    public override void OnUpdate()
    {

    }

    /// <summary>
    /// 关闭菜单并返回登录流程。
    /// </summary>
    private void OnClose()
    {
        ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<LoginProcedure>();
    }

    /// <summary>
    /// 进入资源演示流程。
    /// </summary>
    private void OnOpenAssetsDemo()
    {
        ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<AssetsDemoProcedure>();
    }

    /// <summary>
    /// 进入 UI 演示流程。
    /// </summary>
    private void OnOpenUIDemo()
    {
        ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<UIDemoProcedure>();
    }

    /// <summary>
    /// 进入流程演示流程。
    /// </summary>
    private void OnOpenProcedureDemo()
    {
        ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<ProcedureDemoProcedure>();
    }

    /// <summary>
    /// 进入音频演示流程。
    /// </summary>
    private void OnOpenAudioDemo()
    {
        ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<AudioDemoProcedure>();
    }

    /// <summary>
    /// 进入步骤演示流程。
    /// </summary>
    private void OnOpenStepDemo()
    {
        ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<StepProcedure>();
    }
}
