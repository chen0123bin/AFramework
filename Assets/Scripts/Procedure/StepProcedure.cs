using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LWCore;
using LWFMS;
using Cysharp.Threading.Tasks;
using LWStep;
using System.IO;

[FSMTypeAttribute("Procedure", false)]
public class StepProcedure : BaseFSMState
{
    private List<string> m_EventLogs;
    private string m_SavedContextJson;

    private string m_XmlPath = "Assets/0Res/RawFiles/StepStage4Test.xml";
    private string m_JumpTargetNodeId = "step_priority_gate";
    private bool m_ApplyPresetContextOnStart = true;
    private string m_PresetMode = "A";
    private int m_PresetScore = 5;
    private bool m_PresetIsVip = true;

    public override void OnInit()
    {

    }

    public override void OnEnter(BaseFSMState lastState)
    {
        ManagerUtility.EventMgr.AddListener(StepView.EVENT_CLOSE, OnClose);
        ManagerUtility.EventMgr.AddListener(StepView.EVENT_PREV, OnPrev);
        ManagerUtility.EventMgr.AddListener(StepView.EVENT_NEXT, OnNext);
        ManagerUtility.EventMgr.AddListener<string>(StepView.EVENT_JUMP, OnJumpByView);

        m_EventLogs = new List<string>();
        // 初始化步骤系统并开始流程。
        ManagerUtility.StepMgr.OnAllStepsCompleted += OnAllStepsCompleted;
        ManagerUtility.StepMgr.OnNodeEnter += OnNodeEnter;
        ManagerUtility.StepMgr.OnNodeLeave += OnNodeLeave;
        ManagerUtility.StepMgr.OnNodeChanged += OnNodeChanged;
        ManagerUtility.StepMgr.OnActionChanged += OnActionChanged;
        ManagerUtility.StepMgr.OnJumpProgress += OnJumpProgress;
        ManagerUtility.StepMgr.OnJumpFailed += OnJumpFailed;

        ManagerUtility.StepMgr.LoadGraph(m_XmlPath);
        string graphName = Path.GetFileNameWithoutExtension(m_XmlPath);
        if (string.IsNullOrEmpty(graphName))
        {
            LWDebug.LogError("步骤流程：图名称为空，请检查XML路径");
            return;
        }

        ManagerUtility.StepMgr.Start(graphName);

        //收集所有步骤节点ID
        List<StepNode> nodeList = ManagerUtility.StepMgr.GetAllDisplayNodes();
        StepView stepView = ManagerUtility.UIMgr.OpenView<StepView>(nodeList);
        SyncAllStepStatuses(stepView, nodeList);
        if (m_ApplyPresetContextOnStart)
        {
            StepContext presetContext = new StepContext();
            presetContext.SetValue("mode", m_PresetMode);
            presetContext.SetValue("score", m_PresetScore);
            presetContext.SetValue("isVip", m_PresetIsVip);

            string presetJson = presetContext.ToJson();
            ManagerUtility.StepMgr.LoadContextFromJson(presetJson);

            LWDebug.Log("步骤流程：已加载预设上下文");
        }
    }
    public override void OnLeave(BaseFSMState nextState)
    {
        ManagerUtility.EventMgr.RemoveListener(StepView.EVENT_CLOSE, OnClose);
        ManagerUtility.EventMgr.RemoveListener(StepView.EVENT_PREV, OnPrev);
        ManagerUtility.EventMgr.RemoveListener(StepView.EVENT_NEXT, OnNext);
        ManagerUtility.EventMgr.RemoveListener<string>(StepView.EVENT_JUMP, OnJumpByView);

        IStepManager stepManager = ManagerUtility.StepMgr;
        if (stepManager != null)
        {
            stepManager.OnAllStepsCompleted -= OnAllStepsCompleted;
            stepManager.OnNodeEnter -= OnNodeEnter;
            stepManager.OnNodeLeave -= OnNodeLeave;
            stepManager.OnNodeChanged -= OnNodeChanged;
            stepManager.OnActionChanged -= OnActionChanged;
            stepManager.OnJumpProgress -= OnJumpProgress;
            stepManager.OnJumpFailed -= OnJumpFailed;
        }
        ManagerUtility.UIMgr.CloseView<StepView>();
    }

