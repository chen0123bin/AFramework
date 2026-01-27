using System.Collections.Generic;
using LWStep;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace LWStep.Editor
{
    public class StepGraphView : GraphView
    {
        private StepEditorGraphData m_Data;
        private Dictionary<string, StepNodeView> m_NodeViews;
        public System.Action<List<ISelectable>> SelectionChanged;
        public System.Action GraphChanged;
        private bool m_IsPanning;
        private Vector2 m_LastMousePosition;
        private Port m_PendingOutputPort;
        private Vector2 m_LastMouseWorldPosition;
        private string m_RuntimeNodeId;
        private Dictionary<string, StepNodeStatus> m_RuntimeNodeStatuses;

        /// <summary>
        /// 创建步骤图视图并绑定编辑器数据
        /// </summary>
        public StepGraphView(StepEditorGraphData data)
        {
            m_Data = data;
            m_NodeViews = new Dictionary<string, StepNodeView>();
            m_LastMouseWorldPosition = Vector2.zero;

            GridBackground grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<KeyUpEvent>(OnKeyUp);
            RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUpForPan);

            graphViewChanged = OnGraphViewChanged;

            RebuildView();
        }

        /// <summary>
        /// 获取当前绑定的步骤图编辑器数据
        /// </summary>
        public StepEditorGraphData GetData()
        {
            return m_Data;
        }

        /// <summary>
        /// 根据节点ID获取节点视图
        /// </summary>
        public StepNodeView GetNodeView(string nodeId)
        {
            StepNodeView nodeView;
            if (m_NodeViews.TryGetValue(nodeId, out nodeView))
            {
                return nodeView;
            }
            return null;
        }

        /// <summary>
        /// 根据编辑器数据重建节点与连线视图
        /// </summary>
        public void RebuildView()
        {
            List<GraphElement> elements = new List<GraphElement>();
            foreach (GraphElement element in graphElements)
            {
                elements.Add(element);
            }
            for (int i = 0; i < elements.Count; i++)
            {
                RemoveElement(elements[i]);
            }

            m_NodeViews.Clear();

            for (int i = 0; i < m_Data.Nodes.Count; i++)
            {
                StepEditorNodeData nodeData = m_Data.Nodes[i];
                StepNodeView nodeView = CreateNodeView(nodeData);
                AddElement(nodeView);
                m_NodeViews.Add(nodeData.Id, nodeView);
            }

            for (int i = 0; i < m_Data.Edges.Count; i++)
            {
                StepEditorEdgeData edgeData = m_Data.Edges[i];
                StepNodeView fromView = GetNodeView(edgeData.FromId);
                StepNodeView toView = GetNodeView(edgeData.ToId);
                if (fromView == null || toView == null)
                {
                    continue;
                }
                Edge edge = fromView.OutputPort.ConnectTo(toView.InputPort);
                ConfigureEdgeView(edge, edgeData);
                AddElement(edge);
            }

            UpdateAllNodeTitles();
        }

        /// <summary>
        /// 设置运行时当前节点并刷新视图
        /// </summary>
        public void SetRuntimeNodeId(string nodeId)
        {
            if (m_RuntimeNodeId == nodeId)
            {
                return;
            }
            m_RuntimeNodeId = nodeId;
            UpdateAllNodeTitles();
        }

        public void SetRuntimeNodeStatuses(Dictionary<string, StepNodeStatus> nodeStatuses)
        {
            m_RuntimeNodeStatuses = nodeStatuses;
            UpdateAllNodeTitles();
        }

        /// <summary>
        /// 在指定位置新增节点（写入数据并创建视图）
        /// </summary>
        public StepNodeView AddNode(Vector2 position)
        {
            StepEditorNodeData nodeData = new StepEditorNodeData();
            nodeData.Id = GenerateUniqueNodeId();
            nodeData.Name = nodeData.Id;
            nodeData.Position = position;
            m_Data.Nodes.Add(nodeData);

            StepNodeView nodeView = CreateNodeView(nodeData);
            AddElement(nodeView);
            m_NodeViews.Add(nodeData.Id, nodeView);
            UpdateAllNodeTitles();
            NotifyGraphChanged();
            return nodeView;
        }

        /// <summary>
        /// 在最近一次鼠标位置新增节点
        /// </summary>
        public StepNodeView AddNodeAtLastMousePosition()
        {
            Vector2 localPos = GetLocalMousePosition();
            return AddNode(localPos);
        }

        /// <summary>
        /// 删除节点及其相关连线
        /// </summary>
        public void RemoveNode(StepEditorNodeData nodeData)
        {
            StepNodeView nodeView = GetNodeView(nodeData.Id);
            if (nodeView != null)
            {
                RemoveElement(nodeView);
            }
            m_NodeViews.Remove(nodeData.Id);
            RemoveEdgesByNodeId(nodeData.Id);
            m_Data.Nodes.Remove(nodeData);
            if (m_Data.StartNodeId == nodeData.Id)
            {
                m_Data.StartNodeId = string.Empty;
            }
            UpdateAllNodeTitles();
            NotifyGraphChanged();
        }

        /// <summary>
        /// 设置开始节点并刷新显示
        /// </summary>
        public void SetStartNode(string nodeId)
        {
            m_Data.StartNodeId = nodeId;
            UpdateAllNodeTitles();
            NotifyGraphChanged();
        }

        /// <summary>
        /// 重命名节点ID并同步视图索引
        /// </summary>
        public bool RenameNodeId(string oldId, string newId)
        {
            StepNodeView nodeView;
            if (!m_NodeViews.TryGetValue(oldId, out nodeView))
            {
                return false;
            }
            m_NodeViews.Remove(oldId);
            m_NodeViews.Add(newId, nodeView);
            nodeView.viewDataKey = newId;
            UpdateAllNodeTitles();
            NotifyGraphChanged();
            return true;
        }

        /// <summary>
        /// 构建右键上下文菜单
        /// </summary>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            evt.menu.AppendAction("新增节点", action =>
            {
                AddNodeAtLastMousePosition();
            }, DropdownMenuAction.Status.Normal);
        }

        /// <summary>
        /// 创建节点视图并绑定回调
        /// </summary>
        private StepNodeView CreateNodeView(StepEditorNodeData data)
        {
            StepNodeView nodeView = new StepNodeView(data);
            nodeView.BindPortCallbacks(OnInputPortClicked, OnOutputPortClicked);
            nodeView.DragEnded += OnNodeDragEnded;
            return nodeView;
        }

        /// <summary>
        /// GraphView 结构变更回调（创建/删除连线与节点时同步数据）
        /// </summary>
        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (change.edgesToCreate != null)
            {
                for (int i = 0; i < change.edgesToCreate.Count; i++)
                {
                    Edge edge = change.edgesToCreate[i];
                    StepNodeView fromView = edge.output.node as StepNodeView;
                    StepNodeView toView = edge.input.node as StepNodeView;
                    if (fromView == null || toView == null)
                    {
                        continue;
                    }
                    StepEditorEdgeData existing = m_Data.GetEdge(fromView.Data.Id, toView.Data.Id);
                    if (existing != null)
                    {
                        ConfigureEdgeView(edge, existing);
                        continue;
                    }
                    StepEditorEdgeData edgeData = new StepEditorEdgeData();
                    edgeData.FromId = fromView.Data.Id;
                    edgeData.ToId = toView.Data.Id;
                    edgeData.Priority = 0;
                    edgeData.Condition = string.Empty;
                    edgeData.Tag = string.Empty;
                    m_Data.Edges.Add(edgeData);
                    ConfigureEdgeView(edge, edgeData);
                    NotifyGraphChanged();
                }
            }

            if (change.elementsToRemove != null)
            {
                for (int i = 0; i < change.elementsToRemove.Count; i++)
                {
                    Edge edge = change.elementsToRemove[i] as Edge;
                    if (edge != null)
                    {
                        RemoveEdgeData(edge);
                        NotifyGraphChanged();
                        continue;
                    }
                    StepNodeView nodeView = change.elementsToRemove[i] as StepNodeView;
                    if (nodeView != null)
                    {
                        RemoveNode(nodeView.Data);
                        NotifyGraphChanged();
                        continue;
                    }
                }
            }
            return change;
        }

        /// <summary>
        /// 删除连线对应的数据
        /// </summary>
        private void RemoveEdgeData(Edge edge)
        {
            StepEditorEdgeData edgeData = edge.userData as StepEditorEdgeData;
            if (edgeData != null)
            {
                m_Data.Edges.Remove(edgeData);
                return;
            }
            StepNodeView fromView = edge.output.node as StepNodeView;
            StepNodeView toView = edge.input.node as StepNodeView;
            if (fromView == null || toView == null)
            {
                return;
            }
            StepEditorEdgeData existing = m_Data.GetEdge(fromView.Data.Id, toView.Data.Id);
            if (existing != null)
            {
                m_Data.Edges.Remove(existing);
            }
            NotifyGraphChanged();
        }

        /// <summary>
        /// 删除与指定节点相关的所有连线数据
        /// </summary>
        private void RemoveEdgesByNodeId(string nodeId)
        {
            for (int i = m_Data.Edges.Count - 1; i >= 0; i--)
            {
                StepEditorEdgeData edge = m_Data.Edges[i];
                if (edge.FromId == nodeId || edge.ToId == nodeId)
                {
                    m_Data.Edges.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 刷新指定连线的显示
        /// </summary>
        public void UpdateEdgeView(Edge edge)
        {
            if (edge == null)
            {
                return;
            }
            StepEditorEdgeData edgeData = edge.userData as StepEditorEdgeData;
            if (edgeData == null)
            {
                return;
            }
            ConfigureEdgeView(edge, edgeData);
        }

        /// <summary>
        /// 生成当前图内不重复的节点ID
        /// </summary>
        private string GenerateUniqueNodeId()
        {
            int index = 1;
            while (true)
            {
                string candidate = "node" + index;
                if (m_Data.GetNode(candidate) == null)
                {
                    return candidate;
                }
                index += 1;
            }
        }

        /// <summary>
        /// 刷新所有节点标题（开始节点/运行时节点标记）
        /// </summary>
        private void UpdateAllNodeTitles()
        {
            foreach (KeyValuePair<string, StepNodeView> kvp in m_NodeViews)
            {
                StepNodeStatus status = StepNodeStatus.Unfinished;
                if (m_RuntimeNodeStatuses != null)
                {
                    m_RuntimeNodeStatuses.TryGetValue(kvp.Key, out status);
                }
                kvp.Value.UpdateTitle(m_Data.StartNodeId, m_RuntimeNodeId, status);
            }
        }


        /// <summary>
        /// 鼠标抬起时派发选中变化
        /// </summary>
        private void OnMouseUp(MouseUpEvent evt)
        {
            DispatchSelectionChanged();
        }

        /// <summary>
        /// 鼠标按下：记录鼠标位置、处理空白选中、处理中键拖拽
        /// </summary>
        private void OnMouseDown(MouseDownEvent evt)
        {
            m_LastMouseWorldPosition = this.LocalToWorld(evt.mousePosition);
            if (evt.button == 0)
            {
                VisualElement target = evt.target as VisualElement;
                if (IsClickOnEmpty(target))
                {
                    ClearSelection();
                }
                schedule.Execute(DispatchSelectionChanged);
            }
            if (evt.button != 2)
            {
                return;
            }
            m_IsPanning = true;
            m_LastMousePosition = evt.mousePosition;
            evt.StopPropagation();
        }

        /// <summary>
        /// 鼠标移动：更新鼠标位置并处理中键拖拽平移
        /// </summary>
        private void OnMouseMove(MouseMoveEvent evt)
        {
            m_LastMouseWorldPosition = this.LocalToWorld(evt.mousePosition);
            if (!m_IsPanning)
            {
                return;
            }
            Vector2 delta = evt.mousePosition - m_LastMousePosition;
            m_LastMousePosition = evt.mousePosition;
            Vector3 position = contentViewContainer.transform.position;
            position += new Vector3(delta.x, delta.y, 0.0f);
            contentViewContainer.transform.position = position;
            evt.StopPropagation();
        }

        /// <summary>
        /// 鼠标抬起：结束中键拖拽平移
        /// </summary>
        private void OnMouseUpForPan(MouseUpEvent evt)
        {
            m_LastMouseWorldPosition = this.LocalToWorld(evt.mousePosition);
            if (!m_IsPanning)
            {
                return;
            }
            if (evt.button != 2)
            {
                return;
            }
            m_IsPanning = false;
            evt.StopPropagation();
        }

        /// <summary>
        /// 按键抬起时派发选中变化
        /// </summary>
        private void OnKeyUp(KeyUpEvent evt)
        {
            DispatchSelectionChanged();
        }

        /// <summary>
        /// 派发选中变化事件（GraphView.selection -> List<ISelectable>）
        /// </summary>
        private void DispatchSelectionChanged()
        {
            if (SelectionChanged == null)
            {
                return;
            }
            System.Collections.Generic.List<ISelectable> list = new System.Collections.Generic.List<ISelectable>();
            foreach (ISelectable s in selection)
            {
                list.Add(s);
            }
            SelectionChanged(list);
        }

        /// <summary>
        /// 判断当前点击是否发生在空白区域
        /// </summary>
        private bool IsClickOnEmpty(VisualElement target)
        {
            if (target == null)
            {
                return false;
            }
            GraphElement ancestor = target.GetFirstAncestorOfType<GraphElement>();
            if (ancestor != null)
            {
                return false;
            }
            if (target == this || target == contentViewContainer)
            {
                return true;
            }
            if (target is GridBackground)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 处理输入端口点击
        /// </summary>
        private void OnInputPortClicked(Port inputPort)
        {
            if (m_PendingOutputPort == null)
            {
                return;
            }
            if (inputPort == null)
            {
                return;
            }
            StepNodeView fromView = m_PendingOutputPort.node as StepNodeView;
            StepNodeView toView = inputPort.node as StepNodeView;
            if (fromView == null || toView == null)
            {
                m_PendingOutputPort = null;
                return;
            }
            CreateEdge(fromView, toView, m_PendingOutputPort, inputPort);
            m_PendingOutputPort = null;
            NotifyGraphChanged();
        }

        /// <summary>
        /// 处理输出端口点击
        /// </summary>
        private void OnOutputPortClicked(Port outputPort)
        {
            m_PendingOutputPort = outputPort;
        }

        /// <summary>
        /// 创建连线并写入数据
        /// </summary>
        private void CreateEdge(StepNodeView fromView, StepNodeView toView, Port outputPort, Port inputPort)
        {
            if (fromView == null || toView == null)
            {
                return;
            }
            StepEditorEdgeData existing = m_Data.GetEdge(fromView.Data.Id, toView.Data.Id);
            if (existing != null)
            {
                return;
            }
            StepEditorEdgeData edgeData = new StepEditorEdgeData();
            edgeData.FromId = fromView.Data.Id;
            edgeData.ToId = toView.Data.Id;
            edgeData.Priority = 0;
            edgeData.Condition = string.Empty;
            edgeData.Tag = string.Empty;
            m_Data.Edges.Add(edgeData);

            Edge edge = new Edge();
            edge.output = outputPort;
            edge.input = inputPort;
            ConfigureEdgeView(edge, edgeData);
            outputPort.Connect(edge);
            inputPort.Connect(edge);
            AddElement(edge);
            NotifyGraphChanged();
        }

        /// <summary>
        /// 配置连线视图与数据绑定
        /// </summary>
        private void ConfigureEdgeView(Edge edge, StepEditorEdgeData edgeData)
        {
            if (edge == null || edgeData == null)
            {
                return;
            }
            edge.userData = edgeData;
        }

        /// <summary>
        /// 获取最近鼠标位置对应的GraphView本地坐标
        /// </summary>
        private Vector2 GetLocalMousePosition()
        {
            Vector2 worldPos = m_LastMouseWorldPosition;
            return contentViewContainer.WorldToLocal(worldPos);
        }

        /// <summary>
        /// 节点拖拽结束回调
        /// </summary>
        private void OnNodeDragEnded(StepNodeView nodeView)
        {
            NotifyGraphChanged();
        }

        /// <summary>
        /// 通知图数据已变更
        /// </summary>
        private void NotifyGraphChanged()
        {
            if (GraphChanged != null)
            {
                GraphChanged();
            }
        }
    }
}
