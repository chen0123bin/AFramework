using Cysharp.Threading.Tasks;
using LWCore;
using LWStep;
using System.Collections.Generic;
using UnityEngine;

public class StepDemoRunner : MonoBehaviour
{
    [SerializeField] private string m_XmlPath = "Assets/0Res/RawFiles/StepStage4Test.xml";
    [SerializeField] private string m_GraphId = "step_stage4_demo";
    [SerializeField] private int m_AutoForwardCount = 3;
    [SerializeField] private int m_AutoForwardDelayMs = 3000;
    [SerializeField] private string m_JumpTargetNodeId = "step_priority_gate";
    [SerializeField] private string m_JumpRequiredTag = "normal";
    [SerializeField] private string m_ForwardRequiredTag = "vip";
    [SerializeField] private bool m_ApplyPresetContextOnStart = true;
    [SerializeField] private string m_PresetMode = "A";
    [SerializeField] private int m_PresetScore = 5;
    [SerializeField] private bool m_PresetIsVip = true;

    private List<string> m_EventLogs;
    private string m_SavedContextJson;

    private async void Start()
    {
        await WaitForStepReady();

        m_EventLogs = new List<string>();
        IStepManager stepManager = ManagerUtility.StepMgr;
        stepManager.OnAllStepsCompleted += OnAllStepsCompleted;
        stepManager.OnNodeEnter += OnNodeEnter;
        stepManager.OnNodeLeave += OnNodeLeave;
        stepManager.OnNodeChanged += OnNodeChanged;
        stepManager.OnActionChanged += OnActionChanged;
        stepManager.OnJumpProgress += OnJumpProgress;
        stepManager.OnJumpFailed += OnJumpFailed;
        stepManager.LoadGraph(m_XmlPath);
        stepManager.Start(m_GraphId);

        if (m_ApplyPresetContextOnStart)
        {
            StepContext presetContext = new StepContext();
            presetContext.SetValue("mode", m_PresetMode);
            presetContext.SetValue("score", m_PresetScore);
            presetContext.SetValue("isVip", m_PresetIsVip);
            string presetJson = presetContext.ToJson();
            stepManager.LoadContextFromJson(presetJson);
            LWDebug.Log("步骤Demo：已加载预设上下文");
        }

        // for (int i = 0; i < m_AutoForwardCount; i++)
        // {
        //     await UniTask.Delay(m_AutoForwardDelayMs);
        //     stepManager.Forward();
        // }
    }

    private void OnNodeChanged(string nodeId)
    {
        AddEventLog("节点切换：" + nodeId);
    }

    private void OnNodeEnter(string nodeId)
    {
        AddEventLog("节点进入：" + nodeId);
    }

    private void OnNodeLeave(string nodeId)
    {
        AddEventLog("节点离开：" + nodeId);
    }

    private void OnActionChanged(string actionId)
    {
        AddEventLog("动作切换：" + actionId);
    }

    private void OnJumpProgress(string nodeId)
    {
        AddEventLog("跳转补齐：" + nodeId);
    }

    private void OnJumpFailed(string reason)
    {
        AddEventLog("跳转失败：" + reason);
    }

    private void AddEventLog(string message)
    {
        if (m_EventLogs != null)
        {
            m_EventLogs.Add(message);
        }
        LWDebug.Log(message);
    }

    private async UniTask WaitForStepReady()
    {
        while (ManagerUtility.StepMgr == null || ManagerUtility.AssetsMgr == null || !ManagerUtility.AssetsMgr.IsInitialized)
        {
            await UniTask.Yield();
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (string.IsNullOrEmpty(m_JumpRequiredTag))
            {
                ManagerUtility.StepMgr.JumpTo(m_JumpTargetNodeId);
            }
            else
            {
                ManagerUtility.StepMgr.JumpTo(m_JumpTargetNodeId, m_JumpRequiredTag);
            }
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ManagerUtility.StepMgr.Backward();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (string.IsNullOrEmpty(m_ForwardRequiredTag))
            {
                ManagerUtility.StepMgr.Forward();
            }
            else
            {
                ManagerUtility.StepMgr.Forward(m_ForwardRequiredTag);
            }
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            m_SavedContextJson = ManagerUtility.StepMgr.SaveContextToJson();
            LWDebug.Log("步骤Demo：已保存上下文");
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (!string.IsNullOrEmpty(m_SavedContextJson))
            {
                ManagerUtility.StepMgr.LoadContextFromJson(m_SavedContextJson);
                LWDebug.Log("步骤Demo：已恢复上下文");
            }
        }
    }
    private void OnDestroy()
    {
        IStepManager stepManager = ManagerUtility.StepMgr;
        if (stepManager == null)
        {
            return;
        }

        stepManager.OnAllStepsCompleted -= OnAllStepsCompleted;
        stepManager.OnNodeEnter -= OnNodeEnter;
        stepManager.OnNodeLeave -= OnNodeLeave;
        stepManager.OnNodeChanged -= OnNodeChanged;
        stepManager.OnActionChanged -= OnActionChanged;
        stepManager.OnJumpProgress -= OnJumpProgress;
        stepManager.OnJumpFailed -= OnJumpFailed;
    }

    private void OnAllStepsCompleted()
    {
        AddEventLog("所有步骤完成");
        if (m_EventLogs == null)
        {
            return;
        }
        string combined = string.Join(" | ", m_EventLogs);
        LWDebug.Log("步骤事件序列：" + combined);
    }
}
