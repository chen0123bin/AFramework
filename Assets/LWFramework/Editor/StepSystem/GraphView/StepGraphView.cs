using System.Collections.Generic;
using LWStep;
using LWStep.Editor.Presentation;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace LWStep.Editor
{
    public class StepGraphView : GraphView
    {
        private const string GRAPH_STYLE_PATH = "Assets/LWFramework/Editor/StepSystem/GraphView/StepGraphStyles.uss";
        private const string EDGE_LABEL_ELEMENT_NAME = "step-edge-label";

        private StepEditorGraphData m_Data;
        private Dictionary<string, StepNodeView> m_NodeViews;
        public System.Action<List<ISelectable>> SelectionChanged;
        public System.Action GraphChanged;
        private bool m_IsPanning;
        private Vector2 m_LastMousePosition;
        private Port m_PendingOutputPort;
        private StepEdgeConnectorListener m_EdgeConnectorListener;
        private Vector2 m_LastMouseWorldPosition;
        private string m_RuntimeNodeId;
        private string m_RuntimeCurrentActionName;
        private Dictionary<string, StepNodeStatus> m_RuntimeNodeStatuses;
        private HashSet<string> m_RuntimeTrailNodeIds;

        private bool m_IsRectangleSelecting;
        private bool m_IsRectangleSelectAdditive;
        private Vector2 m_RectangleSelectStartPos;
        private VisualElement m_RectangleSelectBox;

        /// <summary>
        /// 创建步骤图视图并绑定编辑器数据
        /// </summary>
        public StepGraphView(StepEditorGraphData data)
        {
            m_Data = data;
            m_NodeViews = new Dictionary<string, StepNodeView>();
            m_EdgeConnectorListener = new StepEdgeConnectorListener(this);
            m_LastMouseWorldPosition = Vector2.zero;
            m_RuntimeCurrentActionName = string.Empty;
            m_RuntimeTrailNodeIds = new HashSet<string>();

            GridBackground grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            AttachGraphStyleSheet();

            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<KeyUpEvent>(OnKeyUp);
            RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUpForPan);

            graphViewChanged = OnGraphViewChanged;

            RebuildView();

        }

        /// <summary>
        /// 返回起始端口允许连接的目标端口集合。
        /// </summary>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> compatiblePorts = new List<Port>();
            if (startPort == null)
            {
                return compatiblePorts;
            }

            foreach (Port port in ports)
            {
                if (port == null || ReferenceEquals(port, startPort))
                {
                    continue;
                }
                if (ReferenceEquals(port.node, startPort.node))
                {
                    continue;
                }
                if (port.direction == startPort.direction)
                {
                    continue;
                }

                compatiblePorts.Add(port);
            }

            return compatiblePorts;
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
                nodeView.SyncCollapsedStateFromData(true);
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

        /// <summary>
        /// 设置运行时节点状态映射并刷新所有节点展示。
        /// </summary>
        public void SetRuntimeNodeStatuses(Dictionary<string, StepNodeStatus> nodeStatuses)
        {
            m_RuntimeNodeStatuses = nodeStatuses;
            UpdateAllNodeTitles();
        }

        /// <summary>
        /// 设置运行时当前动作名称并刷新节点展示。
        /// </summary>
        public void SetRuntimeCurrentActionName(string actionName)
        {
            string newActionName = actionName ?? string.Empty;
            if (m_RuntimeCurrentActionName == newActionName)
            {
                return;
            }

            m_RuntimeCurrentActionName = newActionName;
            UpdateAllNodeTitles();
        }

        /// <summary>
        /// 设置运行时轨迹节点集合并刷新节点展示。
        /// </summary>
        public void SetRuntimeTrail(List<string> nodeIds)
        {
            HashSet<string> newTrailNodeIds = nodeIds != null ? new HashSet<string>(nodeIds) : new HashSet<string>();
            if (m_RuntimeTrailNodeIds.SetEquals(newTrailNodeIds))
            {
                return;
            }

            m_RuntimeTrailNodeIds = newTrailNodeIds;
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
            nodeView.SyncCollapsedStateFromData();
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
        /// 获取当前选中的节点ID列表。
        /// </summary>
        public List<string> GetSelectedNodeIds()
        {
            List<string> nodeIds = new List<string>();
            foreach (ISelectable selectable in selection)
            {
                StepNodeView nodeView = selectable as StepNodeView;
                if (nodeView == null || nodeView.Data == null || string.IsNullOrEmpty(nodeView.Data.Id))
                {
                    continue;
                }

                nodeIds.Add(nodeView.Data.Id);
            }

            return nodeIds;
        }

        /// <summary>
        /// 按节点ID集合恢复选中状态。
        /// </summary>
        public void SelectNodes(IList<string> nodeIds)
        {
            ClearSelection();
            if (nodeIds == null || nodeIds.Count == 0)
            {
                DispatchSelectionChanged();
                return;
            }

            for (int i = 0; i < nodeIds.Count; i++)
            {
                string nodeId = nodeIds[i];
                if (string.IsNullOrEmpty(nodeId))
                {
                    continue;
                }

                StepNodeView nodeView = GetNodeView(nodeId);
                if (nodeView == null)
                {
                    continue;
                }

                AddToSelection(nodeView);
            }

            DispatchSelectionChanged();
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
            evt.menu.AppendAction("折叠选中节点", action =>
            {
                CollapseSelectedNodes();
            }, GetSelectedNodeCount() > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction("展开选中节点", action =>
            {
                ExpandSelectedNodes();
            }, GetSelectedNodeCount() > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        /// <summary>
        /// 将当前选中的所有节点批量折叠。
        /// </summary>
        public int CollapseSelectedNodes()
        {
            return SetSelectedNodesCollapsed(true);
        }

        /// <summary>
        /// 将当前选中的所有节点批量展开。
        /// </summary>
        public int ExpandSelectedNodes()
        {
            return SetSelectedNodesCollapsed(false);
        }

        /// <summary>
        /// 将当前图中的所有节点批量折叠。
        /// </summary>
        public int CollapseAllNodes()
        {
            return SetAllNodesCollapsed(true);
        }

        /// <summary>
        /// 将当前图中的所有节点批量展开。
        /// </summary>
        public int ExpandAllNodes()
        {
            return SetAllNodesCollapsed(false);
        }

        /// <summary>
        /// 创建节点视图并绑定回调
        /// </summary>
        private StepNodeView CreateNodeView(StepEditorNodeData data)
        {
            StepNodeView nodeView = new StepNodeView(data);
            nodeView.BindPortCallbacks(this, OnInputPortClicked, OnOutputPortClicked, m_EdgeConnectorListener);
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
                    edgeData.ConditionKey = string.Empty;
                    edgeData.ConditionComparisonType = ComparisonType.EqualTo;
                    edgeData.ConditionValue = string.Empty;
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

                StepNodePresentation presentation = StepGraphPresentationBuilder.BuildNodePresentation(
                    kvp.Value.Data,
                    m_Data != null ? m_Data.StartNodeId : string.Empty,
                    m_RuntimeNodeId,
                    status,
                    m_RuntimeTrailNodeIds,
                    kvp.Key == m_RuntimeNodeId ? m_RuntimeCurrentActionName : string.Empty);
                kvp.Value.BindPresentation(presentation);
            }
        }


        /// <summary>
        /// 鼠标抬起时派发选中变化
        /// </summary>
        private void OnMouseUp(MouseUpEvent evt)
        {
            if (m_IsRectangleSelecting && evt.button == 0)
            {
                EndRectangleSelection();
                DispatchSelectionChanged();
                evt.StopPropagation();

            }
            else
            {
                DispatchSelectionChanged();
            }
            for (int i = 0; i < selection.Count; i++)
            {
                StepNodeView selectedNodeView = selection[i] as StepNodeView;
                if (selectedNodeView == null)
                {
                    continue;
                }
                selectedNodeView.ResetOffset();
            }
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
                    BeginRectangleSelection(evt);
                    evt.StopPropagation();
                    return;
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
            if (m_IsRectangleSelecting)
            {
                UpdateRectangleSelection(evt);
                evt.StopPropagation();
                return;
            }
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
        /// 开始框选：记录起点并显示框选矩形
        /// </summary>
        private void BeginRectangleSelection(MouseDownEvent evt)
        {
            m_IsRectangleSelecting = true;
            m_IsRectangleSelectAdditive = evt.shiftKey;
            // evt.mousePosition 在 GraphView 的 MouseDownEvent 中是相对于 GraphView 的
            m_RectangleSelectStartPos = GetContentLocalMousePosition(evt.mousePosition);

            EnsureRectangleSelectBox();
            UpdateRectangleSelectBox(m_RectangleSelectStartPos, m_RectangleSelectStartPos);

            if (!m_IsRectangleSelectAdditive)
            {
                ClearSelection();
            }
        }

        /// <summary>
        /// 更新框选：刷新矩形与命中节点集合
        /// </summary>
        private void UpdateRectangleSelection(MouseMoveEvent evt)
        {
            Vector2 currentPos = GetContentLocalMousePosition(evt.mousePosition);
            UpdateRectangleSelectBox(m_RectangleSelectStartPos, currentPos);

            Rect selectionRect = GetMinMaxRect(m_RectangleSelectStartPos, currentPos);
            UpdateSelectionByRect(selectionRect, m_IsRectangleSelectAdditive);
        }

        /// <summary>
        /// 结束框选：隐藏框选矩形
        /// </summary>
        private void EndRectangleSelection()
        {
            m_IsRectangleSelecting = false;
            if (m_RectangleSelectBox != null)
            {
                m_RectangleSelectBox.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// 确保框选矩形节点存在
        /// </summary>
        private void EnsureRectangleSelectBox()
        {
            if (m_RectangleSelectBox != null)
            {
                m_RectangleSelectBox.style.display = DisplayStyle.Flex;
                return;
            }

            m_RectangleSelectBox = new VisualElement();
            m_RectangleSelectBox.pickingMode = PickingMode.Ignore;
            m_RectangleSelectBox.style.position = Position.Absolute;
            m_RectangleSelectBox.style.borderLeftWidth = 1;
            m_RectangleSelectBox.style.borderRightWidth = 1;
            m_RectangleSelectBox.style.borderTopWidth = 1;
            m_RectangleSelectBox.style.borderBottomWidth = 1;
            m_RectangleSelectBox.style.borderLeftColor = new Color(0.2f, 0.6f, 1.0f, 1.0f);
            m_RectangleSelectBox.style.borderRightColor = new Color(0.2f, 0.6f, 1.0f, 1.0f);
            m_RectangleSelectBox.style.borderTopColor = new Color(0.2f, 0.6f, 1.0f, 1.0f);
            m_RectangleSelectBox.style.borderBottomColor = new Color(0.2f, 0.6f, 1.0f, 1.0f);
            m_RectangleSelectBox.style.backgroundColor = new Color(0.2f, 0.6f, 1.0f, 0.08f);

            contentViewContainer.Add(m_RectangleSelectBox);
        }

        /// <summary>
        /// 更新框选矩形的显示范围
        /// </summary>
        private void UpdateRectangleSelectBox(Vector2 fromPos, Vector2 toPos)
        {
            if (m_RectangleSelectBox == null)
            {
                return;
            }
            Rect rect = GetMinMaxRect(fromPos, toPos);
            m_RectangleSelectBox.style.left = rect.xMin;
            m_RectangleSelectBox.style.top = rect.yMin;
            m_RectangleSelectBox.style.width = rect.width;
            m_RectangleSelectBox.style.height = rect.height;
            m_RectangleSelectBox.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// 根据矩形范围更新选中节点集合
        /// </summary>
        private void UpdateSelectionByRect(Rect rect, bool isAdditive)
        {
            if (!isAdditive)
            {
                ClearSelection();
            }

            foreach (KeyValuePair<string, StepNodeView> kvp in m_NodeViews)
            {
                StepNodeView nodeView = kvp.Value;
                if (nodeView == null)
                {
                    continue;
                }
                Rect nodeRect = nodeView.GetPosition();
                if (!nodeRect.Overlaps(rect))
                {
                    continue;
                }
                AddToSelection(nodeView);
            }
        }

        /// <summary>
        /// 获取事件鼠标位置对应的 contentViewContainer 本地坐标
        /// </summary>
        public Vector2 GetContentLocalMousePosition(Vector2 graphViewLocalMousePosition)
        {
            Vector2 ret = contentViewContainer.WorldToLocal(graphViewLocalMousePosition);
            return ret;
        }


        /// <summary>
        /// 计算两点形成的最小包围矩形
        /// </summary>
        private Rect GetMinMaxRect(Vector2 a, Vector2 b)
        {
            float xMin = Mathf.Min(a.x, b.x);
            float yMin = Mathf.Min(a.y, b.y);
            float xMax = Mathf.Max(a.x, b.x);
            float yMax = Mathf.Max(a.y, b.y);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
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
            List<ISelectable> list = new List<ISelectable>();
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
            CreateEdge(fromView, toView, outputPort, inputPort, null);
        }

        /// <summary>
        /// 创建连线并写入数据。
        /// </summary>
        private void CreateEdge(StepNodeView fromView, StepNodeView toView, Port outputPort, Port inputPort, Edge edgeView)
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
            edgeData.ConditionKey = string.Empty;
            edgeData.ConditionComparisonType = ComparisonType.EqualTo;
            edgeData.ConditionValue = string.Empty;
            m_Data.Edges.Add(edgeData);

            Edge edge = edgeView ?? new Edge();
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
            StepEdgePresentation presentation = StepGraphPresentationBuilder.BuildEdgePresentation(edgeData);
            Label edgeLabel = GetOrCreateEdgeLabel(edge);
            edgeLabel.text = presentation.Label ?? string.Empty;
            edgeLabel.style.display = string.IsNullOrEmpty(edgeLabel.text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <summary>
        /// 为图视图挂载步骤图样式表。
        /// </summary>
        private void AttachGraphStyleSheet()
        {
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(GRAPH_STYLE_PATH);
            if (styleSheet == null)
            {
                return;
            }

            styleSheets.Add(styleSheet);
        }

        /// <summary>
        /// 获取或创建连线标签控件。
        /// </summary>
        private Label GetOrCreateEdgeLabel(Edge edge)
        {
            Label edgeLabel = edge.Q<Label>(EDGE_LABEL_ELEMENT_NAME);
            if (edgeLabel != null)
            {
                return edgeLabel;
            }

            edgeLabel = new Label();
            edgeLabel.name = EDGE_LABEL_ELEMENT_NAME;
            edgeLabel.pickingMode = PickingMode.Ignore;
            edgeLabel.AddToClassList("step-edge-label");
            edge.Add(edgeLabel);
            return edgeLabel;
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
        /// 节点编辑器数据变更后刷新外部脏标记。
        /// </summary>
        public void NotifyNodeDataChanged()
        {
            NotifyGraphChanged();
        }

        /// <summary>
        /// 批量设置当前选中节点的折叠状态。
        /// </summary>
        private int SetSelectedNodesCollapsed(bool isCollapsed)
        {
            int changedCount = 0;
            for (int i = 0; i < selection.Count; i++)
            {
                StepNodeView nodeView = selection[i] as StepNodeView;
                if (nodeView == null)
                {
                    continue;
                }

                if (!nodeView.SetCollapsed(isCollapsed))
                {
                    continue;
                }

                changedCount += 1;
            }

            if (changedCount > 0)
            {
                NotifyGraphChanged();
            }

            return changedCount;
        }

        /// <summary>
        /// 获取当前选中的节点数量。
        /// </summary>
        private int GetSelectedNodeCount()
        {
            int nodeCount = 0;
            for (int i = 0; i < selection.Count; i++)
            {
                if (selection[i] is StepNodeView)
                {
                    nodeCount += 1;
                }
            }

            return nodeCount;
        }

        /// <summary>
        /// 批量设置当前图所有节点的折叠状态。
        /// </summary>
        private int SetAllNodesCollapsed(bool isCollapsed)
        {
            int changedCount = 0;
            foreach (KeyValuePair<string, StepNodeView> pair in m_NodeViews)
            {
                StepNodeView nodeView = pair.Value;
                if (nodeView == null)
                {
                    continue;
                }

                if (!nodeView.SetCollapsed(isCollapsed))
                {
                    continue;
                }

                changedCount += 1;
            }

            if (changedCount > 0)
            {
                NotifyGraphChanged();
            }

            return changedCount;
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

        /// <summary>
        /// 拖拽连线完成时负责写回图数据与连线视图。
        /// </summary>
        private sealed class StepEdgeConnectorListener : IEdgeConnectorListener
        {
            private readonly StepGraphView m_GraphView;

            /// <summary>
            /// 创建拖拽连线监听器。
            /// </summary>
            public StepEdgeConnectorListener(StepGraphView graphView)
            {
                m_GraphView = graphView;
            }

            /// <summary>
            /// 拖拽到空白区域时不做额外处理。
            /// </summary>
            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
            }

            /// <summary>
            /// 拖拽到合法端口时创建最终连线。
            /// </summary>
            public void OnDrop(GraphView graphView, Edge edge)
            {
                if (m_GraphView == null || edge == null || edge.output == null || edge.input == null)
                {
                    return;
                }

                StepNodeView fromView = edge.output.node as StepNodeView;
                StepNodeView toView = edge.input.node as StepNodeView;
                if (fromView == null || toView == null)
                {
                    return;
                }

                m_GraphView.CreateEdge(fromView, toView, edge.output, edge.input, edge);
            }
        }
    }
}
