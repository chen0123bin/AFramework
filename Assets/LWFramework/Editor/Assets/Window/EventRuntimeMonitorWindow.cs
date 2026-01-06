#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using LWCore;
using UnityEditor;
using UnityEngine;

namespace LWCore.Editor
{
    public class EventRuntimeMonitorWindow : EditorWindow
    {
        private enum EventSort
        {
            Name,
            ListenerCount,
            DispatchCount,
        }

        private sealed class EventRow
        {
            public string EventName { get; set; }
            public int ListenerCount { get; set; }
            public long DispatchCount { get; set; }
            public string DelegateTypeName { get; set; }
            public string LastAddCallSite { get; set; }
            public string LastDispatchCallSite { get; set; }
        }

        private readonly List<EventRow> m_Rows = new List<EventRow>();
        private readonly List<LWEventManager.EventRuntimeInfo> m_RuntimeInfos = new List<LWEventManager.EventRuntimeInfo>();
        private readonly List<LWEventManager.EventCallSiteStat> m_AddStats = new List<LWEventManager.EventCallSiteStat>();
        private readonly List<LWEventManager.EventCallSiteStat> m_DispatchStats = new List<LWEventManager.EventCallSiteStat>();
        private readonly Dictionary<string, bool> m_FoldoutStates = new Dictionary<string, bool>();

        private Vector2 m_Scroll;
        private bool m_AutoRefresh = true;
        private double m_RefreshIntervalSec = 0.5;
        private double m_NextRefreshTime;
        private string m_SearchText;
        private bool m_SortAscending;
        private EventSort m_Sort = EventSort.DispatchCount;

        private LWEventManager m_EventManager;


        [MenuItem("LWFramework/Event Runtime Monitor")]
        public static void ShowWindow()
        {
            var window = GetWindow<EventRuntimeMonitorWindow>("Event Runtime");
            window.minSize = new Vector2(980, 420);
        }

        private void OnEnable()
        {
            m_NextRefreshTime = EditorApplication.timeSinceStartup;

            RefreshData();
        }

        private void Update()
        {
            if (!EditorApplication.isPlaying)
            {
                m_EventManager = null;
                return;
            }

            if (!m_AutoRefresh || EditorApplication.timeSinceStartup < m_NextRefreshTime)
            {
                return;
            }

            m_NextRefreshTime = EditorApplication.timeSinceStartup + m_RefreshIntervalSec;
            RefreshData();
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("该窗口用于运行时查看LWEventManager中已注册事件及调用次数，请在Play模式下使用。", MessageType.Info);
                return;
            }

