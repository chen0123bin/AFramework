using Cysharp.Threading.Tasks;
using LWCore;
using LWStep;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class StepDemoRunner : MonoBehaviour
{
#if UNITY_EDITOR
    private const string PREVIEW_XML_PATH_KEY = "LWStep.StepEditor.Preview.XmlPath";
    private const string PREVIEW_START_NODE_ID_KEY = "LWStep.StepEditor.Preview.StartNodeId";
    private const string PREVIEW_JUMP_NODE_ID_KEY = "LWStep.StepEditor.Preview.JumpNodeId";
    private const string PREVIEW_REQUIRED_TAG_KEY = "LWStep.StepEditor.Preview.RequiredTag";
    private const string PREVIEW_ENABLED_KEY = "LWStep.StepEditor.Preview.Enabled";
#endif

    [SerializeField] private string m_XmlPath = "Assets/0Res/RawFiles/StepStage4Test.xml";
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
    private string m_PreviewJumpNodeId;
    private string m_PreviewRequiredTag;
    private string m_PreviewStartNodeId;
    private bool m_UsePreviewConfig;

    /// <summary>
    /// Demo入口：读取预览配置并启动步骤系统
    /// </summary>
    private async void Start()
    {
#if UNITY_EDITOR
        m_UsePreviewConfig = EditorPrefs.GetBool(PREVIEW_ENABLED_KEY, false);
        if (m_UsePreviewConfig)
        {
            string previewXmlPath = EditorPrefs.GetString(PREVIEW_XML_PATH_KEY, string.Empty);
            m_PreviewStartNodeId = EditorPrefs.GetString(PREVIEW_START_NODE_ID_KEY, string.Empty);
            m_PreviewJumpNodeId = EditorPrefs.GetString(PREVIEW_JUMP_NODE_ID_KEY, string.Empty);
            m_PreviewRequiredTag = EditorPrefs.GetString(PREVIEW_REQUIRED_TAG_KEY, string.Empty);

            if (!string.IsNullOrEmpty(previewXmlPath))
            {
                m_XmlPath = previewXmlPath;
            }
        }
#endif

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
        string graphName = Path.GetFileNameWithoutExtension(m_XmlPath);
        if (string.IsNullOrEmpty(graphName))
        {
            LWDebug.LogError("步骤Demo：图名称为空，请检查XML路径");
            return;
        }
        if (m_UsePreviewConfig && !string.IsNullOrEmpty(m_PreviewStartNodeId))
        {
            stepManager.Start(graphName, m_PreviewStartNodeId);
        }
        else
        {
            stepManager.Start(graphName);
        }

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

        if (m_UsePreviewConfig && !string.IsNullOrEmpty(m_PreviewJumpNodeId))
        {
            if (string.IsNullOrEmpty(m_PreviewRequiredTag))
            {
                stepManager.JumpTo(m_PreviewJumpNodeId);
            }
            else
            {
                stepManager.JumpTo(m_PreviewJumpNodeId, m_PreviewRequiredTag);
            }
        }

        // for (int i = 0; i < m_AutoForwardCount; i++)
        // {
        //     await UniTask.Delay(m_AutoForwardDelayMs);
        //     stepManager.Forward();
        // }
    }

    /// <summary>
    /// 节点切换事件回调
    /// </summary>
    private void OnNodeChanged(string nodeId)
    {
        AddEventLog("节点切换：" + nodeId);
    }

    /// <summary>
    /// 节点进入事件回调
    /// </summary>
    private void OnNodeEnter(string nodeId)
    {
        AddEventLog("节点进入：" + nodeId);
    }

    /// <summary>
    /// 节点离开事件回调
    /// </summary>
    private void OnNodeLeave(string nodeId)
    {
        AddEventLog("节点离开：" + nodeId);
    }

    /// <summary>
    /// 动作切换事件回调
    /// </summary>
    private void OnActionChanged(string actionId)
    {
        AddEventLog("动作切换：" + actionId);
    }

    /// <summary>
    /// 跳转补齐过程事件回调
    /// </summary>
    private void OnJumpProgress(string nodeId)
    {
        AddEventLog("跳转补齐：" + nodeId);
    }

    /// <summary>
    /// 跳转失败事件回调
    /// </summary>
    private void OnJumpFailed(string reason)
    {
        AddEventLog("跳转失败：" + reason);
    }

    /// <summary>
    /// 记录并输出事件日志
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
    /// 等待步骤系统与资源系统初始化完成
    /// </summary>
    private async UniTask WaitForStepReady()
    {
        while (ManagerUtility.StepMgr == null || ManagerUtility.AssetsMgr == null || !ManagerUtility.AssetsMgr.IsInitialized)
        {
            await UniTask.Yield();
        }
    }

    /// <summary>
    /// 快捷键控制：空格跳转、左右方向前进/后退、S保存上下文、L恢复上下文
    /// </summary>
    private void Update()
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

    /// <summary>
    /// 组件销毁时解绑步骤事件，避免重复订阅
    /// </summary>
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

    /// <summary>
    /// 全部步骤完成事件回调
    /// </summary>
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
