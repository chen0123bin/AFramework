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
        private VisualElement m_MetadataContainer;
        private Label m_SubtitleLabel;
        private VisualElement m_BadgeContainer;
        private VisualElement m_SummaryContainer;
        private bool m_IsDragging;
        private Vector2 m_DownNodePosition;
        private Vector2 m_Offset;
        public Action<StepNodeView> DragEnded;

        private sealed class StepNodeBindingPlan
        {
            public string Title;
            public string Subtitle;
            public string CurrentActionLine;
            public List<string> Badges;
            public List<string> SummaryLines;
            public List<string> EnabledClasses;

            /// <summary>
            /// 创建节点绑定计划并初始化集合字段。
            /// </summary>
            public StepNodeBindingPlan()
            {
                Title = string.Empty;
                Subtitle = string.Empty;
                CurrentActionLine = string.Empty;
                Badges = new List<string>();
                SummaryLines = new List<string>();
                EnabledClasses = new List<string>();
            }
        }

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
            SetPosition(new Rect(data.Position, new Vector2(180.0f, 120.0f)));
            AddToClassList(NODE_CARD_CLASS);

            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);

            RefreshExpandedState();
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
        public void BindPortCallbacks(Action<Port> onInputClicked, Action<Port> onOutputClicked)
        {
            if (m_InputPort != null)
            {
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

            StepNodeBindingPlan bindingPlan = BuildBindingPlan(presentation);
            title = bindingPlan.Title;

            if (m_SubtitleLabel != null)
            {
                m_SubtitleLabel.text = bindingPlan.Subtitle;
                m_SubtitleLabel.style.display = string.IsNullOrEmpty(m_SubtitleLabel.text) ? DisplayStyle.None : DisplayStyle.Flex;
            }

            RebuildBadgeViews(bindingPlan.Badges);
            RebuildSummaryViews(bindingPlan.SummaryLines, bindingPlan.CurrentActionLine);

            EnableInClassList(NODE_RUNNING_CLASS, bindingPlan.EnabledClasses.Contains(NODE_RUNNING_CLASS));
            EnableInClassList(NODE_COMPLETED_CLASS, bindingPlan.EnabledClasses.Contains(NODE_COMPLETED_CLASS));
            EnableInClassList(NODE_TRAIL_CLASS, bindingPlan.EnabledClasses.Contains(NODE_TRAIL_CLASS));
            EnableInClassList(NODE_WARNING_CLASS, bindingPlan.EnabledClasses.Contains(NODE_WARNING_CLASS));
            EnableInClassList(NODE_ERROR_CLASS, bindingPlan.EnabledClasses.Contains(NODE_ERROR_CLASS));
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
        //记录偏移量
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
            //框选处理
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
        //根据当前鼠标点以及偏移量计算最新的坐标位置
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
        /// 根据展示模型生成节点绑定计划，便于测试渲染映射逻辑。
        /// </summary>
        private static StepNodeBindingPlan BuildBindingPlan(StepNodePresentation presentation)
        {
            StepNodeBindingPlan bindingPlan = new StepNodeBindingPlan();
            if (presentation == null)
            {
                return bindingPlan;
            }

            bindingPlan.Title = presentation.Title ?? string.Empty;
            bindingPlan.Subtitle = presentation.Subtitle ?? string.Empty;

            if (presentation.Badges != null)
            {
                bindingPlan.Badges.AddRange(presentation.Badges);
            }

            bindingPlan.CurrentActionLine = presentation.CurrentActionName ?? string.Empty;

            if (presentation.ActionSummaries != null)
            {
                bindingPlan.SummaryLines.AddRange(presentation.ActionSummaries);
            }

            AppendEnabledClass(bindingPlan.EnabledClasses, NODE_RUNNING_CLASS, presentation.IsRunning);
            AppendEnabledClass(bindingPlan.EnabledClasses, NODE_COMPLETED_CLASS, presentation.IsCompleted);
            AppendEnabledClass(bindingPlan.EnabledClasses, NODE_TRAIL_CLASS, presentation.IsInTrail);
            AppendEnabledClass(bindingPlan.EnabledClasses, NODE_WARNING_CLASS, presentation.HasWarning);
            AppendEnabledClass(bindingPlan.EnabledClasses, NODE_ERROR_CLASS, presentation.HasError);
            return bindingPlan;
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

        /// <summary>
        /// 条件满足时向类名列表追加目标样式类。
        /// </summary>
        private static void AppendEnabledClass(List<string> enabledClasses, string className, bool shouldEnable)
        {
            if (enabledClasses == null || !shouldEnable || string.IsNullOrEmpty(className))
            {
                return;
            }

            enabledClasses.Add(className);
        }
    }
}
