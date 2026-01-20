using System;
using System.Collections.Generic;
using LWCore;

namespace LWStep
{
    /// <summary>
    /// 步骤管理器（XML数据驱动）
    /// </summary>
    public class StepManager : IManager, IStepManager
    {
        public bool IsRunning { get; private set; }
        public string CurrentNodeId { get { return m_CurrentNode != null ? m_CurrentNode.Id : string.Empty; } }

        public event Action<string> OnNodeEnter;
        public event Action<string> OnNodeLeave;
        public event Action<string> OnNodeChanged;
        public event Action<string> OnActionChanged;
        public event Action<string> OnJumpProgress;
        public event Action<string> OnJumpFailed;
        public event Action OnAllStepsCompleted;

        public StepApplyStrategy ApplyStrategy { get; set; }

        private Dictionary<string, StepGraph> m_Graphs;
        private StepGraph m_CurrentGraph;
        private StepNode m_CurrentNode;
        private StepContext m_Context;
        private StepXmlLoader m_Loader;
        private StepActionFactory m_ActionFactory;
        private List<string> m_History;
        private string m_LastActionName;
        private bool m_HasAllStepsCompleted;

        /// <summary>
        /// 初始化管理器
        /// </summary>
        public void Init()
        {
            m_Graphs = new Dictionary<string, StepGraph>();
            m_Context = new StepContext();
            m_Loader = new StepXmlLoader();
            m_ActionFactory = new StepActionFactory();
            m_History = new List<string>();
            IsRunning = false;
            m_HasAllStepsCompleted = false;
            ApplyStrategy = StepApplyStrategy.SkipWithDefault;
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        public void Update()
        {
            if (!IsRunning || m_CurrentNode == null)
            {
                return;
            }

            bool isActionChanged;
            bool isCompleted = m_CurrentNode.Update(out isActionChanged);
            if (isActionChanged)
            {
                string actionName = m_CurrentNode.GetCurrentActionName();
                if (m_LastActionName != actionName)
                {
                    m_LastActionName = actionName;
                    if (OnActionChanged != null)
                    {
                        OnActionChanged(actionName);
                    }
                }
            }

            if (isCompleted)
            {
                List<string> nextNodes = m_CurrentGraph.GetNextNodeIds(m_CurrentNode.Id);
                if (nextNodes.Count == 0)
                {
                    if (!m_HasAllStepsCompleted)
                    {
                        m_HasAllStepsCompleted = true;
                        if (OnAllStepsCompleted != null)
                        {
                            OnAllStepsCompleted();
                        }
                    }
                    return;
                }
                SwitchToNode(nextNodes[0], true);
            }
        }


        /// <summary>
        /// 加载并解析XML，构建步骤图
        /// </summary>
        public void LoadGraph(string xmlAssetPath)
        {
            StepGraph graph = m_Loader.LoadFromAsset(xmlAssetPath, m_ActionFactory);
            if (graph == null)
            {
                LWDebug.LogError("步骤图加载失败: " + xmlAssetPath);
                return;
            }
            if (string.IsNullOrEmpty(graph.Id))
            {
                LWDebug.LogError("步骤图缺少ID");
                return;
            }
            m_Graphs[graph.Id] = graph;
        }

        /// <summary>
        /// 启动步骤图
        /// </summary>
        public void Start(string graphId, string startNodeId = null)
        {
            StepGraph graph;
            if (!m_Graphs.TryGetValue(graphId, out graph))
            {
                LWDebug.LogError("步骤图不存在: " + graphId);
                return;
            }

            string targetNodeId = string.IsNullOrEmpty(startNodeId) ? graph.StartNodeId : startNodeId;
            StepNode node = graph.GetNode(targetNodeId);
            if (node == null)
            {
                LWDebug.LogError("步骤节点不存在: " + targetNodeId);
                return;
            }

            m_CurrentGraph = graph;
            m_CurrentNode = node;
            m_History.Clear();
            m_History.Add(node.Id);
            IsRunning = true;
            m_HasAllStepsCompleted = false;
            m_CurrentNode.Enter(m_Context);
            m_LastActionName = m_CurrentNode.GetCurrentActionName();
            if (OnNodeEnter != null)
            {
                OnNodeEnter(node.Id);
            }
            if (OnNodeChanged != null)
            {
                OnNodeChanged(node.Id);
            }
            NotifyActionChanged();
        }

        /// <summary>
        /// 停止步骤图并清理上下文
        /// </summary>
        public void Stop()
        {
            if (m_CurrentNode != null)
            {
                m_CurrentNode.Leave();
            }
            m_CurrentGraph = null;
            m_CurrentNode = null;
            m_History.Clear();
            m_Context.Clear();
            IsRunning = false;
            m_HasAllStepsCompleted = false;
        }

        /// <summary>
        /// 重启当前图
        /// </summary>
        public void Restart()
        {
            if (m_CurrentGraph == null)
            {
                LWDebug.LogWarning("当前没有可重启的步骤图");
                return;
            }
            Start(m_CurrentGraph.Id, m_CurrentGraph.StartNodeId);
        }

        /// <summary>
        /// 重置上下文
        /// </summary>
        public void ResetContext()
        {
            m_Context.Clear();
        }

        /// <summary>
        /// 前进到下一个节点
        /// </summary>
        public void Forward()
        {
            if (!IsRunning || m_CurrentGraph == null || m_CurrentNode == null)
            {
                LWDebug.LogWarning("步骤系统未运行，无法前进");
                return;
            }

            // 关键：前进前补齐当前节点未完成动作
            string failReason;
            if (!m_CurrentNode.ApplyRemainingWithStrategy(m_Context, ApplyStrategy, out failReason))
            {
                LWDebug.LogWarning("前进被阻止: " + failReason);
                return;
            }
            List<string> nextNodes = m_CurrentGraph.GetNextNodeIds(m_CurrentNode.Id);
            if (nextNodes.Count == 0)
            {
                LWDebug.LogWarning("当前节点没有可前进目标: " + m_CurrentNode.Id);
                return;
            }
            SwitchToNode(nextNodes[0], true);
        }

        /// <summary>
        /// 后退到上一个节点
        /// </summary>
        public void Back()
        {
            if (!IsRunning || m_CurrentNode == null)
            {
                LWDebug.LogWarning("步骤系统未运行，无法后退");
                return;
            }
            if (m_History.Count <= 1)
            {
                LWDebug.LogWarning("没有可后退的历史节点");
                return;
            }

            m_CurrentNode.Leave();
            m_History.RemoveAt(m_History.Count - 1);
            string targetNodeId = m_History[m_History.Count - 1];
            SwitchToNode(targetNodeId, false);
        }

        /// <summary>
        /// 跳转到目标节点（补齐中间步骤结果）
        /// </summary>
        public void JumpTo(string targetNodeId)
        {
            if (!IsRunning || m_CurrentGraph == null || m_CurrentNode == null)
            {
                LWDebug.LogWarning("步骤系统未运行，无法跳转");
                HandleJumpFailed("步骤系统未运行");
                return;
            }

            List<string> path = m_CurrentGraph.FindPath(m_CurrentNode.Id, targetNodeId);
            if (path == null || path.Count == 0)
            {
                LWDebug.LogError("找不到跳转路径: " + targetNodeId);
                HandleJumpFailed("找不到跳转路径: " + targetNodeId);
                return;
            }

            // 关键：先补齐当前节点未完成动作
            string applyFailReason;
            if (!m_CurrentNode.ApplyRemainingWithStrategy(m_Context, ApplyStrategy, out applyFailReason))
            {
                HandleJumpFailed("当前节点补齐失败: " + applyFailReason);
                return;
            }

            // 关键：对中间节点执行快速应用，生成过程结果
            for (int i = 1; i < path.Count - 1; i++)
            {
                string nodeId = path[i];
                StepNode node = m_CurrentGraph.GetNode(nodeId);
                if (node == null)
                {
                    LWDebug.LogError("跳转路径节点不存在: " + nodeId);
                    HandleJumpFailed("跳转路径节点不存在: " + nodeId);
                    return;
                }
                string nodeFailReason;
                if (!node.ApplyWithStrategy(m_Context, ApplyStrategy, out nodeFailReason))
                {
                    HandleJumpFailed("跳转补齐失败: " + nodeId + " " + nodeFailReason);
                    return;
                }
                m_History.Add(node.Id);
                if (OnJumpProgress != null)
                {
                    OnJumpProgress(node.Id);
                }
            }

            SwitchToNode(targetNodeId, true);
        }

        /// <summary>
        /// 获取当前节点的可前进目标集合
        /// </summary>
        public List<string> GetAvailableNextNodes()
        {
            if (m_CurrentGraph == null || m_CurrentNode == null)
            {
                return new List<string>();
            }
            return m_CurrentGraph.GetNextNodeIds(m_CurrentNode.Id);
        }

        private void SwitchToNode(string nodeId, bool appendHistory)
        {
            StepNode node = m_CurrentGraph.GetNode(nodeId);
            if (node == null)
            {
                LWDebug.LogError("步骤节点不存在: " + nodeId);
                return;
            }

            if (m_CurrentNode != null)
            {
                m_CurrentNode.Leave();
                if (OnNodeLeave != null)
                {
                    OnNodeLeave(m_CurrentNode.Id);
                }
            }

            m_CurrentNode = node;
            if (appendHistory)
            {
                m_History.Add(node.Id);
            }

            m_CurrentNode.Enter(m_Context);
            m_HasAllStepsCompleted = false;
            m_LastActionName = m_CurrentNode.GetCurrentActionName();
            if (OnNodeEnter != null)
            {
                OnNodeEnter(node.Id);
            }
            if (OnNodeChanged != null)
            {
                OnNodeChanged(node.Id);
            }
            NotifyActionChanged();
        }

        private void NotifyActionChanged()
        {
            if (OnActionChanged == null)
            {
                return;
            }
            string actionName = m_CurrentNode != null ? m_CurrentNode.GetCurrentActionName() : string.Empty;
            if (m_LastActionName != actionName)
            {
                m_LastActionName = actionName;
                OnActionChanged(actionName);
            }
        }

        private void HandleJumpFailed(string reason)
        {
            LWDebug.LogWarning("跳转被阻止: " + reason);
            if (OnJumpFailed != null)
            {
                OnJumpFailed(reason);
            }
        }
    }
}
