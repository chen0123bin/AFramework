#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace LWCore.Editor
{
    /// <summary>
    /// LWFramework 编辑器工具 Hub：左侧树形导航，右侧显示内容/Inspector/入口按钮。
    /// </summary>
    public sealed class LWFrameworkHubWindow : EditorWindow
    {
        private const float DEFAULT_LEFT_PANEL_WIDTH = 280f;
        private const float LEFT_PANEL_MIN_WIDTH = 200f;
        private const float LEFT_PANEL_MAX_WIDTH = 700f;
        private const float SPLITTER_WIDTH = 4f;
        private const string LEFT_PANEL_WIDTH_PREF_KEY = "LWFramework.Hub.LeftPanelWidth";

        private TreeViewState m_NavigationTreeState;
        private SearchField m_SearchField;
        private HubNavigationTreeView m_NavigationTreeView;
        private string m_SearchText;

        private float m_LeftPanelWidth;
        private bool m_IsResizingLeftPanel;

        private readonly List<HubTreeView> m_Views = new List<HubTreeView>();
        private HubTreeView m_ActiveView;

        private readonly Dictionary<HubTreeView, int> m_ViewToId = new Dictionary<HubTreeView, int>();

        /// <summary>
        /// 打开 Hub 窗口。
        /// </summary>
        [MenuItem("LWFramework/Hub")]
        public static void ShowWindow()
        {
            LWFrameworkHubWindow window = GetWindow<LWFrameworkHubWindow>("LWFramework Hub");
            window.minSize = new Vector2(980, 520);
        }

        /// <summary>
        /// 窗口启用时初始化 TreeView 状态并构建导航树。
        /// </summary>
        private void OnEnable()
        {
            if (m_NavigationTreeState == null)
            {
                m_NavigationTreeState = new TreeViewState();
            }

            if (m_SearchField == null)
            {
                m_SearchField = new SearchField();
            }
            m_LeftPanelWidth = EditorPrefs.GetFloat(LEFT_PANEL_WIDTH_PREF_KEY, DEFAULT_LEFT_PANEL_WIDTH);
            m_LeftPanelWidth = Mathf.Clamp(m_LeftPanelWidth, LEFT_PANEL_MIN_WIDTH, LEFT_PANEL_MAX_WIDTH);
            BuildManualViews();
            RebuildTree();
        }

        /// <summary>
        /// 窗口关闭/禁用时释放缓存的 Inspector Editor。
        /// </summary>
        private void OnDisable()
        {
            m_IsResizingLeftPanel = false;
            if (m_ActiveView != null)
            {
                m_ActiveView.OnDeselected();
                m_ActiveView = null;
            }
        }

        /// <summary>
        /// IMGUI 主绘制入口：绘制工具栏、左树与右侧内容。
        /// </summary>
        private void OnGUI()
        {
            DrawToolbar();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawLeftTree();
                DrawSplitter();
                DrawRightContent();
            }
        }

        /// <summary>
        /// 绘制顶部工具栏：刷新与搜索。
        /// </summary>
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    RebuildTree();
                }

                GUILayout.Space(6);
                GUILayout.Label("搜索", GUILayout.Width(30));
                m_SearchText = m_SearchField.OnToolbarGUI(m_SearchText);
                if (m_NavigationTreeView != null)
                {
                    m_NavigationTreeView.searchString = m_SearchText;
                }

                GUILayout.FlexibleSpace();
            }
        }

        /// <summary>
        /// 绘制左侧树形导航。
        /// </summary>
        private void DrawLeftTree()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(m_LeftPanelWidth)))
            {
                Rect rect = GUILayoutUtility.GetRect(m_LeftPanelWidth, position.height - EditorGUIUtility.singleLineHeight, GUILayout.ExpandHeight(true));
                if (m_NavigationTreeView != null)
                {
                    m_NavigationTreeView.OnGUI(rect);
                }
            }
        }

        /// <summary>
        /// 绘制左右区域分隔条，并支持鼠标拖拽调整左侧宽度。
        /// </summary>
        private void DrawSplitter()
        {
            Rect splitterRect = GUILayoutUtility.GetRect(SPLITTER_WIDTH, SPLITTER_WIDTH, GUILayout.ExpandHeight(true));
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            Event currentEvent = Event.current;

            if (currentEvent.type == EventType.MouseDown && splitterRect.Contains(currentEvent.mousePosition) && currentEvent.button == 0)
            {
                m_IsResizingLeftPanel = true;
                GUIUtility.hotControl = controlId;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseDrag && m_IsResizingLeftPanel && GUIUtility.hotControl == controlId)
            {
                m_LeftPanelWidth = Mathf.Clamp(currentEvent.mousePosition.x, LEFT_PANEL_MIN_WIDTH, LEFT_PANEL_MAX_WIDTH);
                EditorPrefs.SetFloat(LEFT_PANEL_WIDTH_PREF_KEY, m_LeftPanelWidth);
                currentEvent.Use();
                Repaint();
            }
            else if (currentEvent.type == EventType.MouseUp && m_IsResizingLeftPanel && GUIUtility.hotControl == controlId)
            {
                m_IsResizingLeftPanel = false;
                GUIUtility.hotControl = 0;
                currentEvent.Use();
            }
        }

        /// <summary>
        /// 绘制右侧内容区：根据左侧选择展示菜单入口或资源 Inspector。
        /// </summary>
        private void DrawRightContent()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                if (m_ActiveView != null)
                {
                    m_ActiveView.OnContentGUI();
                    return;
                }

                DrawWelcome();
            }
        }

        /// <summary>
        /// 绘制默认欢迎页。
        /// </summary>
        private void DrawWelcome()
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField("LWFramework Hub", EditorStyles.largeLabel);
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox("左侧选择一个工具/配置/入口；右侧会显示对应交互内容。", MessageType.Info);
        }

        /// <summary>
        /// 手动注册左侧树节点与右侧页面。
        /// </summary>
        private void BuildManualViews()
        {
            m_Views.Clear();

            m_Views.Add(new WelcomeHubView("首页/欢迎", string.Empty));

            m_Views.Add(new MenuItemHubView("Event/Runtime Monitor", string.Empty, "LWFramework/Event/Runtime Monitor"));
            m_Views.Add(new MenuItemHubView("Assets/Dashboard", string.Empty, "LWFramework/Assets/Dashboard"));

            m_Views.Add(new ScriptableObjectTypeHubView("配置/LWAssetsBuildConfig", string.Empty, typeof(LWAssets.Editor.LWAssetsBuildConfig)));
            m_Views.Add(new ScriptableObjectTypeHubView("配置/LWAssetsConfig", string.Empty, typeof(LWAssets.LWAssetsConfig)));
        }

        /// <summary>
        /// 重建左侧树：收集 LWFramework 菜单项与配置资源。
        /// </summary>
        private void RebuildTree()
        {
            TreeViewItem root = new TreeViewItem(0, -1, "Root");
            int idCounter = 1;
            Dictionary<int, HubTreeView> idToView = new Dictionary<int, HubTreeView>();
            m_ViewToId.Clear();

            for (int i = 0; i < m_Views.Count; i++)
            {
                HubTreeView view = m_Views[i];
                if (view == null || string.IsNullOrEmpty(view.NodePath))
                {
                    continue;
                }

                AddPathAsNodes(root, view.NodePath, view.Icon, view, ref idCounter, idToView);
            }

            if (m_NavigationTreeState == null)
            {
                m_NavigationTreeState = new TreeViewState();
            }

            if (m_NavigationTreeView == null)
            {
                m_NavigationTreeView = new HubNavigationTreeView(m_NavigationTreeState, OnSelectView);
            }
            m_NavigationTreeView.SetData(root, idToView);
            m_NavigationTreeView.Reload();
            m_NavigationTreeView.ExpandAll();

            if (m_ActiveView != null && m_ViewToId.TryGetValue(m_ActiveView, out int activeId))
            {
                m_NavigationTreeView.SetSelection(new List<int>(1) { activeId }, TreeViewSelectionOptions.FireSelectionChanged);
                return;
            }

            for (int i = 0; i < m_Views.Count; i++)
            {
                HubTreeView view = m_Views[i];
                if (view != null && m_ViewToId.TryGetValue(view, out int id))
                {
                    m_NavigationTreeView.SetSelection(new List<int>(1) { id }, TreeViewSelectionOptions.FireSelectionChanged);
                    return;
                }
            }
        }

        /// <summary>
        /// 左侧选中某个页面时切换右侧内容。
        /// </summary>
        /// <param name="view">选中的页面。</param>
        private void OnSelectView(HubTreeView view)
        {
            if (view == null)
            {
                return;
            }

            if (m_ActiveView == view)
            {
                return;
            }

            if (m_ActiveView != null)
            {
                m_ActiveView.OnDeselected();
            }

            m_ActiveView = view;
            m_ActiveView.OnSelected();
            Repaint();
        }

        /// <summary>
        /// 将形如 "Event/EventMonitor" 的路径拆成多级节点并挂到树上。
        /// </summary>
        private void AddPathAsNodes(TreeViewItem root, string nodePath, Texture2D icon, HubTreeView view, ref int idCounter, Dictionary<int, HubTreeView> idToView)
        {
            string[] parts = nodePath.Split('/');
            TreeViewItem current = root;

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (string.IsNullOrEmpty(part))
                {
                    continue;
                }

                bool isLeaf = i == parts.Length - 1;
                if (current.children == null)
                {
                    current.children = new List<TreeViewItem>();
                }

                TreeViewItem existing = null;
                for (int j = 0; j < current.children.Count; j++)
                {
                    TreeViewItem child = current.children[j];
                    if (child != null && child.displayName == part)
                    {
                        existing = child;
                        break;
                    }
                }

                if (existing != null)
                {
                    current = existing;
                    continue;
                }

                int id = idCounter++;
                TreeViewItem item = new TreeViewItem(id, current.depth + 1, part);
                if (isLeaf)
                {
                    item.icon = icon;
                    idToView[id] = view;
                    m_ViewToId[view] = id;
                }

                current.AddChild(item);
                current = item;
            }
        }

        private sealed class HubNavigationTreeView : TreeView
        {
            private readonly Action<HubTreeView> m_OnSelectView;
            private TreeViewItem m_Root;
            private Dictionary<int, HubTreeView> m_IdToView;

            /// <summary>
            /// 创建左侧导航树。
            /// </summary>
            /// <param name="state">TreeView 状态。</param>
            /// <param name="onSelectView">选中页面回调。</param>
            public HubNavigationTreeView(TreeViewState state, Action<HubTreeView> onSelectView)
                : base(state)
            {
                m_OnSelectView = onSelectView;
                showBorder = true;
            }

            /// <summary>
            /// 设置树数据源。
            /// </summary>
            public void SetData(TreeViewItem root, Dictionary<int, HubTreeView> idToView)
            {
                m_Root = root;
                m_IdToView = idToView;
            }

            /// <summary>
            /// 构建根节点。
            /// </summary>
            protected override TreeViewItem BuildRoot()
            {
                return m_Root ?? new TreeViewItem(0, -1, "Root");
            }

            /// <summary>
            /// 选择变更时通知窗口切换页面。
            /// </summary>
            protected override void SelectionChanged(IList<int> selectedIds)
            {
                if (selectedIds == null || selectedIds.Count == 0)
                {
                    return;
                }

                int id = selectedIds[0];
                if (m_IdToView == null || !m_IdToView.TryGetValue(id, out HubTreeView view))
                {
                    return;
                }

                m_OnSelectView?.Invoke(view);
            }
        }

        private sealed class WelcomeHubView : HubTreeView
        {
            public WelcomeHubView(string nodePath, string iconPath)
                : base(nodePath, iconPath)
            {
            }

            protected override void DrawContent()
            {
                GUILayout.Space(8);
                EditorGUILayout.LabelField("LWFramework Hub", EditorStyles.largeLabel);
                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox("这是一个手动注册的 Hub 示例页。\n你可以通过 new XxxTreeView(\"模块/页面\", \"iconPath\") 把页面挂到左侧树上。", MessageType.Info);
            }
        }

        private sealed class MenuItemHubView : HubTreeView
        {
            private readonly string m_MenuPath;

            /// <summary>
            /// 创建菜单入口页。
            /// </summary>
            /// <param name="nodePath">左侧树节点路径。</param>
            /// <param name="iconPath">图标路径。</param>
            /// <param name="menuPath">Unity 菜单路径（用于 ExecuteMenuItem）。</param>
            public MenuItemHubView(string nodePath, string iconPath, string menuPath)
                : base(nodePath, iconPath)
            {
                m_MenuPath = menuPath;
            }

            protected override void DrawContent()
            {
                GUILayout.Space(8);
                EditorGUILayout.LabelField(m_MenuPath, EditorStyles.boldLabel);
                EditorGUILayout.Space(8);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("打开/执行", GUILayout.Width(120)))
                    {
                        EditorApplication.ExecuteMenuItem(m_MenuPath);
                    }

                    if (GUILayout.Button("复制路径", GUILayout.Width(100)))
                    {
                        EditorGUIUtility.systemCopyBuffer = m_MenuPath;
                    }

                    GUILayout.FlexibleSpace();
                }
            }
        }

        private sealed class ScriptableObjectTypeHubView : HubTreeView
        {
            private readonly Type m_AssetType;
            private UnityEngine.Object m_Asset;
            private UnityEditor.Editor m_Inspector;
            private Vector2 m_Scroll;

            /// <summary>
            /// 创建 ScriptableObject 配置页（按类型查找/创建并内嵌 Inspector）。
            /// </summary>
            /// <param name="nodePath">左侧树节点路径。</param>
            /// <param name="iconPath">图标路径。</param>
            /// <param name="assetType">ScriptableObject 类型。</param>
            public ScriptableObjectTypeHubView(string nodePath, string iconPath, Type assetType)
                : base(nodePath, iconPath)
            {
                m_AssetType = assetType;
            }

            public override void OnDeselected()
            {
                if (m_Inspector != null)
                {
                    DestroyImmediate(m_Inspector);
                    m_Inspector = null;
                }
            }

            protected override void DrawContent()
            {
                GUILayout.Space(8);
                string title = m_AssetType != null ? m_AssetType.Name : "Config";
                EditorGUILayout.LabelField(title, EditorStyles.largeLabel);
                EditorGUILayout.Space(6);

                using (new EditorGUILayout.HorizontalScope())
                {
                    m_Asset = EditorGUILayout.ObjectField("资源", m_Asset, m_AssetType, false);

                    if (GUILayout.Button("查找", GUILayout.Width(60)))
                    {
                        FindFirstAsset();
                    }

                    using (new EditorGUI.DisabledScope(m_AssetType == null || !typeof(ScriptableObject).IsAssignableFrom(m_AssetType)))
                    {
                        if (GUILayout.Button("创建", GUILayout.Width(60)))
                        {
                            CreateAsset();
                        }
                    }

                    using (new EditorGUI.DisabledScope(m_Asset == null))
                    {
                        if (GUILayout.Button("定位", GUILayout.Width(60)))
                        {
                            EditorGUIUtility.PingObject(m_Asset);
                            Selection.activeObject = m_Asset;
                        }
                    }
                }

                EditorGUILayout.Space(6);

                if (m_Asset == null)
                {
                    EditorGUILayout.HelpBox("未绑定资源，点击“查找”自动定位一个同类型资源，或点击“创建”生成新的资产。", MessageType.Info);
                    return;
                }

                if (m_Inspector == null || m_Inspector.target != m_Asset)
                {
                    if (m_Inspector != null)
                    {
                        DestroyImmediate(m_Inspector);
                        m_Inspector = null;
                    }

                    UnityEditor.Editor.CreateCachedEditor(m_Asset, null, ref m_Inspector);
                }

                using (EditorGUILayout.ScrollViewScope scrollViewScope = new EditorGUILayout.ScrollViewScope(m_Scroll))
                {
                    m_Scroll = scrollViewScope.scrollPosition;
                    if (m_Inspector != null)
                    {
                        m_Inspector.OnInspectorGUI();
                    }
                }
            }

            /// <summary>
            /// 查找同类型的第一个资源并绑定。
            /// </summary>
            private void FindFirstAsset()
            {
                if (m_AssetType == null)
                {
                    return;
                }

                string[] guids = AssetDatabase.FindAssets($"t:{m_AssetType.Name}");
                if (guids == null || guids.Length <= 0)
                {
                    m_Asset = null;
                    return;
                }

                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                m_Asset = AssetDatabase.LoadAssetAtPath(assetPath, m_AssetType);
            }

            /// <summary>
            /// 创建新的 ScriptableObject 资产。
            /// </summary>
            private void CreateAsset()
            {
                if (m_AssetType == null || !typeof(ScriptableObject).IsAssignableFrom(m_AssetType))
                {
                    return;
                }

                string defaultName = m_AssetType.Name;
                string path = EditorUtility.SaveFilePanelInProject("Create Config", defaultName, "asset", string.Empty);
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                ScriptableObject asset = ScriptableObject.CreateInstance(m_AssetType);
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                m_Asset = asset;
                EditorGUIUtility.PingObject(m_Asset);
                Selection.activeObject = m_Asset;
            }
        }
    }
}
#endif
