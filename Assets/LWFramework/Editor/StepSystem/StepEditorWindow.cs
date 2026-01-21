using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using LWCore;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LWStep.Editor
{
    public class StepEditorWindow : EditorWindow
    {
        private const string PREVIEW_XML_PATH_KEY = "LWStep.StepEditor.Preview.XmlPath";
        private const string PREVIEW_START_NODE_ID_KEY = "LWStep.StepEditor.Preview.StartNodeId";
        private const string PREVIEW_JUMP_NODE_ID_KEY = "LWStep.StepEditor.Preview.JumpNodeId";
        private const string PREVIEW_REQUIRED_TAG_KEY = "LWStep.StepEditor.Preview.RequiredTag";
        private const string PREVIEW_ENABLED_KEY = "LWStep.StepEditor.Preview.Enabled";

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

        private static Type[] s_ActionTypes;
        private static string[] s_ActionTypeNames;
        private static Dictionary<Type, List<ActionParamMember>> s_ActionParamMembersCache = new Dictionary<Type, List<ActionParamMember>>();


        private TextAsset m_PreviewXmlAsset;
        private string m_PreviewXmlPath;
        private string m_PreviewStartNodeId;
        private string m_PreviewJumpNodeId;
        private string m_PreviewRequiredTag;
        private string m_RuntimeNodeId;
        private Vector2 m_NodeInspectorScroll;

        private class ActionParamMember
        {
            public string Key;
            public Type ValueType;
            public FieldInfo Field;
            public PropertyInfo Property;
        }

        private class StepEditorUndoState : ScriptableObject
        {
            public string GraphJson;
        }

        [MenuItem("LWFramework/Step/Step Editor")]
        /// <summary>
        /// 打开步骤图编辑器窗口
        /// </summary>
        public static void ShowWindow()
        {
            StepEditorWindow window = GetWindow<StepEditorWindow>();
            window.titleContent = new GUIContent("Step Editor");
        }

        /// <summary>
        /// 窗口启用时初始化数据与回调
        /// </summary>
        private void OnEnable()
        {
            if (m_Data == null)
            {
                m_Data = new StepEditorGraphData();
            }
            EnsureUndoState();
            LoadPreviewSettings();
            RefreshActionTypes();
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.update += OnEditorUpdate;
        }

        /// <summary>
        /// 窗口关闭/禁用时解绑回调
        /// </summary>
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.update -= OnEditorUpdate;
        }

        /// <summary>
        /// 构建UI Toolkit界面
        /// </summary>
        public void CreateGUI()
        {
            rootVisualElement.Clear();

            Toolbar toolbar = new Toolbar();
            Button newButton = new Button(OnNewGraph) { text = "新建" };
            Button importButton = new Button(OnImportXml) { text = "导入XML" };
            Button exportButton = new Button(OnExportXml) { text = "导出XML" };
            Button validateButton = new Button(OnValidateGraph) { text = "校验" };
            Button frameButton = new Button(OnFrameAll) { text = "居中" };
            Button previewButton = new Button(OnPreviewPlayMode) { text = "预览PlayMode" };
            toolbar.Add(newButton);
            toolbar.Add(importButton);
            toolbar.Add(exportButton);
            toolbar.Add(validateButton);
            toolbar.Add(frameButton);
            toolbar.Add(previewButton);
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

        /// <summary>
        /// 加载图数据并重建GraphView
        /// </summary>
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

        /// <summary>
        /// 选中项变化：更新当前选中节点/连线
        /// </summary>
        private void OnSelectionChanged(List<ISelectable> selection)
        {
            Debug.Log($"OnSelectionChanged: {selection.Count}");
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

        /// <summary>
        /// 编辑器更新时同步运行时节点显示
        /// </summary>
        private void OnEditorUpdate()
        {
            if (m_GraphView == null)
            {
                return;
            }
            if (!EditorApplication.isPlaying)
            {
                if (!string.IsNullOrEmpty(m_RuntimeNodeId))
                {
                    m_RuntimeNodeId = string.Empty;
                    m_GraphView.SetRuntimeNodeId(string.Empty);
                }
                return;
            }
            IStepManager stepManager = ManagerUtility.StepMgr;
            if (stepManager == null || !stepManager.IsRunning)
            {
                if (!string.IsNullOrEmpty(m_RuntimeNodeId))
                {
                    m_RuntimeNodeId = string.Empty;
                    m_GraphView.SetRuntimeNodeId(string.Empty);
                }
                return;
            }
            string currentNodeId = stepManager.CurrentNodeId;
            if (m_RuntimeNodeId == currentNodeId)
            {
                return;
            }
            m_RuntimeNodeId = currentNodeId;
            m_GraphView.SetRuntimeNodeId(m_RuntimeNodeId);
        }

        /// <summary>
        /// 绘制右侧检查器面板（根据选中项切换节点/连线/图设置）
        /// </summary>
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

        /// <summary>
        /// 绘制节点面板（节点基本信息/动作列表/快捷操作）
        /// </summary>
        private void DrawNodeInspector()
        {
            m_NodeInspectorScroll = EditorGUILayout.BeginScrollView(m_NodeInspectorScroll, false, true);
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
                DrawActionTypeSelector(action);
                DrawActionTypedParameters(action);

                if (EditorGUI.EndChangeCheck())
                {
                    SaveUndoSnapshot("编辑动作");
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
                if (GUILayout.Button("跳转到此节点"))
                {
                    JumpToRuntimeNode(m_SelectedNode.Id);
                }
                if (GUILayout.Button("删除节点"))
                {
                    StepEditorNodeData nodeToRemove = m_SelectedNode;
                    m_SelectedNode = null;
                    m_GraphView.RemoveNode(nodeToRemove);
                    SaveUndoSnapshot("删除节点");
                }
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 运行时跳转到指定节点
        /// </summary>
        private void JumpToRuntimeNode(string nodeId)
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }
            if (string.IsNullOrEmpty(nodeId))
            {
                return;
            }
            IStepManager stepManager = ManagerUtility.StepMgr;
            if (stepManager == null || !stepManager.IsRunning)
            {
                return;
            }
            stepManager.JumpTo(nodeId);
        }



        private void RefreshActionTypes()
        {
            List<Type> typeList = new List<Type>();

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly assembly = assemblies[i];
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }

                if (types == null)
                {
                    continue;
                }

                for (int j = 0; j < types.Length; j++)
                {
                    Type type = types[j];
                    if (type == null)
                    {
                        continue;
                    }
                    if (type.IsAbstract)
                    {
                        continue;
                    }
                    if (!typeof(BaseStepAction).IsAssignableFrom(type))
                    {
                        continue;
                    }
                    typeList.Add(type);
                }
            }

            typeList.Sort(CompareTypeFullName);
            s_ActionTypes = typeList.ToArray();
            s_ActionTypeNames = new string[s_ActionTypes.Length];
            for (int i = 0; i < s_ActionTypes.Length; i++)
            {
                Type type = s_ActionTypes[i];
                s_ActionTypeNames[i] = type.FullName != null ? type.FullName : type.Name;
            }
        }

        private static int CompareTypeFullName(Type a, Type b)
        {
            string aName = a != null ? (a.FullName != null ? a.FullName : a.Name) : string.Empty;
            string bName = b != null ? (b.FullName != null ? b.FullName : b.Name) : string.Empty;
            return string.CompareOrdinal(aName, bName);
        }

        private static Type FindActionType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            if (s_ActionTypes != null)
            {
                for (int i = 0; i < s_ActionTypes.Length; i++)
                {
                    Type type = s_ActionTypes[i];
                    if (type != null && string.Equals(type.FullName, typeName, StringComparison.Ordinal))
                    {
                        return type;
                    }
                }
            }

            Type directType = Type.GetType(typeName);
            if (directType != null)
            {
                return directType;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type[] types;
                try
                {
                    types = assemblies[i].GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }

                if (types == null)
                {
                    continue;
                }

                for (int j = 0; j < types.Length; j++)
                {
                    Type type = types[j];
                    if (type == null)
                    {
                        continue;
                    }
                    if (string.Equals(type.FullName, typeName, StringComparison.Ordinal))
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        private static List<ActionParamMember> GetOrCreateActionParamMembers(Type actionType)
        {
            if (actionType == null)
            {
                return null;
            }

            List<ActionParamMember> cached;
            if (s_ActionParamMembersCache.TryGetValue(actionType, out cached))
            {
                return cached;
            }

            List<ActionParamMember> members = new List<ActionParamMember>();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            FieldInfo[] fields = actionType.GetFields(flags);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (field == null)
                {
                    continue;
                }

                StepParamAttribute attr = Attribute.GetCustomAttribute(field, typeof(StepParamAttribute), true) as StepParamAttribute;
                if (attr == null || string.IsNullOrEmpty(attr.Key))
                {
                    continue;
                }

                ActionParamMember member = new ActionParamMember();
                member.Key = attr.Key;
                member.ValueType = field.FieldType;
                member.Field = field;
                member.Property = null;
                members.Add(member);
            }

            PropertyInfo[] properties = actionType.GetProperties(flags);
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];
                if (property == null || !property.CanWrite || !property.CanRead)
                {
                    continue;
                }
                if (property.GetIndexParameters() != null && property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                StepParamAttribute attr = Attribute.GetCustomAttribute(property, typeof(StepParamAttribute), true) as StepParamAttribute;
                if (attr == null || string.IsNullOrEmpty(attr.Key))
                {
                    continue;
                }

                ActionParamMember member = new ActionParamMember();
                member.Key = attr.Key;
                member.ValueType = property.PropertyType;
                member.Field = null;
                member.Property = property;
                members.Add(member);
            }

            s_ActionParamMembersCache[actionType] = members;
            return members;
        }
        /// <summary>
        /// 绘制步骤动作的类型选择器
        /// </summary>
        /// <param name="action"></param>
        private void DrawActionTypeSelector(StepEditorActionData action)
        {
            if (action == null)
            {
                return;
            }
            if (s_ActionTypeNames == null)
            {
                RefreshActionTypes();
            }

            if (s_ActionTypeNames == null || s_ActionTypeNames.Length == 0)
            {
                EditorGUILayout.HelpBox("未找到可用的步骤动作类型（BaseStepAction 派生类）", MessageType.Info);
                action.TypeName = EditorGUILayout.TextField("类型", action.TypeName);
                return;
            }

            string currentTypeName = action.TypeName != null ? action.TypeName : string.Empty;
            int matchIndex = -1;
            for (int i = 0; i < s_ActionTypeNames.Length; i++)
            {
                if (string.Equals(s_ActionTypeNames[i], currentTypeName, StringComparison.Ordinal))
                {
                    matchIndex = i;
                    break;
                }
            }

            List<string> options = new List<string>(s_ActionTypeNames.Length + 1);
            options.Add("未选择");
            for (int i = 0; i < s_ActionTypeNames.Length; i++)
            {
                options.Add(s_ActionTypeNames[i]);
            }

            int currentIndex = matchIndex >= 0 ? matchIndex + 1 : 0;
            int newIndex = EditorGUILayout.Popup("类型", currentIndex, options.ToArray());
            if (newIndex != currentIndex)
            {
                if (newIndex == 0)
                {
                    action.TypeName = matchIndex >= 0 ? string.Empty : currentTypeName;
                }
                else
                {

                    int typeIndex = newIndex - 1;
                    if (typeIndex >= 0 && typeIndex < s_ActionTypeNames.Length)
                    {
                        action.TypeName = s_ActionTypeNames[typeIndex];
                    }
                    //切换不同type 重置参数
                    ResetActionParameters(action, action.TypeName);
                }
            }


        }

        private void ResetActionParameters(StepEditorActionData action, string typeName)
        {
            if (action.Parameters == null)
            {
                action.Parameters = new List<StepEditorParameterData>();
            }
            else
            {
                action.Parameters.Clear();
            }

            Type actionType = FindActionType(typeName);
            if (actionType == null)
            {
                return;
            }

            List<ActionParamMember> members = GetOrCreateActionParamMembers(actionType);
            if (members == null || members.Count == 0)
            {
                return;
            }

            for (int i = 0; i < members.Count; i++)
            {
                ActionParamMember member = members[i];
                if (member == null || string.IsNullOrEmpty(member.Key) || member.ValueType == null)
                {
                    continue;
                }

                object defaultValue = GetDefaultValue(member.ValueType);
                string rawValue = ConvertToRawString(defaultValue, member.ValueType);
                StepEditorParameterData param = new StepEditorParameterData();
                param.Key = member.Key;
                param.Value = rawValue;
                action.Parameters.Add(param);
            }
        }
        /// <summary>
        /// 绘制步骤动作的类型化参数
        /// </summary>
        /// <param name="action"></param>
        private void DrawActionTypedParameters(StepEditorActionData action)
        {
            if (action == null)
            {
                return;
            }

            Type actionType = FindActionType(action.TypeName);
            if (actionType == null)
            {
                return;
            }

            List<ActionParamMember> members = GetOrCreateActionParamMembers(actionType);
            if (members == null || members.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("参数（类型化）", EditorStyles.boldLabel);

            for (int i = 0; i < members.Count; i++)
            {
                ActionParamMember member = members[i];
                if (member == null || string.IsNullOrEmpty(member.Key) || member.ValueType == null)
                {
                    continue;
                }

                string rawValue;
                int existingIndex;
                bool hasRawValue = TryGetParameter(action.Parameters, member.Key, out rawValue, out existingIndex);

                object currentValue;
                if (hasRawValue)
                {
                    object parsedValue;
                    if (TryParseEditorValue(rawValue, member.ValueType, out parsedValue))
                    {
                        currentValue = parsedValue;
                    }
                    else
                    {
                        currentValue = GetDefaultValue(member.ValueType);
                    }
                }
                else
                {
                    currentValue = GetDefaultValue(member.ValueType);
                }

                EditorGUI.BeginChangeCheck();
                object newValue = DrawValueField(member.Key, member.ValueType, currentValue);
                if (EditorGUI.EndChangeCheck())
                {
                    string newRawValue = ConvertToRawString(newValue, member.ValueType);
                    SetOrUpdateParameter(action.Parameters, member.Key, newRawValue);
                }
            }
        }


        private static bool TryGetParameter(List<StepEditorParameterData> parameters, string key, out string value, out int index)
        {
            value = string.Empty;
            index = -1;
            if (parameters == null || parameters.Count == 0 || string.IsNullOrEmpty(key))
            {
                return false;
            }

            for (int i = 0; i < parameters.Count; i++)
            {
                StepEditorParameterData param = parameters[i];
                if (param != null && string.Equals(param.Key, key, StringComparison.Ordinal))
                {
                    value = param.Value != null ? param.Value : string.Empty;
                    index = i;
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// 设置或更新步骤动作的参数
        /// </summary>
        /// <param name="parameters">参数数据</param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private static void SetOrUpdateParameter(List<StepEditorParameterData> parameters, string key, string value)
        {
            if (parameters == null)
            {
                return;
            }
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            for (int i = 0; i < parameters.Count; i++)
            {
                StepEditorParameterData param = parameters[i];
                if (param != null && string.Equals(param.Key, key, StringComparison.Ordinal))
                {
                    param.Value = value;
                    return;
                }
            }

            StepEditorParameterData newParam = new StepEditorParameterData();
            newParam.Key = key;
            newParam.Value = value;
            parameters.Add(newParam);
        }

        private static object DrawValueField(string label, Type valueType, object value)
        {
            if (valueType == typeof(string))
            {
                string current = value as string;
                if (current == null)
                {
                    current = string.Empty;
                }
                return EditorGUILayout.TextField(label, current);
            }

            if (valueType == typeof(int))
            {
                int current = value is int ? (int)value : 0;
                return EditorGUILayout.IntField(label, current);
            }

            if (valueType == typeof(float))
            {
                float current = value is float ? (float)value : 0f;
                return EditorGUILayout.FloatField(label, current);
            }

            if (valueType == typeof(double))
            {
                double current = value is double ? (double)value : 0d;
                return EditorGUILayout.DoubleField(label, current);
            }

            if (valueType == typeof(long))
            {
                long current = value is long ? (long)value : 0L;
                return EditorGUILayout.LongField(label, current);
            }

            if (valueType == typeof(bool))
            {
                bool current = value is bool && (bool)value;
                return EditorGUILayout.Toggle(label, current);
            }

            if (valueType.IsEnum)
            {
                Enum current = value as Enum;
                if (current == null)
                {
                    Array values = Enum.GetValues(valueType);
                    if (values != null && values.Length > 0)
                    {
                        current = (Enum)values.GetValue(0);
                    }
                }
                if (current != null)
                {
                    return EditorGUILayout.EnumPopup(label, current);
                }
                return value;
            }

            EditorGUILayout.LabelField(label, "不支持类型: " + valueType.Name);
            return value;
        }

        private static object GetDefaultValue(Type valueType)
        {
            if (valueType == typeof(string))
            {
                return string.Empty;
            }
            if (valueType == typeof(int))
            {
                return 0;
            }
            if (valueType == typeof(float))
            {
                return 0f;
            }
            if (valueType == typeof(double))
            {
                return 0d;
            }
            if (valueType == typeof(long))
            {
                return 0L;
            }
            if (valueType == typeof(bool))
            {
                return false;
            }
            if (valueType != null && valueType.IsEnum)
            {
                Array values = Enum.GetValues(valueType);
                if (values != null && values.Length > 0)
                {
                    return values.GetValue(0);
                }
            }
            return null;
        }

        private static bool TryParseEditorValue(string rawValue, Type valueType, out object parsedValue)
        {
            parsedValue = null;
            if (valueType == typeof(string))
            {
                parsedValue = rawValue != null ? rawValue : string.Empty;
                return true;
            }

            if (valueType == typeof(int))
            {
                int intValue;
                if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue))
                {
                    parsedValue = intValue;
                    return true;
                }
                return false;
            }

            if (valueType == typeof(float))
            {
                float floatValue;
                if (float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out floatValue))
                {
                    parsedValue = floatValue;
                    return true;
                }
                return false;
            }

            if (valueType == typeof(double))
            {
                double doubleValue;
                if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue))
                {
                    parsedValue = doubleValue;
                    return true;
                }
                return false;
            }

            if (valueType == typeof(long))
            {
                long longValue;
                if (long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue))
                {
                    parsedValue = longValue;
                    return true;
                }
                return false;
            }

            if (valueType == typeof(bool))
            {
                bool boolValue;
                if (bool.TryParse(rawValue, out boolValue))
                {
                    parsedValue = boolValue;
                    return true;
                }
                return false;
            }

            if (valueType != null && valueType.IsEnum)
            {
                try
                {
                    object enumValue = Enum.Parse(valueType, rawValue, true);
                    parsedValue = enumValue;
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private static string ConvertToRawString(object value, Type valueType)
        {
            if (valueType == typeof(string))
            {
                return value as string ?? string.Empty;
            }

            if (valueType == typeof(int))
            {
                int intValue = value is int ? (int)value : 0;
                return intValue.ToString(CultureInfo.InvariantCulture);
            }

            if (valueType == typeof(float))
            {
                float floatValue = value is float ? (float)value : 0f;
                return floatValue.ToString(CultureInfo.InvariantCulture);
            }

            if (valueType == typeof(double))
            {
                double doubleValue = value is double ? (double)value : 0d;
                return doubleValue.ToString(CultureInfo.InvariantCulture);
            }

            if (valueType == typeof(long))
            {
                long longValue = value is long ? (long)value : 0L;
                return longValue.ToString(CultureInfo.InvariantCulture);
            }

            if (valueType == typeof(bool))
            {
                bool boolValue = value is bool && (bool)value;
                return boolValue.ToString();
            }

            if (valueType != null && valueType.IsEnum)
            {
                Enum enumValue = value as Enum;
                if (enumValue != null)
                {
                    return enumValue.ToString();
                }
                return string.Empty;
            }

            return value != null ? value.ToString() : string.Empty;
        }

        /// <summary>
        /// 绘制连线面板（条件/标签/优先级）
        /// </summary>
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
                if (m_SelectedEdgeView != null)
                {
                    m_GraphView.UpdateEdgeView(m_SelectedEdgeView);
                }
                SaveUndoSnapshot("编辑连线");
            }

            string conditionError = GetEdgeConditionError(m_SelectedEdge.Condition);
            if (!string.IsNullOrEmpty(conditionError))
            {
                EditorGUILayout.HelpBox(conditionError, MessageType.Warning);
            }
            string tagError = GetEdgeTagError(m_SelectedEdge.Tag);
            if (!string.IsNullOrEmpty(tagError))
            {
                EditorGUILayout.HelpBox(tagError, MessageType.Warning);
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

        /// <summary>
        /// 绘制图设置面板（开始节点/运行时预览参数）
        /// </summary>
        private void DrawGraphInspector()
        {
            EditorGUILayout.LabelField("图设置", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
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

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("运行时预览", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            TextAsset newPreviewAsset = EditorGUILayout.ObjectField("预览XML", m_PreviewXmlAsset, typeof(TextAsset), false) as TextAsset;
            if (newPreviewAsset != m_PreviewXmlAsset)
            {
                m_PreviewXmlAsset = newPreviewAsset;
                m_PreviewXmlPath = m_PreviewXmlAsset != null ? AssetDatabase.GetAssetPath(m_PreviewXmlAsset) : string.Empty;
            }
            m_PreviewStartNodeId = EditorGUILayout.TextField("开始节点", m_PreviewStartNodeId);
            m_PreviewJumpNodeId = EditorGUILayout.TextField("定位节点", m_PreviewJumpNodeId);
            m_PreviewRequiredTag = EditorGUILayout.TextField("定位标签", m_PreviewRequiredTag);
            if (EditorGUI.EndChangeCheck())
            {
                SavePreviewSettings();
            }

            if (GUILayout.Button("进入PlayMode预览"))
            {
                OnPreviewPlayMode();
            }
        }

        /// <summary>
        /// 尝试重命名节点ID并同步更新相关连线与开始节点
        /// </summary>
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

        /// <summary>
        /// 获取当前所有节点ID列表（用于下拉选择）
        /// </summary>
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

        /// <summary>
        /// 获取开始节点在ID列表中的索引
        /// </summary>
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

        /// <summary>
        /// 新建空步骤图
        /// </summary>
        private void OnNewGraph()
        {
            StepEditorGraphData data = new StepEditorGraphData();
            LoadGraphData(data);
            SaveUndoSnapshot("新建图");
        }

        /// <summary>
        /// 从文件导入步骤图XML
        /// </summary>
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

        /// <summary>
        /// 导出当前步骤图为XML文件
        /// </summary>
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

        /// <summary>
        /// 校验当前步骤图数据并弹窗提示结果
        /// </summary>
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

        /// <summary>
        /// 视图居中显示所有节点
        /// </summary>
        private void OnFrameAll()
        {
            if (m_GraphView != null)
            {
                m_GraphView.FrameAll();
            }
        }

        /// <summary>
        /// 进入PlayMode预览（保存预览参数并切换到播放模式）
        /// </summary>
        private void OnPreviewPlayMode()
        {
            SavePreviewSettings();
            if (string.IsNullOrEmpty(m_PreviewXmlPath))
            {
                EditorUtility.DisplayDialog("预览失败", "请先选择预览XML", "确定");
                return;
            }
            EditorPrefs.SetBool(PREVIEW_ENABLED_KEY, true);
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = true;
            }
        }

        /// <summary>
        /// 读取本地保存的预览参数
        /// </summary>
        private void LoadPreviewSettings()
        {
            m_PreviewXmlPath = EditorPrefs.GetString(PREVIEW_XML_PATH_KEY, string.Empty);
            m_PreviewStartNodeId = EditorPrefs.GetString(PREVIEW_START_NODE_ID_KEY, string.Empty);
            m_PreviewJumpNodeId = EditorPrefs.GetString(PREVIEW_JUMP_NODE_ID_KEY, string.Empty);
            m_PreviewRequiredTag = EditorPrefs.GetString(PREVIEW_REQUIRED_TAG_KEY, string.Empty);

            if (!string.IsNullOrEmpty(m_PreviewXmlPath))
            {
                m_PreviewXmlAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(m_PreviewXmlPath);
            }
        }

        /// <summary>
        /// 保存预览参数到本地（EditorPrefs）
        /// </summary>
        private void SavePreviewSettings()
        {
            EditorPrefs.SetString(PREVIEW_XML_PATH_KEY, m_PreviewXmlPath ?? string.Empty);
            EditorPrefs.SetString(PREVIEW_START_NODE_ID_KEY, m_PreviewStartNodeId ?? string.Empty);
            EditorPrefs.SetString(PREVIEW_JUMP_NODE_ID_KEY, m_PreviewJumpNodeId ?? string.Empty);
            EditorPrefs.SetString(PREVIEW_REQUIRED_TAG_KEY, m_PreviewRequiredTag ?? string.Empty);
        }

        /// <summary>
        /// 图数据变更：写入Undo快照
        /// </summary>
        private void OnGraphChanged()
        {
            SaveUndoSnapshot("修改步骤图");
        }

        /// <summary>
        /// 确保Undo状态对象存在
        /// </summary>
        private void EnsureUndoState()
        {
            if (m_UndoState == null)
            {
                m_UndoState = CreateInstance<StepEditorUndoState>();
                m_UndoState.hideFlags = HideFlags.HideAndDontSave;
                m_UndoState.GraphJson = string.Empty;
            }
        }

        /// <summary>
        /// 保存Undo快照（使用Json序列化步骤图数据）
        /// </summary>
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

        /// <summary>
        /// Undo/Redo回调：从快照还原步骤图数据
        /// </summary>
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

        /// <summary>
        /// 校验步骤图数据：基础字段、条件/标签格式、DAG结构、可达与孤立节点
        /// </summary>
        private List<string> ValidateGraphData()
        {
            List<string> errors = new List<string>();
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

            for (int i = 0; i < m_Data.Edges.Count; i++)
            {
                StepEditorEdgeData edge = m_Data.Edges[i];
                string conditionError = GetEdgeConditionError(edge.Condition);
                if (!string.IsNullOrEmpty(conditionError))
                {
                    errors.Add("连线条件非法: " + edge.FromId + " -> " + edge.ToId + "，" + conditionError);
                }
                string tagError = GetEdgeTagError(edge.Tag);
                if (!string.IsNullOrEmpty(tagError))
                {
                    errors.Add("连线标签非法: " + edge.FromId + " -> " + edge.ToId + "，" + tagError);
                }
            }

            StepGraph graph = new StepGraph(m_Data.StartNodeId);
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
        /// 校验连线条件表达式格式
        /// </summary>
        private string GetEdgeConditionError(string condition)
        {
            if (string.IsNullOrEmpty(condition))
            {
                return string.Empty;
            }
            string trimmed = condition.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return string.Empty;
            }
            int notEqualIndex = trimmed.IndexOf("!=", StringComparison.Ordinal);
            int equalIndex = trimmed.IndexOf("==", StringComparison.Ordinal);
            bool hasNotEqual = notEqualIndex >= 0;
            bool hasEqual = equalIndex >= 0;
            if (hasEqual && hasNotEqual)
            {
                return "条件格式不支持同时包含==和!=";
            }
            if (hasEqual)
            {
                if (trimmed.IndexOf("==", equalIndex + 2, StringComparison.Ordinal) >= 0)
                {
                    return "条件格式仅支持单个==";
                }
                string left = trimmed.Substring(0, equalIndex).Trim();
                string right = trimmed.Substring(equalIndex + 2).Trim();
                if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
                {
                    return "条件格式需要包含左右值";
                }
                return string.Empty;
            }
            if (hasNotEqual)
            {
                if (trimmed.IndexOf("!=", notEqualIndex + 2, StringComparison.Ordinal) >= 0)
                {
                    return "条件格式仅支持单个!=";
                }
                string left = trimmed.Substring(0, notEqualIndex).Trim();
                string right = trimmed.Substring(notEqualIndex + 2).Trim();
                if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
                {
                    return "条件格式需要包含左右值";
                }
                return string.Empty;
            }
            return string.Empty;
        }

        /// <summary>
        /// 校验连线标签格式（非空且无前后空格）
        /// </summary>
        private string GetEdgeTagError(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return string.Empty;
            }
            string trimmed = tag.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return "标签不能为空";
            }
            if (!string.Equals(trimmed, tag, StringComparison.Ordinal))
            {
                return "标签存在前后空格";
            }
            return string.Empty;
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
