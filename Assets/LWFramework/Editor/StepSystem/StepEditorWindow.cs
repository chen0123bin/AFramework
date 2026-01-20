using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LWStep.Editor
{
    public class StepEditorWindow : EditorWindow
    {
        private StepEditorGraphData m_Data;
        private StepGraphView m_GraphView;
        private VisualElement m_GraphContainer;
        private VisualElement m_RightPanel;
        private IMGUIContainer m_Inspector;
        private StepEditorUndoState m_UndoState;
        private bool m_IsApplyingUndo;

        private StepEditorNodeData m_SelectedNode;
        private StepEditorEdgeData m_SelectedEdge;
        private Edge m_SelectedEdgeView;

        private class StepEditorUndoState : ScriptableObject
        {
            public string GraphJson;
        }

        [MenuItem("LWFramework/Step/Step Editor")]
        public static void ShowWindow()
        {
            StepEditorWindow window = GetWindow<StepEditorWindow>();
            window.titleContent = new GUIContent("Step Editor");
        }

        private void OnEnable()
        {
            if (m_Data == null)
            {
                m_Data = new StepEditorGraphData();
            }
            EnsureUndoState();
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();

            Toolbar toolbar = new Toolbar();
            Button newButton = new Button(OnNewGraph) { text = "新建" };
            Button importButton = new Button(OnImportXml) { text = "导入XML" };
            Button exportButton = new Button(OnExportXml) { text = "导出XML" };
            Button validateButton = new Button(OnValidateGraph) { text = "校验" };
            Button frameButton = new Button(OnFrameAll) { text = "居中" };
            toolbar.Add(newButton);
            toolbar.Add(importButton);
            toolbar.Add(exportButton);
            toolbar.Add(validateButton);
            toolbar.Add(frameButton);
            rootVisualElement.Add(toolbar);

            VisualElement body = new VisualElement();
            body.style.flexGrow = 1;
            body.style.flexDirection = FlexDirection.Row;
            rootVisualElement.Add(body);

            m_GraphContainer = new VisualElement();
            m_GraphContainer.style.flexGrow = 1;
            body.Add(m_GraphContainer);

            m_RightPanel = new VisualElement();
            m_RightPanel.style.width = 320;
            m_RightPanel.style.flexShrink = 0;
            body.Add(m_RightPanel);

            m_Inspector = new IMGUIContainer(DrawInspectorGUI);
            m_Inspector.style.flexGrow = 1;
            m_RightPanel.Add(m_Inspector);

            LoadGraphData(m_Data);
            SaveUndoSnapshot("初始化");
        }

        private void LoadGraphData(StepEditorGraphData data)
        {
            m_Data = data;
            m_SelectedNode = null;
            m_SelectedEdge = null;
            m_SelectedEdgeView = null;

            if (m_GraphView != null)
            {
                m_GraphView.SelectionChanged -= OnSelectionChanged;
                m_GraphView.GraphChanged -= OnGraphChanged;
                m_GraphView.RemoveFromHierarchy();
            }

            m_GraphView = new StepGraphView(m_Data);
            m_GraphView.SelectionChanged += OnSelectionChanged;
            m_GraphView.GraphChanged += OnGraphChanged;
            m_GraphView.style.flexGrow = 1;
            m_GraphContainer.Add(m_GraphView);
        }

        private void OnSelectionChanged(List<ISelectable> selection)
        {
            m_SelectedNode = null;
            m_SelectedEdge = null;
            m_SelectedEdgeView = null;

            if (selection != null && selection.Count > 0)
            {
                StepNodeView nodeView = selection[0] as StepNodeView;
                if (nodeView != null)
                {
                    m_SelectedNode = nodeView.Data;
                    return;
                }
                Edge edge = selection[0] as Edge;
                if (edge != null)
                {
                    m_SelectedEdgeView = edge;
                    m_SelectedEdge = edge.userData as StepEditorEdgeData;
                }
            }
        }

        private void DrawInspectorGUI()
        {
            if (m_Data == null)
            {
                return;
            }

            if (m_SelectedNode != null)
            {
                DrawNodeInspector();
                return;
            }

            if (m_SelectedEdge != null)
            {
                DrawEdgeInspector();
                return;
            }

            DrawGraphInspector();
        }

        private void DrawNodeInspector()
        {
            EditorGUILayout.LabelField("节点", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            string newId = EditorGUILayout.TextField("节点ID", m_SelectedNode.Id);
            if (newId != m_SelectedNode.Id)
            {
                TryRenameNode(m_SelectedNode, newId);
            }
            m_SelectedNode.Name = EditorGUILayout.TextField("节点名称", m_SelectedNode.Name);
            m_SelectedNode.Position = EditorGUILayout.Vector2Field("位置", m_SelectedNode.Position);
            if (EditorGUI.EndChangeCheck())
            {
                SaveUndoSnapshot("编辑节点");
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("动作", EditorStyles.boldLabel);

            for (int i = 0; i < m_SelectedNode.Actions.Count; i++)
            {
                StepEditorActionData action = m_SelectedNode.Actions[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUI.BeginChangeCheck();
                action.TypeName = EditorGUILayout.TextField("类型", action.TypeName);

                for (int j = 0; j < action.Parameters.Count; j++)
                {
                    StepEditorParameterData param = action.Parameters[j];
                    EditorGUILayout.BeginHorizontal();
                    param.Key = EditorGUILayout.TextField(param.Key, GUILayout.Width(120));
                    param.Value = EditorGUILayout.TextField(param.Value);
                    if (GUILayout.Button("删除", GUILayout.Width(50)))
                    {
                        action.Parameters.RemoveAt(j);
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (EditorGUI.EndChangeCheck())
                {
                    SaveUndoSnapshot("编辑动作");
                }

                if (GUILayout.Button("新增参数"))
                {
                    action.Parameters.Add(new StepEditorParameterData());
                    SaveUndoSnapshot("新增参数");
                }

                if (GUILayout.Button("删除动作"))
                {
                    m_SelectedNode.Actions.RemoveAt(i);
                    EditorGUILayout.EndVertical();
                    SaveUndoSnapshot("删除动作");
                    break;
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("新增动作"))
            {
                m_SelectedNode.Actions.Add(new StepEditorActionData());
                SaveUndoSnapshot("新增动作");
            }

            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("设为开始节点"))
                {
                    m_GraphView.SetStartNode(m_SelectedNode.Id);
                    SaveUndoSnapshot("设置开始节点");
                }
                if (GUILayout.Button("删除节点"))
                {
                    StepEditorNodeData nodeToRemove = m_SelectedNode;
                    m_SelectedNode = null;
                    m_GraphView.RemoveNode(nodeToRemove);
                    SaveUndoSnapshot("删除节点");
                }
            }
        }

        private void DrawEdgeInspector()
        {
            EditorGUILayout.LabelField("连线", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("起点", m_SelectedEdge.FromId);
            EditorGUILayout.LabelField("终点", m_SelectedEdge.ToId);
            EditorGUI.BeginChangeCheck();
            m_SelectedEdge.Priority = EditorGUILayout.IntField("优先级", m_SelectedEdge.Priority);
            m_SelectedEdge.Condition = EditorGUILayout.TextField("条件", m_SelectedEdge.Condition);
            m_SelectedEdge.Tag = EditorGUILayout.TextField("标签", m_SelectedEdge.Tag);
            if (EditorGUI.EndChangeCheck())
            {
                SaveUndoSnapshot("编辑连线");
            }

            if (GUILayout.Button("删除连线"))
            {
                if (m_SelectedEdgeView != null)
                {
                    m_GraphView.RemoveElement(m_SelectedEdgeView);
                    m_SelectedEdgeView = null;
                    m_SelectedEdge = null;
                    SaveUndoSnapshot("删除连线");
                }
            }
        }

        private void DrawGraphInspector()
        {
            EditorGUILayout.LabelField("图设置", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            m_Data.GraphId = EditorGUILayout.TextField("图ID", m_Data.GraphId);

            string[] nodeIds = GetNodeIdList();
            if (nodeIds.Length > 0)
            {
                int currentIndex = GetStartNodeIndex(nodeIds, m_Data.StartNodeId);
                int newIndex = EditorGUILayout.Popup("开始节点", currentIndex, nodeIds);
                if (newIndex >= 0 && newIndex < nodeIds.Length)
                {
                    m_GraphView.SetStartNode(nodeIds[newIndex]);
                }
            }
            else
            {
                EditorGUILayout.LabelField("开始节点", "无节点");
            }

            if (EditorGUI.EndChangeCheck())
            {
                SaveUndoSnapshot("编辑图设置");
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("节点数", m_Data.Nodes.Count.ToString());
            EditorGUILayout.LabelField("连线数", m_Data.Edges.Count.ToString());
        }

        private void TryRenameNode(StepEditorNodeData node, string newId)
        {
            if (string.IsNullOrEmpty(newId))
            {
                EditorUtility.DisplayDialog("节点ID非法", "节点ID不能为空", "确定");
                return;
            }
            if (m_Data.GetNode(newId) != null)
            {
                EditorUtility.DisplayDialog("节点ID重复", "节点ID已存在，请更换", "确定");
                return;
            }

            string oldId = node.Id;
            node.Id = newId;
            if (node.Name == oldId)
            {
                node.Name = newId;
            }

            for (int i = 0; i < m_Data.Edges.Count; i++)
            {
                StepEditorEdgeData edge = m_Data.Edges[i];
                if (edge.FromId == oldId)
                {
                    edge.FromId = newId;
                }
                if (edge.ToId == oldId)
                {
                    edge.ToId = newId;
                }
            }

            if (m_Data.StartNodeId == oldId)
            {
                m_Data.StartNodeId = newId;
            }

            m_GraphView.RenameNodeId(oldId, newId);
            SaveUndoSnapshot("重命名节点");
        }

        private string[] GetNodeIdList()
        {
            int count = m_Data.Nodes.Count;
            string[] result = new string[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = m_Data.Nodes[i].Id;
            }
            return result;
        }

        private int GetStartNodeIndex(string[] nodeIds, string startId)
        {
            for (int i = 0; i < nodeIds.Length; i++)
            {
                if (nodeIds[i] == startId)
                {
                    return i;
                }
            }
            return 0;
        }

        private void OnNewGraph()
        {
            StepEditorGraphData data = new StepEditorGraphData();
            LoadGraphData(data);
            SaveUndoSnapshot("新建图");
        }

        private void OnImportXml()
        {
            string path = EditorUtility.OpenFilePanel("导入步骤XML", Application.dataPath, "xml");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            string xmlText = File.ReadAllText(path);
            StepEditorGraphData data = StepXmlImporter.LoadFromText(xmlText);
            if (data == null)
            {
                EditorUtility.DisplayDialog("导入失败", "XML 解析失败", "确定");
                return;
            }
            LoadGraphData(data);
            SaveUndoSnapshot("导入图");
        }

        private void OnExportXml()
        {
            string path = EditorUtility.SaveFilePanel("导出步骤XML", Application.dataPath, "StepGraph.xml", "xml");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            StepXmlExporter.SaveToFile(path, m_Data);
            AssetDatabase.Refresh();
        }

        private void OnValidateGraph()
        {
            List<string> errors = ValidateGraphData();
            if (errors.Count == 0)
            {
                EditorUtility.DisplayDialog("校验通过", "步骤图无错误", "确定");
                return;
            }
            string message = string.Join("\n", errors);
            EditorUtility.DisplayDialog("校验失败", message, "确定");
        }

        private void OnFrameAll()
        {
            if (m_GraphView != null)
            {
                m_GraphView.FrameAll();
            }
        }

        private void OnGraphChanged()
        {
            SaveUndoSnapshot("修改步骤图");
        }

        private void EnsureUndoState()
        {
            if (m_UndoState == null)
            {
                m_UndoState = CreateInstance<StepEditorUndoState>();
                m_UndoState.hideFlags = HideFlags.HideAndDontSave;
                m_UndoState.GraphJson = string.Empty;
            }
        }

        private void SaveUndoSnapshot(string actionName)
        {
            if (m_IsApplyingUndo)
            {
                return;
            }
            EnsureUndoState();
            Undo.RegisterCompleteObjectUndo(m_UndoState, actionName);
            m_UndoState.GraphJson = JsonUtility.ToJson(m_Data);
            EditorUtility.SetDirty(m_UndoState);
        }

        private void OnUndoRedo()
        {
            if (m_UndoState == null)
            {
                return;
            }
            if (string.IsNullOrEmpty(m_UndoState.GraphJson))
            {
                return;
            }
            m_IsApplyingUndo = true;
            StepEditorGraphData data = JsonUtility.FromJson<StepEditorGraphData>(m_UndoState.GraphJson);
            if (data == null)
            {
                data = new StepEditorGraphData();
            }
            LoadGraphData(data);
            m_IsApplyingUndo = false;
        }

        private List<string> ValidateGraphData()
        {
            List<string> errors = new List<string>();
            if (string.IsNullOrEmpty(m_Data.GraphId))
            {
                errors.Add("图ID不能为空");
            }
            if (m_Data.Nodes.Count == 0)
            {
                errors.Add("步骤图节点为空");
                return errors;
            }

            HashSet<string> nodeIds = new HashSet<string>();
            for (int i = 0; i < m_Data.Nodes.Count; i++)
            {
                StepEditorNodeData node = m_Data.Nodes[i];
                if (string.IsNullOrEmpty(node.Id))
                {
                    errors.Add("存在空节点ID");
                    continue;
                }
                if (!nodeIds.Add(node.Id))
                {
                    errors.Add("节点ID重复: " + node.Id);
                }
            }

            if (!string.IsNullOrEmpty(m_Data.StartNodeId) && !nodeIds.Contains(m_Data.StartNodeId))
            {
                errors.Add("开始节点不存在: " + m_Data.StartNodeId);
            }

            StepGraph graph = new StepGraph(m_Data.GraphId, m_Data.StartNodeId);
            for (int i = 0; i < m_Data.Nodes.Count; i++)
            {
                StepEditorNodeData node = m_Data.Nodes[i];
                StepNode stepNode = new StepNode(node.Id, node.Name);
                graph.AddNode(stepNode);
            }
            for (int i = 0; i < m_Data.Edges.Count; i++)
            {
                StepEditorEdgeData edge = m_Data.Edges[i];
                StepEdge stepEdge = new StepEdge(edge.FromId, edge.ToId, edge.Priority, edge.Condition, edge.Tag);
                graph.AddEdge(stepEdge);
            }
            graph.BuildIndex();
            List<string> graphErrors = graph.Validate();
            for (int i = 0; i < graphErrors.Count; i++)
            {
                errors.Add(graphErrors[i]);
            }

            AppendReachableAndIsolatedErrors(errors, nodeIds);
            return errors;
        }

        /// <summary>
        /// 追加不可达与孤立节点校验结果
        /// </summary>
        private void AppendReachableAndIsolatedErrors(List<string> errors, HashSet<string> nodeIds)
        {
            Dictionary<string, List<string>> outgoing = new Dictionary<string, List<string>>();
            Dictionary<string, int> inDegrees = new Dictionary<string, int>();
            Dictionary<string, int> outDegrees = new Dictionary<string, int>();

            foreach (string nodeId in nodeIds)
            {
                inDegrees.Add(nodeId, 0);
                outDegrees.Add(nodeId, 0);
            }

            for (int i = 0; i < m_Data.Edges.Count; i++)
            {
                StepEditorEdgeData edge = m_Data.Edges[i];
                if (!nodeIds.Contains(edge.FromId) || !nodeIds.Contains(edge.ToId))
                {
                    continue;
                }

                List<string> list;
                if (!outgoing.TryGetValue(edge.FromId, out list))
                {
                    list = new List<string>();
                    outgoing.Add(edge.FromId, list);
                }
                list.Add(edge.ToId);
                outDegrees[edge.FromId] += 1;
                inDegrees[edge.ToId] += 1;
            }

            AppendIsolatedNodeErrors(errors, nodeIds, inDegrees, outDegrees);
            AppendUnreachableNodeErrors(errors, nodeIds, outgoing);
        }

        /// <summary>
        /// 追加孤立节点提示（无入边无出边）
        /// </summary>
        private void AppendIsolatedNodeErrors(List<string> errors, HashSet<string> nodeIds, Dictionary<string, int> inDegrees, Dictionary<string, int> outDegrees)
        {
            foreach (string nodeId in nodeIds)
            {
                if (inDegrees[nodeId] == 0 && outDegrees[nodeId] == 0)
                {
                    errors.Add("孤立节点: " + nodeId);
                }
            }
        }

        /// <summary>
        /// 追加不可达节点提示（从开始节点无法到达）
        /// </summary>
        private void AppendUnreachableNodeErrors(List<string> errors, HashSet<string> nodeIds, Dictionary<string, List<string>> outgoing)
        {
            if (string.IsNullOrEmpty(m_Data.StartNodeId))
            {
                return;
            }
            if (!nodeIds.Contains(m_Data.StartNodeId))
            {
                return;
            }

            HashSet<string> reachable = new HashSet<string>();
            CollectReachableNodes(m_Data.StartNodeId, outgoing, reachable);
            foreach (string nodeId in nodeIds)
            {
                if (!reachable.Contains(nodeId))
                {
                    errors.Add("不可达节点: " + nodeId);
                }
            }
        }

        /// <summary>
        /// 收集从指定起点可达的节点集合
        /// </summary>
        private void CollectReachableNodes(string startNodeId, Dictionary<string, List<string>> outgoing, HashSet<string> reachable)
        {
            Queue<string> queue = new Queue<string>();
            reachable.Add(startNodeId);
            queue.Enqueue(startNodeId);

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                List<string> list;
                if (!outgoing.TryGetValue(current, out list))
                {
                    continue;
                }
                for (int i = 0; i < list.Count; i++)
                {
                    string next = list[i];
                    if (reachable.Contains(next))
                    {
                        continue;
                    }
                    reachable.Add(next);
                    queue.Enqueue(next);
                }
            }
        }
    }
}
