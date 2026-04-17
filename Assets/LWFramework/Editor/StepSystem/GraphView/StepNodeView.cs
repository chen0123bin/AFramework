using System;
using System.Collections.Generic;
using LWStep;
using LWStep.Editor.Presentation;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace LWStep.Editor
{
    public class StepNodeView : Node
    {
        private const string NODE_CARD_CLASS = "step-node-card";
        private const string NODE_RUNNING_CLASS = "step-node-running";
        private const string NODE_COMPLETED_CLASS = "step-node-completed";
        private const string NODE_TRAIL_CLASS = "step-node-trail";
        private const string NODE_WARNING_CLASS = "step-node-warning";
        private const string NODE_ERROR_CLASS = "step-node-error";

        private StepGraphView m_GraphView;
        private StepEditorNodeData m_Data;
        private Port m_InputPort;
        private Port m_OutputPort;
        private EdgeConnector<Edge> m_InputEdgeConnector;
        private EdgeConnector<Edge> m_OutputEdgeConnector;
        private VisualElement m_MetadataContainer;
        private Label m_SubtitleLabel;
        private VisualElement m_BadgeContainer;
        private VisualElement m_SummaryContainer;
        private bool m_IsDragging;
        private Vector2 m_DownNodePosition;
        private Vector2 m_Offset;
        public Action<StepNodeView> DragEnded;

        public StepEditorNodeData Data
        {
            get { return m_Data; }
        }

        public Port InputPort
        {
            get { return m_InputPort; }
        }

        public Port OutputPort
        {
            get { return m_OutputPort; }
        }

        /// <summary>
        /// 创建节点视图并绑定编辑器数据
        /// </summary>
        public StepNodeView(StepEditorNodeData data)
        {
            m_GraphView = GetFirstAncestorOfType<StepGraphView>();
            m_Data = data;
            title = data.Id;
            viewDataKey = data.Id;

            m_InputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            m_InputPort.portName = "In";
            inputContainer.Add(m_InputPort);

            m_OutputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            m_OutputPort.portName = "Out";
            outputContainer.Add(m_OutputPort);

            BuildPresentationContainer();
            RegisterCollapseStateSync();
            SetPosition(new Rect(data.Position, new Vector2(180.0f, 120.0f)));
            AddToClassList(NODE_CARD_CLASS);

            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RefreshPorts();
        }

        /// <summary>
        /// 设置节点位置并同步写回编辑器数据
        /// </summary>
        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            if (m_Data != null)
            {
                m_Data.Position = newPos.position;
            }
        }

        /// <summary>
        /// 绑定端口点击回调
        /// </summary>
        public void BindPortCallbacks(StepGraphView graphView, Action<Port> onInputClicked, Action<Port> onOutputClicked, IEdgeConnectorListener edgeConnectorListener)
        {
            m_GraphView = graphView;

            if (m_InputPort != null)
            {
                AttachInputEdgeConnector(edgeConnectorListener);
                m_InputPort.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button != 0)
                    {
                        return;
                    }
                    if (onInputClicked != null)
                    {
                        onInputClicked(m_InputPort);
                    }
                });
            }

            if (m_OutputPort != null)
            {
                AttachOutputEdgeConnector(edgeConnectorListener);
                m_OutputPort.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button != 0)
                    {
                        return;
                    }
                    if (onOutputClicked != null)
                    {
                        onOutputClicked(m_OutputPort);
                    }
                });
            }
        }

        /// <summary>
        /// 绑定节点展示模型并刷新卡片内容与状态样式。
        /// </summary>
        public void BindPresentation(StepNodePresentation presentation)
        {
            if (presentation == null)
            {
                return;
            }

            title = presentation.Title ?? string.Empty;

            if (m_SubtitleLabel != null)
            {
                m_SubtitleLabel.text = presentation.Subtitle ?? string.Empty;
                m_SubtitleLabel.style.display = string.IsNullOrEmpty(m_SubtitleLabel.text) ? DisplayStyle.None : DisplayStyle.Flex;
            }

            RebuildBadgeViews(presentation.Badges);
            RebuildSummaryViews(presentation.ActionSummaries, presentation.CurrentActionName);

            EnableInClassList(NODE_RUNNING_CLASS, presentation.IsRunning);
            EnableInClassList(NODE_COMPLETED_CLASS, presentation.IsCompleted);
            EnableInClassList(NODE_TRAIL_CLASS, presentation.IsInTrail);
            EnableInClassList(NODE_WARNING_CLASS, presentation.HasWarning);
            EnableInClassList(NODE_ERROR_CLASS, presentation.HasError);
        }

        /// <summary>
        /// 在节点挂入图视图后，根据数据同步初始折叠态。
        /// </summary>
        public void SyncCollapsedStateFromData(bool isFirst = false)
        {
            ApplyCollapsedState(isFirst);       
        }

        /// <summary>
        /// 切换节点折叠状态并写回编辑器数据。
        /// </summary>
        public void ToggleCollapsed()
        {
            if (m_Data == null)
            {
                return;
            }

            bool isChanged = SetCollapsed(!m_Data.IsCollapsed);
            if (!isChanged)
            {
                return;
            }

            StepGraphView graphView = GetStepGraphView();
            if (graphView != null)
            {
                graphView.NotifyNodeDataChanged();
            }
        }

        /// <summary>
        /// 直接设置节点折叠状态，并返回是否发生变化。
        /// </summary>
        public bool SetCollapsed(bool isCollapsed)
        {
            if (m_Data == null)
            {
                return false;
            }
            if (m_Data.IsCollapsed == isCollapsed)
            {
                return false;
            }

            m_Data.IsCollapsed = isCollapsed;
            ApplyCollapsedState();
            return true;
        }

        /// <summary>
        /// 处理鼠标按下事件
        /// </summary>
        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0)
            {
                return;
            }
            if (IsInteractionHandledByChild(evt.target as VisualElement))
            {
                return;
            }
            m_IsDragging = true;
            Vector2 downMousePosition = GetStepGraphView().GetContentLocalMousePosition(evt.mousePosition);
            RecordOffset(downMousePosition);
            for (int i = 0; i < m_GraphView.selection.Count; i++)
            {
                StepNodeView selectedNodeView = m_GraphView.selection[i] as StepNodeView;
                if (selectedNodeView == null)
                {
                    continue;
                }
                if (!ReferenceEquals(selectedNodeView, this))
                {
                    selectedNodeView.RecordOffset(downMousePosition);
                }
            }
        }
        /// <summary>
        /// 记录拖拽起点与节点初始位置之间的偏移量。
        /// </summary>
        public void RecordOffset(Vector2 downMousePosition)
        {
            m_DownNodePosition = GetPosition().position;
            m_Offset = downMousePosition - m_DownNodePosition;
        }

        /// <summary>
        /// 重置拖拽偏移与拖拽状态。
        /// </summary>
        public void ResetOffset()
        {
            m_Offset = Vector2.zero;
            m_IsDragging = false;
        }
        /// <summary>
        /// 处理鼠标移动事件
        /// </summary>
        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (!m_IsDragging)
            {
                return;
            }
            Vector2 graphMousePosition = GetStepGraphView().GetContentLocalMousePosition(evt.mousePosition);
            int selectedNodeCount = 0;
            bool isThisNodeSelected = false;
            for (int i = 0; i < m_GraphView.selection.Count; i++)
            {
                StepNodeView selectedNodeView = m_GraphView.selection[i] as StepNodeView;
                if (selectedNodeView == null)
                {
                    continue;
                }

                selectedNodeCount += 1;
                if (ReferenceEquals(selectedNodeView, this))
                {
                    isThisNodeSelected = true;
                }
            }
            // 多选拖拽时，所有选中节点都要基于各自偏移量同步更新位置。
            if (selectedNodeCount > 1 && isThisNodeSelected)
            {
                for (int i = 0; i < m_GraphView.selection.Count; i++)
                {
                    StepNodeView selectedNodeView = m_GraphView.selection[i] as StepNodeView;
                    if (selectedNodeView == null)
                    {
                        continue;
                    }
                    selectedNodeView.ApplyTargets(graphMousePosition);
                }
            }
            else
            {
                ApplyTargets(graphMousePosition);
            }
        }
        /// <summary>
        /// 根据当前鼠标位置和记录偏移量更新节点位置。
        /// </summary>
        public void ApplyTargets(Vector2 mousePosition)
        {
            Rect selfRect = GetPosition();
            selfRect.position = mousePosition - m_Offset;
            SetPosition(selfRect);
        }

        /// <summary>
        /// 获取所属的步骤图视图实例。
        /// </summary>
        private StepGraphView GetStepGraphView()
        {
            if (m_GraphView == null)
            {
                m_GraphView = GetFirstAncestorOfType<StepGraphView>();
            }
            return m_GraphView;
        }

        /// <summary>
        /// 处理鼠标抬起事件
        /// </summary>
        private void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button != 0)
            {
                return;
            }
            if (!m_IsDragging)
            {
                return;
            }
            if (DragEnded != null)
            {
                DragEnded(this);
            }
        }

        /// <summary>
        /// 构建节点卡片扩展内容容器。
        /// </summary>
        private void BuildPresentationContainer()
        {
            m_MetadataContainer = new VisualElement();
            m_MetadataContainer.style.flexDirection = FlexDirection.Column;

            m_SubtitleLabel = new Label();
            m_SubtitleLabel.AddToClassList("step-node-subtitle");

            m_BadgeContainer = new VisualElement();
            m_BadgeContainer.AddToClassList("step-node-badge-row");

            m_SummaryContainer = new VisualElement();
            m_SummaryContainer.AddToClassList("step-node-summary-row");

            m_MetadataContainer.Add(m_SubtitleLabel);
            m_MetadataContainer.Add(m_BadgeContainer);
            m_MetadataContainer.Add(m_SummaryContainer);
            extensionContainer.Add(m_MetadataContainer);
        }

        /// <summary>
        /// 监听节点默认折叠箭头的交互并同步折叠状态。
        /// </summary>
        private void RegisterCollapseStateSync()
        {
            if (titleButtonContainer == null)
            {
                return;
            }

            titleButtonContainer.RegisterCallback<MouseUpEvent>(OnTitleButtonMouseUp);
        }

        /// <summary>
        /// 为输入端口挂载拖拽连线连接器。
        /// </summary>
        private void AttachInputEdgeConnector(IEdgeConnectorListener edgeConnectorListener)
        {
            if (m_InputPort == null || edgeConnectorListener == null || m_InputEdgeConnector != null)
            {
                return;
            }

            m_InputEdgeConnector = new EdgeConnector<Edge>(edgeConnectorListener);
            m_InputPort.AddManipulator(m_InputEdgeConnector);
        }

        /// <summary>
        /// 为输出端口挂载拖拽连线连接器。
        /// </summary>
        private void AttachOutputEdgeConnector(IEdgeConnectorListener edgeConnectorListener)
        {
            if (m_OutputPort == null || edgeConnectorListener == null || m_OutputEdgeConnector != null)
            {
                return;
            }

            m_OutputEdgeConnector = new EdgeConnector<Edge>(edgeConnectorListener);
            m_OutputPort.AddManipulator(m_OutputEdgeConnector);
        }

        /// <summary>
        /// 根据节点数据刷新折叠态与按钮文案。
        /// </summary>
        private void ApplyCollapsedState(bool isFirst = false)
        {
            if (isFirst)
            {
                if(m_Data == null || !m_Data.IsCollapsed)
                {
                    RefreshExpandedState();
                }
            }
            else
            {
                expanded = m_Data == null || !m_Data.IsCollapsed;           
                if(expanded)
                {
                    RefreshExpandedState();
                }
            }

            RefreshPorts();
        }

        /// <summary>
        /// 判断当前事件是否应由端口或按钮等子控件接管。
        /// </summary>
        private bool IsInteractionHandledByChild(VisualElement target)
        {
            if (target == null)
            {
                return false;
            }

            Button button = target as Button;
            if (button == null)
            {
                button = target.GetFirstAncestorOfType<Button>();
            }
            if (button != null && titleButtonContainer != null && titleButtonContainer.Contains(button))
            {
                return true;
            }

            if (target is Port || target.GetFirstAncestorOfType<Port>() != null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 处理标题按钮区域抬起事件，并在默认折叠箭头点击后同步折叠状态。
        /// </summary>
        private void OnTitleButtonMouseUp(MouseUpEvent evt)
        {
            if (evt.button != 0)
            {
                return;
            }

            Button button = evt.target as Button;
            if (button == null)
            {
                button = (evt.target as VisualElement)?.GetFirstAncestorOfType<Button>();
            }
            if (button == null)
            {
                return;
            }

            schedule.Execute(SyncCollapsedStateFromExpanded);
        }

        /// <summary>
        /// 根据当前 Node 的 expanded 状态回写编辑器节点数据。
        /// </summary>
        private void SyncCollapsedStateFromExpanded()
        {
            bool isChanged = SetCollapsed(!expanded);
            if (!isChanged)
            {
                return;
            }

            StepGraphView graphView = GetStepGraphView();
            if (graphView != null)
            {
                graphView.NotifyNodeDataChanged();
            }
        }

        /// <summary>
        /// 根据展示模型重建徽标视图。
        /// </summary>
        private void RebuildBadgeViews(List<string> badges)
        {
            if (m_BadgeContainer == null)
            {
                return;
            }

            m_BadgeContainer.Clear();
            if (badges == null || badges.Count == 0)
            {
                m_BadgeContainer.style.display = DisplayStyle.None;
                return;
            }

            for (int i = 0; i < badges.Count; i++)
            {
                string badgeText = badges[i];
                if (string.IsNullOrEmpty(badgeText))
                {
                    continue;
                }

                Label badgeLabel = new Label(badgeText);
                badgeLabel.AddToClassList("step-node-badge");
                m_BadgeContainer.Add(badgeLabel);
            }

            m_BadgeContainer.style.display = m_BadgeContainer.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// 根据展示模型重建动作摘要视图。
        /// </summary>
        private void RebuildSummaryViews(List<string> actionSummaries, string currentActionName)
        {
            if (m_SummaryContainer == null)
            {
                return;
            }

            m_SummaryContainer.Clear();
            if (!string.IsNullOrEmpty(currentActionName))
            {
                Label currentActionLabel = new Label("Current:" + currentActionName);
                currentActionLabel.AddToClassList("step-node-current-action");
                m_SummaryContainer.Add(currentActionLabel);
            }

            if (actionSummaries != null)
            {
                for (int i = 0; i < actionSummaries.Count; i++)
                {
                    string summary = actionSummaries[i];
                    if (string.IsNullOrEmpty(summary))
                    {
                        continue;
                    }

                    Label summaryLabel = new Label(summary);
                    summaryLabel.AddToClassList("step-node-summary-item");
                    m_SummaryContainer.Add(summaryLabel);
                }
            }

            m_SummaryContainer.style.display = m_SummaryContainer.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