            if (m_EventManager == null)
            {
                EditorGUILayout.HelpBox("未找到 LWEventManager，请确认启动流程已创建 IEventManager 管理器。", MessageType.Warning);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"事件数：{m_Rows.Count}", EditorStyles.miniLabel, GUILayout.Width(100));
                GUILayout.FlexibleSpace();
                m_Sort = (EventSort)EditorGUILayout.EnumPopup(m_Sort, GUILayout.Width(110));
                m_SortAscending = GUILayout.Toggle(m_SortAscending, "升序", GUILayout.Width(50));
            }

            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("事件名", GUILayout.Width(260));
                GUILayout.Label("监听数", GUILayout.Width(60));
                GUILayout.Label("调用次数", GUILayout.Width(80));
                GUILayout.Label("委托类型", GUILayout.Width(160));
                GUILayout.Label("最后Add", GUILayout.Width(190));
                GUILayout.Label("最后Dispatch", GUILayout.Width(190));
                GUILayout.FlexibleSpace();
                GUILayout.Label("操作", GUILayout.Width(80));
            }

            var filteredRows = FilterAndSort(m_Rows, m_SearchText, m_Sort, m_SortAscending);

            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            for (int i = 0; i < filteredRows.Count; i++)
            {
                DrawEventRow(filteredRows[i]);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    RefreshData();
                }

                m_AutoRefresh = GUILayout.Toggle(m_AutoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.Width(70));

                GUILayout.Space(10);

                m_SearchText = GUILayout.TextField(m_SearchText ?? string.Empty, GUILayout.MinWidth(220));
                if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(22)))
                {
                    m_SearchText = string.Empty;
                    GUI.FocusControl(null);
                }

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || m_EventManager == null))
                {
                    if (GUILayout.Button("清零计数", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    {
                        m_EventManager.ClearDispatchCounts();
                        RefreshData();
                    }
                }
            }
        }

        private void DrawEventRow(EventRow row)
        {
            bool expanded = false;
            if (!string.IsNullOrEmpty(row.EventName))
            {
                m_FoldoutStates.TryGetValue(row.EventName, out expanded);
            }

            using (new EditorGUILayout.HorizontalScope())
            {


                EditorGUILayout.BeginHorizontal(GUILayout.Width(260)); // 设置整体布局的宽度为200px
                bool nextExpanded = EditorGUILayout.Foldout(expanded, row.EventName);
                EditorGUILayout.EndHorizontal();


                if (nextExpanded != expanded && !string.IsNullOrEmpty(row.EventName))
                {
                    m_FoldoutStates[row.EventName] = nextExpanded;
                    expanded = nextExpanded;
                }

                GUILayout.Label(row.ListenerCount.ToString(), GUILayout.Width(60));
                GUILayout.Label(row.DispatchCount.ToString(), GUILayout.Width(80));
                GUILayout.Label(string.IsNullOrEmpty(row.DelegateTypeName) ? "-" : row.DelegateTypeName, GUILayout.Width(160));

                DrawCallSiteButton(row.LastAddCallSite, 190);
                DrawCallSiteButton(row.LastDispatchCallSite, 190);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("复制名称", GUILayout.Width(70)))
                {
                    EditorGUIUtility.systemCopyBuffer = row.EventName;
                }
            }

            if (expanded)
            {
                DrawEventDetails(row.EventName);
                EditorGUILayout.Space(6);
            }
        }

        private void DrawEventDetails(string eventName)
        {
            if (m_EventManager == null || string.IsNullOrEmpty(eventName))
            {
                return;
            }

            m_EventManager.GetCallSiteStats(eventName, m_AddStats, m_DispatchStats);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Add位置统计", EditorStyles.boldLabel);
                if (m_AddStats.Count == 0)
                {
                    EditorGUILayout.LabelField("-", EditorStyles.miniLabel);
                }
                else
                {
                    for (int i = 0; i < m_AddStats.Count; i++)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label(m_AddStats[i].Count.ToString(), GUILayout.Width(50));
                            DrawCallSiteButton(m_AddStats[i].Location, 820);
                        }
                    }
                }

                EditorGUILayout.Space(6);

                EditorGUILayout.LabelField("Dispatch位置统计", EditorStyles.boldLabel);
                if (m_DispatchStats.Count == 0)
                {
                    EditorGUILayout.LabelField("-", EditorStyles.miniLabel);
                }
                else
                {
                    for (int i = 0; i < m_DispatchStats.Count; i++)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label(m_DispatchStats[i].Count.ToString(), GUILayout.Width(50));
                            DrawCallSiteButton(m_DispatchStats[i].Location, 820);
                        }
                    }
                }
            }
        }

        private static void DrawCallSiteButton(string callSite, float width)
        {
            if (string.IsNullOrEmpty(callSite))
            {
                GUILayout.Label("-", GUILayout.Width(width));
                return;
            }

            bool clickable = callSite.StartsWith("Assets/");
            if (!clickable)
            {
                GUILayout.Label(callSite, GUILayout.Width(width));
                return;
            }

            if (GUILayout.Button(callSite, EditorStyles.linkLabel, GUILayout.Width(width)))
            {
                OpenCallSite(callSite);
            }
        }

        private static void OpenCallSite(string callSite)
        {
            if (string.IsNullOrEmpty(callSite))
            {
                return;
            }

            int barIdx = callSite.IndexOf('|');
            string left = barIdx >= 0 ? callSite.Substring(0, barIdx) : callSite;

            int colonIdx = left.LastIndexOf(':');
            string assetPath = colonIdx >= 0 ? left.Substring(0, colonIdx) : left;
            int line = 0;
            if (colonIdx >= 0)
            {
                int.TryParse(left.Substring(colonIdx + 1), out line);
            }

            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
            if (script == null)
            {
                return;
            }

            if (line > 0)
            {
                AssetDatabase.OpenAsset(script, line);
            }
            else
            {
                AssetDatabase.OpenAsset(script);
            }
        }

        private void RefreshData()
        {
            m_Rows.Clear();

            if (!EditorApplication.isPlaying)
            {
                return;
            }

            if (m_EventManager == null)
            {
                m_EventManager = ManagerUtility.EventMgr as LWEventManager;
            }

            if (m_EventManager == null)
            {
                return;
            }

            m_RuntimeInfos.Clear();
            m_EventManager.GetRuntimeInfos(m_RuntimeInfos);

            for (int i = 0; i < m_RuntimeInfos.Count; i++)
            {
                var info = m_RuntimeInfos[i];
                m_Rows.Add(new EventRow
                {
                    EventName = info.EventName,
                    ListenerCount = info.ListenerCount,
                    DispatchCount = info.DispatchCount,
                    DelegateTypeName = info.DelegateTypeName,
                    LastAddCallSite = info.LastAddCallSite,
                    LastDispatchCallSite = info.LastDispatchCallSite,
                });
            }
        }

        private static List<EventRow> FilterAndSort(List<EventRow> source, string searchText, EventSort sort, bool ascending)
        {
            IEnumerable<EventRow> query = source;

            if (!string.IsNullOrEmpty(searchText))
            {
                var lower = searchText.ToLowerInvariant();
                query = query.Where(r => (r.EventName ?? string.Empty).ToLowerInvariant().Contains(lower));
            }

            query = sort switch
            {
                EventSort.Name => ascending
                    ? query.OrderBy(r => r.EventName)
                    : query.OrderByDescending(r => r.EventName),
                EventSort.ListenerCount => ascending
                    ? query.OrderBy(r => r.ListenerCount)
                    : query.OrderByDescending(r => r.ListenerCount),
                _ => ascending
                    ? query.OrderBy(r => r.DispatchCount)
                    : query.OrderByDescending(r => r.DispatchCount),
            };

            return query.ToList();
        }
    }
}
#endif
