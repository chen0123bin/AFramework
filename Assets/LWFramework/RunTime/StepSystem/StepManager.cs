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

        private Dictionary<string, StepGraph> m_Graphs;
        private StepGraph m_CurrentGraph;
        private StepNode m_CurrentNode;
        private StepContext m_Context;
        private StepXmlLoader m_Loader;
        private StepActionFactory m_ActionFactory;
        private List<string> m_History;
        private List<string> m_ForwardHistory;
        private List<IStepBaselineStateAction> m_BaselineStateActions;
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
            m_ForwardHistory = new List<string>();
            m_BaselineStateActions = new List<IStepBaselineStateAction>();
            IsRunning = false;
            m_HasAllStepsCompleted = false;
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
                NotifyActionChanged(false);
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
                //SwitchToNode(nextNodes[0], true);
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
            m_ForwardHistory.Clear();
            CaptureBaselineSnapshots(graph);
            IsRunning = true;
            m_HasAllStepsCompleted = false;
            m_CurrentNode.Enter(m_Context);
            m_LastActionName = string.Empty;
            if (OnNodeEnter != null)
            {
                OnNodeEnter(node.Id);
            }
            if (OnNodeChanged != null)
            {
                OnNodeChanged(node.Id);
            }
            NotifyActionChanged(true);
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
            m_ForwardHistory.Clear();
            m_BaselineStateActions.Clear();
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
            m_CurrentNode.ApplyRemaining(m_Context);

            if (m_ForwardHistory.Count > 0)
            {
                string redoNodeId = m_ForwardHistory[m_ForwardHistory.Count - 1];
                m_ForwardHistory.RemoveAt(m_ForwardHistory.Count - 1);
                SwitchToNode(redoNodeId, true);
                return;
            }

            m_ForwardHistory.Clear();
            List<string> nextNodes = m_CurrentGraph.GetNextNodeIds(m_CurrentNode.Id, m_Context);
            if (nextNodes.Count == 0)
            {
                LWDebug.LogWarning("当前节点没有可前进目标: " + m_CurrentNode.Id);
                return;
            }
            SwitchToNode(nextNodes[0], true);
        }

        public void Forward(string requiredTag)
        {
            if (!IsRunning || m_CurrentGraph == null || m_CurrentNode == null)
            {
                LWDebug.LogWarning("步骤系统未运行，无法前进");
                return;
            }

            m_CurrentNode.ApplyRemaining(m_Context);

            if (m_ForwardHistory.Count > 0)
            {
                string redoNodeId = m_ForwardHistory[m_ForwardHistory.Count - 1];
                m_ForwardHistory.RemoveAt(m_ForwardHistory.Count - 1);
                SwitchToNode(redoNodeId, true);
                return;
            }

            m_ForwardHistory.Clear();
            List<string> nextNodes = m_CurrentGraph.GetNextNodeIds(m_CurrentNode.Id, m_Context, requiredTag);
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
        public void Backward()
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

            m_CurrentNode.ApplyRemaining(m_Context);

            string currentNodeId = m_History[m_History.Count - 1];
            m_History.RemoveAt(m_History.Count - 1);
            m_ForwardHistory.Add(currentNodeId);
            string targetNodeId = m_History[m_History.Count - 1];
            SwitchToNodeWithRebuild(targetNodeId, m_History.Count - 1);
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

            if (string.IsNullOrEmpty(targetNodeId))
            {
                LWDebug.LogWarning("目标节点ID为空，无法跳转");
                HandleJumpFailed("目标节点ID为空");
                return;
            }

            if (m_CurrentNode.Id == targetNodeId)
            {
                return;
            }

            StepNode targetNode = m_CurrentGraph.GetNode(targetNodeId);
            if (targetNode == null)
            {
                LWDebug.LogError("步骤节点不存在: " + targetNodeId);
                HandleJumpFailed("步骤节点不存在: " + targetNodeId);
                return;
            }

            int targetHistoryIndex = m_History.LastIndexOf(targetNodeId);
            if (targetHistoryIndex >= 0 && targetHistoryIndex < m_History.Count - 1)
            {
                m_CurrentNode.ApplyRemaining(m_Context);

                m_ForwardHistory.Clear();
                for (int i = m_History.Count - 1; i > targetHistoryIndex; i--)
                {
                    m_ForwardHistory.Add(m_History[i]);
                }
                m_History.RemoveRange(targetHistoryIndex + 1, m_History.Count - (targetHistoryIndex + 1));
                SwitchToNodeWithRebuild(targetNodeId, targetHistoryIndex);
                return;
            }

            m_ForwardHistory.Clear();

            List<string> path = m_CurrentGraph.FindPath(m_CurrentNode.Id, targetNodeId, m_Context);
            bool hasPath = path != null && path.Count > 0;

            // 关键：先补齐当前节点未完成动作
            m_CurrentNode.ApplyRemaining(m_Context);

            if (hasPath)
            {
                for (int i = 1; i < path.Count - 1; i++)
                {
                    string nodeId = path[i];
                    StepNode node = m_CurrentGraph.GetNode(nodeId);
                    if (node == null)
                    {
                        continue;
                    }
                    node.Apply(m_Context);
                    m_History.Add(node.Id);
                    if (OnJumpProgress != null)
                    {
                        OnJumpProgress(node.Id);
                    }
                }
            }
            else
            {
                LWDebug.LogWarning("找不到跳转路径，已直接跳转到目标节点: " + targetNodeId);
                HandleJumpFailed("找不到跳转路径，已直接跳转: " + targetNodeId);
            }

            SwitchToNode(targetNodeId, true);
        }

        public void JumpTo(string targetNodeId, string requiredTag)
        {
            if (!IsRunning || m_CurrentGraph == null || m_CurrentNode == null)
            {
                LWDebug.LogWarning("步骤系统未运行，无法跳转");
                HandleJumpFailed("步骤系统未运行");
                return;
            }

            if (string.IsNullOrEmpty(targetNodeId))
            {
                LWDebug.LogWarning("目标节点ID为空，无法跳转");
                HandleJumpFailed("目标节点ID为空");
                return;
            }

            if (m_CurrentNode.Id == targetNodeId)
            {
                return;
            }

            StepNode targetNode = m_CurrentGraph.GetNode(targetNodeId);
            if (targetNode == null)
            {
                LWDebug.LogError("步骤节点不存在: " + targetNodeId);
                HandleJumpFailed("步骤节点不存在: " + targetNodeId);
                return;
            }

            int targetHistoryIndex = m_History.LastIndexOf(targetNodeId);
            if (targetHistoryIndex >= 0 && targetHistoryIndex < m_History.Count - 1)
            {
                m_CurrentNode.ApplyRemaining(m_Context);

                m_ForwardHistory.Clear();
                for (int i = m_History.Count - 1; i > targetHistoryIndex; i--)
                {
                    m_ForwardHistory.Add(m_History[i]);
                }
                m_History.RemoveRange(targetHistoryIndex + 1, m_History.Count - (targetHistoryIndex + 1));
                SwitchToNodeWithRebuild(targetNodeId, targetHistoryIndex);
                return;
            }

            m_ForwardHistory.Clear();

            List<string> path = m_CurrentGraph.FindPath(m_CurrentNode.Id, targetNodeId, m_Context, requiredTag);
            bool hasPath = path != null && path.Count > 0;

            m_CurrentNode.ApplyRemaining(m_Context);

            if (hasPath)
            {
                for (int i = 1; i < path.Count - 1; i++)
                {
                    string nodeId = path[i];
                    StepNode node = m_CurrentGraph.GetNode(nodeId);
                    if (node == null)
                    {
                        continue;
                    }
                    node.Apply(m_Context);
                    m_History.Add(node.Id);
                    if (OnJumpProgress != null)
                    {
                        OnJumpProgress(node.Id);
                    }
                }
            }
            else
            {
                LWDebug.LogWarning("找不到跳转路径，已直接跳转到目标节点: " + targetNodeId);
                HandleJumpFailed("找不到跳转路径，已直接跳转: " + targetNodeId);
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
            return m_CurrentGraph.GetNextNodeIds(m_CurrentNode.Id, m_Context);
        }

        public List<string> GetAvailableNextNodes(string requiredTag)
        {
            if (m_CurrentGraph == null || m_CurrentNode == null)
            {
                return new List<string>();
            }
            return m_CurrentGraph.GetNextNodeIds(m_CurrentNode.Id, m_Context, requiredTag);
        }

        public string SaveContextToJson()
        {
            if (m_Context == null)
            {
                return string.Empty;
            }
            return m_Context.ToJson();
        }

        public void LoadContextFromJson(string json)
        {
            if (m_Context == null)
            {
                return;
            }
            m_Context.LoadFromJson(json);
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
            m_LastActionName = string.Empty;
            if (OnNodeEnter != null)
            {
                OnNodeEnter(node.Id);
            }
            if (OnNodeChanged != null)
            {
                OnNodeChanged(node.Id);
            }
            NotifyActionChanged(true);
        }

        private void SwitchToNodeWithRebuild(string nodeId, int historyIndex)
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

            RebuildStateToHistoryIndex(historyIndex);

            m_CurrentNode = node;
            m_CurrentNode.Enter(m_Context);
            m_HasAllStepsCompleted = false;
            m_LastActionName = string.Empty;
            if (OnNodeEnter != null)
            {
                OnNodeEnter(node.Id);
            }
            if (OnNodeChanged != null)
            {
                OnNodeChanged(node.Id);
            }
            NotifyActionChanged(true);
        }

        private void CaptureBaselineSnapshots(StepGraph graph)
        {
            m_Context.Clear();
            m_BaselineStateActions.Clear();
            if (graph == null)
            {
                return;
            }

            List<StepNode> nodes = graph.GetAllNodesSnapshot();
            for (int i = 0; i < nodes.Count; i++)
            {
                StepNode node = nodes[i];
                if (node == null)
                {
                    continue;
                }
                List<BaseStepAction> actions = node.GetActionsSnapshot();
                for (int j = 0; j < actions.Count; j++)
                {
                    BaseStepAction action = actions[j];
                    IStepBaselineStateAction baselineStateAction = action as IStepBaselineStateAction;
                    if (baselineStateAction == null)
                    {
                        continue;
                    }
                    m_BaselineStateActions.Add(baselineStateAction);
                    baselineStateAction.CaptureBaselineState();
                }
            }
        }

        /// <summary>
        /// 确保已捕获基线快照（用于异常流程的兜底）
        /// </summary>
        private void EnsureBaselineSnapshotsCaptured()
        {
            if (m_CurrentGraph == null)
            {
                return;
            }
            if (m_BaselineStateActions.Count > 0)
            {
                return;
            }
            CaptureBaselineSnapshots(m_CurrentGraph);
        }

        /// <summary>
        /// 恢复到基线快照
        /// </summary>
        private void RestoreBaselineSnapshots()
        {
            for (int i = 0; i < m_BaselineStateActions.Count; i++)
            {
                IStepBaselineStateAction action = m_BaselineStateActions[i];
                if (action == null)
                {
                    continue;
                }
                action.RestoreBaselineState();
            }
        }

        private void RebuildStateToHistoryIndex(int historyIndex)
        {
            if (m_CurrentGraph == null)
            {
                return;
            }

            EnsureBaselineSnapshotsCaptured();

            if (historyIndex <= 0)
            {
                RestoreBaselineSnapshots();
                m_Context.Clear();
                return;
            }

            RestoreBaselineSnapshots();
            m_Context.Clear();

            for (int i = 0; i < historyIndex; i++)
            {
                if (i < 0 || i >= m_History.Count)
                {
                    continue;
                }
                string nodeId = m_History[i];
                StepNode node = m_CurrentGraph.GetNode(nodeId);
                if (node == null)
                {
                    continue;
                }
                node.Apply(m_Context);
            }
        }


        private void NotifyActionChanged(bool force)
        {
            if (OnActionChanged == null)
            {
                return;
            }
            string actionName = m_CurrentNode != null ? m_CurrentNode.GetCurrentActionName() : string.Empty;
            if (force || m_LastActionName != actionName)
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
