#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace LWAssets.Editor
{
    public class AssetRuntimeMonitorWindow : EditorWindow
    {
        private enum Tab
        {
            Assets = 0,
            Bundles = 1,
        }

        private enum AssetSort
        {
            Path,
            RefCount,
            Memory,
            LoadTime,
        }

        private enum BundleSort
        {
            Name,
            RefCount,
            FileSize,
            LoadTime,
            AssetCount,
            AssetMemory,
        }

        private sealed class AssetRow
        {
            public HandleBase HandleBase;
            public long MemoryBytes;
        }

        private sealed class BundleRow
        {
            public string BundleName;
            public int RefCount;
            public long FileSizeBytes;
            public double LoadTimeMs;
            public int AssetCount;
            public long AssetMemoryBytes;
            public bool IsValid;
        }

        private Tab _tab;
        private Vector2 _scroll;

        private bool _autoRefresh = true;
        private double _refreshIntervalSec = 0.5;
        private double _nextRefreshTime;

        private string _searchText;
        private bool _sortAscending;
        private AssetSort _assetSort = AssetSort.Memory;
        private BundleSort _bundleSort = BundleSort.RefCount;



        private readonly List<AssetRow> _assetRows = new List<AssetRow>();
        private readonly List<BundleRow> _bundleRows = new List<BundleRow>();

        //通过反射获取运行时的加载器和Bundle缓存
        private AssetLoaderBase _loader;
        private Dictionary<string, BundleHandle> _bundleHandleCache;
        private Dictionary<string, HandleBase> _handleBaseCache;
        /// <summary>
        /// 打开运行时监控窗口
        /// </summary>
        [MenuItem("LWAssets/Runtime Monitor")]
        public static void ShowWindow()
        {
            var window = GetWindow<AssetRuntimeMonitorWindow>("LWAssets Runtime");
            window.minSize = new Vector2(780, 420);
        }

        void GetLoader()
        {
            if (_loader != null)
            {
                return;
            }
            var loaderField = typeof(LWAssets).GetField("_loader",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            _loader = loaderField?.GetValue(null) as AssetLoaderBase;
            if (_loader != null)
            {
                //loader通过反射获取_bundleHandleCache
                var bundleHandleCacheField = _loader.GetType().GetField("_bundleHandleCache",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                _bundleHandleCache = bundleHandleCacheField?.GetValue(_loader) as Dictionary<string, BundleHandle>;
                //通过反射获取_assetHandleCache
                var assetHandleCacheField = _loader.GetType().GetField("_handleBaseCache",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                _handleBaseCache = assetHandleCacheField?.GetValue(_loader) as Dictionary<string, HandleBase>;
            }


        }
        /// <summary>
        /// 初始化窗口状态并首次拉取数据
        /// </summary>
        private void OnEnable()
        {
            _nextRefreshTime = EditorApplication.timeSinceStartup;
            RefreshData();
        }

        /// <summary>
        /// 自动刷新驱动（仅Play模式生效）
        /// </summary>
        private void Update()
        {
            if (!EditorApplication.isPlaying && _loader != null)
            {
                _loader = null;
            }

            if (!_autoRefresh || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < _nextRefreshTime)
            {

                return;
            }
            _nextRefreshTime = EditorApplication.timeSinceStartup + _refreshIntervalSec;
            GetLoader();
            RefreshData();
            Repaint();

        }

        /// <summary>
        /// 绘制窗口UI
        /// </summary>
        private void OnGUI()
        {
            DrawToolbar();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("该窗口用于运行时查看LWAssets已加载资源，请在Play模式下使用。", MessageType.Info);
                return;
            }

            if (!LWAssets.IsInitialized || _loader == null)
            {
                EditorGUILayout.HelpBox("LWAssets 尚未初始化，暂无数据。", MessageType.Warning);
                return;
            }

            _tab = (Tab)GUILayout.Toolbar((int)_tab, new[] { "Assets", "Bundles" }, GUILayout.Height(24));
            EditorGUILayout.Space(6);

            switch (_tab)
            {
                case Tab.Assets:
                    DrawAssetsTab();
                    break;
                case Tab.Bundles:
                    DrawBundlesTab();
                    break;
            }
        }

        /// <summary>
        /// 绘制顶部工具栏
        /// </summary>
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    RefreshData();
                }

                _autoRefresh = GUILayout.Toggle(_autoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.Width(70));

                GUILayout.Space(10);

                _searchText = GUILayout.TextField(_searchText ?? string.Empty, GUILayout.MinWidth(220));
                if (GUILayout.Button(string.Empty))
                {
                    _searchText = string.Empty;
                    GUI.FocusControl(null);
                }

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || !LWAssets.IsInitialized))
                {
                    if (GUILayout.Button("卸载未使用", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    {
                        UnloadUnusedAsync().Forget();
                    }

                    if (GUILayout.Button("强制卸载全部", EditorStyles.toolbarButton, GUILayout.Width(90)))
                    {
                        _loader.ForceReleaseAll();
                        RefreshData();
                    }

                    if (GUILayout.Button("GC", EditorStyles.toolbarButton, GUILayout.Width(30)))
                    {
                        GC.Collect();
                        RefreshData();
                    }
                }
            }
        }

        /// <summary>
        /// 绘制资源列表页
        /// </summary>
        private void DrawAssetsTab()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"已跟踪资源：{_assetRows.Count}", EditorStyles.miniLabel, GUILayout.Width(120));
                GUILayout.FlexibleSpace();
                _assetSort = (AssetSort)EditorGUILayout.EnumPopup(_assetSort, GUILayout.Width(110));
                _sortAscending = GUILayout.Toggle(_sortAscending, "升序", GUILayout.Width(50));
            }

            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("资源路径", GUILayout.Width(360));
                GUILayout.Label("类型", GUILayout.Width(110));
                GUILayout.Label("Bundle", GUILayout.Width(180));
                GUILayout.Label("引用", GUILayout.Width(45));
                GUILayout.Label("内存", GUILayout.Width(80));
                GUILayout.Label("耗时(ms)", GUILayout.Width(70));
                GUILayout.FlexibleSpace();
                GUILayout.Label("操作", GUILayout.Width(120));
            }

            var rows = FilterAndSortAssets(_assetRows, _searchText, _assetSort, _sortAscending);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < rows.Count; i++)
            {
                DrawAssetRow(rows[i]);
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制单行资源数据
        /// </summary>
        private void DrawAssetRow(AssetRow row)
        {
            var handle = row.HandleBase;
            if (handle == null) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(handle.Path, EditorStyles.linkLabel, GUILayout.Width(360)))
                {
                    PingAsset(handle);
                }
                if (handle is AssetHandle assetHandle)
                {
                    GUILayout.Label(assetHandle.AssetType ?? "-", GUILayout.Width(110));
                }
                else if (handle is SceneHandle sceneHandle)
                {
                    GUILayout.Label("Scene", GUILayout.Width(110));
                }
                else if (handle is RawFileHandle rawFileHandle)
                {
                    GUILayout.Label("RawFile", GUILayout.Width(110));
                }
                GUILayout.Label(handle.BundleName ?? "-", GUILayout.Width(180));
                GUILayout.Label(handle.RefCount.ToString(), GUILayout.Width(45));
                GUILayout.Label(FormatBytes(row.MemoryBytes), GUILayout.Width(80));
                GUILayout.Label(handle.LastLoadTimeMs.ToString("F2"), GUILayout.Width(70));

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(!LWAssets.IsInitialized || _loader == null))
                {
                    if (GUILayout.Button("Release", GUILayout.Width(60)))
                    {
                        _loader.Release(handle.Path);
                        RefreshData();
                    }

                    if (GUILayout.Button("Force", GUILayout.Width(50)))
                    {
                        _loader.ForceReleaseAsset(handle.Path);
                        RefreshData();
                    }
                }
            }
        }

        /// <summary>
        /// 绘制Bundle列表页
        /// </summary>
        private void DrawBundlesTab()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"已加载Bundle：{_bundleRows.Count}", EditorStyles.miniLabel, GUILayout.Width(120));
                GUILayout.FlexibleSpace();

                // _bundleUnloadAllLoadedObjects = GUILayout.Toggle(_bundleUnloadAllLoadedObjects, "卸载所有已加载对象", GUILayout.Width(140));
                _bundleSort = (BundleSort)EditorGUILayout.EnumPopup(_bundleSort, GUILayout.Width(110));
                _sortAscending = GUILayout.Toggle(_sortAscending, "升序", GUILayout.Width(50));
            }

            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Bundle", GUILayout.Width(240));
                GUILayout.Label("引用", GUILayout.Width(45));
                GUILayout.Label("文件大小", GUILayout.Width(80));
                GUILayout.Label("耗时(ms)", GUILayout.Width(70));
                GUILayout.Label("资源数", GUILayout.Width(55));
                GUILayout.Label("资源内存", GUILayout.Width(80));
                GUILayout.FlexibleSpace();
                GUILayout.Label("操作", GUILayout.Width(90));
            }

            var rows = FilterAndSortBundles(_bundleRows, _searchText, _bundleSort, _sortAscending);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < rows.Count; i++)
            {
                DrawBundleRow(rows[i]);
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制单行Bundle数据
        /// </summary>
        private void DrawBundleRow(BundleRow row)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!row.IsValid))
                {
                    GUILayout.Label(row.BundleName, GUILayout.Width(240));
                }

                GUILayout.Label(row.RefCount.ToString(), GUILayout.Width(45));
                GUILayout.Label(FormatBytes(row.FileSizeBytes), GUILayout.Width(80));
                GUILayout.Label(row.LoadTimeMs.ToString("F2"), GUILayout.Width(70));
                GUILayout.Label(row.AssetCount.ToString(), GUILayout.Width(55));
                GUILayout.Label(FormatBytes(row.AssetMemoryBytes), GUILayout.Width(80));

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(!LWAssets.IsInitialized || _loader == null))
                {
                    if (GUILayout.Button("卸载", GUILayout.Width(60)))
                    {
                        _loader.ForceUnloadBundle(row.BundleName);
                        RefreshData();
                    }
                }
            }
        }

        /// <summary>
        /// 从运行时Loader拉取数据并构建展示行
        /// </summary>
        private void RefreshData()
        {
            _assetRows.Clear();
            _bundleRows.Clear();

            if (!EditorApplication.isPlaying) return;
            if (!LWAssets.IsInitialized) return;



            if (_handleBaseCache != null)
            {
                foreach (var handle in _handleBaseCache.Values)
                {
                    long mem = 0;
                    if (handle.IsValid)
                    {
                        if (handle is AssetHandle assetHandle)
                        {
                            mem = Profiler.GetRuntimeMemorySizeLong(assetHandle.AssetObject);
                            _assetRows.Add(new AssetRow { HandleBase = assetHandle, MemoryBytes = mem });
                        }
                        else if (handle is SceneHandle sceneHandle)
                        {
                            _assetRows.Add(new AssetRow { HandleBase = sceneHandle, MemoryBytes = 0 });
                        }
                        else if (handle is RawFileHandle rawFileHandleHandle)
                        {
                            _assetRows.Add(new AssetRow { HandleBase = rawFileHandleHandle, MemoryBytes = rawFileHandleHandle.FileSizeBytes });
                        }
                    }

                }
            }

            if (_bundleHandleCache != null)
            {
                var assetsByBundle = _assetRows
                    .Where(r => r.HandleBase != null)
                    .GroupBy(r => r.HandleBase.BundleName)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var kvp in _bundleHandleCache)
                {
                    var name = kvp.Key;
                    var handle = kvp.Value;

                    assetsByBundle.TryGetValue(name, out var assetRows);
                    var assetCount = assetRows?.Count ?? 0;
                    var assetMem = assetRows == null ? 0 : assetRows.Sum(a => a.MemoryBytes);

                    _bundleRows.Add(new BundleRow
                    {
                        BundleName = name,
                        RefCount = handle?.RefCount ?? 0,
                        FileSizeBytes = handle?.FileSizeBytes ?? 0,
                        LoadTimeMs = handle?.LastLoadTimeMs ?? 0,
                        AssetCount = assetCount,
                        AssetMemoryBytes = assetMem,
                        IsValid = handle != null && !handle.IsDisposed && handle.IsValid,
                    });
                }

            }
        }


        /// <summary>
        /// 资源数据筛选与排序
        /// </summary>
        private static List<AssetRow> FilterAndSortAssets(List<AssetRow> source, string searchText, AssetSort sort, bool ascending)
        {
            IEnumerable<AssetRow> query = source;

            if (!string.IsNullOrEmpty(searchText))
            {
                var lower = searchText.ToLowerInvariant();
                query = query.Where(r => r.HandleBase != null && (r.HandleBase.Path ?? string.Empty).ToLowerInvariant().Contains(lower));
            }


            query = sort switch
            {
                AssetSort.Path => ascending
                    ? query.OrderBy(r => r.HandleBase?.Path)
                    : query.OrderByDescending(r => r.HandleBase?.Path),
                AssetSort.RefCount => ascending
                    ? query.OrderBy(r => r.HandleBase?.RefCount ?? 0)
                    : query.OrderByDescending(r => r.HandleBase?.RefCount ?? 0),
                AssetSort.LoadTime => ascending
                    ? query.OrderBy(r => r.HandleBase?.LastLoadTimeMs ?? 0)
                    : query.OrderByDescending(r => r.HandleBase?.LastLoadTimeMs ?? 0),
                _ => ascending
                    ? query.OrderBy(r => r.MemoryBytes)
                    : query.OrderByDescending(r => r.MemoryBytes),
            };

            return query.ToList();
        }
        /// <summary>
        /// Bundle数据筛选与排序
        /// </summary>
        private static List<BundleRow> FilterAndSortBundles(List<BundleRow> source, string searchText, BundleSort sort, bool ascending)
        {
            IEnumerable<BundleRow> query = source;

            if (!string.IsNullOrEmpty(searchText))
            {
                var lower = searchText.ToLowerInvariant();
                query = query.Where(r => (r.BundleName ?? string.Empty).ToLowerInvariant().Contains(lower));
            }

            query = sort switch
            {
                BundleSort.Name => ascending ? query.OrderBy(r => r.BundleName) : query.OrderByDescending(r => r.BundleName),
                BundleSort.RefCount => ascending ? query.OrderBy(r => r.RefCount) : query.OrderByDescending(r => r.RefCount),
                BundleSort.FileSize => ascending ? query.OrderBy(r => r.FileSizeBytes) : query.OrderByDescending(r => r.FileSizeBytes),
                BundleSort.LoadTime => ascending ? query.OrderBy(r => r.LoadTimeMs) : query.OrderByDescending(r => r.LoadTimeMs),
                BundleSort.AssetCount => ascending ? query.OrderBy(r => r.AssetCount) : query.OrderByDescending(r => r.AssetCount),
                _ => ascending ? query.OrderBy(r => r.AssetMemoryBytes) : query.OrderByDescending(r => r.AssetMemoryBytes),
            };

            return query.ToList();
        }

        /// <summary>
        /// 在编辑器中定位/高亮资源对象
        /// </summary>
        private static void PingAsset(HandleBase handle)
        {
            if (handle == null) return;

            if (handle is AssetHandle assetHandle)
            {
                EditorGUIUtility.PingObject(assetHandle.AssetObject);
                Selection.activeObject = assetHandle.AssetObject;
                var projectAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetHandle.Path);
                if (projectAsset != null)
                {
                    EditorGUIUtility.PingObject(projectAsset);
                }
                return;
            }
            else if (handle is SceneHandle sceneHandle)
            {

            }
        }

        /// <summary>
        /// 执行卸载未使用资源并刷新显示
        /// </summary>
        private async UniTaskVoid UnloadUnusedAsync()
        {
            if (!LWAssets.IsInitialized || _loader == null) return;

            await _loader.UnloadUnusedAssetsAsync();

            RefreshData();
            Repaint();
        }

        /// <summary>
        /// 格式化字节数显示
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0";
            if (bytes < 1024) return bytes + "B";
            if (bytes < 1024 * 1024) return (bytes / 1024f).ToString("F1") + "KB";
            if (bytes < 1024L * 1024L * 1024L) return (bytes / (1024f * 1024f)).ToString("F1") + "MB";
            return (bytes / (1024f * 1024f * 1024f)).ToString("F2") + "GB";
        }
    }
}
#endif
