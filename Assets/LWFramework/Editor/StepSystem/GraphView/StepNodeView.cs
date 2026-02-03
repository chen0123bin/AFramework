using System;
using LWStep;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace LWStep.Editor
{
    public class StepNodeView : Node
    {
        private StepEditorNodeData m_Data;
        private Port m_InputPort;
        private Port m_OutputPort;
        private bool m_IsDragging;
        private Vector2 m_LastMousePosition;
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
            m_Data = data;
            title = data.Id;
            viewDataKey = data.Id;

            m_InputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            m_InputPort.portName = "In";
            inputContainer.Add(m_InputPort);

            m_OutputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            m_OutputPort.portName = "Out";
            outputContainer.Add(m_OutputPort);

            SetPosition(new Rect(data.Position, new Vector2(180.0f, 120.0f)));

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
        /// 处理鼠标按下事件
        /// </summary>
        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0)
            {
                return;
            }
            m_IsDragging = true;
            m_LastMousePosition = evt.mousePosition;
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
            Vector2 delta = evt.mousePosition - m_LastMousePosition;
            m_LastMousePosition = evt.mousePosition;
            ApplyDeltaToDraggingTargets(delta);
        }

        /// <summary>
        /// 将拖拽增量应用到目标节点（支持多选节点整体移动）
        /// </summary>
        private void ApplyDeltaToDraggingTargets(Vector2 delta)
        {
            StepGraphView graphView = GetFirstAncestorOfType<StepGraphView>();
            if (graphView == null)
            {
                Rect rect = GetPosition();
                rect.position += delta;
                SetPosition(rect);
                return;
            }

            int selectedNodeCount = 0;
            bool isThisNodeSelected = false;
            for (int i = 0; i < graphView.selection.Count; i++)
            {
                StepNodeView selectedNodeView = graphView.selection[i] as StepNodeView;
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

            if (selectedNodeCount > 1 && isThisNodeSelected)
            {
                for (int i = 0; i < graphView.selection.Count; i++)
                {
                    StepNodeView selectedNodeView = graphView.selection[i] as StepNodeView;
                    if (selectedNodeView == null)
                    {
                        continue;
                    }
                    Rect rect = selectedNodeView.GetPosition();
                    rect.position += delta;
                    selectedNodeView.SetPosition(rect);
                }
                return;
            }

            Rect selfRect = GetPosition();
            selfRect.position += delta;
            SetPosition(selfRect);
        }

        /// <summary>
        /// 处理鼠标抬起事件
        /// </summary>
        private void OnMouseUp(MouseUpEvent evt)
        {
            if (!m_IsDragging)
            {
                return;
            }
            if (evt.button != 0)
            {
                return;
            }
            m_IsDragging = false;
            if (DragEnded != null)
            {
                DragEnded(this);
            }
        }

        /// <summary>
        /// 更新节点标题与运行时高亮
        /// </summary>
        public void UpdateTitle(string startNodeId, string runtimeNodeId, StepNodeStatus status)
        {
            if (m_Data == null)
            {
                return;
            }
            // Debug.Log("UpdateTitle: " + m_Data.Id + " startNodeId=" + startNodeId + " runtimeNodeId=" + runtimeNodeId);
            bool isStart = !string.IsNullOrEmpty(startNodeId) && m_Data.Id == startNodeId;
            bool isRuntime = !string.IsNullOrEmpty(runtimeNodeId) && m_Data.Id == runtimeNodeId;
            StepNodeStatus displayStatus = isRuntime ? StepNodeStatus.Running : status;


            string suffix = string.Empty;
            if (isStart && displayStatus == StepNodeStatus.Running)
            {
                suffix = " (Start, 运行中)";
            }

            title = m_Data.Id + suffix;

            if (displayStatus == StepNodeStatus.Running)
            {
                mainContainer.style.borderLeftWidth = 4;
                mainContainer.style.borderLeftColor = new Color(1f, 0.8f, 0.2f, 1f);
                mainContainer.style.backgroundColor = new Color(1f, 0.9f, 0.2f, 0.08f);
            }
            else if (displayStatus == StepNodeStatus.Completed)
            {
                mainContainer.style.borderLeftWidth = 4;
                mainContainer.style.borderLeftColor = new Color(0.2f, 0.8f, 0.4f, 1f);
                mainContainer.style.backgroundColor = new Color(0.2f, 0.8f, 0.4f, 0.08f);
            }
            else
            {
                mainContainer.style.borderLeftWidth = 0;
                mainContainer.style.borderLeftColor = new Color(0f, 0f, 0f, 0f);
                mainContainer.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
            }
        }
    }
}
