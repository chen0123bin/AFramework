using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace LWStep.Editor
{
    public class StepGraphView : GraphView
    {
        private StepEditorGraphData m_Data;
        private Dictionary<string, StepNodeView> m_NodeViews;
        public System.Action<System.Collections.Generic.List<ISelectable>> SelectionChanged;
        public System.Action GraphChanged;
        private bool m_IsPanning;
        private Vector2 m_LastMousePosition;
        private Port m_PendingOutputPort;
        private Vector2 m_LastMouseWorldPosition;

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
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUpForPan);

            graphViewChanged = OnGraphViewChanged;

            RebuildView();
        }

        public StepEditorGraphData GetData()
        {
            return m_Data;
        }

        public StepNodeView GetNodeView(string nodeId)
        {
            StepNodeView nodeView;
            if (m_NodeViews.TryGetValue(nodeId, out nodeView))
            {
                return nodeView;
            }
            return null;
        }

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
                edge.userData = edgeData;
                AddElement(edge);
            }

            UpdateAllNodeTitles();
        }

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

        public StepNodeView AddNodeAtLastMousePosition()
        {
            Vector2 localPos = GetLocalMousePosition();
            return AddNode(localPos);
        }

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

        public void SetStartNode(string nodeId)
        {
            m_Data.StartNodeId = nodeId;
            UpdateAllNodeTitles();
            NotifyGraphChanged();
        }

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

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            evt.menu.AppendAction("新增节点", action =>
            {
                AddNodeAtLastMousePosition();
            }, DropdownMenuAction.Status.Normal);
        }

        private StepNodeView CreateNodeView(StepEditorNodeData data)
        {
            StepNodeView nodeView = new StepNodeView(data);
            nodeView.BindPortCallbacks(OnInputPortClicked, OnOutputPortClicked);
            nodeView.DragEnded += OnNodeDragEnded;
            return nodeView;
        }

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
                        edge.userData = existing;
                        continue;
                    }
                    StepEditorEdgeData edgeData = new StepEditorEdgeData();
                    edgeData.FromId = fromView.Data.Id;
                    edgeData.ToId = toView.Data.Id;
                    edgeData.Priority = 0;
                    edgeData.Condition = string.Empty;
                    edgeData.Tag = string.Empty;
                    m_Data.Edges.Add(edgeData);
                    edge.userData = edgeData;
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

        private void UpdateAllNodeTitles()
        {
            foreach (KeyValuePair<string, StepNodeView> kvp in m_NodeViews)
            {
                kvp.Value.UpdateTitle(m_Data.StartNodeId);
            }
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            DispatchSelectionChanged();
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            m_LastMouseWorldPosition = this.LocalToWorld(evt.mousePosition);
            if (evt.button != 2)
            {
                return;
            }
            m_IsPanning = true;
            m_LastMousePosition = evt.mousePosition;
            evt.StopPropagation();
        }

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

        private void OnKeyUp(KeyUpEvent evt)
        {
            DispatchSelectionChanged();
        }

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
            edge.userData = edgeData;
            outputPort.Connect(edge);
            inputPort.Connect(edge);
            AddElement(edge);
            NotifyGraphChanged();
        }

        private Vector2 GetLocalMousePosition()
        {
            Vector2 worldPos = m_LastMouseWorldPosition;
            return contentViewContainer.WorldToLocal(worldPos);
        }

        private void OnNodeDragEnded(StepNodeView nodeView)
        {
            NotifyGraphChanged();
        }

        private void NotifyGraphChanged()
        {
            if (GraphChanged != null)
            {
                GraphChanged();
            }
        }
    }
}