    public override void OnTermination()
    {

    }

    public override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //ManagerUtility.StepMgr.JumpTo(m_JumpTargetNodeId);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            m_SavedContextJson = ManagerUtility.StepMgr.GetContextToJson();
            LWDebug.Log("步骤流程：已保存上下文");
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (!string.IsNullOrEmpty(m_SavedContextJson))
            {
                ManagerUtility.StepMgr.LoadContextFromJson(m_SavedContextJson);
                LWDebug.Log("步骤流程：已恢复上下文");
            }
        }
    }

    /// <summary>
    /// 关闭步骤演示流程并返回菜单流程。
    /// </summary>
    private void OnClose()
    {
        ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<MenuProcedure>();
    }



    /// <summary>
    /// 步骤系统：节点切换事件。
    /// </summary>
    private void OnNodeChanged(string nodeId)
    {
        AddEventLog("节点切换：" + nodeId);
        SyncAllStepStatuses();
    }

    /// <summary>
    /// 步骤系统：节点进入事件。
    /// </summary>
    private void OnNodeEnter(string nodeId)
    {
        AddEventLog("节点进入：" + nodeId);
        SyncAllStepStatuses();
    }

    /// <summary>
    /// 步骤系统：节点离开事件。
    /// </summary>
    private void OnNodeLeave(string nodeId)
    {
        AddEventLog("节点离开：" + nodeId);
        SyncAllStepStatuses();
    }

    /// <summary>
    /// 步骤系统：动作切换事件。
    /// </summary>
    private void OnActionChanged(string actionId)
    {
        AddEventLog("动作切换：" + actionId);
    }

    /// <summary>
    /// 步骤系统：跳转补齐事件。
    /// </summary>
    private void OnJumpProgress(string nodeId)
    {
        AddEventLog("跳转补齐：" + nodeId);
        SyncAllStepStatuses();
    }

    /// <summary>
    /// 步骤系统：跳转失败事件。
    /// </summary>
    private void OnJumpFailed(string reason)
    {
        AddEventLog("跳转失败：" + reason);
    }

    /// <summary>
    /// 步骤系统：全部完成事件。
    /// </summary>
    private void OnAllStepsCompleted()
    {
        AddEventLog("所有步骤完成");
        SyncAllStepStatuses();
        if (m_EventLogs == null)
        {
            return;
        }
        string combined = string.Join(" | ", m_EventLogs);
        LWDebug.Log("步骤事件序列：" + combined);
    }

    /// <summary>
    /// 记录步骤事件日志。
    /// </summary>
    private void AddEventLog(string message)
    {
        if (m_EventLogs != null)
        {
            m_EventLogs.Add(message);
        }
        LWDebug.Log(message);
    }

    /// <summary>
    /// 界面事件：上一条。
    /// </summary>
    private void OnPrev()
    {
        ManagerUtility.StepMgr.Backward();
    }

    /// <summary>
    /// 界面事件：下一条。
    /// </summary>
    private void OnNext()
    {
        ManagerUtility.StepMgr.Forward();
    }

    /// <summary>
    /// 界面事件：跳转到指定步骤。
    /// </summary>
    private void OnJumpByView(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            return;
        }
        ManagerUtility.StepMgr.JumpTo(nodeId);
    }

    private void SyncAllStepStatuses()
    {
        StepView stepView = ManagerUtility.UIMgr.GetView<StepView>();
        List<StepNode> nodeList = ManagerUtility.StepMgr.GetAllNodes();
        SyncAllStepStatuses(stepView, nodeList);
    }

    private void SyncAllStepStatuses(StepView stepView, List<StepNode> nodeList)
    {
        if (stepView == null || !stepView.IsOpen || nodeList == null || nodeList.Count == 0)
        {
            return;
        }

        for (int i = 0; i < nodeList.Count; i++)
        {
            StepNode node = nodeList[i];
            stepView.UpdateNodeStatus(node.Id, node.Status);
        }
    }
}
